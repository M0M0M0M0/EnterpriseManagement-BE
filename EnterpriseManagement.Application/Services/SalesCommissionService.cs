using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class SalesCommissionService : ISalesCommissionService
{
    private readonly ISalesCommissionRepository _commissionRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;

    public SalesCommissionService(
        ISalesCommissionRepository commissionRepository,
        ISaleRepository saleRepository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver)
    {
        _commissionRepository = commissionRepository;
        _saleRepository = saleRepository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
    }

    public async Task<CommissionDto> CalculateAsync(CalculateCommissionRequest request, string requesterEmployeeCode)
    {
        if (request.PeriodEndDate < request.PeriodStartDate)
        {
            throw new InvalidOperationException("Ngày kết thúc kỳ phải sau hoặc bằng ngày bắt đầu.");
        }

        var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' not found.");

        var requester = await _employeeRepository.GetByEmployeeCodeAsync(requesterEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");

        // Chỉ người đang thực sự chịu trách nhiệm duyệt cho nhân viên này (quản lý trực tiếp,
        // hoặc người được đẩy lên khi quản lý trực tiếp đang nghỉ phép) mới được tính hoa hồng
        // cho họ — tránh 1 Manager tính hộ hoa hồng cho nhân viên của Manager khác.
        var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(employee);
        if (effectiveApprover?.Id != requester.Id)
        {
            throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
        }

        // Plan được suy ra tự động từ nhân viên (gán qua trang KPI Plan) — Manager không cần
        // chọn tay Plan mỗi lần tính, và không thể tính nhầm hoa hồng theo Plan sai.
        if (employee.KpiPlanId is null || employee.KpiPlan is null)
        {
            throw new InvalidOperationException(
                $"Nhân viên '{employee.EmployeeCode}' chưa được gán vào KPI Plan nào. Vui lòng gán Plan trước khi tính hoa hồng.");
        }

        if (!employee.KpiPlan.IsActive)
        {
            throw new InvalidOperationException($"KPI Plan '{employee.KpiPlan.PlanName}' đang bị Ngừng, không thể dùng để tính hoa hồng.");
        }

        var confirmedSales = await _saleRepository.GetConfirmedByEmployeeAndRangeAsync(
            employee.Id, request.PeriodStartDate, request.PeriodEndDate);
        var totalRevenue = confirmedSales.Sum(s => s.Amount);

        var matchedLevel = employee.KpiPlan.Levels
            .Where(l => l.MinimumRevenue <= totalRevenue)
            .OrderByDescending(l => l.MinimumRevenue)
            .FirstOrDefault();

        if (matchedLevel is null)
        {
            throw new InvalidOperationException(
                $"Doanh số {totalRevenue:N0} chưa đạt mức KPI thấp nhất trong Plan '{employee.KpiPlan.PlanName}'.");
        }

        var commission = new SalesCommission
        {
            EmployeeId = employee.Id,
            PeriodStartDate = request.PeriodStartDate,
            PeriodEndDate = request.PeriodEndDate,
            TotalRevenue = totalRevenue,
            KpiLevelId = matchedLevel.Id,
            CommissionRate = matchedLevel.CommissionRate,
            CommissionAmount = totalRevenue * matchedLevel.CommissionRate,
            Status = CommissionStatus.Pending,
            CreatedAt = VietnamClock.Now
        };

        await _commissionRepository.AddAsync(commission);
        await _commissionRepository.SaveChangesAsync();

        var created = await _commissionRepository.GetByIdAsync(commission.Id);
        return ToDto(created!);
    }

    public async Task<IEnumerable<CommissionDto>> GetMineAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var commissions = await _commissionRepository.GetByEmployeeIdAsync(employee.Id);
        return commissions.Select(ToDto);
    }

    public async Task<IEnumerable<CommissionDto>> GetPendingAsync()
    {
        var commissions = await _commissionRepository.GetPendingAsync();
        return commissions.Select(ToDto);
    }

    public async Task<IEnumerable<CommissionDto>> GetHistoryAsync()
    {
        var commissions = await _commissionRepository.GetAllAsync();
        return commissions.Select(ToDto);
    }

    public async Task<CommissionDto> ApproveAsync(long commissionId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var commission = await _commissionRepository.GetByIdAsync(commissionId)
            ?? throw new InvalidOperationException($"Commission {commissionId} not found.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(commission.Employee);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (commission.Status != CommissionStatus.Pending)
        {
            throw new InvalidOperationException("Only pending commissions can be approved.");
        }

        commission.Status = CommissionStatus.Approved;
        commission.ApprovedBy = approver.Id;
        commission.Approver = approver;
        commission.ApprovedAt = VietnamClock.Now;
        commission.UpdatedAt = VietnamClock.Now;

        await _commissionRepository.SaveChangesAsync();

        return ToDto(commission);
    }

    private static CommissionDto ToDto(SalesCommission commission) => new()
    {
        Id = commission.Id,
        EmployeeCode = commission.Employee.EmployeeCode,
        EmployeeName = $"{commission.Employee.FirstName} {commission.Employee.LastName}",
        PeriodStartDate = commission.PeriodStartDate,
        PeriodEndDate = commission.PeriodEndDate,
        TotalRevenue = commission.TotalRevenue,
        KpiPlanName = commission.KpiLevel.KpiPlan.PlanName,
        LevelOrder = commission.KpiLevel.LevelOrder,
        CommissionRate = commission.CommissionRate,
        CommissionAmount = commission.CommissionAmount,
        Status = commission.Status.ToString(),
        ApproverName = commission.Approver is null ? null : $"{commission.Approver.FirstName} {commission.Approver.LastName}",
        ApprovedAt = commission.ApprovedAt
    };
}

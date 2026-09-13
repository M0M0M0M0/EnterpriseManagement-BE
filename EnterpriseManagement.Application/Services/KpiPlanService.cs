using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;

namespace EnterpriseManagement.Application.Services;

public class KpiPlanService : IKpiPlanService
{
    private readonly IKpiPlanRepository _kpiPlanRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;

    public KpiPlanService(
        IKpiPlanRepository kpiPlanRepository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver)
    {
        _kpiPlanRepository = kpiPlanRepository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
    }

    public async Task<IEnumerable<KpiPlanDto>> GetAllAsync()
    {
        var plans = await _kpiPlanRepository.GetAllAsync();
        return plans.Select(ToDto);
    }

    public async Task<KpiPlanDto> GetByIdAsync(long id)
    {
        var plan = await _kpiPlanRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"KPI plan {id} not found.");
        return ToDto(plan);
    }

    public async Task<KpiPlanDto> CreateAsync(CreateKpiPlanRequest request)
    {
        ValidateName(request.PlanName);
        var levels = ValidateAndBuildLevels(request.Levels);

        var plan = new KpiPlan
        {
            PlanName = request.PlanName,
            Description = request.Description,
            IsActive = true,
            CreatedAt = VietnamClock.Now,
            Levels = levels
        };

        await _kpiPlanRepository.AddAsync(plan);
        await _kpiPlanRepository.SaveChangesAsync();

        return ToDto(plan);
    }

    public async Task<KpiPlanDto> UpdateAsync(long id, UpdateKpiPlanRequest request)
    {
        var plan = await _kpiPlanRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"KPI plan {id} not found.");

        ValidateName(request.PlanName);
        var levels = ValidateAndBuildLevels(request.Levels);

        plan.PlanName = request.PlanName;
        plan.Description = request.Description;
        plan.UpdatedAt = VietnamClock.Now;

        // Thay toàn bộ danh sách mức — đơn giản hơn nhiều so với diff từng level, và Plan luôn
        // được sửa nguyên khối từ UI (bảng các mức) nên không mất dữ liệu người dùng không cố ý xoá.
        plan.Levels.Clear();
        foreach (var level in levels)
        {
            plan.Levels.Add(level);
        }

        await _kpiPlanRepository.SaveChangesAsync();

        return ToDto(plan);
    }

    public async Task<KpiPlanDto> SetActiveAsync(long id, bool isActive)
    {
        var plan = await _kpiPlanRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"KPI plan {id} not found.");

        plan.IsActive = isActive;
        plan.UpdatedAt = VietnamClock.Now;

        await _kpiPlanRepository.SaveChangesAsync();

        return ToDto(plan);
    }

    public async Task<KpiPlanDto> AssignEmployeesAsync(long id, IReadOnlyList<string> employeeCodes, string requesterEmployeeCode, bool isAdmin)
    {
        var plan = await _kpiPlanRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"KPI plan {id} not found.");

        var requester = await _employeeRepository.GetByEmployeeCodeAsync(requesterEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");

        var allEmployees = (await _employeeRepository.GetAllAsync()).ToList();
        var codeSet = employeeCodes.ToHashSet();

        var toAssign = allEmployees.Where(e => codeSet.Contains(e.EmployeeCode)).ToList();
        if (toAssign.Count != codeSet.Count)
        {
            var missing = codeSet.Except(toAssign.Select(e => e.EmployeeCode));
            throw new InvalidOperationException($"Không tìm thấy nhân viên: {string.Join(", ", missing)}.");
        }

        var toUnassign = allEmployees.Where(e => e.KpiPlanId == id && !codeSet.Contains(e.EmployeeCode)).ToList();

        // Chỉ được gán/gỡ nhân viên trong team mình quản lý trực tiếp (hoặc người đang được đẩy
        // trách nhiệm duyệt lên mình khi quản lý trực tiếp của họ nghỉ phép) — cùng quy tắc với
        // SalesCommissionService, tránh 1 Manager cấu hình KPI cho nhân viên của Manager khác.
        if (!isAdmin)
        {
            foreach (var employee in toAssign.Concat(toUnassign))
            {
                var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(employee);
                if (effectiveApprover?.Id != requester.Id)
                {
                    throw new InvalidOperationException(
                        $"Bạn không phải quản lý trực tiếp của nhân viên '{employee.EmployeeCode}'.");
                }
            }
        }

        foreach (var employee in toAssign)
        {
            employee.KpiPlanId = id;
            _employeeRepository.Update(employee);
        }

        foreach (var employee in toUnassign)
        {
            employee.KpiPlanId = null;
            _employeeRepository.Update(employee);
        }

        await _employeeRepository.SaveChangesAsync();

        var updated = await _kpiPlanRepository.GetByIdAsync(id);
        return ToDto(updated!);
    }

    private static void ValidateName(string planName)
    {
        if (string.IsNullOrWhiteSpace(planName))
        {
            throw new InvalidOperationException("Vui lòng nhập tên KPI Plan.");
        }
    }

    private static List<KpiLevel> ValidateAndBuildLevels(List<KpiLevelInput> input)
    {
        if (input.Count == 0)
        {
            throw new InvalidOperationException("Vui lòng thêm ít nhất 1 mức KPI.");
        }

        if (input.Select(l => l.LevelOrder).Distinct().Count() != input.Count)
        {
            throw new InvalidOperationException("Thứ tự mức KPI (Level) không được trùng nhau trong cùng 1 Plan.");
        }

        foreach (var level in input)
        {
            if (level.MinimumRevenue < 0)
            {
                throw new InvalidOperationException("Doanh số tối thiểu phải là số không âm.");
            }

            if (level.CommissionRate <= 0)
            {
                throw new InvalidOperationException("Tỷ lệ hoa hồng phải lớn hơn 0.");
            }
        }

        var now = VietnamClock.Now;
        return input
            .Select(l => new KpiLevel
            {
                LevelOrder = l.LevelOrder,
                MinimumRevenue = l.MinimumRevenue,
                CommissionRate = l.CommissionRate,
                CreatedAt = now
            })
            .ToList();
    }

    private static KpiPlanDto ToDto(KpiPlan plan) => new()
    {
        Id = plan.Id,
        PlanName = plan.PlanName,
        Description = plan.Description,
        IsActive = plan.IsActive,
        Levels = plan.Levels
            .OrderBy(l => l.LevelOrder)
            .Select(l => new KpiLevelDto
            {
                Id = l.Id,
                LevelOrder = l.LevelOrder,
                MinimumRevenue = l.MinimumRevenue,
                CommissionRate = l.CommissionRate
            })
            .ToList(),
        AssignedEmployees = plan.Employees
            .Select(e => new AssignedEmployeeDto
            {
                EmployeeCode = e.EmployeeCode,
                FullName = $"{e.FirstName} {e.LastName}"
            })
            .ToList()
    };
}

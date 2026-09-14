using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerService _customerService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;

    public SaleService(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        ICustomerService customerService,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _customerService = customerService;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
    }

    public async Task<SaleDto> SubmitAsync(SubmitSaleRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var customer = await ResolveCustomerAsync(request, employeeCode);

        var sale = new Sale
        {
            SaleCode = await GenerateUniqueSaleCodeAsync(),
            CustomerId = customer.Id,
            EmployeeId = employee.Id,
            Amount = request.Amount,
            OrderDate = VietnamClock.Now,
            Status = SaleStatus.Pending,
            Note = request.Note,
            CreatedAt = VietnamClock.Now
        };

        await _saleRepository.AddAsync(sale);
        await _saleRepository.SaveChangesAsync();

        var created = await _saleRepository.GetByCodeAsync(sale.SaleCode);
        return ToDto(created!);
    }

    // Chọn đúng 1 trong 2: khách hàng có sẵn (CustomerCode) hoặc khách hàng mới nhập kèm
    // (NewCustomerName+NewCustomerPhone) — khách hàng mới được tạo/dùng lại qua
    // FindOrCreatePotentialAsync (dedupe theo SĐT), ở trạng thái Potential cho đến khi sale
    // này được duyệt (xem ChangeStatusAsync).
    private async Task<Customer> ResolveCustomerAsync(SubmitSaleRequest request, string employeeCode)
    {
        var hasExistingCustomer = !string.IsNullOrWhiteSpace(request.CustomerCode);
        var hasNewCustomer = !string.IsNullOrWhiteSpace(request.NewCustomerName) || !string.IsNullOrWhiteSpace(request.NewCustomerPhone);

        if (hasExistingCustomer == hasNewCustomer)
        {
            throw new InvalidOperationException(
                "Vui lòng chọn một khách hàng có sẵn HOẶC nhập thông tin khách hàng mới (không được cả hai hoặc để trống cả hai).");
        }

        if (hasExistingCustomer)
        {
            return await _customerRepository.GetByCodeAsync(request.CustomerCode!)
                ?? throw new InvalidOperationException($"Customer code '{request.CustomerCode}' not found.");
        }

        if (string.IsNullOrWhiteSpace(request.NewCustomerName) || string.IsNullOrWhiteSpace(request.NewCustomerPhone))
        {
            throw new InvalidOperationException("Vui lòng nhập đủ tên và số điện thoại cho khách hàng mới.");
        }

        return await _customerService.FindOrCreatePotentialAsync(
            request.NewCustomerName, request.NewCustomerPhone, request.NewCustomerEmail, request.NewCustomerAddress, employeeCode);
    }

    public async Task<IEnumerable<SaleDto>> GetByEmployeeAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var sales = await _saleRepository.GetByEmployeeIdAsync(employee.Id);
        return sales.Select(ToDto);
    }

    public async Task<SaleDto> UpdateAsync(long saleId, string employeeCode, UpdateSaleRequest request)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId)
            ?? throw new InvalidOperationException($"Sale {saleId} not found.");

        if (sale.Employee.EmployeeCode != employeeCode)
        {
            throw new InvalidOperationException("Only the employee who submitted this sale can edit it.");
        }

        if (sale.Status != SaleStatus.Pending)
        {
            throw new InvalidOperationException("Only pending sales can be edited.");
        }

        sale.Amount = request.Amount;
        sale.Note = request.Note;
        sale.UpdatedAt = VietnamClock.Now;

        await _saleRepository.SaveChangesAsync();

        return ToDto(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var sales = await _saleRepository.GetPendingAsync();
        return await FilterByTeamAsync(sales, requesterEmployeeCode, isAdmin);
    }

    public async Task<IEnumerable<SaleDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var sales = await _saleRepository.GetAllAsync();
        return await FilterByTeamAsync(sales, requesterEmployeeCode, isAdmin);
    }

    // ADMIN thấy toàn bộ. Manager thấy sale của người mình quản lý trực tiếp — hoặc của người
    // mà quản lý trực tiếp của họ đang nghỉ phép (đẩy việc duyệt lên mình), xem
    // IApprovalDelegationResolver để biết chi tiết.
    private async Task<IEnumerable<SaleDto>> FilterByTeamAsync(
        IEnumerable<Sale> sales, string requesterEmployeeCode, bool isAdmin)
    {
        if (isAdmin)
        {
            return sales.Select(ToDto);
        }

        var requester = await _employeeRepository.GetByEmployeeCodeAsync(requesterEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");

        var result = new List<SaleDto>();
        foreach (var sale in sales)
        {
            var approver = await _approvalDelegationResolver.ResolveApproverAsync(sale.Employee);
            if (approver?.Id == requester.Id)
            {
                result.Add(ToDto(sale));
            }
        }

        return result;
    }

    public async Task<SaleDto> ApproveAsync(long saleId, string approverEmployeeCode, bool isAdmin)
    {
        var sale = await ChangeStatusAsync(saleId, approverEmployeeCode, isAdmin, SaleStatus.Confirmed, "approved", null);
        return ToDto(sale);
    }

    public async Task<SaleDto> RejectAsync(long saleId, string approverEmployeeCode, bool isAdmin, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");
        }

        var sale = await ChangeStatusAsync(saleId, approverEmployeeCode, isAdmin, SaleStatus.Cancelled, "rejected", rejectionReason);
        return ToDto(sale);
    }

    // Nhân viên tự hủy sale của mình khi còn Pending — không cần thẩm quyền duyệt, khác với
    // reject (chỉ Manager/Admin mới reject được). Pattern giống LeaveRequestService.CancelAsync.
    public async Task<SaleDto> CancelAsync(long saleId, string employeeCode)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId)
            ?? throw new InvalidOperationException($"Sale {saleId} not found.");

        if (sale.Employee.EmployeeCode != employeeCode)
        {
            throw new InvalidOperationException("Only the employee who submitted this sale can cancel it.");
        }

        if (sale.Status != SaleStatus.Pending)
        {
            throw new InvalidOperationException("Only pending sales can be cancelled.");
        }

        sale.Status = SaleStatus.Cancelled;
        sale.UpdatedAt = VietnamClock.Now;

        await _saleRepository.SaveChangesAsync();

        return ToDto(sale);
    }

    private async Task<Sale> ChangeStatusAsync(long saleId, string approverEmployeeCode, bool isAdmin, SaleStatus newStatus, string action, string? rejectionReason)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var sale = await _saleRepository.GetByIdAsync(saleId)
            ?? throw new InvalidOperationException($"Sale {saleId} not found.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(sale.Employee);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (sale.Status != SaleStatus.Pending)
        {
            throw new InvalidOperationException($"Only pending sales can be {action}.");
        }

        sale.Status = newStatus;
        sale.RejectionReason = rejectionReason;
        sale.ApprovedBy = approver.Id;
        sale.Approver = approver;
        sale.ApprovedAt = VietnamClock.Now;
        sale.UpdatedAt = VietnamClock.Now;

        // Khách hàng mới nhập kèm sale chỉ được "công nhận" (Active, hiện trong danh sách chọn
        // cho sale khác) sau khi sale này được duyệt. Nếu bị từ chối, khách hàng vẫn giữ
        // nguyên Potential — không cần xử lý gì thêm ở nhánh reject.
        if (newStatus == SaleStatus.Confirmed && sale.Customer.Status == CustomerStatus.Potential)
        {
            sale.Customer.Status = CustomerStatus.Active;
            sale.Customer.UpdatedAt = VietnamClock.Now;
        }

        await _saleRepository.SaveChangesAsync();

        return sale;
    }

    private async Task<string> GenerateUniqueSaleCodeAsync()
    {
        string code;
        do
        {
            code = RandomCodeGenerator.Generate(10);
        }
        while (await _saleRepository.GetByCodeAsync(code) is not null);

        return code;
    }

    private static SaleDto ToDto(Sale sale) => new()
    {
        Id = sale.Id,
        SaleCode = sale.SaleCode,
        CustomerCode = sale.Customer.CustomerCode,
        CustomerName = sale.Customer.CustomerName,
        CustomerPhone = sale.Customer.Phone,
        EmployeeCode = sale.Employee.EmployeeCode,
        EmployeeName = $"{sale.Employee.FirstName} {sale.Employee.LastName}",
        DepartmentCode = sale.Employee.Department.DepartmentCode,
        DepartmentName = sale.Employee.Department.DepartmentName,
        Amount = sale.Amount,
        OrderDate = sale.OrderDate,
        Status = sale.Status.ToString(),
        Note = sale.Note,
        RejectionReason = sale.RejectionReason,
        ApproverEmployeeCode = sale.Approver?.EmployeeCode,
        ApproverName = sale.Approver is null ? null : $"{sale.Approver.FirstName} {sale.Approver.LastName}",
        ApprovedAt = sale.ApprovedAt
    };
}

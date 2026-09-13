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
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;

    public SaleService(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
    }

    public async Task<SaleDto> SubmitAsync(SubmitSaleRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var customer = await _customerRepository.GetByCodeAsync(request.CustomerCode)
            ?? throw new InvalidOperationException($"Customer code '{request.CustomerCode}' not found.");

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
        var sale = await ChangeStatusAsync(saleId, approverEmployeeCode, isAdmin, SaleStatus.Confirmed, "approved");
        return ToDto(sale);
    }

    public async Task<SaleDto> RejectAsync(long saleId, string approverEmployeeCode, bool isAdmin)
    {
        var sale = await ChangeStatusAsync(saleId, approverEmployeeCode, isAdmin, SaleStatus.Cancelled, "rejected");
        return ToDto(sale);
    }

    private async Task<Sale> ChangeStatusAsync(long saleId, string approverEmployeeCode, bool isAdmin, SaleStatus newStatus, string action)
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
        sale.ApprovedBy = approver.Id;
        sale.Approver = approver;
        sale.ApprovedAt = VietnamClock.Now;
        sale.UpdatedAt = VietnamClock.Now;

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
        EmployeeCode = sale.Employee.EmployeeCode,
        EmployeeName = $"{sale.Employee.FirstName} {sale.Employee.LastName}",
        Amount = sale.Amount,
        OrderDate = sale.OrderDate,
        Status = sale.Status.ToString(),
        Note = sale.Note,
        ApproverName = sale.Approver is null ? null : $"{sale.Approver.FirstName} {sale.Approver.LastName}",
        ApprovedAt = sale.ApprovedAt
    };
}

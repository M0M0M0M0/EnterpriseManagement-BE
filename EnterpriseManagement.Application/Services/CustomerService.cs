using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public CustomerService(ICustomerRepository customerRepository, IEmployeeRepository employeeRepository)
    {
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(ToDto);
    }

    public async Task<CustomerDto?> GetByCodeAsync(string customerCode)
    {
        var customer = await _customerRepository.GetByCodeAsync(customerCode);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var customer = new Customer
        {
            CustomerCode = await GenerateUniqueCustomerCodeAsync(),
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            AssignedEmployeeId = employee.Id,
            Status = CustomerStatus.Active,
            CreatedAt = VietnamClock.Now
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        var created = await _customerRepository.GetByCodeAsync(customer.CustomerCode);
        return ToDto(created!);
    }

    private async Task<string> GenerateUniqueCustomerCodeAsync()
    {
        string code;
        do
        {
            code = RandomCodeGenerator.Generate(8);
        }
        while (await _customerRepository.GetByCodeAsync(code) is not null);

        return code;
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        CustomerCode = customer.CustomerCode,
        CustomerName = customer.CustomerName,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address,
        AssignedEmployeeName = customer.AssignedEmployee is null
            ? null
            : $"{customer.AssignedEmployee.FirstName} {customer.AssignedEmployee.LastName}",
        Status = customer.Status.ToString()
    };
}

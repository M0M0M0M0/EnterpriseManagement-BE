using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICurrentUserService _currentUserService;

    public CustomersController(ICustomerService customerService, ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{customerCode}")]
    public async Task<ActionResult<CustomerDto>> GetByCode(string customerCode)
    {
        var customer = await _customerService.GetByCodeAsync(customerCode);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [RequirePermission("customer.create")]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request)
    {
        try
        {
            var created = await _customerService.CreateAsync(request, _currentUserService.EmployeeCode!);
            return CreatedAtAction(nameof(GetByCode), new { customerCode = created.CustomerCode }, created);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

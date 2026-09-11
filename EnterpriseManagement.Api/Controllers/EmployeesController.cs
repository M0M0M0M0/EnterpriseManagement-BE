using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [RequirePermission("employee.view.all")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    [HttpGet("{employeeCode}")]
    public async Task<ActionResult<EmployeeDto>> GetByCode(string employeeCode)
    {
        var employee = await _employeeService.GetByCodeAsync(employeeCode);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [RequirePermission("employee.manage")]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest request)
    {
        var created = await _employeeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetByCode), new { employeeCode = created.EmployeeCode }, created);
    }

    [HttpGet("team/{managerEmployeeCode}")]
    [RequirePermission("employee.view.team")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetTeam(string managerEmployeeCode)
    {
        try
        {
            var team = await _employeeService.GetTeamAsync(managerEmployeeCode);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{employeeCode}")]
    [RequirePermission("employee.manage")]
    public async Task<ActionResult<EmployeeDto>> Update(string employeeCode, UpdateEmployeeRequest request)
    {
        try
        {
            var updated = await _employeeService.UpdateAsync(employeeCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{employeeCode}/active")]
    [RequirePermission("employee.manage")]
    public async Task<ActionResult<EmployeeDto>> SetActive(string employeeCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _employeeService.SetActiveAsync(employeeCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

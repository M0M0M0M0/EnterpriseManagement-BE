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
    private readonly ICurrentUserService _currentUserService;

    public EmployeesController(IEmployeeService employeeService, ICurrentUserService currentUserService)
    {
        _employeeService = employeeService;
        _currentUserService = currentUserService;
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
    public async Task<ActionResult<CreateEmployeeResult>> Create(CreateEmployeeRequest request)
    {
        try
        {
            // Tạo hồ sơ + tài khoản; mật khẩu sinh chỉ trả về đúng lần này.
            return Ok(await _employeeService.CreateAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpGet("team/{managerEmployeeCode}/all")]
    [RequirePermission("employee.view.team")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetTeamRecursive(string managerEmployeeCode)
    {
        try
        {
            var team = await _employeeService.GetTeamRecursiveAsync(managerEmployeeCode);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("org-tree")]
    [RequirePermission("employee.view.team")]
    public async Task<ActionResult<IEnumerable<OrgTreeNodeDto>>> GetOrgTree()
    {
        try
        {
            var tree = await _employeeService.GetOrgTreeAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN"));
            return Ok(tree);
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
            return ErrorResult(employeeCode, ex);
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
            return ErrorResult(employeeCode, ex);
        }
    }

    // Không tìm thấy chính nhân viên đang sửa -> 404; còn lại là lỗi dữ liệu nhập -> 400.
    private ActionResult ErrorResult(string employeeCode, InvalidOperationException ex) =>
        ex.Message == $"Không tìm thấy nhân viên có mã '{employeeCode}'."
            ? NotFound(new { message = ex.Message })
            : BadRequest(new { message = ex.Message });
}

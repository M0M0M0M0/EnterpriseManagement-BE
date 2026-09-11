using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll()
    {
        var departments = await _departmentService.GetAllAsync();
        return Ok(departments);
    }

    [HttpGet("{departmentCode}")]
    public async Task<ActionResult<DepartmentDto>> GetByCode(string departmentCode)
    {
        var department = await _departmentService.GetByCodeAsync(departmentCode);
        return department is null ? NotFound() : Ok(department);
    }

    [HttpPost]
    [RequirePermission("department.manage")]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentRequest request)
    {
        try
        {
            var created = await _departmentService.CreateAsync(request);
            return CreatedAtAction(nameof(GetByCode), new { departmentCode = created.DepartmentCode }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{departmentCode}")]
    [RequirePermission("department.manage")]
    public async Task<ActionResult<DepartmentDto>> Update(string departmentCode, UpdateDepartmentRequest request)
    {
        try
        {
            var updated = await _departmentService.UpdateAsync(departmentCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{departmentCode}/active")]
    [RequirePermission("department.manage")]
    public async Task<ActionResult<DepartmentDto>> SetActive(string departmentCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _departmentService.SetActiveAsync(departmentCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

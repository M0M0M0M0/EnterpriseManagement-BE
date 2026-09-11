using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/roles")]
[RequirePermission("role.manage")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        var roles = await _roleService.GetAllAsync();
        return Ok(roles);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request)
    {
        try
        {
            var created = await _roleService.CreateAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{roleCode}")]
    public async Task<ActionResult<RoleDto>> Update(string roleCode, UpdateRoleRequest request)
    {
        try
        {
            var updated = await _roleService.UpdateAsync(roleCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{roleCode}/active")]
    public async Task<ActionResult<RoleDto>> SetActive(string roleCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _roleService.SetActiveAsync(roleCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

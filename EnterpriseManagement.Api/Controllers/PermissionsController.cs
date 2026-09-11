using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "ADMIN")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll()
    {
        var permissions = await _permissionService.GetAllAsync();
        return Ok(permissions);
    }

    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create(CreatePermissionRequest request)
    {
        try
        {
            var created = await _permissionService.CreateAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{permissionCode}")]
    public async Task<ActionResult<PermissionDto>> Update(string permissionCode, UpdatePermissionRequest request)
    {
        try
        {
            var updated = await _permissionService.UpdateAsync(permissionCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{permissionCode}/active")]
    public async Task<ActionResult<PermissionDto>> SetActive(string permissionCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _permissionService.SetActiveAsync(permissionCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

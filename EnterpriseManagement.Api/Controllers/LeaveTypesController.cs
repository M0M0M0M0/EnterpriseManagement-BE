using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetAll()
    {
        var leaveTypes = await _leaveTypeService.GetAllAsync();
        return Ok(leaveTypes);
    }

    [HttpGet("{leaveTypeCode}")]
    public async Task<ActionResult<LeaveTypeDto>> GetByCode(string leaveTypeCode)
    {
        var leaveType = await _leaveTypeService.GetByCodeAsync(leaveTypeCode);
        return leaveType is null ? NotFound() : Ok(leaveType);
    }

    [HttpPost]
    [RequirePermission("leavetype.manage")]
    public async Task<ActionResult<LeaveTypeDto>> Create(CreateLeaveTypeRequest request)
    {
        try
        {
            var created = await _leaveTypeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetByCode), new { leaveTypeCode = created.LeaveTypeCode }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

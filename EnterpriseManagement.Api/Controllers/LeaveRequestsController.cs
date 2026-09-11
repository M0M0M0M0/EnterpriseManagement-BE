using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService, ICurrentUserService currentUserService)
    {
        _leaveRequestService = leaveRequestService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveRequestDto>> Submit(SubmitLeaveRequest request)
    {
        try
        {
            var result = await _leaveRequestService.SubmitAsync(request, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetMine()
    {
        var requests = await _leaveRequestService.GetByEmployeeAsync(_currentUserService.EmployeeCode!);
        return Ok(requests);
    }

    [HttpPut("{id}/cancel")]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(long id)
    {
        try
        {
            var result = await _leaveRequestService.CancelAsync(id, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending")]
    [RequirePermission("leave.request.view")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPending()
    {
        var pending = await _leaveRequestService.GetPendingAsync(_currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"));
        return Ok(pending);
    }

    [HttpGet("history")]
    [RequirePermission("leave.request.view")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetHistory()
    {
        var history = await _leaveRequestService.GetHistoryAsync(_currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"));
        return Ok(history);
    }

    [HttpPut("{id}/approve")]
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveRequestDto>> Approve(long id)
    {
        try
        {
            var result = await _leaveRequestService.ApproveAsync(id, _currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/reject")]
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveRequestDto>> Reject(long id, RejectLeaveRequest request)
    {
        try
        {
            var result = await _leaveRequestService.RejectAsync(id, _currentUserService.EmployeeCode!, request.RejectionReason, _currentUserService.IsInRole("ADMIN"));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

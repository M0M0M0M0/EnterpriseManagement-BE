using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Xin làm thêm giờ (OT): nhân viên gửi (overtime.request.self), quản lý duyệt (overtime.request.approve).
[ApiController]
[Route("api/overtime-requests")]
[Authorize]
public class OvertimeRequestsController : ControllerBase
{
    private readonly IOvertimeService _service;
    private readonly ICurrentUserService _currentUserService;

    public OvertimeRequestsController(IOvertimeService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [RequirePermission("overtime.request.self")]
    public async Task<ActionResult<OvertimeRequestDto>> Submit(SubmitOvertimeRequest request)
    {
        try
        {
            return Ok(await _service.SubmitAsync(request, _currentUserService.EmployeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [RequirePermission("overtime.request.self")]
    public async Task<ActionResult<IEnumerable<OvertimeRequestDto>>> GetMine() =>
        Ok(await _service.GetMineAsync(_currentUserService.EmployeeCode));

    [HttpGet("quota")]
    [RequirePermission("overtime.request.self")]
    public async Task<ActionResult<OvertimeQuotaDto>> GetQuota() =>
        Ok(await _service.GetQuotaAsync(_currentUserService.EmployeeCode));

    [HttpPut("{id}/cancel")]
    [RequirePermission("overtime.request.self")]
    public async Task<ActionResult<OvertimeRequestDto>> Cancel(long id)
    {
        try
        {
            return Ok(await _service.CancelAsync(id, _currentUserService.EmployeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending")]
    [RequirePermission("overtime.request.view")]
    public async Task<ActionResult<IEnumerable<OvertimeRequestDto>>> GetPending() =>
        Ok(await _service.GetPendingAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN")));

    [HttpGet("history")]
    [RequirePermission("overtime.request.view")]
    public async Task<ActionResult<IEnumerable<OvertimeRequestDto>>> GetHistory() =>
        Ok(await _service.GetHistoryAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN")));

    [HttpPut("{id}/approve")]
    [RequirePermission("overtime.request.approve")]
    public async Task<ActionResult<OvertimeRequestDto>> Approve(long id)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id, _currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/reject")]
    [RequirePermission("overtime.request.approve")]
    public async Task<ActionResult<OvertimeRequestDto>> Reject(long id, RejectOvertimeRequest request)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, _currentUserService.EmployeeCode, request.RejectionReason, _currentUserService.IsInRole("ADMIN")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

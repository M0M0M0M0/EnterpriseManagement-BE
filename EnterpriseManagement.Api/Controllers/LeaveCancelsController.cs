using EnterpriseManagement.Api.Common;
using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Xin hủy đơn nghỉ phép đã duyệt nhưng chưa bắt đầu: nhân viên gửi (leave.request.self), quản lý duyệt (leave.request.approve).
[ApiController]
[Route("api/leave-cancels")]
[Authorize]
public class LeaveCancelsController : ControllerBase
{
    private readonly ILeaveCancelService _service;
    private readonly ICurrentUserService _currentUserService;

    public LeaveCancelsController(ILeaveCancelService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveCancelRequestDto>> Submit(SubmitLeaveCancelRequest request)
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
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<IEnumerable<LeaveCancelRequestDto>>> GetMine() =>
        Ok(await _service.GetMineAsync(_currentUserService.EmployeeCode));

    [HttpPut("{id}/withdraw")]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveCancelRequestDto>> Withdraw(long id)
    {
        try
        {
            return Ok(await _service.WithdrawAsync(id, _currentUserService.EmployeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending")]
    [RequirePermission("leave.request.view")]
    public async Task<ActionResult<IEnumerable<LeaveCancelRequestDto>>> GetPending() =>
        Ok(await _service.GetPendingAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN")));

    [HttpGet("history")]
    [RequirePermission("leave.request.view")]
    public async Task<ActionResult<IEnumerable<LeaveCancelRequestDto>>> GetHistory(
        [FromQuery] string? status, [FromQuery] string? q, [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
    {
        var history = await _service.GetHistoryAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN"));
        var filtered = history
            .Where(r => r.Status != "Pending")
            .Where(r => string.IsNullOrEmpty(status) || r.Status == status)
            .Where(r => Paging.Matches(q, r.EmployeeName, r.EmployeeCode))
            .OrderByDescending(r => r.Id);
        return Ok(this.Paged(filtered, page, pageSize));
    }

    [HttpPut("{id}/approve")]
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveCancelRequestDto>> Approve(long id)
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
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveCancelRequestDto>> Reject(long id, RejectLeaveCancelRequest request)
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

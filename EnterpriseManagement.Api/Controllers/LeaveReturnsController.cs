using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Yêu cầu đi làm lại khi đang có nghỉ phép đã duyệt. Nhân viên gửi (leave.request.self), Manager duyệt (leave.request.approve),
// người có quyền xử lý (attendance.note.manage) xác nhận các buổi đã bắt đầu.
[ApiController]
[Route("api/leave-returns")]
[Authorize]
public class LeaveReturnsController : ControllerBase
{
    private readonly ILeaveReturnService _service;
    private readonly ICurrentUserService _currentUserService;

    public LeaveReturnsController(ILeaveReturnService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    // Xem trước các buổi sẽ được gỡ khỏi nghỉ phép; không lưu gì.
    [HttpGet("preview")]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveReturnPreviewDto>> Preview([FromQuery] DateOnly fromDate, [FromQuery] string fromSession)
    {
        try
        {
            return Ok(await _service.PreviewAsync(_currentUserService.EmployeeCode!, fromDate, fromSession));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<LeaveReturnRequestDto>> Submit(SubmitLeaveReturnRequest request)
    {
        try
        {
            return Ok(await _service.SubmitAsync(_currentUserService.EmployeeCode!, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [RequirePermission("leave.request.self")]
    public async Task<ActionResult<IEnumerable<LeaveReturnRequestDto>>> GetMine() =>
        Ok(await _service.GetMineAsync(_currentUserService.EmployeeCode!));

    // ---- Manager ----

    [HttpGet("pending")]
    [RequirePermission("leave.request.view")]
    public async Task<ActionResult<IEnumerable<LeaveReturnRequestDto>>> GetPending() =>
        Ok(await _service.GetPendingForManagerAsync(_currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN")));

    [HttpPut("{id}/approve")]
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveReturnRequestDto>> Approve(long id)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id, _currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/reject")]
    [RequirePermission("leave.request.approve")]
    public async Task<ActionResult<LeaveReturnRequestDto>> Reject(long id, RejectLeaveReturnRequest request)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, _currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"), request.Reason));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ---- Người xử lý (HR) ----

    [HttpGet("awaiting-processing")]
    [RequirePermission("attendance.note.manage")]
    public async Task<ActionResult<IEnumerable<LeaveReturnRequestDto>>> GetAwaitingProcessing() =>
        Ok(await _service.GetAwaitingProcessingAsync());

    [HttpPut("{id}/process")]
    [RequirePermission("attendance.note.manage")]
    public async Task<ActionResult<LeaveReturnRequestDto>> Process(long id, ProcessLeaveReturnRequest request)
    {
        try
        {
            return Ok(await _service.ProcessAsync(id, _currentUserService.EmployeeCode!, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/reject-processing")]
    [RequirePermission("attendance.note.manage")]
    public async Task<ActionResult<LeaveReturnRequestDto>> RejectProcessing(long id, RejectLeaveReturnRequest request)
    {
        try
        {
            return Ok(await _service.RejectProcessingAsync(id, _currentUserService.EmployeeCode!, request.Reason));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

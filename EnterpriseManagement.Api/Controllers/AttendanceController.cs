using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceAdjustmentService _attendanceAdjustmentService;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceController(
        IAttendanceService attendanceService,
        IAttendanceAdjustmentService attendanceAdjustmentService,
        ICurrentUserService currentUserService)
    {
        _attendanceService = attendanceService;
        _attendanceAdjustmentService = attendanceAdjustmentService;
        _currentUserService = currentUserService;
    }

    [HttpPost("punch")]
    [RequirePermission("attendance.punch")]
    public async Task<ActionResult<AttendanceRecordDto>> Punch()
    {
        try
        {
            var result = await _attendanceService.PunchAsync(_currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{employeeCode}/history")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetHistory(string employeeCode)
    {
        if (_currentUserService.IsInRole("EMPLOYEE") && employeeCode != _currentUserService.EmployeeCode)
        {
            return Forbid();
        }

        try
        {
            var history = await _attendanceService.GetHistoryAsync(employeeCode);
            return Ok(history);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("department/{departmentCode}")]
    [RequirePermission("attendance.view.team")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetByDepartment(
        string departmentCode, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        try
        {
            var records = await _attendanceService.GetByDepartmentAsync(departmentCode, startDate, endDate);
            return Ok(records);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("adjustments")]
    [RequirePermission("attendance.adjustment.self")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> SubmitAdjustment(SubmitAdjustmentRequest request)
    {
        try
        {
            var result = await _attendanceAdjustmentService.SubmitAsync(request, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("adjustments/mine")]
    [RequirePermission("attendance.adjustment.self")]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentDto>>> GetMyAdjustments()
    {
        var adjustments = await _attendanceAdjustmentService.GetByEmployeeAsync(_currentUserService.EmployeeCode!);
        return Ok(adjustments);
    }

    [HttpGet("adjustments/pending")]
    [RequirePermission("attendance.adjustment.view")]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentDto>>> GetPendingAdjustments()
    {
        var pending = await _attendanceAdjustmentService.GetPendingAsync();
        return Ok(pending);
    }

    [HttpPut("adjustments/{id}/approve")]
    [RequirePermission("attendance.adjustment.approve")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> ApproveAdjustment(long id)
    {
        try
        {
            var result = await _attendanceAdjustmentService.ApproveAsync(id, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("adjustments/{id}/reject")]
    [RequirePermission("attendance.adjustment.approve")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> RejectAdjustment(long id)
    {
        try
        {
            var result = await _attendanceAdjustmentService.RejectAsync(id, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

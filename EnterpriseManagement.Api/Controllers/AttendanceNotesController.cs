using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Ghi chú ngoại lệ chấm công theo buổi (quên chấm công, vào muộn, vắng...). Chỉ người có quyền attendance.note.manage dùng được.
[ApiController]
[Route("api/attendance-notes")]
[Authorize]
public class AttendanceNotesController : ControllerBase
{
    private readonly IAttendanceNoteService _service;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceNotesController(IAttendanceNoteService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [RequirePermission("attendance.note.manage")]
    public async Task<ActionResult<IEnumerable<AttendanceNoteDto>>> GetActive(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? employeeCode)
    {
        try
        {
            return Ok(await _service.GetActiveAsync(from, to, employeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequirePermission("attendance.note.manage")]
    public async Task<ActionResult<IEnumerable<AttendanceNoteDto>>> Create(CreateAttendanceNoteRequest request)
    {
        try
        {
            return Ok(await _service.CreateAsync(_currentUserService.EmployeeCode!, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [RequirePermission("attendance.note.manage")]
    public async Task<IActionResult> Revoke(long id)
    {
        try
        {
            await _service.RevokeAsync(id, _currentUserService.EmployeeCode!);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

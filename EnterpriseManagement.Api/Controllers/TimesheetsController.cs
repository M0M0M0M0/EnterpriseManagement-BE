using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Bảng công tháng của HR: xem tổng hợp, xuất CSV, chốt/mở lại bảng công (khoá dữ liệu của tháng).
[ApiController]
[Route("api/timesheets")]
[Authorize]
[RequirePermission("timesheet.manage")]
public class TimesheetsController : ControllerBase
{
    private readonly ITimesheetService _service;
    private readonly ICurrentUserService _currentUserService;

    public TimesheetsController(ITimesheetService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<TimesheetDto>> Get([FromQuery] int year, [FromQuery] int month, [FromQuery] string? departmentCode)
    {
        try
        {
            return Ok(await _service.GetAsync(year, month, departmentCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int year, [FromQuery] int month, [FromQuery] string? departmentCode)
    {
        try
        {
            var bytes = await _service.ExportCsvAsync(year, month, departmentCode);
            return File(bytes, "text/csv; charset=utf-8", $"bang-cong-{year}-{month:00}.csv");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("periods")]
    public async Task<ActionResult<IEnumerable<AttendancePeriodDto>>> GetClosedPeriods([FromQuery] int year) =>
        Ok(await _service.GetClosedPeriodsAsync(year));

    [HttpPost("close")]
    public async Task<ActionResult<AttendancePeriodDto>> Close([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            return Ok(await _service.CloseAsync(year, month, _currentUserService.EmployeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("close")]
    public async Task<IActionResult> Reopen([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            await _service.ReopenAsync(year, month);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

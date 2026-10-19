using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Lịch ngày nghỉ lễ của công ty. Ai đăng nhập cũng xem được (để hiển thị lịch); chỉ người có quyền holiday.manage mới thêm/xoá.
[ApiController]
[Route("api/holidays")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayService _service;

    public HolidaysController(IHolidayService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HolidayDto>>> GetByYear([FromQuery] int? year) =>
        Ok(await _service.GetByYearAsync(year ?? VietnamClock.Today.Year));

    [HttpPost]
    [RequirePermission("holiday.manage")]
    public async Task<ActionResult<HolidayDto>> Create(CreateHolidayRequest request)
    {
        try
        {
            return Ok(await _service.CreateAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [RequirePermission("holiday.manage")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

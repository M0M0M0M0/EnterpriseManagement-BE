using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Hộp thông báo của chính người đăng nhập. Tài khoản không có hồ sơ nhân viên (vd admin) nhận danh sách rỗng.
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(INotificationService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetList(
        [FromQuery] bool unreadOnly = false, [FromQuery] int take = 50, [FromQuery] long? beforeId = null) =>
        Ok(await _service.GetListAsync(_currentUserService.EmployeeCode!, unreadOnly, take, beforeId));

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount() =>
        Ok(new UnreadCountDto { Count = await _service.GetUnreadCountAsync(_currentUserService.EmployeeCode!) });

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        try
        {
            await _service.MarkReadAsync(_currentUserService.EmployeeCode!, id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _service.MarkAllReadAsync(_currentUserService.EmployeeCode!);
        return NoContent();
    }

    // Đăng ký token Firebase của thiết bị cho tài khoản hiện tại (gọi sau khi đăng nhập và mỗi khi token đổi).
    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken(RegisterDeviceTokenRequest request)
    {
        try
        {
            await _service.RegisterDeviceTokenAsync(_currentUserService.EmployeeCode!, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Gỡ token khi đăng xuất để thiết bị không nhận thông báo của người cũ.
    [HttpDelete("device-token")]
    public async Task<IActionResult> UnregisterDeviceToken([FromQuery] string token)
    {
        await _service.UnregisterDeviceTokenAsync(_currentUserService.EmployeeCode!, token);
        return NoContent();
    }
}

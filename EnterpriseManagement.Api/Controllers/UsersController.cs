using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[RequirePermission("user.manage")]
// Tài khoản gắn 1-1 với hồ sơ nhân viên: tạo tài khoản và đổi email nằm ở POST/PUT /api/employees.
// Ở đây chỉ còn khoá/mở đăng nhập, đặt lại mật khẩu và đổi vai trò.
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpPut("{username}/roles")]
    public async Task<ActionResult<UserDto>> UpdateRoles(string username, UpdateUserRolesRequest request)
    {
        try
        {
            return Ok(await _userService.UpdateRolesAsync(username, request.RoleCodes));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{username}/active")]
    public async Task<ActionResult<UserDto>> SetActive(string username, SetActiveRequest request)
    {
        try
        {
            var updated = await _userService.SetActiveAsync(username, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{username}/reset-password")]
    public async Task<ActionResult<ResetPasswordResult>> ResetPassword(string username)
    {
        try
        {
            var result = await _userService.ResetPasswordAsync(username);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

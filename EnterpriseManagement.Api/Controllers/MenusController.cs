using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/menus")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ICurrentUserService _currentUserService;

    public MenusController(IMenuService menuService, ICurrentUserService currentUserService)
    {
        _menuService = menuService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<IEnumerable<MenuDto>>> GetAll()
    {
        var menus = await _menuService.GetAllAsync();
        return Ok(menus);
    }

    // Sidebar frontend gọi endpoint này để lấy đúng danh sách menu mà user đang đăng nhập
    // được phép thấy, theo Permission đã gán qua Role — thay vì FE tự hardcode danh sách menu.
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<MenuDto>>> GetMine()
    {
        var menus = await _menuService.GetMineAsync(_currentUserService.Permissions);
        return Ok(menus);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MenuDto>> Create(CreateMenuRequest request)
    {
        try
        {
            var created = await _menuService.CreateAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{menuCode}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MenuDto>> Update(string menuCode, UpdateMenuRequest request)
    {
        try
        {
            var updated = await _menuService.UpdateAsync(menuCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{menuCode}/visible")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MenuDto>> SetVisible(string menuCode, SetVisibleRequest request)
    {
        try
        {
            var updated = await _menuService.SetVisibleAsync(menuCode, request.IsVisible);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{menuCode}/active")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MenuDto>> SetActive(string menuCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _menuService.SetActiveAsync(menuCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

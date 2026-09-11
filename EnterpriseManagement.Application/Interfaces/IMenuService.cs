using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<MenuDto>> GetAllAsync();
    Task<IEnumerable<MenuDto>> GetMineAsync(IEnumerable<string> userPermissions);
    Task<MenuDto> CreateAsync(CreateMenuRequest request);
    Task<MenuDto> UpdateAsync(string menuCode, UpdateMenuRequest request);
    Task<MenuDto> SetVisibleAsync(string menuCode, bool isVisible);
    Task<MenuDto> SetActiveAsync(string menuCode, bool isActive);
}

namespace EnterpriseManagement.Domain.Entities.Identity;

public class Menu
{
    public long Id { get; set; }

    public long? ParentMenuId { get; set; }
    public Menu? ParentMenu { get; set; }
    public ICollection<Menu> ChildMenus { get; set; } = new List<Menu>();

    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MenuPermission> MenuPermissions { get; set; } = new List<MenuPermission>();
}

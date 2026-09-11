namespace EnterpriseManagement.Application.DTOs;

public class MenuDto
{
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Route { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<string> Permissions { get; set; } = new();
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
}

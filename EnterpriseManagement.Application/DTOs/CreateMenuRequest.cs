namespace EnterpriseManagement.Application.DTOs;

public class CreateMenuRequest
{
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Route { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<string> Permissions { get; set; } = new();
}

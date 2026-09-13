namespace EnterpriseManagement.Application.DTOs;

public class UpdatePositionRequest
{
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RankLevel { get; set; }
    public string RoleCode { get; set; } = string.Empty;
}

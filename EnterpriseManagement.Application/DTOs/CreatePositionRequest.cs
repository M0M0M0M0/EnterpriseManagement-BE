namespace EnterpriseManagement.Application.DTOs;

public class CreatePositionRequest
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

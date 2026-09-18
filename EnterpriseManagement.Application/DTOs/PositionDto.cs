namespace EnterpriseManagement.Application.DTOs;

public class PositionDto
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public decimal? StandardSalary { get; set; }
}

namespace EnterpriseManagement.Application.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Username { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? IpAddress { get; set; }
}

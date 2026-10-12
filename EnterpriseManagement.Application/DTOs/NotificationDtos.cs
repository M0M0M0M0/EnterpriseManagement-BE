namespace EnterpriseManagement.Application.DTOs;

public class NotificationDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Route { get; set; }
    public long? RefId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = string.Empty;

    // android | ios | web
    public string Platform { get; set; } = "android";
}

namespace EnterpriseManagement.Application.DTOs;

// Các giá trị Module và Action đang có trong nhật ký, để giao diện làm ô chọn thay vì nhập tay.
public class AuditLogFilterOptionsDto
{
    public List<string> Modules { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}

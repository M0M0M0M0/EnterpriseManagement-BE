namespace EnterpriseManagement.Domain.Entities.Attendance;

// Ngày nghỉ lễ của công ty (theo luật định hoặc nghỉ bù). Không phải ngày làm việc: không bị tính vắng, không tính vào ngày nghỉ phép.
public class Holiday
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

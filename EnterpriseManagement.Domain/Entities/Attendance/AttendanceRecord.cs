using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Attendance;

public class AttendanceRecord
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public long? ShiftId { get; set; }
    public WorkShift? Shift { get; set; }

    public DateOnly AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? WorkingHours { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }

    // Vị trí GPS và ảnh chụp lúc chấm công, chỉ để làm bằng chứng cho quản lý xem lại (không dùng nhận diện
    // khuôn mặt). PhotoPath là đường dẫn tương đối trong thư mục lưu ảnh của server, không phải URL công khai.
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public string? CheckInPhotoPath { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string? CheckOutPhotoPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AttendanceAdjustment> Adjustments { get; set; } = new List<AttendanceAdjustment>();
}

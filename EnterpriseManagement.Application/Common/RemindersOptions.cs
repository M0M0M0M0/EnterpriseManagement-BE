namespace EnterpriseManagement.Application.Common;

// Cấu hình nhắc nhở chấm công (mục "Reminders" trong appsettings.json).
public class RemindersOptions
{
    public const string SectionName = "Reminders";

    public bool Enabled { get; set; } = true;

    // Giờ gửi nhắc chấm công vào (mặc định 07:45, trước giờ làm 08:00 mười lăm phút).
    public string CheckInTime { get; set; } = "07:45";

    // Giờ gửi nhắc chấm công ra (mặc định 17:00, hết giờ làm việc).
    public string CheckOutTime { get; set; } = "17:00";

    // Sau giờ nhắc bao nhiêu phút thì còn được gửi bù (vd backend khởi động lại muộn); quá khung này thì bỏ qua.
    public int WindowMinutes { get; set; } = 30;
}

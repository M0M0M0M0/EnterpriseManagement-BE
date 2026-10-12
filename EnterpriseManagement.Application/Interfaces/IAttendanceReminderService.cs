namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceReminderService
{
    // Gửi nhắc nhở chấm công nếu đang trong khung giờ nhắc (mỗi nhân viên mỗi loại một lần mỗi ngày). Trả về số thông báo đã gửi.
    Task<int> RunAsync(DateTime now);
}

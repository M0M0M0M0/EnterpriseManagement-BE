using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Common;

// Các hàm dựng nội dung thông báo tiếng Việt dùng chung.
public static class NotificationText
{
    public static string Range(DateTime start, DateTime end) =>
        start.Date == end.Date ? start.ToString("dd/MM/yyyy") : $"{start:dd/MM} - {end:dd/MM/yyyy}";

    public static string Range(DateOnly start, DateOnly end) =>
        start == end ? start.ToString("dd/MM/yyyy") : $"{start:dd/MM} - {end:dd/MM/yyyy}";

    public static string Session(DaySession session) => session == DaySession.Morning ? "buổi sáng" : "buổi chiều";

    public static string Session(LeaveSession? session) => session switch
    {
        LeaveSession.Morning => "buổi sáng",
        LeaveSession.Afternoon => "buổi chiều",
        _ => "cả ngày"
    };

    public static string Status(SessionStatus status) => status switch
    {
        SessionStatus.OnTime => "đi làm đúng giờ",
        SessionStatus.Late => "đi muộn",
        SessionStatus.Absent => "vắng",
        SessionStatus.OnLeave => "nghỉ phép",
        _ => "chưa có dữ liệu"
    };

    public static string Name(Domain.Entities.HR.Employee employee) => $"{employee.FirstName} {employee.LastName}";
}

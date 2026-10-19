using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Common;

// Tài khoản hệ thống (admin kỹ thuật): không tham gia nghiệp vụ nên không được làm quản lý/trưởng phòng.
// Khác với người không thuộc diện chấm công (vd giám đốc, TracksAttendance = false): họ vẫn có thể là quản lý của người khác
// nhưng không chấm công, không xin nghỉ/OT/điều chỉnh công và không nằm trong bảng công.
public static class SystemAccount
{
    public const string EmployeeCode = "ADMIN";

    public static bool IsSystem(Employee employee) => employee.EmployeeCode == EmployeeCode;

    public const string NotTrackedMessage = "Tài khoản này không thuộc diện chấm công nên không dùng chức năng này.";
}

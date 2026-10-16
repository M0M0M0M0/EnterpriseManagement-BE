using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceService
{
    // Chỉ chấm công được khi đang trong bán kính cho phép quanh vị trí công ty và có ảnh kèm theo;
    // vị trí và ảnh được lưu lại làm bằng chứng cho giờ vào hoặc giờ ra tương ứng.
    Task<AttendanceRecordDto> PunchAsync(string employeeCode, PunchRequest request);

    OfficeLocationDto GetOfficeLocation();
    Task<IEnumerable<AttendanceRecordDto>> GetHistoryAsync(string employeeCode);
    // Chấm công của toàn công ty trong khoảng ngày, tuỳ chọn lọc theo phòng ban (dành cho người có quyền xem tất cả).
    Task<IEnumerable<AttendanceRecordDto>> GetAllAsync(DateOnly startDate, DateOnly endDate, string? departmentCode);
    Task<IEnumerable<AttendanceRecordDto>> GetByDepartmentAsync(string departmentCode, DateOnly startDate, DateOnly endDate);

    // Chấm công của toàn bộ cấp dưới (cả cấp dưới của cấp dưới) của một quản lý, không gồm chính họ.
    Task<IEnumerable<AttendanceRecordDto>> GetByTeamAsync(string managerEmployeeCode, DateOnly startDate, DateOnly endDate);

    // Trạng thái hiệu lực từng ngày (tách sáng/chiều) trong khoảng [from, to], gồm cả cuối tuần và ngày chưa có dữ liệu.
    Task<IEnumerable<AttendanceCalendarDayDto>> GetCalendarAsync(string employeeCode, DateOnly from, DateOnly to);

    // Cùng logic PunchAsync dùng để xác định Present/Late/HalfDayAbsent theo giờ check-in — expose
    // ra để AttendanceAdjustmentService tính lại đúng trạng thái khi duyệt yêu cầu sửa giờ chấm công
    // (thay vì chỉ có 1 nhánh Absent -> Present như trước).
    Task<AttendanceStatus> DetermineCheckInStatusAsync(long employeeId, DateOnly date, DateTime checkInTime);
}

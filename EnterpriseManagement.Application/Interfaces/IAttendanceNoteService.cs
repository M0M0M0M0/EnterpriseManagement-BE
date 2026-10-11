using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceNoteService
{
    // Tạo ghi chú cho từng (ngày, buổi) trong khoảng. Có hiệu lực ngay, ưu tiên hơn chấm công gốc khi tính ngày công.
    // Trả về các ghi chú đã tạo (mỗi buổi một dòng).
    Task<IEnumerable<AttendanceNoteDto>> CreateAsync(string creatorEmployeeCode, CreateAttendanceNoteRequest request);

    // Thu hồi một ghi chú (không còn hiệu lực, giữ lại làm lịch sử); trạng thái buổi đó quay về theo dữ liệu gốc.
    Task RevokeAsync(long noteId, string revokerEmployeeCode);

    Task<IEnumerable<AttendanceNoteDto>> GetActiveAsync(DateOnly from, DateOnly to, string? employeeCode);
}

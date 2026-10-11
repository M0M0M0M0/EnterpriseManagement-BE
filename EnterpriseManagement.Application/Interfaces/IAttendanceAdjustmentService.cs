using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceAdjustmentService
{
    Task<AttendanceAdjustmentDto> SubmitAsync(SubmitAdjustmentRequest request, string employeeCode);
    Task<IEnumerable<AttendanceAdjustmentDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin);
    Task<IEnumerable<AttendanceAdjustmentDto>> GetByEmployeeAsync(string employeeCode);
    Task<AttendanceAdjustmentDto> ApproveAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin);
    Task<AttendanceAdjustmentDto> RejectAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin);

    // Bước 2 sau khi Manager đã duyệt: người có quyền xử lý (HR) áp dụng đúng giờ đề xuất vào dữ liệu chấm công, hoặc từ chối.
    Task<IEnumerable<AttendanceAdjustmentDto>> GetPendingProcessingAsync();
    Task<AttendanceAdjustmentDto> ApplyAsync(long adjustmentId, string processorEmployeeCode);
    Task<AttendanceAdjustmentDto> RejectProcessingAsync(long adjustmentId, string processorEmployeeCode, string? reason);
}

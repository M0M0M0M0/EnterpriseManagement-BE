using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveReturnService
{
    // Xem trước các buổi sẽ được gỡ khỏi nghỉ phép nếu xin đi làm lại từ (ngày, buổi) này. Không lưu gì.
    Task<LeaveReturnPreviewDto> PreviewAsync(string employeeCode, DateOnly fromDate, string fromSession);

    Task<LeaveReturnRequestDto> SubmitAsync(string employeeCode, SubmitLeaveReturnRequest request);
    Task<IEnumerable<LeaveReturnRequestDto>> GetMineAsync(string employeeCode);

    // Bước Manager: danh sách chờ duyệt thuộc quyền của người gọi, duyệt hoặc từ chối.
    Task<IEnumerable<LeaveReturnRequestDto>> GetPendingForManagerAsync(string managerEmployeeCode, bool isAdmin);
    Task<LeaveReturnRequestDto> ApproveAsync(long id, string managerEmployeeCode, bool isAdmin);
    Task<LeaveReturnRequestDto> RejectAsync(long id, string managerEmployeeCode, bool isAdmin, string? reason);

    // Bước xử lý (HR): các yêu cầu còn buổi đã bắt đầu cần xác nhận đã đi làm.
    Task<IEnumerable<LeaveReturnRequestDto>> GetAwaitingProcessingAsync();
    Task<LeaveReturnRequestDto> ProcessAsync(long id, string processorEmployeeCode, ProcessLeaveReturnRequest request);
    Task<LeaveReturnRequestDto> RejectProcessingAsync(long id, string processorEmployeeCode, string? reason);
}

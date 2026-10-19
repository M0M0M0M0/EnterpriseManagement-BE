using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveCancelService
{
    Task<LeaveCancelRequestDto> SubmitAsync(SubmitLeaveCancelRequest request, string employeeCode);
    Task<IEnumerable<LeaveCancelRequestDto>> GetMineAsync(string employeeCode);
    Task<LeaveCancelRequestDto> WithdrawAsync(long id, string employeeCode);

    Task<IEnumerable<LeaveCancelRequestDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin);
    Task<IEnumerable<LeaveCancelRequestDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin);
    Task<LeaveCancelRequestDto> ApproveAsync(long id, string approverEmployeeCode, bool isAdmin);
    Task<LeaveCancelRequestDto> RejectAsync(long id, string approverEmployeeCode, string? reason, bool isAdmin);
}

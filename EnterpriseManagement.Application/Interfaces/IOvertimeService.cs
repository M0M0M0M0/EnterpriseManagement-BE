using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IOvertimeService
{
    Task<OvertimeRequestDto> SubmitAsync(SubmitOvertimeRequest request, string employeeCode);
    Task<IEnumerable<OvertimeRequestDto>> GetMineAsync(string employeeCode);
    Task<OvertimeQuotaDto> GetQuotaAsync(string employeeCode);
    Task<OvertimeRequestDto> CancelAsync(long id, string employeeCode);

    Task<IEnumerable<OvertimeRequestDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin);
    Task<IEnumerable<OvertimeRequestDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin);
    Task<OvertimeRequestDto> ApproveAsync(long id, string approverEmployeeCode, bool isAdmin);
    Task<OvertimeRequestDto> RejectAsync(long id, string approverEmployeeCode, string? reason, bool isAdmin);
}

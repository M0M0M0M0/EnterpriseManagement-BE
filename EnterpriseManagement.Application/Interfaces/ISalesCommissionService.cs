using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ISalesCommissionService
{
    Task<CommissionDto> CalculateAsync(CalculateCommissionRequest request, string requesterEmployeeCode);
    Task<IEnumerable<CommissionDto>> GetMineAsync(string employeeCode);
    Task<IEnumerable<CommissionDto>> GetPendingAsync();
    Task<IEnumerable<CommissionDto>> GetHistoryAsync();
    Task<CommissionDto> ApproveAsync(long commissionId, string approverEmployeeCode, bool isAdmin);
}

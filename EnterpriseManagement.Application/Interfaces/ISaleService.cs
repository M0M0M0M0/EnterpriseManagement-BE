using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ISaleService
{
    Task<SaleDto> SubmitAsync(SubmitSaleRequest request, string employeeCode);
    Task<IEnumerable<SaleDto>> GetByEmployeeAsync(string employeeCode);
    Task<SaleDto> UpdateAsync(long saleId, string employeeCode, UpdateSaleRequest request);
    Task<IEnumerable<SaleDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin);
    Task<IEnumerable<SaleDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin);
    Task<SaleDto> ApproveAsync(long saleId, string approverEmployeeCode, bool isAdmin);
    Task<SaleDto> RejectAsync(long saleId, string approverEmployeeCode, bool isAdmin, string rejectionReason);
    Task<SaleDto> CancelAsync(long saleId, string employeeCode);
}

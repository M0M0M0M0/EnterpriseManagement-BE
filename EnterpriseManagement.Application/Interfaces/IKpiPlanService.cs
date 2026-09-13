using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IKpiPlanService
{
    Task<IEnumerable<KpiPlanDto>> GetAllAsync();
    Task<KpiPlanDto> GetByIdAsync(long id);
    Task<KpiPlanDto> CreateAsync(CreateKpiPlanRequest request);
    Task<KpiPlanDto> UpdateAsync(long id, UpdateKpiPlanRequest request);
    Task<KpiPlanDto> SetActiveAsync(long id, bool isActive);

    // Ghi đè toàn bộ danh sách nhân viên thuộc Plan: nhân viên có trong employeeCodes sẽ được
    // gán Plan này, nhân viên đang thuộc Plan này nhưng không còn trong danh sách sẽ bị gỡ.
    // Chỉ được gán nhân viên trong team mình quản lý trực tiếp (trừ khi isAdmin) — cùng quy tắc
    // với SalesCommissionService.CalculateAsync/ApproveAsync.
    Task<KpiPlanDto> AssignEmployeesAsync(long id, IReadOnlyList<string> employeeCodes, string requesterEmployeeCode, bool isAdmin);
}

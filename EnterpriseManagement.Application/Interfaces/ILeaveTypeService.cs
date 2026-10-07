using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveTypeService
{
    Task<IEnumerable<LeaveTypeDto>> GetAllAsync();
    Task<LeaveTypeDto?> GetByCodeAsync(string leaveTypeCode);
    Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeRequest request);
    Task<LeaveTypeDto> UpdateAsync(string leaveTypeCode, UpdateLeaveTypeRequest request);
    Task<LeaveTypeDto> SetActiveAsync(string leaveTypeCode, bool isActive);
    Task DeleteAsync(string leaveTypeCode);
}

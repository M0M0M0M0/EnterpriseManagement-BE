using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeaveTypeService(ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IEnumerable<LeaveTypeDto>> GetAllAsync()
    {
        var leaveTypes = await _leaveTypeRepository.GetAllAsync();
        return leaveTypes.Select(ToDto);
    }

    public async Task<LeaveTypeDto?> GetByCodeAsync(string leaveTypeCode)
    {
        var leaveType = await _leaveTypeRepository.GetByCodeAsync(leaveTypeCode);
        return leaveType is null ? null : ToDto(leaveType);
    }

    public async Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeRequest request)
    {
        var existing = await _leaveTypeRepository.GetByCodeAsync(request.LeaveTypeCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Leave type code '{request.LeaveTypeCode}' already exists.");
        }

        if (!Enum.TryParse<LeaveUnit>(request.AccrualUnit, true, out var accrualUnit))
        {
            throw new InvalidOperationException($"Invalid accrual unit '{request.AccrualUnit}'.");
        }

        if (!Enum.TryParse<LeaveAccrualPeriod>(request.AccrualPeriod, true, out var accrualPeriod))
        {
            throw new InvalidOperationException($"Invalid accrual period '{request.AccrualPeriod}'.");
        }

        var leaveType = new LeaveType
        {
            LeaveTypeCode = request.LeaveTypeCode,
            LeaveTypeName = request.LeaveTypeName,
            AccrualAmount = request.AccrualAmount,
            AccrualUnit = accrualUnit,
            AccrualPeriod = accrualPeriod,
            IsPaid = request.IsPaid,
            Description = request.Description,
            IsActive = true
        };

        await _leaveTypeRepository.AddAsync(leaveType);
        await _leaveTypeRepository.SaveChangesAsync();

        return ToDto(leaveType);
    }

    private static LeaveTypeDto ToDto(LeaveType leaveType) => new()
    {
        LeaveTypeCode = leaveType.LeaveTypeCode,
        LeaveTypeName = leaveType.LeaveTypeName,
        AccrualAmount = leaveType.AccrualAmount,
        AccrualUnit = leaveType.AccrualUnit.ToString(),
        AccrualPeriod = leaveType.AccrualPeriod.ToString(),
        IsPaid = leaveType.IsPaid,
        Description = leaveType.Description,
        IsActive = leaveType.IsActive
    };
}

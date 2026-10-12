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
            throw new InvalidOperationException($"Mã loại nghỉ '{request.LeaveTypeCode}' đã tồn tại.");
        }

        if (!Enum.TryParse<LeaveUnit>(request.AccrualUnit, true, out var accrualUnit))
        {
            throw new InvalidOperationException($"Đơn vị tích luỹ '{request.AccrualUnit}' không hợp lệ.");
        }

        if (!Enum.TryParse<LeaveAccrualPeriod>(request.AccrualPeriod, true, out var accrualPeriod))
        {
            throw new InvalidOperationException($"Chu kỳ tích luỹ '{request.AccrualPeriod}' không hợp lệ.");
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

    public async Task<LeaveTypeDto> UpdateAsync(string leaveTypeCode, UpdateLeaveTypeRequest request)
    {
        var leaveType = await GetOrThrowAsync(leaveTypeCode);

        if (string.IsNullOrWhiteSpace(request.LeaveTypeName))
        {
            throw new InvalidOperationException("Tên loại nghỉ không được để trống.");
        }

        if (request.AccrualAmount <= 0)
        {
            throw new InvalidOperationException("Số lượng được cấp phải lớn hơn 0.");
        }

        if (!Enum.TryParse<LeaveUnit>(request.AccrualUnit, true, out var accrualUnit))
        {
            throw new InvalidOperationException($"Đơn vị tích luỹ '{request.AccrualUnit}' không hợp lệ.");
        }

        if (!Enum.TryParse<LeaveAccrualPeriod>(request.AccrualPeriod, true, out var accrualPeriod))
        {
            throw new InvalidOperationException($"Chu kỳ tích luỹ '{request.AccrualPeriod}' không hợp lệ.");
        }

        // Đổi đơn vị khi đã có đơn/số dư sẽ làm lệch số liệu cũ (ngày thành giờ...), nên chỉ cho đổi khi chưa dùng.
        if (accrualUnit != leaveType.AccrualUnit && await _leaveTypeRepository.IsInUseAsync(leaveType.Id))
        {
            throw new InvalidOperationException("Loại nghỉ này đã có đơn hoặc số dư nên không đổi được đơn vị (ngày/giờ).");
        }

        leaveType.LeaveTypeName = request.LeaveTypeName.Trim();
        leaveType.AccrualAmount = request.AccrualAmount;
        leaveType.AccrualUnit = accrualUnit;
        leaveType.AccrualPeriod = accrualPeriod;
        leaveType.IsPaid = request.IsPaid;
        leaveType.Description = request.Description;

        await _leaveTypeRepository.SaveChangesAsync();
        return ToDto(leaveType);
    }

    public async Task<LeaveTypeDto> SetActiveAsync(string leaveTypeCode, bool isActive)
    {
        var leaveType = await GetOrThrowAsync(leaveTypeCode);
        leaveType.IsActive = isActive;
        await _leaveTypeRepository.SaveChangesAsync();
        return ToDto(leaveType);
    }

    public async Task DeleteAsync(string leaveTypeCode)
    {
        var leaveType = await GetOrThrowAsync(leaveTypeCode);

        if (await _leaveTypeRepository.IsInUseAsync(leaveType.Id))
        {
            throw new InvalidOperationException("Loại nghỉ này đã có đơn nghỉ hoặc số dư liên quan nên không xoá được. Hãy tắt (ngừng sử dụng) thay vì xoá.");
        }

        _leaveTypeRepository.Remove(leaveType);
        await _leaveTypeRepository.SaveChangesAsync();
    }

    private async Task<LeaveType> GetOrThrowAsync(string leaveTypeCode) =>
        await _leaveTypeRepository.GetByCodeAsync(leaveTypeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy loại nghỉ có mã '{leaveTypeCode}'.");

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

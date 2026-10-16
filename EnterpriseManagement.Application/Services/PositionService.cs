using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _positionRepository;
    private readonly IPositionSalaryRepository _positionSalaryRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public PositionService(
        IPositionRepository positionRepository,
        IPositionSalaryRepository positionSalaryRepository,
        IRoleRepository roleRepository,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _positionRepository = positionRepository;
        _positionSalaryRepository = positionSalaryRepository;
        _roleRepository = roleRepository;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "Position", entityId, _currentUserService.IpAddress);

    public async Task<IEnumerable<PositionDto>> GetAllAsync()
    {
        var positions = await _positionRepository.GetAllAsync();
        return positions.Select(ToDto);
    }

    public async Task<PositionDto?> GetByCodeAsync(string positionCode)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode);
        return position is null ? null : ToDto(position);
    }

    public async Task<PositionDto> CreateAsync(CreatePositionRequest request)
    {
        var existing = await _positionRepository.GetByCodeAsync(request.PositionCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Mã chức vụ '{request.PositionCode}' đã tồn tại.");
        }

        var position = new Position
        {
            PositionCode = request.PositionCode,
            PositionName = request.PositionName,
            Description = request.Description,
            RoleCode = await ResolveRoleCodeAsync(request.RoleCode),
            IsActive = true,
            CreatedAt = VietnamClock.Now
        };

        await _positionRepository.AddAsync(position);
        await _positionRepository.SaveChangesAsync();
        await LogAsync("Create", position.Id);

        return ToDto(position);
    }

    public async Task<PositionDto> SetStandardSalaryAsync(string positionCode, decimal standardSalary)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Không tìm thấy chức vụ có mã '{positionCode}'.");

        var salary = await _positionSalaryRepository.GetByPositionIdAsync(position.Id);
        if (salary is null)
        {
            salary = new PositionSalary
            {
                PositionId = position.Id,
                StandardSalary = standardSalary,
                CreatedAt = VietnamClock.Now
            };
            await _positionSalaryRepository.AddAsync(salary);
        }
        else
        {
            salary.StandardSalary = standardSalary;
            salary.UpdatedAt = VietnamClock.Now;
        }

        await _positionSalaryRepository.SaveChangesAsync();

        var updated = await _positionRepository.GetByCodeAsync(positionCode);
        return ToDto(updated!);
    }

    public async Task<PositionDto> UpdateAsync(string positionCode, UpdatePositionRequest request)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Không tìm thấy chức vụ có mã '{positionCode}'.");

        position.PositionName = request.PositionName;
        position.Description = request.Description;
        if (request.RoleCode is not null)
        {
            position.RoleCode = await ResolveRoleCodeAsync(request.RoleCode);
        }
        position.UpdatedAt = VietnamClock.Now;

        await _positionRepository.SaveChangesAsync();
        await LogAsync("Update", position.Id);

        var updated = await _positionRepository.GetByCodeAsync(positionCode);
        return ToDto(updated!);
    }

    public async Task<PositionDto> SetActiveAsync(string positionCode, bool isActive)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Không tìm thấy chức vụ có mã '{positionCode}'.");

        position.IsActive = isActive;
        position.UpdatedAt = VietnamClock.Now;

        await _positionRepository.SaveChangesAsync();
        await LogAsync(isActive ? "Activate" : "Deactivate", position.Id);

        return ToDto(position);
    }

    // Rỗng = không có vai trò mặc định; có giá trị thì phải là mã vai trò đang tồn tại.
    private async Task<string?> ResolveRoleCodeAsync(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode)) return null;

        var role = await _roleRepository.GetByCodeAsync(roleCode.Trim())
            ?? throw new InvalidOperationException($"Không tìm thấy vai trò có mã '{roleCode}'.");
        return role.RoleCode;
    }

    private static PositionDto ToDto(Position position) => new()
    {
        PositionCode = position.PositionCode,
        PositionName = position.PositionName,
        Description = position.Description,
        RoleCode = position.RoleCode,
        IsActive = position.IsActive,
        StandardSalary = position.Salary?.StandardSalary
    };
}

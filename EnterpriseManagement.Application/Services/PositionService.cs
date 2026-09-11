using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _positionRepository;
    private readonly IPositionSalaryRepository _positionSalaryRepository;

    public PositionService(IPositionRepository positionRepository, IPositionSalaryRepository positionSalaryRepository)
    {
        _positionRepository = positionRepository;
        _positionSalaryRepository = positionSalaryRepository;
    }

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
            throw new InvalidOperationException($"Position code '{request.PositionCode}' already exists.");
        }

        var position = new Position
        {
            PositionCode = request.PositionCode,
            PositionName = request.PositionName,
            Description = request.Description,
            RankLevel = request.RankLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _positionRepository.AddAsync(position);
        await _positionRepository.SaveChangesAsync();

        return ToDto(position);
    }

    public async Task<PositionDto> SetStandardSalaryAsync(string positionCode, decimal standardSalary)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Position code '{positionCode}' not found.");

        var salary = await _positionSalaryRepository.GetByPositionIdAsync(position.Id);
        if (salary is null)
        {
            salary = new PositionSalary
            {
                PositionId = position.Id,
                StandardSalary = standardSalary,
                CreatedAt = DateTime.UtcNow
            };
            await _positionSalaryRepository.AddAsync(salary);
        }
        else
        {
            salary.StandardSalary = standardSalary;
            salary.UpdatedAt = DateTime.UtcNow;
        }

        await _positionSalaryRepository.SaveChangesAsync();

        var updated = await _positionRepository.GetByCodeAsync(positionCode);
        return ToDto(updated!);
    }

    public async Task<PositionDto> UpdateAsync(string positionCode, UpdatePositionRequest request)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Position code '{positionCode}' not found.");

        position.PositionName = request.PositionName;
        position.Description = request.Description;
        position.RankLevel = request.RankLevel;
        position.UpdatedAt = DateTime.UtcNow;

        await _positionRepository.SaveChangesAsync();

        var updated = await _positionRepository.GetByCodeAsync(positionCode);
        return ToDto(updated!);
    }

    public async Task<PositionDto> SetActiveAsync(string positionCode, bool isActive)
    {
        var position = await _positionRepository.GetByCodeAsync(positionCode)
            ?? throw new InvalidOperationException($"Position code '{positionCode}' not found.");

        position.IsActive = isActive;
        position.UpdatedAt = DateTime.UtcNow;

        await _positionRepository.SaveChangesAsync();

        return ToDto(position);
    }

    private static PositionDto ToDto(Position position) => new()
    {
        PositionCode = position.PositionCode,
        PositionName = position.PositionName,
        Description = position.Description,
        IsActive = position.IsActive,
        StandardSalary = position.Salary?.StandardSalary,
        RankLevel = position.RankLevel
    };
}

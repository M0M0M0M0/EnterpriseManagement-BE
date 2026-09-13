using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        return departments.Select(ToDto);
    }

    public async Task<DepartmentDto?> GetByCodeAsync(string departmentCode)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode);
        return department is null ? null : ToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request)
    {
        var existing = await _departmentRepository.GetByCodeAsync(request.DepartmentCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Department code '{request.DepartmentCode}' already exists.");
        }

        var department = new Department
        {
            DepartmentCode = request.DepartmentCode,
            DepartmentName = request.DepartmentName,
            ManagerId = request.ManagerId,
            Description = request.Description,
            IsActive = true,
            CreatedAt = VietnamClock.Now
        };

        await _departmentRepository.AddAsync(department);
        await _departmentRepository.SaveChangesAsync();

        var created = await _departmentRepository.GetByCodeAsync(department.DepartmentCode);
        return ToDto(created!);
    }

    public async Task<DepartmentDto> UpdateAsync(string departmentCode, UpdateDepartmentRequest request)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Department code '{departmentCode}' not found.");

        department.DepartmentName = request.DepartmentName;
        department.ManagerId = request.ManagerId;
        department.Description = request.Description;
        department.UpdatedAt = VietnamClock.Now;

        await _departmentRepository.SaveChangesAsync();

        var updated = await _departmentRepository.GetByCodeAsync(departmentCode);
        return ToDto(updated!);
    }

    public async Task<DepartmentDto> SetActiveAsync(string departmentCode, bool isActive)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Department code '{departmentCode}' not found.");

        department.IsActive = isActive;
        department.UpdatedAt = VietnamClock.Now;

        await _departmentRepository.SaveChangesAsync();

        return ToDto(department);
    }

    private static DepartmentDto ToDto(Department department) => new()
    {
        DepartmentCode = department.DepartmentCode,
        DepartmentName = department.DepartmentName,
        ManagerName = department.Manager is null ? null : $"{department.Manager.FirstName} {department.Manager.LastName}",
        Description = department.Description,
        IsActive = department.IsActive
    };
}

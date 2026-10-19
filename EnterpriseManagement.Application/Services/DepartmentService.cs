using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "Department", entityId, _currentUserService.IpAddress);

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
            throw new InvalidOperationException($"Mã phòng ban '{request.DepartmentCode}' đã tồn tại.");
        }

        if (request.ManagerId is not null)
        {
            var head = await _employeeRepository.GetByIdAsync(request.ManagerId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy nhân viên được chọn làm trưởng phòng.");
            EnsureCanBeHead(head);
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
        await LogAsync("Create", department.Id);

        var created = await _departmentRepository.GetByCodeAsync(department.DepartmentCode);
        return ToDto(created!);
    }

    public async Task<DepartmentDto> UpdateAsync(string departmentCode, UpdateDepartmentRequest request)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{departmentCode}'.");

        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            throw new InvalidOperationException("Tên phòng ban không được để trống.");
        }

        // Đổi mã phòng ban: nhân viên gắn với phòng ban theo id nên đổi mã không làm mất dữ liệu, chỉ cần không trùng mã khác.
        var newCode = request.DepartmentCode?.Trim();
        if (!string.IsNullOrEmpty(newCode) && newCode != department.DepartmentCode)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(newCode, "^[A-Za-z0-9_-]{2,20}$"))
            {
                throw new InvalidOperationException("Mã phòng ban gồm 2-20 ký tự chữ, số, gạch dưới hoặc gạch ngang (không dấu, không dấu cách).");
            }

            if (await _departmentRepository.GetByCodeAsync(newCode) is not null)
            {
                throw new InvalidOperationException($"Mã phòng ban '{newCode}' đã tồn tại.");
            }

            department.DepartmentCode = newCode;
        }

        if (request.ManagerCode is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ManagerCode))
            {
                department.ManagerId = null;
            }
            else
            {
                var manager = await _employeeRepository.GetByEmployeeCodeAsync(request.ManagerCode)
                    ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{request.ManagerCode}'.");
                EnsureCanBeHead(manager);
                department.ManagerId = manager.Id;
            }
        }

        department.DepartmentName = request.DepartmentName.Trim();
        department.Description = request.Description;
        department.UpdatedAt = VietnamClock.Now;

        await _departmentRepository.SaveChangesAsync();
        await LogAsync("Update", department.Id);

        var updated = await _departmentRepository.GetByCodeAsync(department.DepartmentCode);
        return ToDto(updated!);
    }

    public async Task<DepartmentDto> SetActiveAsync(string departmentCode, bool isActive)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{departmentCode}'.");

        department.IsActive = isActive;
        department.UpdatedAt = VietnamClock.Now;

        await _departmentRepository.SaveChangesAsync();
        await LogAsync(isActive ? "Activate" : "Deactivate", department.Id);

        return ToDto(department);
    }

    // Tài khoản hệ thống (không chấm công, vd admin) không được làm trưởng phòng.
    private static void EnsureCanBeHead(Employee employee)
    {
        if (SystemAccount.IsSystem(employee))
        {
            throw new InvalidOperationException("Tài khoản hệ thống không thể được chọn làm trưởng phòng.");
        }
    }

    private static DepartmentDto ToDto(Department department) => new()
    {
        DepartmentCode = department.DepartmentCode,
        DepartmentName = department.DepartmentName,
        ManagerCode = department.Manager?.EmployeeCode,
        ManagerName = department.Manager is null ? null : $"{department.Manager.FirstName} {department.Manager.LastName}",
        Description = department.Description,
        IsActive = department.IsActive
    };
}

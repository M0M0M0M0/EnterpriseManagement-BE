using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(ToDto);
    }

    public async Task<EmployeeDto?> GetByCodeAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
    {
        var department = await _departmentRepository.GetByCodeAsync(request.DepartmentCode)
            ?? throw new InvalidOperationException($"Department code '{request.DepartmentCode}' not found.");
        var position = await _positionRepository.GetByCodeAsync(request.PositionCode)
            ?? throw new InvalidOperationException($"Position code '{request.PositionCode}' not found.");
        var manager = await ResolveManagerAsync(request.ManagerCode);

        var employee = new Employee
        {
            EmployeeCode = await GenerateUniqueEmployeeCodeAsync(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            Gender = ParseGender(request.Gender),
            DepartmentId = department.Id,
            PositionId = position.Id,
            ManagerId = manager?.Id,
            HireDate = request.HireDate,
            EmploymentStatus = EmploymentStatus.Probation,
            CreatedAt = VietnamClock.Now
        };

        await _employeeRepository.AddAsync(employee);
        await _employeeRepository.SaveChangesAsync();

        var created = await _employeeRepository.GetByIdAsync(employee.Id);
        return ToDto(created!);
    }

    public async Task<IEnumerable<EmployeeDto>> GetTeamAsync(string managerEmployeeCode)
    {
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{managerEmployeeCode}' not found.");

        var team = await _employeeRepository.GetByManagerIdAsync(manager.Id);
        return team.Select(ToDto);
    }

    public async Task<EmployeeDto> UpdateAsync(string employeeCode, UpdateEmployeeRequest request)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");
        var department = await _departmentRepository.GetByCodeAsync(request.DepartmentCode)
            ?? throw new InvalidOperationException($"Department code '{request.DepartmentCode}' not found.");
        var position = await _positionRepository.GetByCodeAsync(request.PositionCode)
            ?? throw new InvalidOperationException($"Position code '{request.PositionCode}' not found.");
        var manager = await ResolveManagerAsync(request.ManagerCode);

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new InvalidOperationException("Vui lòng nhập đủ họ tên.");
        }

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Phone = request.Phone;
        employee.Address = request.Address;
        employee.DepartmentId = department.Id;
        employee.PositionId = position.Id;
        employee.ManagerId = manager?.Id;
        employee.UpdatedAt = VietnamClock.Now;

        await _employeeRepository.SaveChangesAsync();

        var updated = await _employeeRepository.GetByIdAsync(employee.Id);
        return ToDto(updated!);
    }

    private async Task<Employee?> ResolveManagerAsync(string? managerCode)
    {
        if (string.IsNullOrWhiteSpace(managerCode)) return null;
        return await _employeeRepository.GetByEmployeeCodeAsync(managerCode)
            ?? throw new InvalidOperationException($"Employee code '{managerCode}' not found.");
    }

    public async Task<EmployeeDto> SetActiveAsync(string employeeCode, bool isActive)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        employee.EmploymentStatus = isActive ? EmploymentStatus.Active : EmploymentStatus.Terminated;
        employee.UpdatedAt = VietnamClock.Now;

        await _employeeRepository.SaveChangesAsync();

        var updated = await _employeeRepository.GetByIdAsync(employee.Id);
        return ToDto(updated!);
    }

    private static Gender? ParseGender(string? gender) =>
        Enum.TryParse<Gender>(gender, ignoreCase: true, out var parsed) ? parsed : null;

    private async Task<string> GenerateUniqueEmployeeCodeAsync()
    {
        string code;
        do
        {
            code = RandomCodeGenerator.Generate(8);
        }
        while (await _employeeRepository.GetByEmployeeCodeAsync(code) is not null);

        return code;
    }

    private static EmployeeDto ToDto(Employee employee) => new()
    {
        EmployeeCode = employee.EmployeeCode,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        FullName = $"{employee.FirstName} {employee.LastName}",
        Email = employee.Email,
        Phone = employee.Phone,
        Address = employee.Address,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender?.ToString(),
        DepartmentCode = employee.Department.DepartmentCode,
        DepartmentName = employee.Department.DepartmentName,
        PositionCode = employee.Position.PositionCode,
        PositionName = employee.Position.PositionName,
        ManagerCode = employee.Manager?.EmployeeCode,
        EmploymentStatus = employee.EmploymentStatus.ToString(),
        HireDate = employee.HireDate
    };
}

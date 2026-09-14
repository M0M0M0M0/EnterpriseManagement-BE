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
    private readonly ISaleRepository _saleRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ISaleRepository saleRepository,
        IAttendanceRepository attendanceRepository,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _saleRepository = saleRepository;
        _attendanceRepository = attendanceRepository;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "Employee", entityId, _currentUserService.IpAddress);

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
        await LogAsync("Create", employee.Id);

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

    // Root là danh sách cấp dưới TRỰC TIẾP của người gọi (không bọc thêm 1 node "chính mình"),
    // mỗi node đệ quy xuống hết các cấp dưới của nó — khác GetTeamAsync (chỉ 1 cấp). isAdmin=true
    // thì lấy toàn bộ nhân viên có ManagerId null (CEO) làm root để Admin xem được cả công ty.
    public async Task<IEnumerable<OrgTreeNodeDto>> GetOrgTreeAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var allEmployees = (await _employeeRepository.GetAllAsync()).ToList();
        var childrenByManagerId = allEmployees
            .Where(e => e.ManagerId.HasValue)
            .GroupBy(e => e.ManagerId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<Employee> roots;
        if (isAdmin)
        {
            roots = allEmployees.Where(e => e.ManagerId is null).ToList();
        }
        else
        {
            var requester = allEmployees.FirstOrDefault(e => e.EmployeeCode == requesterEmployeeCode)
                ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");
            roots = childrenByManagerId.TryGetValue(requester.Id, out var direct) ? direct : new List<Employee>();
        }

        var today = VietnamClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nodes = new List<OrgTreeNodeDto>();
        foreach (var root in roots)
        {
            nodes.Add(await BuildOrgTreeNodeAsync(root, childrenByManagerId, today, monthStart));
        }

        return nodes;
    }

    private async Task<OrgTreeNodeDto> BuildOrgTreeNodeAsync(
        Employee employee, Dictionary<long, List<Employee>> childrenByManagerId, DateOnly today, DateOnly monthStart)
    {
        var sales = await _saleRepository.GetByEmployeeIdAsync(employee.Id);
        var monthlyRevenue = sales
            .Where(s => s.Status == SaleStatus.Confirmed && DateOnly.FromDateTime(s.OrderDate) >= monthStart)
            .Sum(s => s.Amount);

        var todayRecord = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today);

        var children = childrenByManagerId.TryGetValue(employee.Id, out var direct) ? direct : new List<Employee>();
        var subordinates = new List<OrgTreeNodeDto>();
        foreach (var child in children)
        {
            subordinates.Add(await BuildOrgTreeNodeAsync(child, childrenByManagerId, today, monthStart));
        }

        return new OrgTreeNodeDto
        {
            EmployeeCode = employee.EmployeeCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            PositionName = employee.Position.PositionName,
            DepartmentName = employee.Department.DepartmentName,
            EmploymentStatus = employee.EmploymentStatus.ToString(),
            TodayAttendanceStatus = todayRecord?.Status.ToString() ?? "ChuaChamCong",
            MonthlyRevenue = monthlyRevenue,
            SubordinateCount = children.Count,
            Subordinates = subordinates
        };
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
        await LogAsync("Update", employee.Id);

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
        await LogAsync(isActive ? "Activate" : "Deactivate", employee.Id);

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

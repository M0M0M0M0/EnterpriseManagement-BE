using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Hồ sơ nhân viên và tài khoản đăng nhập gắn 1-1: tạo cùng lúc, sửa cùng màn hình, nghỉ việc thì khoá tài khoản.
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserAccountRules _rules;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        IEffectiveAttendanceService effective,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _effective = effective;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _rules = new UserAccountRules(userRepository, roleRepository, currentUserService);
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "Employee", entityId, _currentUserService.IpAddress);

    private Task LogUserAsync(string action, long? userId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "User", userId, _currentUserService.IpAddress);

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

    // Tạo nhân viên = tạo hồ sơ + tài khoản đăng nhập trong cùng một lần lưu (không bao giờ có hồ sơ thiếu tài khoản).
    public async Task<CreateEmployeeResult> CreateAsync(CreateEmployeeRequest request)
    {
        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        if (firstName.Length == 0 || lastName.Length == 0)
        {
            throw new InvalidOperationException("Vui lòng nhập đủ họ tên.");
        }

        var username = UserAccountRules.NormalizeUsername(request.Username);
        var email = UserAccountRules.NormalizeEmail(request.Email);

        if (await _userRepository.GetByUsernameAsync(username) is not null)
        {
            throw new InvalidOperationException($"Tên đăng nhập '{username}' đã tồn tại.");
        }

        await EnsureEmailNotUsedAsync(email, exceptEmployeeId: null);

        var department = await _departmentRepository.GetByCodeAsync(request.DepartmentCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{request.DepartmentCode}'.");
        var position = await _positionRepository.GetByCodeAsync(request.PositionCode)
            ?? throw new InvalidOperationException($"Không tìm thấy chức vụ có mã '{request.PositionCode}'.");
        var manager = await ResolveManagerAsync(request.ManagerCode, self: null);

        // Vai trò: chọn tay > vai trò mặc định của chức vụ > EMPLOYEE.
        var roleCode = !string.IsNullOrWhiteSpace(request.RoleCode)
            ? request.RoleCode.Trim()
            : !string.IsNullOrWhiteSpace(position.RoleCode) ? position.RoleCode : "EMPLOYEE";
        var role = await _roleRepository.GetByCodeAsync(roleCode)
            ?? throw new InvalidOperationException($"Không tìm thấy vai trò có mã '{roleCode}'.");

        var status = EmploymentStatus.Probation;
        if (!string.IsNullOrWhiteSpace(request.EmploymentStatus)
            && !Enum.TryParse(request.EmploymentStatus, ignoreCase: true, out status))
        {
            throw new InvalidOperationException($"Trạng thái làm việc '{request.EmploymentStatus}' không hợp lệ.");
        }

        var now = VietnamClock.Now;
        var generatedPassword = RandomCodeGenerator.Generate(10);

        var employee = new Employee
        {
            EmployeeCode = await GenerateUniqueEmployeeCodeAsync(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = request.Phone,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            Gender = ParseGender(request.Gender),
            DepartmentId = department.Id,
            PositionId = position.Id,
            ManagerId = manager?.Id,
            HireDate = request.HireDate == default ? VietnamClock.Today : request.HireDate,
            EmploymentStatus = status,
            TracksAttendance = true,
            CreatedAt = now
        };

        // Tạo ở trạng thái đã nghỉ việc thì tài khoản cũng khoá luôn cho khớp quy tắc "nghỉ việc = khoá tài khoản".
        var user = new User
        {
            Employee = employee,
            Username = username,
            PasswordHash = _passwordHasher.Hash(generatedPassword),
            IsActive = status is not (EmploymentStatus.Resigned or EmploymentStatus.Terminated),
            CreatedAt = now
        };
        user.UserRoles.Add(new UserRole { User = user, Role = role, RoleId = role.Id, AssignedAt = now });
        employee.User = user;

        await _employeeRepository.AddAsync(employee);
        await _employeeRepository.SaveChangesAsync();
        await LogAsync("Create", employee.Id);
        await LogUserAsync("Create", user.Id);

        var created = await _employeeRepository.GetByIdAsync(employee.Id);
        return new CreateEmployeeResult
        {
            Employee = ToDto(created!),
            GeneratedPassword = generatedPassword
        };
    }

    public async Task<IEnumerable<EmployeeDto>> GetTeamAsync(string managerEmployeeCode)
    {
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerEmployeeCode}'.");

        var team = await _employeeRepository.GetByManagerIdAsync(manager.Id);
        return team.Select(ToDto);
    }

    // Khác GetTeamAsync (chỉ cấp dưới trực tiếp) — trả về TOÀN BỘ cấp dưới nhiều tầng dưới
    // quyền quản lý của người gọi, dạng danh sách phẳng (không phải cây), để trang "Nhân viên
    // của tôi" hiện được cả người do nhân viên dưới mình quản lý, không chỉ báo cáo trực tiếp.
    public async Task<IEnumerable<EmployeeDto>> GetTeamRecursiveAsync(string managerEmployeeCode)
    {
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerEmployeeCode}'.");

        var allEmployees = (await _employeeRepository.GetAllAsync()).ToList();
        var childrenByManagerId = allEmployees
            .Where(e => e.ManagerId.HasValue)
            .GroupBy(e => e.ManagerId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // CollectSubordinates đi theo thứ tự cây (quản lý trực tiếp trước, rồi mới tới cấp dưới
        // của người đó) nên danh sách trả về đã đúng thứ tự trên xuống dưới, không cần sắp lại
        // theo cấp bậc chức vụ.
        var result = new List<Employee>();
        CollectSubordinates(manager.Id, childrenByManagerId, result);

        return result.Select(ToDto);
    }

    private static void CollectSubordinates(long managerId, Dictionary<long, List<Employee>> childrenByManagerId, List<Employee> result)
    {
        if (!childrenByManagerId.TryGetValue(managerId, out var direct)) return;

        foreach (var child in direct)
        {
            result.Add(child);
            CollectSubordinates(child.Id, childrenByManagerId, result);
        }
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
                ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{requesterEmployeeCode}'.");
            roots = childrenByManagerId.TryGetValue(requester.Id, out var direct) ? direct : new List<Employee>();
        }

        var today = VietnamClock.Today;
        var nodes = new List<OrgTreeNodeDto>();
        foreach (var root in roots)
        {
            nodes.Add(await BuildOrgTreeNodeAsync(root, childrenByManagerId, today));
        }

        return nodes;
    }

    private async Task<OrgTreeNodeDto> BuildOrgTreeNodeAsync(
        Employee employee, Dictionary<long, List<Employee>> childrenByManagerId, DateOnly today)
    {
        var todayDay = await _effective.GetDayAsync(employee.Id, today);

        var children = childrenByManagerId.TryGetValue(employee.Id, out var direct) ? direct : new List<Employee>();
        var subordinates = new List<OrgTreeNodeDto>();
        foreach (var child in children)
        {
            subordinates.Add(await BuildOrgTreeNodeAsync(child, childrenByManagerId, today));
        }

        return new OrgTreeNodeDto
        {
            EmployeeCode = employee.EmployeeCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            PositionName = employee.Position.PositionName,
            DepartmentName = employee.Department.DepartmentName,
            EmploymentStatus = employee.EmploymentStatus.ToString(),
            TodayAttendanceStatus = todayDay.OverallStatus == "None" ? "ChuaChamCong" : todayDay.OverallStatus,
            SubordinateCount = children.Count,
            Subordinates = subordinates
        };
    }

    public async Task<EmployeeDto> UpdateAsync(string employeeCode, UpdateEmployeeRequest request)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");
        var department = await _departmentRepository.GetByCodeAsync(request.DepartmentCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{request.DepartmentCode}'.");
        var position = await _positionRepository.GetByCodeAsync(request.PositionCode)
            ?? throw new InvalidOperationException($"Không tìm thấy chức vụ có mã '{request.PositionCode}'.");
        var manager = await ResolveManagerAsync(request.ManagerCode, employee);

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new InvalidOperationException("Vui lòng nhập đủ họ tên.");
        }

        string? newEmail = null;
        if (request.Email is not null)
        {
            newEmail = UserAccountRules.NormalizeEmail(request.Email);
            await EnsureEmailNotUsedAsync(newEmail, exceptEmployeeId: employee.Id);
        }

        var user = employee.User
            ?? throw new InvalidOperationException($"Nhân viên '{employeeCode}' chưa có tài khoản đăng nhập.");

        var rolesChanged = false;
        if (request.RoleCodes is not null)
        {
            var before = user.UserRoles.Select(ur => ur.Role.RoleCode).OrderBy(c => c).ToList();
            await _rules.ApplyRolesAsync(user, request.RoleCodes);
            rolesChanged = !before.SequenceEqual(user.UserRoles.Select(ur => ur.Role.RoleCode).OrderBy(c => c));
        }

        var now = VietnamClock.Now;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone;
        employee.Address = request.Address;
        if (newEmail is not null) employee.Email = newEmail;
        employee.DepartmentId = department.Id;
        employee.PositionId = position.Id;
        employee.ManagerId = manager?.Id;
        employee.UpdatedAt = now;
        if (rolesChanged) user.UpdatedAt = now;

        await _employeeRepository.SaveChangesAsync();
        await LogAsync("Update", employee.Id);
        if (rolesChanged) await LogUserAsync("UpdateRoles", user.Id);

        var updated = await _employeeRepository.GetByIdAsync(employee.Id);
        return ToDto(updated!);
    }

    private async Task EnsureEmailNotUsedAsync(string email, long? exceptEmployeeId)
    {
        var owner = await _employeeRepository.GetByEmailAsync(email);
        if (owner is not null && owner.Id != exceptEmployeeId)
        {
            throw new InvalidOperationException($"Email '{email}' đã được dùng cho nhân viên khác ({owner.EmployeeCode}).");
        }
    }

    // Tài khoản hệ thống (không chấm công, vd admin) không được làm quản lý trực tiếp của ai; không ai tự quản lý chính mình.
    private async Task<Employee?> ResolveManagerAsync(string? managerCode, Employee? self)
    {
        if (string.IsNullOrWhiteSpace(managerCode)) return null;
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerCode}'.");

        if (!manager.TracksAttendance)
        {
            throw new InvalidOperationException("Tài khoản hệ thống không thể được chọn làm quản lý.");
        }

        if (self is not null && manager.Id == self.Id)
        {
            throw new InvalidOperationException("Nhân viên không thể tự làm quản lý của chính mình.");
        }

        return manager;
    }

    // Cho nghỉ việc = EmploymentStatus Resigned + khoá tài khoản; Phục hồi = Active + mở tài khoản.
    public async Task<EmployeeDto> SetActiveAsync(string employeeCode, bool isActive)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");
        var user = employee.User
            ?? throw new InvalidOperationException($"Nhân viên '{employeeCode}' chưa có tài khoản đăng nhập.");

        if (!isActive)
        {
            if (_rules.IsCurrentUser(user))
            {
                throw new InvalidOperationException("Bạn không thể tự cho mình nghỉ việc.");
            }

            if (user.IsActive)
            {
                await _rules.EnsureNotLockingOutAsync(user, removingAdminRole: false);
            }
        }

        var accountChanged = user.IsActive != isActive;
        var now = VietnamClock.Now;

        employee.EmploymentStatus = isActive ? EmploymentStatus.Active : EmploymentStatus.Resigned;
        employee.UpdatedAt = now;
        user.IsActive = isActive;
        if (accountChanged) user.UpdatedAt = now;

        await _employeeRepository.SaveChangesAsync();
        await LogAsync(isActive ? "Activate" : "Deactivate", employee.Id);
        if (accountChanged) await LogUserAsync(isActive ? "Unlock" : "Lock", user.Id);

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

    public static EmployeeDto ToDto(Employee employee) => new()
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
        ManagerName = employee.Manager is null ? null : $"{employee.Manager.FirstName} {employee.Manager.LastName}",
        EmploymentStatus = employee.EmploymentStatus.ToString(),
        HireDate = employee.HireDate,
        TracksAttendance = employee.TracksAttendance,
        Username = employee.User?.Username ?? string.Empty,
        RoleCodes = employee.User?.UserRoles.Select(ur => ur.Role.RoleCode).OrderBy(c => c).ToList() ?? new List<string>(),
        RoleNames = employee.User?.UserRoles.OrderBy(ur => ur.Role.RoleCode).Select(ur => ur.Role.RoleName).ToList() ?? new List<string>(),
        IsAccountActive = employee.User?.IsActive ?? false,
        LastLoginAt = employee.User?.LastLoginAt
    };
}

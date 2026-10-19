using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Application.Services;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Enums;
using Xunit;

namespace EnterpriseManagement.Tests;

// Kiểm thử gộp Hồ sơ nhân viên + Tài khoản ở tầng service (repository giả lập trong bộ nhớ).
public class EmployeeAccountTests
{
    // ---------------- Tạo nhân viên kèm tài khoản ----------------

    [Fact]
    public async Task Create_CreatesProfileAndAccount_WithRoleFromPosition()
    {
        var f = new Fixture();
        var result = await f.Service.CreateAsync(f.NewRequest("nguyenvana", "a@congty.vn", position: "HEAD"));

        Assert.Equal("nguyenvana", result.Employee.Username);
        Assert.Equal("a@congty.vn", result.Employee.Email);
        Assert.Equal(new[] { "MANAGER" }, result.Employee.RoleCodes);
        Assert.True(result.Employee.IsAccountActive);
        Assert.Equal(10, result.GeneratedPassword.Length);

        var user = Assert.Single(f.Store.Users, u => u.Username == "nguyenvana");
        Assert.Equal("hash:" + result.GeneratedPassword, user.PasswordHash);
        Assert.Same(user.Employee, f.Store.Employees.Single(e => e.EmployeeCode == result.Employee.EmployeeCode));
        Assert.Contains(f.Audit.Entries, e => e.Module == "Employee" && e.Action == "Create");
        Assert.Contains(f.Audit.Entries, e => e.Module == "User" && e.Action == "Create");
    }

    [Fact]
    public async Task Create_PositionWithoutRole_DefaultsToEmployee_AndExplicitRoleWins()
    {
        var f = new Fixture();
        var plain = await f.Service.CreateAsync(f.NewRequest("plain.user", "p@congty.vn", position: "NOROLE"));
        Assert.Equal(new[] { "EMPLOYEE" }, plain.Employee.RoleCodes);

        var chosen = await f.Service.CreateAsync(f.NewRequest("chosen_user", "c@congty.vn", position: "STAFF", role: "MANAGER"));
        Assert.Equal(new[] { "MANAGER" }, chosen.Employee.RoleCodes);
    }

    [Theory]
    [InlineData("ab", "x@congty.vn", "Tên đăng nhập")]
    [InlineData("có dấu", "x@congty.vn", "Tên đăng nhập")]
    [InlineData("valid.name", "khong-phai-email", "Email không hợp lệ")]
    [InlineData("valid.name", "a@localhost", "Email không hợp lệ")]
    public async Task Create_InvalidUsernameOrEmail_Throws(string username, string email, string expected)
    {
        var f = new Fixture();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateAsync(f.NewRequest(username, email)));
        Assert.Contains(expected, ex.Message);
        Assert.Empty(f.Store.Employees.Where(e => e.EmployeeCode != "E1" && e.EmployeeCode != "ADMIN"));
    }

    [Fact]
    public async Task Create_DuplicateUsernameOrEmail_Throws()
    {
        var f = new Fixture();
        var dupUser = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateAsync(f.NewRequest("USER.E1", "new@congty.vn")));
        Assert.Contains("đã tồn tại", dupUser.Message);

        var dupEmail = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateAsync(f.NewRequest("brand.new", "E1@CONGTY.VN")));
        Assert.Contains("đã được dùng", dupEmail.Message);
    }

    [Fact]
    public async Task Create_SystemAccountAsManager_Throws()
    {
        var f = new Fixture();
        var request = f.NewRequest("someone", "s@congty.vn");
        request.ManagerCode = "ADMIN";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CreateAsync(request));
        Assert.Contains("Tài khoản hệ thống", ex.Message);
    }

    // ---------------- Sửa email / vai trò ----------------

    [Fact]
    public async Task Update_ChangesEmailAndRoles()
    {
        var f = new Fixture();
        var dto = await f.Service.UpdateAsync("E1", f.UpdateRequest(email: "moi@congty.vn", roles: new() { "MANAGER", "EMPLOYEE" }));

        Assert.Equal("moi@congty.vn", dto.Email);
        Assert.Equal(new[] { "EMPLOYEE", "MANAGER" }, dto.RoleCodes);
    }

    [Fact]
    public async Task Update_EmptyRolesOrDuplicateEmail_Throws()
    {
        var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.UpdateAsync("E1", f.UpdateRequest(roles: new())));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.UpdateAsync("E1", f.UpdateRequest(email: "admin@enterprise.local")));
        Assert.Contains("đã được dùng", ex.Message);
    }

    [Fact]
    public async Task Update_CannotRemoveOwnAdminRole()
    {
        var f = new Fixture(currentUsername: "admin");
        var request = f.UpdateRequest(roles: new() { "EMPLOYEE" });
        request.DepartmentCode = "SYS";
        request.PositionCode = "SYSADMIN";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.UpdateAsync("ADMIN", request));
        Assert.Contains("ADMIN của chính mình", ex.Message);
    }

    // ---------------- Nghỉ việc / phục hồi ----------------

    [Fact]
    public async Task Deactivate_ResignsAndLocksAccount_ReactivateRestoresBoth()
    {
        var f = new Fixture();
        var off = await f.Service.SetActiveAsync("E1", false);
        Assert.Equal("Resigned", off.EmploymentStatus);
        Assert.False(off.IsAccountActive);
        Assert.False(f.Store.Users.Single(u => u.Username == "user.e1").IsActive);

        var on = await f.Service.SetActiveAsync("E1", true);
        Assert.Equal("Active", on.EmploymentStatus);
        Assert.True(on.IsAccountActive);
        Assert.Contains(f.Audit.Entries, e => e.Module == "User" && e.Action == "Lock");
        Assert.Contains(f.Audit.Entries, e => e.Module == "User" && e.Action == "Unlock");
    }

    [Fact]
    public async Task Deactivate_Self_Throws()
    {
        var f = new Fixture(currentUsername: "user.e1");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.SetActiveAsync("E1", false));
        Assert.Contains("tự cho mình nghỉ việc", ex.Message);
        Assert.True(f.Store.Users.Single(u => u.Username == "user.e1").IsActive);
    }

    [Fact]
    public async Task LockAccount_Self_Throws_ButOtherAccountOk()
    {
        var f = new Fixture(currentUsername: "admin");
        var users = new UserService(f.UserRepo, f.RoleRepo, new FakeHasher(), null!, f.Audit, f.CurrentUser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => users.SetActiveAsync("admin", false));
        Assert.Contains("chính mình", ex.Message);

        var locked = await users.SetActiveAsync("user.e1", false);
        Assert.False(locked.IsActive);
        // Khoá tài khoản tạm không đổi trạng thái làm việc.
        Assert.Equal(EmploymentStatus.Active, f.Store.Employees.Single(e => e.EmployeeCode == "E1").EmploymentStatus);
    }

    // ---------------- Hạ tầng giả lập ----------------

    private sealed class Store
    {
        public List<Role> Roles { get; } = new();
        public List<Department> Departments { get; } = new();
        public List<Position> Positions { get; } = new();
        public List<Employee> Employees { get; } = new();
        public List<User> Users { get; } = new();
        private long _nextId = 100;
        public long NextId() => _nextId++;

        // Mô phỏng EF: gán Id cho bản ghi mới và nối lại navigation theo khoá ngoại.
        public void Sync()
        {
            foreach (var e in Employees)
            {
                if (e.Id == 0) e.Id = NextId();
                e.Department = Departments.Single(d => d.Id == e.DepartmentId);
                e.Position = Positions.Single(p => p.Id == e.PositionId);
                e.Manager = e.ManagerId is null ? null : Employees.Single(m => m.Id == e.ManagerId);
                if (e.User is not null && !Users.Contains(e.User)) Users.Add(e.User);
            }

            foreach (var u in Users)
            {
                if (u.Id == 0) u.Id = NextId();
                u.EmployeeId = u.Employee.Id;
                foreach (var ur in u.UserRoles) { ur.UserId = u.Id; ur.RoleId = ur.Role.Id; }
            }
        }
    }

    private sealed class Fixture
    {
        public Store Store { get; } = new();
        public FakeUserRepo UserRepo { get; }
        public FakeRoleRepo RoleRepo { get; }
        public FakeAudit Audit { get; } = new();
        public FakeCurrentUser CurrentUser { get; }
        public EmployeeService Service { get; }

        public Fixture(string currentUsername = "admin")
        {
            var admin = new Role { Id = 1, RoleCode = "ADMIN", RoleName = "Quản trị viên", IsActive = true };
            var manager = new Role { Id = 2, RoleCode = "MANAGER", RoleName = "Quản lý", IsActive = true };
            var employee = new Role { Id = 3, RoleCode = "EMPLOYEE", RoleName = "Nhân viên", IsActive = true };
            Store.Roles.AddRange(new[] { admin, manager, employee });

            Store.Departments.Add(new Department { Id = 1, DepartmentCode = "SYS", DepartmentName = "Quản trị hệ thống" });
            Store.Departments.Add(new Department { Id = 2, DepartmentCode = "IT", DepartmentName = "Kỹ thuật" });
            Store.Positions.Add(new Position { Id = 1, PositionCode = "SYSADMIN", PositionName = "Quản trị viên", RoleCode = "ADMIN" });
            Store.Positions.Add(new Position { Id = 2, PositionCode = "HEAD", PositionName = "Trưởng phòng", RoleCode = "MANAGER" });
            Store.Positions.Add(new Position { Id = 3, PositionCode = "STAFF", PositionName = "Nhân viên", RoleCode = "EMPLOYEE" });
            Store.Positions.Add(new Position { Id = 4, PositionCode = "NOROLE", PositionName = "Chức vụ không vai trò" });

            AddPerson(1, "ADMIN", "admin", "admin@enterprise.local", 1, 1, admin, tracks: false);
            AddPerson(2, "E1", "user.e1", "e1@congty.vn", 2, 2, manager, tracks: true);
            Store.Sync();

            CurrentUser = new FakeCurrentUser(currentUsername);
            UserRepo = new FakeUserRepo(Store);
            RoleRepo = new FakeRoleRepo(Store);
            Service = new EmployeeService(
                new FakeEmployeeRepo(Store), new FakeDepartmentRepo(Store), new FakePositionRepo(Store), null!,
                UserRepo, RoleRepo, new FakeHasher(), Audit, CurrentUser);
        }

        private void AddPerson(long id, string code, string username, string email, long deptId, long posId, Role role, bool tracks)
        {
            var e = new Employee
            {
                Id = id, EmployeeCode = code, FirstName = "Họ", LastName = code, Email = email,
                DepartmentId = deptId, PositionId = posId, HireDate = new DateOnly(2026, 1, 1),
                EmploymentStatus = EmploymentStatus.Active, TracksAttendance = tracks
            };
            var u = new User { Id = id, Username = username, Employee = e, EmployeeId = id, IsActive = true, PasswordHash = "hash:x" };
            u.UserRoles.Add(new UserRole { User = u, Role = role, RoleId = role.Id, UserId = id });
            e.User = u;
            Store.Employees.Add(e);
            Store.Users.Add(u);
        }

        public CreateEmployeeRequest NewRequest(string username, string email, string position = "STAFF", string? role = null) => new()
        {
            FirstName = "Nguyễn Văn", LastName = "A", DepartmentCode = "IT", PositionCode = position,
            HireDate = new DateOnly(2026, 10, 1), Username = username, Email = email, RoleCode = role
        };

        public UpdateEmployeeRequest UpdateRequest(string? email = null, List<string>? roles = null) => new()
        {
            FirstName = "Họ", LastName = "E1", DepartmentCode = "IT", PositionCode = "HEAD", Email = email, RoleCodes = roles
        };
    }

    private sealed class FakeEmployeeRepo(Store s) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(long id) => Task.FromResult(s.Employees.FirstOrDefault(e => e.Id == id));
        public Task<IEnumerable<Employee>> GetAllAsync() => Task.FromResult<IEnumerable<Employee>>(s.Employees.ToList());
        public Task<Employee?> GetByEmployeeCodeAsync(string code) => Task.FromResult(s.Employees.FirstOrDefault(e => e.EmployeeCode == code));
        public Task<Employee?> GetByEmailAsync(string email) =>
            Task.FromResult(s.Employees.FirstOrDefault(e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<IEnumerable<Employee>> GetByManagerIdAsync(long managerId) =>
            Task.FromResult<IEnumerable<Employee>>(s.Employees.Where(e => e.ManagerId == managerId).ToList());
        public Task AddAsync(Employee employee) { s.Employees.Add(employee); return Task.CompletedTask; }
        public void Update(Employee employee) { }
        public void Delete(Employee employee) => s.Employees.Remove(employee);
        public Task<int> SaveChangesAsync() { s.Sync(); return Task.FromResult(1); }
    }

    private sealed class FakeUserRepo(Store s) : IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username) =>
            Task.FromResult(s.Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
        public Task<User?> GetByEmployeeCodeAsync(string employeeCode) =>
            Task.FromResult(s.Users.FirstOrDefault(u => u.Employee != null && u.Employee.EmployeeCode == employeeCode));
        public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult<IEnumerable<User>>(s.Users.ToList());
        public Task AddAsync(User user) { s.Users.Add(user); return Task.CompletedTask; }
        public void RemoveUserRole(UserRole userRole) { }
        public Task<int> SaveChangesAsync() { s.Sync(); return Task.FromResult(1); }
    }

    private sealed class FakeRoleRepo(Store s) : IRoleRepository
    {
        public Task<Role?> GetByCodeAsync(string roleCode) => Task.FromResult(s.Roles.FirstOrDefault(r => r.RoleCode == roleCode));
        public Task<IEnumerable<Role>> GetAllAsync() => Task.FromResult<IEnumerable<Role>>(s.Roles);
        public Task AddAsync(Role role) { s.Roles.Add(role); return Task.CompletedTask; }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
    }

    private sealed class FakeDepartmentRepo(Store s) : IDepartmentRepository
    {
        public Task<Department?> GetByCodeAsync(string code) => Task.FromResult(s.Departments.FirstOrDefault(d => d.DepartmentCode == code));
        public Task<IEnumerable<Department>> GetAllAsync() => Task.FromResult<IEnumerable<Department>>(s.Departments);
        public Task AddAsync(Department department) { s.Departments.Add(department); return Task.CompletedTask; }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
    }

    private sealed class FakePositionRepo(Store s) : IPositionRepository
    {
        public Task<Position?> GetByCodeAsync(string code) => Task.FromResult(s.Positions.FirstOrDefault(p => p.PositionCode == code));
        public Task<IEnumerable<Position>> GetAllAsync() => Task.FromResult<IEnumerable<Position>>(s.Positions);
        public Task AddAsync(Position position) { s.Positions.Add(position); return Task.CompletedTask; }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hashedPassword) => hashedPassword == "hash:" + password;
    }

    private sealed class FakeAudit : IAuditLogService
    {
        public List<(string Action, string Module)> Entries { get; } = new();

        public Task LogAsync(long? userId, string action, string module, long? entityId = null, string? ipAddress = null)
        {
            Entries.Add((action, module));
            return Task.CompletedTask;
        }

        public Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync() => throw new NotSupportedException();
        public Task<IEnumerable<AuditLogDto>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCurrentUser(string username) : ICurrentUserService
    {
        public long? UserId => 1;
        public string EmployeeCode => string.Empty;
        public string? Username => username;
        public string? IpAddress => null;
        public bool IsInRole(string role) => false;
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
    }
}

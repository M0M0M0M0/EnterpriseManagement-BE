using System.Reflection;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.Auditing;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Entities.Payroll;
using EnterpriseManagement.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();

    // HR
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PositionSalary> PositionSalaries => Set<PositionSalary>();

    // Attendance
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceAdjustment> AttendanceAdjustments => Set<AttendanceAdjustment>();

    // Leave
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    // Sales
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<KpiPlan> KpiPlans => Set<KpiPlan>();
    public DbSet<KpiLevel> KpiLevels => Set<KpiLevel>();
    public DbSet<SalesCommission> SalesCommissions => Set<SalesCommission>();

    // Payroll
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();

    // Auditing
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

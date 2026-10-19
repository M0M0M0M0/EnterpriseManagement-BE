using System.Text;
using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Api.Services;
using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Application.Services;
using EnterpriseManagement.Infrastructure.Persistence;
using EnterpriseManagement.Infrastructure.Repositories;
using EnterpriseManagement.Infrastructure.Security;
using EnterpriseManagement.Infrastructure.Storage;
using EnterpriseManagement.Infrastructure.Push;
using EnterpriseManagement.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IMenuService, MenuService>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IApprovalDelegationResolver, ApprovalDelegationResolver>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IPositionSalaryRepository, PositionSalaryRepository>();
var attendanceLocation = builder.Configuration.GetSection(AttendanceLocationOptions.SectionName)
    .Get<AttendanceLocationOptions>() ?? new AttendanceLocationOptions();
builder.Services.AddSingleton(attendanceLocation);
var attendancePhotoDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["Attendance:PhotoDirectory"] ?? "App_Data/attendance-photos");
builder.Services.AddSingleton<IAttendancePhotoStorage>(new FileSystemAttendancePhotoStorage(attendancePhotoDirectory));
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IEffectiveAttendanceRepository, EffectiveAttendanceRepository>();
builder.Services.AddScoped<IEffectiveAttendanceService, EffectiveAttendanceService>();
builder.Services.AddScoped<IAttendanceNoteRepository, AttendanceNoteRepository>();
builder.Services.AddScoped<ILeaveReturnRepository, LeaveReturnRepository>();
builder.Services.AddScoped<ILeaveReturnService, LeaveReturnService>();
builder.Services.AddScoped<IAttendanceNoteService, AttendanceNoteService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceAdjustmentRepository, AttendanceAdjustmentRepository>();
builder.Services.AddScoped<IAttendanceAdjustmentService, AttendanceAdjustmentService>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IOvertimeRepository, OvertimeRepository>();
builder.Services.AddScoped<IOvertimeService, OvertimeService>();
builder.Services.AddScoped<IAttendancePeriodRepository, AttendancePeriodRepository>();
builder.Services.AddScoped<IPeriodLockService, PeriodLockService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
var evidenceRetentionDays = builder.Configuration.GetValue<int?>("Attendance:EvidenceRetentionDays") ?? PrivacyPolicy.DefaultRetentionDays;
builder.Services.AddScoped<IPrivacyConsentService>(sp => new PrivacyConsentService(
    sp.GetRequiredService<IUserRepository>(), sp.GetRequiredService<IAuditLogService>(), sp.GetRequiredService<ICurrentUserService>(), evidenceRetentionDays));
builder.Services.AddHostedService(sp => new AttendanceEvidenceRetentionWorker(
    sp.GetRequiredService<IServiceScopeFactory>(), sp.GetRequiredService<ILogger<AttendanceEvidenceRetentionWorker>>(), evidenceRetentionDays));
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Thông báo: hộp thông báo trong DB + thông báo đẩy Firebase (tự tắt nếu chưa có file khoá tài khoản dịch vụ).
var remindersOptions = builder.Configuration.GetSection(RemindersOptions.SectionName).Get<RemindersOptions>() ?? new RemindersOptions();
builder.Services.AddSingleton(remindersOptions);
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
if (!string.IsNullOrWhiteSpace(firebaseCredentialsPath) && !Path.IsPathRooted(firebaseCredentialsPath))
{
    firebaseCredentialsPath = Path.Combine(builder.Environment.ContentRootPath, firebaseCredentialsPath);
}
builder.Services.AddSingleton<IPushSender>(sp => new FirebasePushSender(firebaseCredentialsPath, sp.GetRequiredService<ILogger<FirebasePushSender>>()));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAttendanceReminderService, AttendanceReminderService>();
builder.Services.AddHostedService<AttendanceReminderWorker>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                // Flutter web (app mobile), chạy cố định qua `flutter run -d chrome --web-port=5080`.
                "http://localhost:5080",
                "http://127.0.0.1:5080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DataSeeder.SeedAsync(context, hasher);
    await MenuSeeder.SeedAsync(context);
    await ActionPermissionSeeder.SeedAsync(context);
    await LeaveTypeSeeder.SeedAsync(context);
    await PositionSeeder.SeedAsync(context);
    await HrRoleSeeder.SeedAsync(context);
    await HolidaySeeder.SeedAsync(context);
    await scope.ServiceProvider.GetRequiredService<IHolidayService>().ReloadAsync();

    if (app.Environment.IsDevelopment())
    {
        await DemoDataSeeder.SeedAsync(context, hasher);
    }
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

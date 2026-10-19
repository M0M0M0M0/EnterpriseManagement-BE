using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Application.Services;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using Xunit;

namespace EnterpriseManagement.Tests;

// Đồng ý chính sách thu thập vị trí/ảnh chấm công: phải đồng ý đúng bản hiện hành mới chấm công được, rút lại được.
public class PrivacyConsentTests
{
    private static (PrivacyConsentService Service, User User) Create()
    {
        var user = new User { Id = 1, Username = "e1", Employee = new Employee { EmployeeCode = "E1" } };
        return (new PrivacyConsentService(new Repo(user), new Audit(), new Current(), 180), user);
    }

    [Fact]
    public async Task NotAcceptedByDefault_PunchingIsBlocked()
    {
        var (service, _) = Create();
        Assert.False((await service.GetAsync("E1")).Accepted);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnsureAcceptedAsync("E1"));
        Assert.Contains("đồng ý", ex.Message);
    }

    [Fact]
    public async Task Accept_RecordsVersionAndTime_AndAllowsPunching()
    {
        var (service, user) = Create();
        var result = await service.AcceptAsync("E1");

        Assert.True(result.Accepted);
        Assert.Equal(PrivacyPolicy.CurrentVersion, user.PrivacyConsentVersion);
        Assert.NotNull(user.PrivacyConsentAt);
        await service.EnsureAcceptedAsync("E1");
    }

    [Fact]
    public async Task Withdraw_BlocksPunchingAgain()
    {
        var (service, user) = Create();
        await service.AcceptAsync("E1");
        await service.WithdrawAsync("E1");

        Assert.Null(user.PrivacyConsentVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnsureAcceptedAsync("E1"));
    }

    [Fact]
    public async Task OlderPolicyVersion_MustBeAcceptedAgain()
    {
        var (service, user) = Create();
        user.PrivacyConsentVersion = PrivacyPolicy.CurrentVersion - 1;
        user.PrivacyConsentAt = DateTime.UtcNow;

        Assert.False((await service.GetAsync("E1")).Accepted);
    }

    [Fact]
    public void Policy_MentionsRetentionDays()
    {
        var (service, _) = Create();
        var policy = service.GetPolicy();
        Assert.Equal(180, policy.RetentionDays);
        Assert.Contains(policy.Sections, s => s.Body.Contains("180 ngày"));
    }

    private sealed class Repo(User user) : IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username) => Task.FromResult<User?>(user);
        public Task<User?> GetByEmployeeCodeAsync(string employeeCode) => Task.FromResult<User?>(user);
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task AddAsync(User u) => throw new NotImplementedException();
        public void RemoveUserRole(UserRole userRole) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
    }

    private sealed class Audit : IAuditLogService
    {
        public Task LogAsync(long? userId, string action, string module, long? entityId = null, string? ipAddress = null) => Task.CompletedTask;
        public Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync() => throw new NotImplementedException();
        public Task<IEnumerable<AuditLogDto>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date) => throw new NotImplementedException();
    }

    private sealed class Current : ICurrentUserService
    {
        public long? UserId => 1;
        public string EmployeeCode => "E1";
        public string? Username => "e1";
        public string? IpAddress => null;
        public bool IsInRole(string role) => false;
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
    }
}

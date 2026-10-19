using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Application.Services;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;
using Xunit;

namespace EnterpriseManagement.Tests;

// Ngày lễ và làm thêm giờ (OT). Ngày lễ giả lập dùng năm 2030 để không đụng các test khác (lịch ngày lễ là trạng thái tĩnh).
// 2030-03-12 là thứ Ba, 2030-03-16 là thứ Bảy.
public class HolidayAndOvertimeTests
{
    private const long Emp = 1;
    private static readonly DateOnly Holiday = new(2030, 3, 12);
    private static readonly DateOnly HolidayToday = new(2030, 3, 20);

    private static void WithHoliday() => WorkCalendar.Load(new[] { (Holiday, "Ngày lễ thử") });

    private static EffectiveInputs Inputs() => new()
    {
        HireDates = new Dictionary<long, DateOnly?> { [Emp] = new DateOnly(2029, 1, 1) }
    };

    // ---- Ngày lễ ----

    [Fact]
    public void Holiday_IsNotAWorkingDay_AndNotAbsent()
    {
        WithHoliday();
        var day = EffectiveDayCalculator.Compute(Emp, Holiday, Inputs(), HolidayToday);

        Assert.True(day.IsHoliday);
        Assert.False(day.IsWeekend);
        Assert.Equal("Ngày lễ thử", day.HolidayName);
        Assert.Equal(SessionStatus.None, day.Morning);
        Assert.Equal(SessionStatus.None, day.Afternoon);
        Assert.Equal(SessionSource.Holiday, day.MorningSource);
    }

    [Fact]
    public void LeaveSpanningAHoliday_DoesNotCoverTheHoliday()
    {
        WithHoliday();
        var leave = new LeaveCoverage(1, Emp, new DateOnly(2030, 3, 11), new DateOnly(2030, 3, 13), LeaveSession.FullDay, "Phép năm");
        var covered = EffectiveDayCalculator.CoveredSessions(leave, Array.Empty<ActiveReturn>()).ToList();

        Assert.Equal(4, covered.Count);
        Assert.DoesNotContain(covered, c => c.Date == Holiday);
    }

    [Fact]
    public void CountWeekdays_ExcludesHolidays()
    {
        WithHoliday();
        Assert.Equal(4, DateRangeHelper.CountWeekdays(new DateOnly(2030, 3, 11), new DateOnly(2030, 3, 15)));
    }

    [Fact]
    public void NonWorkingReason_NamesTheHoliday()
    {
        WithHoliday();
        Assert.Contains("Ngày lễ thử", WorkCalendar.NonWorkingReason(Holiday));
        Assert.Contains("Chủ nhật", WorkCalendar.NonWorkingReason(new DateOnly(2030, 3, 17)));
    }

    // ---- OT trong công của ngày ----

    [Fact]
    public void ApprovedOvertime_IsAddedToThatDay()
    {
        WithHoliday();
        var inputs = Inputs();
        inputs.Overtimes.Add(new OvertimeEntry(Emp, new DateOnly(2030, 3, 14), 90));
        inputs.Overtimes.Add(new OvertimeEntry(Emp, new DateOnly(2030, 3, 14), 30));

        var day = EffectiveDayCalculator.Compute(Emp, new DateOnly(2030, 3, 14), inputs, HolidayToday);
        Assert.Equal(120, day.OvertimeMinutes);
    }

    [Fact]
    public void OvertimeOnWeekendOrHoliday_MakesTheDayVisible()
    {
        WithHoliday();
        var inputs = Inputs();
        inputs.Overtimes.Add(new OvertimeEntry(Emp, new DateOnly(2030, 3, 16), 120));
        inputs.Overtimes.Add(new OvertimeEntry(Emp, Holiday, 60));

        Assert.True(EffectiveDayCalculator.Compute(Emp, new DateOnly(2030, 3, 16), inputs, HolidayToday).HasData);
        Assert.True(EffectiveDayCalculator.Compute(Emp, Holiday, inputs, HolidayToday).HasData);
        Assert.False(EffectiveDayCalculator.Compute(Emp, new DateOnly(2030, 3, 17), inputs, HolidayToday).HasData);
    }

    // ---- Quy tắc xin OT ----

    private static DateOnly NextWeekday()
    {
        var date = VietnamClock.Today.AddDays(2);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) date = date.AddDays(1);
        return date;
    }

    private static DateOnly NextSaturday()
    {
        var date = VietnamClock.Today.AddDays(2);
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
        return date;
    }

    private static OvertimeService Service(FakeOvertimeRepository? repo = null, FakePeriodLock? periodLock = null)
    {
        WorkCalendar.Load(Array.Empty<(DateOnly, string)>());
        return new OvertimeService(
            repo ?? new FakeOvertimeRepository(), new FakeEmployees(), new FakeResolver(), new FakeEffective(),
            periodLock ?? new FakePeriodLock(), new FakeNotifications(), new FakeAudit(), new FakeCurrentUser());
    }

    private static SubmitOvertimeRequest Request(DateOnly date, string start, string end) =>
        new() { Date = date, StartTime = start, EndTime = end, Reason = "Chạy báo cáo cuối tháng" };

    private static async Task<string> ErrorOf(Func<Task> action) =>
        (await Assert.ThrowsAsync<InvalidOperationException>(action)).Message;

    [Fact]
    public async Task Overtime_OnWorkday_MustStartAfterWorkingHours()
    {
        var message = await ErrorOf(() => Service().SubmitAsync(Request(NextWeekday(), "16:30", "18:00"), "E1"));
        Assert.Contains("17:00", message);
    }

    [Fact]
    public async Task Overtime_OnWorkday_IsLimitedToFourHours()
    {
        var message = await ErrorOf(() => Service().SubmitAsync(Request(NextWeekday(), "17:00", "21:30"), "E1"));
        Assert.Contains("4 giờ", message);
    }

    [Fact]
    public async Task Overtime_MustBeInHalfHourSteps()
    {
        await ErrorOf(() => Service().SubmitAsync(Request(NextWeekday(), "17:00", "18:10"), "E1"));
    }

    [Fact]
    public async Task Overtime_ValidWorkdayRequest_IsCreatedAsPending()
    {
        var repo = new FakeOvertimeRepository();
        var dto = await Service(repo).SubmitAsync(Request(NextWeekday(), "17:30", "19:30"), "E1");

        Assert.Equal("Pending", dto.Status);
        Assert.Equal(120, dto.Minutes);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Overtime_OnWeekend_CanBeLongerAndEarlier()
    {
        var dto = await Service().SubmitAsync(Request(NextSaturday(), "08:00", "18:00"), "E1");
        Assert.Equal(600, dto.Minutes);
    }

    [Fact]
    public async Task Overtime_CannotBeInThePast()
    {
        await ErrorOf(() => Service().SubmitAsync(Request(VietnamClock.Today.AddDays(-1), "17:00", "19:00"), "E1"));
    }

    [Fact]
    public async Task Overtime_OverlappingAnotherRequest_IsRejected()
    {
        var repo = new FakeOvertimeRepository();
        var date = NextWeekday();
        await Service(repo).SubmitAsync(Request(date, "17:00", "19:00"), "E1");

        var message = await ErrorOf(() => Service(repo).SubmitAsync(Request(date, "18:00", "20:00"), "E1"));
        Assert.Contains("trùng", message);
    }

    [Fact]
    public async Task Overtime_OverMonthlyLimit_IsRejected()
    {
        var repo = new FakeOvertimeRepository { ExistingMinutes = OvertimeService.MonthLimitMinutes - 60 };
        var message = await ErrorOf(() => Service(repo).SubmitAsync(Request(NextWeekday(), "17:00", "19:00"), "E1"));
        Assert.Contains("40 giờ/tháng", message);
    }

    [Fact]
    public async Task Overtime_InClosedMonth_IsRejected()
    {
        var message = await ErrorOf(() => Service(periodLock: new FakePeriodLock { Closed = true }).SubmitAsync(Request(NextWeekday(), "17:00", "19:00"), "E1"));
        Assert.Contains("đã được chốt", message);
    }

    // ---- Bản giả tối thiểu cho các phụ thuộc ----

    private sealed class FakeOvertimeRepository : IOvertimeRepository
    {
        public List<OvertimeRequest> Items { get; } = new();
        public int ExistingMinutes { get; set; }

        public Task<OvertimeRequest?> GetByIdAsync(long id) => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
        public Task<List<OvertimeRequest>> GetByEmployeeAsync(long employeeId) => Task.FromResult(Items.ToList());
        public Task<List<OvertimeRequest>> GetPendingAsync() => Task.FromResult(Items.ToList());
        public Task<List<OvertimeRequest>> GetAllAsync() => Task.FromResult(Items.ToList());
        public Task<List<OvertimeRequest>> GetActiveByEmployeeAndDateAsync(long employeeId, DateOnly date) =>
            Task.FromResult(Items.Where(i => i.Date == date).ToList());
        public Task<int> SumActiveMinutesAsync(long employeeId, DateOnly from, DateOnly to, long? excludeId = null) =>
            Task.FromResult(ExistingMinutes);

        public Task AddAsync(OvertimeRequest request)
        {
            request.Id = Items.Count + 1;
            request.Employee = Employee();
            Items.Add(request);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync() => Task.FromResult(1);
    }

    private static Employee Employee() => new()
    {
        Id = Emp, EmployeeCode = "E1", FirstName = "An", LastName = "Nguyễn", TracksAttendance = true
    };

    private sealed class FakeEmployees : IEmployeeRepository
    {
        public Task<Employee?> GetByEmployeeCodeAsync(string employeeCode) => Task.FromResult<Employee?>(Employee());
        public Task<Employee?> GetByIdAsync(long id) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetByManagerIdAsync(long managerId) => throw new NotImplementedException();
        public Task AddAsync(Employee employee) => throw new NotImplementedException();
        public void Update(Employee employee) => throw new NotImplementedException();
        public void Delete(Employee employee) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class FakeResolver : IApprovalDelegationResolver
    {
        public Task<Employee?> ResolveApproverAsync(Employee employee) => Task.FromResult<Employee?>(null);
    }

    private sealed class FakeEffective : IEffectiveAttendanceService
    {
        public Task<EffectiveDay> GetDayAsync(long employeeId, DateOnly date) =>
            Task.FromResult(EffectiveDayCalculator.Compute(employeeId, date, new EffectiveInputs(), date));
        public Task<List<EffectiveDay>> GetRangeAsync(long employeeId, DateOnly from, DateOnly to) => throw new NotImplementedException();
        public Task<List<EffectiveDay>> GetDaysWithDataAsync(long employeeId) => throw new NotImplementedException();
        public Task<List<EffectiveDay>> GetDaysWithDataAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to) => throw new NotImplementedException();
        public Task<List<EffectiveDay>> GetAllWorkingDaysAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to) => throw new NotImplementedException();
        public Task<SessionStatus> GetSessionStatusAsync(long employeeId, DateOnly date, DaySession session) => throw new NotImplementedException();
        public Task<bool> IsOnLeaveNowAsync(long employeeId) => throw new NotImplementedException();
    }

    private sealed class FakePeriodLock : IPeriodLockService
    {
        public bool Closed { get; set; }
        public Task<bool> IsClosedAsync(DateOnly date) => Task.FromResult(Closed);
        public Task EnsureOpenAsync(DateOnly date) =>
            Closed ? throw new InvalidOperationException("Bảng công tháng đã được chốt.") : Task.CompletedTask;
        public Task EnsureOpenAsync(DateOnly from, DateOnly to) => EnsureOpenAsync(from);
    }

    private sealed class FakeNotifications : INotificationService
    {
        public Task NotifyAsync(long employeeId, string type, string title, string body, string? route = null, long? refId = null) => Task.CompletedTask;
        public Task NotifyApproverAsync(Employee requester, string type, string title, string body, string? route = null, long? refId = null) => Task.CompletedTask;
        public Task NotifyPermissionHoldersAsync(string permissionCode, long excludeEmployeeId, string type, string title, string body, string? route = null, long? refId = null) => Task.CompletedTask;
        public Task<IEnumerable<NotificationDto>> GetListAsync(string employeeCode, bool unreadOnly, int take, long? beforeId) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(string employeeCode) => throw new NotImplementedException();
        public Task MarkReadAsync(string employeeCode, long id) => throw new NotImplementedException();
        public Task MarkAllReadAsync(string employeeCode) => throw new NotImplementedException();
        public Task RegisterDeviceTokenAsync(string employeeCode, RegisterDeviceTokenRequest request) => throw new NotImplementedException();
        public Task UnregisterDeviceTokenAsync(string employeeCode, string token) => throw new NotImplementedException();
    }

    private sealed class FakeAudit : IAuditLogService
    {
        public Task LogAsync(long? userId, string action, string module, long? entityId = null, string? ipAddress = null) => Task.CompletedTask;
        public Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync() => throw new NotImplementedException();
        public Task<IEnumerable<AuditLogDto>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date) => throw new NotImplementedException();
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public long? UserId => 1;
        public string EmployeeCode => "E1";
        public string? Username => "e1";
        public string? IpAddress => null;
        public bool IsInRole(string role) => false;
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
    }
}

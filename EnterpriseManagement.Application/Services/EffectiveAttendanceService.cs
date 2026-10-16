using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class EffectiveAttendanceService : IEffectiveAttendanceService
{
    private static readonly DateOnly MinDate = new(2000, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);

    private readonly IEffectiveAttendanceRepository _repository;

    public EffectiveAttendanceService(IEffectiveAttendanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<EffectiveDay> GetDayAsync(long employeeId, DateOnly date)
    {
        var inputs = await _repository.LoadAsync(new[] { employeeId }, date, date);
        return EffectiveDayCalculator.Compute(employeeId, date, inputs, VietnamClock.Today);
    }

    public async Task<List<EffectiveDay>> GetRangeAsync(long employeeId, DateOnly from, DateOnly to)
    {
        var inputs = await _repository.LoadAsync(new[] { employeeId }, from, to);
        var today = VietnamClock.Today;

        var days = new List<EffectiveDay>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            days.Add(EffectiveDayCalculator.Compute(employeeId, date, inputs, today));
        }

        return days;
    }

    public Task<List<EffectiveDay>> GetDaysWithDataAsync(long employeeId) =>
        GetDaysWithDataAsync(new[] { employeeId }, MinDate, MaxDate);

    public async Task<List<EffectiveDay>> GetDaysWithDataAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to)
    {
        var inputs = await _repository.LoadAsync(employeeIds, from, to);
        var today = VietnamClock.Today;
        var result = new List<EffectiveDay>();

        foreach (var employeeId in employeeIds)
        {
            // Chỉ xét những ngày thật sự có dữ liệu, không duyệt từng ngày của cả khoảng (khoảng có thể rất dài).
            var dates = new HashSet<DateOnly>();

            foreach (var punch in inputs.Punches.Where(p => p.EmployeeId == employeeId)) dates.Add(punch.Date);
            foreach (var note in inputs.Notes.Where(n => n.EmployeeId == employeeId)) dates.Add(note.Date);
            foreach (var ret in inputs.Returns.Where(r => r.EmployeeId == employeeId && r.WorkedStatus != null)) dates.Add(ret.Date);
            foreach (var leave in inputs.Leaves.Where(l => l.EmployeeId == employeeId))
            {
                foreach (var (date, _) in EffectiveDayCalculator.CoveredSessions(leave, inputs.Returns))
                {
                    dates.Add(date);
                }
            }

            foreach (var date in dates.Where(d => d >= from && d <= to))
            {
                var day = EffectiveDayCalculator.Compute(employeeId, date, inputs, today);
                if (day.HasData) result.Add(day);
            }
        }

        return result.OrderByDescending(d => d.Date).ThenBy(d => d.EmployeeId).ToList();
    }

    public async Task<List<EffectiveDay>> GetAllWorkingDaysAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to)
    {
        var inputs = await _repository.LoadAsync(employeeIds, from, to);
        var today = VietnamClock.Today;
        var result = new List<EffectiveDay>();

        foreach (var employeeId in employeeIds)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var day = EffectiveDayCalculator.Compute(employeeId, date, inputs, today);
                // Cuối tuần chỉ giữ khi có dữ liệu thật (vd có người vẫn chấm công thứ Bảy).
                if (day.IsWeekend && !day.HasData) continue;
                result.Add(day);
            }
        }

        return result.OrderByDescending(d => d.Date).ThenBy(d => d.EmployeeId).ToList();
    }

    public async Task<SessionStatus> GetSessionStatusAsync(long employeeId, DateOnly date, DaySession session)
    {
        var day = await GetDayAsync(employeeId, date);
        return session == DaySession.Morning ? day.Morning : day.Afternoon;
    }

    public async Task<bool> IsOnLeaveNowAsync(long employeeId)
    {
        var now = VietnamClock.Now;
        var session = now.TimeOfDay < new TimeSpan(12, 0, 0) ? DaySession.Morning : DaySession.Afternoon;
        return await GetSessionStatusAsync(employeeId, DateOnly.FromDateTime(now), session) == SessionStatus.OnLeave;
    }
}

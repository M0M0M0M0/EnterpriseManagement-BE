using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Common;

// ---- Dữ liệu đầu vào cho hàm tính (đã được nạp sẵn từ DB, hàm tính không truy cập DB) ----

// Một đơn nghỉ đã duyệt, tính theo buổi (Session != null). Nghỉ theo giờ không nằm trong mô hình này.
public sealed record LeaveCoverage(
    long RequestId, long EmployeeId, DateOnly StartDate, DateOnly EndDate, LeaveSession Session, string LeaveTypeName);

// Một buổi đã bị gỡ khỏi đơn nghỉ (yêu cầu đi làm lại đã có hiệu lực).
public sealed record ActiveReturn(
    long LeaveRequestId, long EmployeeId, DateOnly Date, DaySession Session, SessionStatus? WorkedStatus);

// Ghi chú ngoại lệ của HR còn hiệu lực.
public sealed record SessionNote(long EmployeeId, DateOnly Date, DaySession Session, SessionStatus Status, string Note);

// Một lần chấm công thật (có giờ vào). Bản ghi chưa có giờ vào là bản ghi giữ chỗ, KHÔNG được đưa vào đây.
public sealed record Punch(long EmployeeId, DateOnly Date, DateTime CheckIn, DateTime? CheckOut, decimal? WorkingHours);

public sealed class EffectiveInputs
{
    public List<LeaveCoverage> Leaves { get; init; } = new();
    public List<ActiveReturn> Returns { get; init; } = new();
    public List<SessionNote> Notes { get; init; } = new();
    public List<Punch> Punches { get; init; } = new();

    // Ngày vào làm của từng nhân viên; ngày làm việc trước ngày này không bị tính là vắng.
    public Dictionary<long, DateOnly?> HireDates { get; init; } = new();

    // Nhân viên không chấm công (tài khoản hệ thống như admin): ngày không có dữ liệu không bị tính là vắng.
    public HashSet<long> NonTrackingEmployeeIds { get; init; } = new();
}

// ---- Kết quả ----

public enum SessionSource
{
    None,
    Weekend,
    Leave,
    ReturnConfirmed,
    Note,
    Attendance,
    Missing
}

public sealed class EffectiveDay
{
    public long EmployeeId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsWeekend { get; init; }

    public SessionStatus Morning { get; init; }
    public SessionStatus Afternoon { get; init; }
    public SessionSource MorningSource { get; init; }
    public SessionSource AfternoonSource { get; init; }

    public long? LeaveRequestId { get; init; }
    public string? LeaveTypeName { get; init; }
    public string? MorningNote { get; init; }
    public string? AfternoonNote { get; init; }

    public DateTime? CheckIn { get; init; }
    public DateTime? CheckOut { get; init; }
    public decimal? WorkingHours { get; init; }

    // Có dữ liệu nào để hiển thị không (chấm công thật, nghỉ phép, ghi chú hoặc đi làm lại). Dùng cho danh sách lịch sử.
    public bool HasData =>
        CheckIn is not null
        || MorningSource is SessionSource.Leave or SessionSource.Note or SessionSource.ReturnConfirmed
        || AfternoonSource is SessionSource.Leave or SessionSource.Note or SessionSource.ReturnConfirmed;

    // Trạng thái cả ngày dạng chuỗi cũ (Present/Late/HalfDay/...) để các màn hình cũ vẫn hiển thị được.
    // Dữ liệu chính xác nằm ở Morning và Afternoon.
    public string OverallStatus
    {
        get
        {
            var am = Morning;
            var pm = Afternoon;

            if (am == SessionStatus.None && pm == SessionStatus.None) return "None";
            if (am == SessionStatus.OnLeave && pm == SessionStatus.OnLeave) return "OnLeave";
            if (am == SessionStatus.OnLeave || pm == SessionStatus.OnLeave)
            {
                var other = am == SessionStatus.OnLeave ? pm : am;
                return other == SessionStatus.Absent ? "HalfDayAbsent" : "HalfDay";
            }
            if (am == SessionStatus.Absent && pm == SessionStatus.Absent) return "Absent";
            if (am == SessionStatus.Absent || pm == SessionStatus.Absent) return "HalfDayAbsent";
            if (am == SessionStatus.Late || pm == SessionStatus.Late) return "Late";
            return "Present";
        }
    }
}

// ---- Hàm tính ----

public static class EffectiveDayCalculator
{
    // Quy tắc giờ chuẩn (giữ đúng như AttendanceService.DetermineCheckInStatusAsync trước đây):
    // vào trước/đúng 08:15 là đúng giờ, từ 10:00 trở đi coi như vắng buổi sáng; buổi chiều chuẩn là 13:15,
    // từ 15:00 trở đi coi như vắng buổi chiều (cùng quy tắc "muộn quá 2 giờ so với giờ vào ca" như buổi sáng).
    public static readonly TimeOnly MorningDeadline = new(8, 15);
    public static readonly TimeOnly MorningAbsentFrom = new(10, 0);
    public static readonly TimeOnly AfternoonDeadline = new(13, 15);
    public static readonly TimeOnly AfternoonAbsentFrom = new(15, 0);

    // Tan làm 17:00; từ 18:00 trở đi khoá chấm công (vào và ra). Ai quên chấm công ra thì gửi yêu cầu điều chỉnh chấm công.
    public static readonly TimeOnly PunchLockFrom = new(18, 0);

    public static bool IsPunchLocked(DateTime now) => TimeOnly.FromDateTime(now) >= PunchLockFrom;

    public static readonly TimeOnly MorningStart = new(8, 0);
    public static readonly TimeOnly AfternoonStart = new(13, 0);

    public static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    // Các (ngày, buổi) mà một đơn nghỉ còn phủ sau khi trừ các buổi đã bị gỡ. Bỏ qua cuối tuần.
    public static IEnumerable<(DateOnly Date, DaySession Session)> CoveredSessions(
        LeaveCoverage leave, IEnumerable<ActiveReturn> returns)
    {
        var removed = returns
            .Where(r => r.LeaveRequestId == leave.RequestId)
            .Select(r => (r.Date, r.Session))
            .ToHashSet();

        for (var date = leave.StartDate; date <= leave.EndDate; date = date.AddDays(1))
        {
            if (IsWeekend(date)) continue;

            if (leave.Session is LeaveSession.FullDay or LeaveSession.Morning && !removed.Contains((date, DaySession.Morning)))
                yield return (date, DaySession.Morning);
            if (leave.Session is LeaveSession.FullDay or LeaveSession.Afternoon && !removed.Contains((date, DaySession.Afternoon)))
                yield return (date, DaySession.Afternoon);
        }
    }

    public static EffectiveDay Compute(long employeeId, DateOnly date, EffectiveInputs inputs, DateOnly today)
    {
        var punch = inputs.Punches.FirstOrDefault(p => p.EmployeeId == employeeId && p.Date == date);

        if (IsWeekend(date))
        {
            return new EffectiveDay
            {
                EmployeeId = employeeId, Date = date, IsWeekend = true,
                Morning = SessionStatus.None, Afternoon = SessionStatus.None,
                MorningSource = SessionSource.Weekend, AfternoonSource = SessionSource.Weekend,
                CheckIn = punch?.CheckIn, CheckOut = punch?.CheckOut, WorkingHours = punch?.WorkingHours
            };
        }

        var leaves = inputs.Leaves.Where(l => l.EmployeeId == employeeId && l.StartDate <= date && l.EndDate >= date).ToList();
        var employeeReturns = inputs.Returns.Where(r => r.EmployeeId == employeeId && r.Date == date).ToList();

        var morningCover = FindCover(leaves, employeeReturns, date, DaySession.Morning);
        var afternoonCover = FindCover(leaves, employeeReturns, date, DaySession.Afternoon);

        var (morning, morningSource, morningNote) = ResolveSession(
            employeeId, date, DaySession.Morning, morningCover, employeeReturns, inputs, punch, today);
        var (afternoon, afternoonSource, afternoonNote) = ResolveSession(
            employeeId, date, DaySession.Afternoon, afternoonCover, employeeReturns, inputs, punch, today);

        var leaveForDisplay = morningCover ?? afternoonCover;
        return new EffectiveDay
        {
            EmployeeId = employeeId, Date = date, IsWeekend = false,
            Morning = morning, Afternoon = afternoon,
            MorningSource = morningSource, AfternoonSource = afternoonSource,
            LeaveRequestId = leaveForDisplay?.RequestId,
            LeaveTypeName = leaveForDisplay?.LeaveTypeName,
            MorningNote = morningNote, AfternoonNote = afternoonNote,
            CheckIn = punch?.CheckIn, CheckOut = punch?.CheckOut, WorkingHours = punch?.WorkingHours
        };
    }

    // Đơn nghỉ nào (nếu có) còn phủ buổi này sau khi trừ các buổi đã bị gỡ khỏi chính đơn đó.
    private static LeaveCoverage? FindCover(
        List<LeaveCoverage> leaves, List<ActiveReturn> returnsOfDay, DateOnly date, DaySession session)
    {
        foreach (var leave in leaves)
        {
            var coversSession = leave.Session == LeaveSession.FullDay
                || (leave.Session == LeaveSession.Morning && session == DaySession.Morning)
                || (leave.Session == LeaveSession.Afternoon && session == DaySession.Afternoon);
            if (!coversSession) continue;

            // Tầng phủ gắn với đúng đơn của nó: chỉ gỡ buổi khỏi đơn có cùng mã, không ảnh hưởng đơn khác cùng ngày.
            var removed = returnsOfDay.Any(r => r.LeaveRequestId == leave.RequestId && r.Session == session);
            if (!removed) return leave;
        }

        return null;
    }

    private static (SessionStatus Status, SessionSource Source, string? Note) ResolveSession(
        long employeeId, DateOnly date, DaySession session, LeaveCoverage? cover, List<ActiveReturn> returnsOfDay,
        EffectiveInputs inputs, Punch? punch, DateOnly today)
    {
        // 1. Còn đơn nghỉ phủ buổi này.
        if (cover is not null) return (SessionStatus.OnLeave, SessionSource.Leave, null);

        // 2. Buổi này vừa được gỡ khỏi nghỉ phép và HR đã xác nhận trạng thái đi làm.
        var confirmed = returnsOfDay.FirstOrDefault(r => r.Session == session && r.WorkedStatus is not null);
        if (confirmed is not null) return (confirmed.WorkedStatus!.Value, SessionSource.ReturnConfirmed, null);

        // 3. Ghi chú ngoại lệ của HR.
        var note = inputs.Notes.FirstOrDefault(n => n.EmployeeId == employeeId && n.Date == date && n.Session == session);
        if (note is not null) return (note.Status, SessionSource.Note, note.Note);

        // 4. Chấm công gốc.
        if (punch is not null)
        {
            var ci = TimeOnly.FromDateTime(punch.CheckIn);
            var status = session == DaySession.Morning
                ? (ci <= MorningDeadline ? SessionStatus.OnTime : ci < MorningAbsentFrom ? SessionStatus.Late : SessionStatus.Absent)
                // Buổi chiều: mốc chuẩn 13:15, từ 15:00 là vắng; dùng chung cho cả ngày bình thường và ngày đã nghỉ phép buổi sáng.
                : (ci <= AfternoonDeadline ? SessionStatus.OnTime : ci < AfternoonAbsentFrom ? SessionStatus.Late : SessionStatus.Absent);
            return (status, SessionSource.Attendance, null);
        }

        // 5. Không có dữ liệu: ngày làm việc đã qua (kể từ ngày vào làm) mà không có gì thì là vắng không phép.
        //    Riêng người không chấm công (tài khoản hệ thống) thì không bao giờ bị tính vắng.
        if (inputs.NonTrackingEmployeeIds.Contains(employeeId)) return (SessionStatus.None, SessionSource.None, null);

        inputs.HireDates.TryGetValue(employeeId, out var hireDate);
        var beforeHire = hireDate is not null && date < hireDate.Value;
        if (date < today && !beforeHire) return (SessionStatus.Absent, SessionSource.Missing, null);

        return (SessionStatus.None, SessionSource.None, null);
    }
}

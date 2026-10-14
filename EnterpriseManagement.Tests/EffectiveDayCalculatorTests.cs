using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Enums;
using Xunit;

namespace EnterpriseManagement.Tests;

// Kiểm thử hàm tính trạng thái hiệu lực theo các tình huống đã thống nhất. Dùng ngày cố định:
// 2026-10-05 là thứ Hai, hôm nay giả định là thứ Tư 2026-10-07.
public class EffectiveDayCalculatorTests
{
    private const long Emp = 1;
    private static readonly DateOnly Mon = new(2026, 10, 5);
    private static readonly DateOnly Tue = new(2026, 10, 6);
    private static readonly DateOnly Wed = new(2026, 10, 7);
    private static readonly DateOnly Thu = new(2026, 10, 8);
    private static readonly DateOnly Fri = new(2026, 10, 9);
    private static readonly DateOnly Sat = new(2026, 10, 10);
    private static readonly DateOnly Today = Wed;

    private static EffectiveInputs Inputs() => new()
    {
        HireDates = new Dictionary<long, DateOnly?> { [Emp] = new DateOnly(2026, 1, 1) }
    };

    private static Punch PunchAt(DateOnly date, int h, int m) =>
        new(Emp, date, date.ToDateTime(new TimeOnly(h, m)), null, null);

    private static EffectiveDay Day(EffectiveInputs inputs, DateOnly date) =>
        EffectiveDayCalculator.Compute(Emp, date, inputs, Today);

    // ---- Cuối tuần và ngày không có dữ liệu ----

    [Fact]
    public void Weekend_IsWeekendAndHasNoStatus()
    {
        var day = Day(Inputs(), Sat);
        Assert.True(day.IsWeekend);
        Assert.Equal(SessionStatus.None, day.Morning);
        Assert.Equal(SessionStatus.None, day.Afternoon);
    }

    [Fact]
    public void PastWorkdayWithoutAnything_IsAbsent()
    {
        var day = Day(Inputs(), Mon);
        Assert.Equal(SessionStatus.Absent, day.Morning);
        Assert.Equal(SessionStatus.Absent, day.Afternoon);
        Assert.Equal(SessionSource.Missing, day.MorningSource);
    }

    [Fact]
    public void WorkdayBeforeHireDate_IsNotAbsent()
    {
        var inputs = new EffectiveInputs { HireDates = new Dictionary<long, DateOnly?> { [Emp] = Tue } };
        Assert.Equal(SessionStatus.None, Day(inputs, Mon).Morning);
        Assert.Equal(SessionStatus.Absent, Day(inputs, Tue).Morning);
    }

    [Fact]
    public void TodayAndFutureWithoutPunch_AreNone()
    {
        Assert.Equal(SessionStatus.None, Day(Inputs(), Today).Morning);
        Assert.Equal(SessionStatus.None, Day(Inputs(), Thu).Afternoon);
    }

    // ---- Chấm công gốc ----

    [Theory]
    [InlineData(8, 0, SessionStatus.OnTime, SessionStatus.OnTime)]
    [InlineData(8, 15, SessionStatus.OnTime, SessionStatus.OnTime)]
    [InlineData(8, 30, SessionStatus.Late, SessionStatus.OnTime)]
    [InlineData(10, 30, SessionStatus.Absent, SessionStatus.OnTime)]
    [InlineData(14, 0, SessionStatus.Absent, SessionStatus.Late)]
    [InlineData(14, 59, SessionStatus.Absent, SessionStatus.Late)]
    [InlineData(15, 0, SessionStatus.Absent, SessionStatus.Absent)]
    [InlineData(18, 55, SessionStatus.Absent, SessionStatus.Absent)]
    public void Punch_DerivesEachSessionFromCheckInTime(int h, int m, SessionStatus morning, SessionStatus afternoon)
    {
        var inputs = Inputs();
        inputs.Punches.Add(PunchAt(Mon, h, m));

        var day = Day(inputs, Mon);
        Assert.Equal(morning, day.Morning);
        Assert.Equal(afternoon, day.Afternoon);
    }

    [Fact]
    public void PlaceholderRecordWithoutCheckIn_IsIgnored()
    {
        // Bản ghi giữ chỗ (nghỉ phép cũ, điều chỉnh công) không có giờ vào nên không được nạp vào Punches.
        var day = Day(Inputs(), Mon);
        Assert.Equal(SessionSource.Missing, day.MorningSource);
    }

    // ---- Nghỉ phép ----

    [Fact]
    public void FullDayLeave_CoversBothSessions()
    {
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Mon, LeaveSession.FullDay, "Nghỉ có phép"));

        var day = Day(inputs, Mon);
        Assert.Equal(SessionStatus.OnLeave, day.Morning);
        Assert.Equal(SessionStatus.OnLeave, day.Afternoon);
        Assert.Equal("OnLeave", day.OverallStatus);
    }

    [Fact]
    public void MorningLeave_AfternoonComesFromPunch()
    {
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Mon, LeaveSession.Morning, "Nghỉ có phép"));
        inputs.Punches.Add(PunchAt(Mon, 13, 10));

        var day = Day(inputs, Mon);
        Assert.Equal(SessionStatus.OnLeave, day.Morning);
        Assert.Equal(SessionStatus.OnTime, day.Afternoon);
        Assert.Equal("HalfDay", day.OverallStatus);
    }

    [Fact]
    public void MorningLeave_LateAfternoon_IsLate()
    {
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Mon, LeaveSession.Morning, "Nghỉ có phép"));
        inputs.Punches.Add(PunchAt(Mon, 13, 40));

        Assert.Equal(SessionStatus.Late, Day(inputs, Mon).Afternoon);
    }

    [Fact]
    public void MultiDayLeave_SkipsWeekendsInCoverage()
    {
        var leave = new LeaveCoverage(10, Emp, Fri, new DateOnly(2026, 10, 12), LeaveSession.FullDay, "Nghỉ có phép");
        var covered = EffectiveDayCalculator.CoveredSessions(leave, new List<ActiveReturn>()).ToList();

        // Thứ Sáu và thứ Hai, mỗi ngày hai buổi, bỏ thứ Bảy và Chủ nhật.
        Assert.Equal(4, covered.Count);
        Assert.DoesNotContain(covered, c => c.Date == Sat);
    }

    // ---- Đi làm lại (tầng phủ gắn với đơn) ----

    private static void ReturnFrom(EffectiveInputs inputs, long requestId, DateOnly from, DateOnly to, SessionStatus? worked = null)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (EffectiveDayCalculator.IsWeekend(d)) continue;
            inputs.Returns.Add(new ActiveReturn(requestId, Emp, d, DaySession.Morning, worked));
            inputs.Returns.Add(new ActiveReturn(requestId, Emp, d, DaySession.Afternoon, worked));
        }
    }

    [Fact]
    public void ReturnToWork_RemovesDaysOnlyFromThatRequest()
    {
        // Nghỉ thứ Hai đến thứ Sáu, đi làm lại từ thứ Tư: thứ Hai, Ba vẫn nghỉ; thứ Tư, Năm, Sáu hết nghỉ.
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Fri, LeaveSession.FullDay, "Nghỉ có phép"));
        ReturnFrom(inputs, 10, Wed, Fri);

        Assert.Equal(SessionStatus.OnLeave, Day(inputs, Mon).Morning);
        Assert.Equal(SessionStatus.OnLeave, Day(inputs, Tue).Afternoon);
        Assert.NotEqual(SessionStatus.OnLeave, Day(inputs, Wed).Morning);
        Assert.NotEqual(SessionStatus.OnLeave, Day(inputs, Thu).Afternoon);
        Assert.NotEqual(SessionStatus.OnLeave, Day(inputs, Fri).Morning);
    }

    [Fact]
    public void SecondLeaveRequest_IsNotMaskedByReturnOfFirstRequest()
    {
        // R1 (T2-T6) đã gỡ T4-T6. R2 xin lại T4-T5: T4, T5 phải nghỉ bình thường qua R2, T6 vẫn đi làm.
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Fri, LeaveSession.FullDay, "Nghỉ có phép"));
        ReturnFrom(inputs, 10, Wed, Fri);
        inputs.Leaves.Add(new LeaveCoverage(20, Emp, Wed, Thu, LeaveSession.FullDay, "Nghỉ có phép"));

        Assert.Equal(SessionStatus.OnLeave, Day(inputs, Wed).Morning);
        Assert.Equal(SessionStatus.OnLeave, Day(inputs, Thu).Afternoon);
        Assert.NotEqual(SessionStatus.OnLeave, Day(inputs, Fri).Morning);
        Assert.Equal(20, Day(inputs, Wed).LeaveRequestId);
    }

    [Fact]
    public void ReturnConfirmedByHr_UsesWorkedStatus()
    {
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Fri, LeaveSession.FullDay, "Nghỉ có phép"));
        inputs.Returns.Add(new ActiveReturn(10, Emp, Tue, DaySession.Morning, SessionStatus.OnTime));
        inputs.Returns.Add(new ActiveReturn(10, Emp, Tue, DaySession.Afternoon, SessionStatus.Late));

        var day = Day(inputs, Tue);
        Assert.Equal(SessionStatus.OnTime, day.Morning);
        Assert.Equal(SessionStatus.Late, day.Afternoon);
        Assert.Equal(SessionSource.ReturnConfirmed, day.AfternoonSource);
    }

    [Fact]
    public void ReturnOfHalfDay_OnlyThatSessionIsRemoved()
    {
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Mon, LeaveSession.FullDay, "Nghỉ có phép"));
        inputs.Returns.Add(new ActiveReturn(10, Emp, Mon, DaySession.Afternoon, SessionStatus.OnTime));

        var day = Day(inputs, Mon);
        Assert.Equal(SessionStatus.OnLeave, day.Morning);
        Assert.Equal(SessionStatus.OnTime, day.Afternoon);
    }

    // ---- Ghi chú HR ----

    [Fact]
    public void Note_OverridesPunchForThatSessionOnly()
    {
        var inputs = Inputs();
        inputs.Punches.Add(PunchAt(Mon, 8, 0));
        inputs.Notes.Add(new SessionNote(Emp, Mon, DaySession.Afternoon, SessionStatus.Late, "Vào chiều muộn 30 phút"));

        var day = Day(inputs, Mon);
        Assert.Equal(SessionStatus.OnTime, day.Morning);
        Assert.Equal(SessionStatus.Late, day.Afternoon);
        Assert.Equal(SessionSource.Note, day.AfternoonSource);
        Assert.Equal("Vào chiều muộn 30 phút", day.AfternoonNote);
        Assert.Equal("Late", day.OverallStatus);
    }

    [Fact]
    public void Note_CoversMissingPunch()
    {
        var inputs = Inputs();
        inputs.Notes.Add(new SessionNote(Emp, Mon, DaySession.Morning, SessionStatus.OnTime, "Quên chấm công"));
        inputs.Notes.Add(new SessionNote(Emp, Mon, DaySession.Afternoon, SessionStatus.OnTime, "Quên chấm công"));

        Assert.Equal("Present", Day(inputs, Mon).OverallStatus);
    }

    [Fact]
    public void LeaveStillCovering_WinsOverNote()
    {
        // Ghi chú không được tạo lên buổi đang nghỉ phép (bị chặn ở tầng dịch vụ), nhưng nếu dữ liệu có thì nghỉ phép thắng.
        var inputs = Inputs();
        inputs.Leaves.Add(new LeaveCoverage(10, Emp, Mon, Mon, LeaveSession.FullDay, "Nghỉ có phép"));
        inputs.Notes.Add(new SessionNote(Emp, Mon, DaySession.Morning, SessionStatus.Absent, "x"));

        Assert.Equal(SessionStatus.OnLeave, Day(inputs, Mon).Morning);
    }

    // ---- Tóm tắt cả ngày ----

    [Theory]
    [InlineData(SessionStatus.OnTime, SessionStatus.OnTime, "Present")]
    [InlineData(SessionStatus.Late, SessionStatus.OnTime, "Late")]
    [InlineData(SessionStatus.Absent, SessionStatus.OnTime, "HalfDayAbsent")]
    [InlineData(SessionStatus.Absent, SessionStatus.Absent, "Absent")]
    [InlineData(SessionStatus.OnLeave, SessionStatus.OnLeave, "OnLeave")]
    [InlineData(SessionStatus.OnLeave, SessionStatus.OnTime, "HalfDay")]
    [InlineData(SessionStatus.None, SessionStatus.None, "None")]
    public void OverallStatus_SummarizesSessions(SessionStatus am, SessionStatus pm, string expected)
    {
        var day = new EffectiveDay { Morning = am, Afternoon = pm };
        Assert.Equal(expected, day.OverallStatus);
    }
}

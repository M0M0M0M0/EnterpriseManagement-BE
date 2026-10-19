using System.Globalization;
using System.Text;
using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Bảng công tháng cho HR: tổng hợp từ cùng nguồn tính ngày hiệu lực, có thể chốt (khoá dữ liệu tháng) và xuất CSV để tính lương.
public class TimesheetService : ITimesheetService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IAttendancePeriodRepository _periodRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public TimesheetService(
        IEmployeeRepository employeeRepository,
        IEffectiveAttendanceService effective,
        IAttendancePeriodRepository periodRepository,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _effective = effective;
        _periodRepository = periodRepository;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private static void ValidateMonth(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12) throw new InvalidOperationException("Tháng không hợp lệ.");
    }

    public Task<TimesheetDto> GetAsync(int year, int month, string? departmentCode) =>
        BuildAsync(year, month, all => all.Where(e => string.IsNullOrWhiteSpace(departmentCode) || e.Department?.DepartmentCode == departmentCode));

    // Bảng công của team (cấp dưới, đệ quy) của một quản lý: quản lý chỉ xem, không chốt.
    public Task<TimesheetDto> GetTeamAsync(int year, int month, string managerEmployeeCode) =>
        BuildAsync(year, month, all =>
        {
            var list = all.ToList();
            var manager = list.FirstOrDefault(e => e.EmployeeCode == managerEmployeeCode)
                ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerEmployeeCode}'.");

            // Đi xuống cây quản lý (ManagerId); HashSet chặn vòng lặp nếu dữ liệu quản lý bị khép kín.
            var children = list.Where(e => e.ManagerId.HasValue).ToLookup(e => e.ManagerId!.Value);
            var team = new List<Employee>();
            var visited = new HashSet<long> { manager.Id };
            var queue = new Queue<long>();
            queue.Enqueue(manager.Id);
            while (queue.Count > 0)
            {
                foreach (var child in children[queue.Dequeue()])
                {
                    if (!visited.Add(child.Id)) continue;
                    team.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            return team;
        });

    private async Task<TimesheetDto> BuildAsync(int year, int month, Func<IEnumerable<Employee>, IEnumerable<Employee>> scope)
    {
        ValidateMonth(year, month);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        // Không có ngày nghỉ việc nên người đã nghỉ việc/chấm dứt hợp đồng không nằm trong bảng công; người không thuộc diện chấm công cũng không.
        var employees = scope(await _employeeRepository.GetAllAsync())
            .Where(e => e.TracksAttendance
                && e.HireDate <= end
                && e.EmploymentStatus is not (EmploymentStatus.Resigned or EmploymentStatus.Terminated))
            .OrderBy(e => e.EmployeeCode)
            .ToList();

        var days = employees.Count == 0
            ? new List<EffectiveDay>()
            : await _effective.GetAllWorkingDaysAsync(employees.Select(e => e.Id).ToList(), start, end);
        var daysByEmployee = days.ToLookup(d => d.EmployeeId);

        var rows = new List<TimesheetRowDto>();
        foreach (var employee in employees)
        {
            decimal worked = 0, leave = 0, absent = 0;
            var late = 0;
            var overtimeMinutes = 0;
            foreach (var day in daysByEmployee[employee.Id])
            {
                overtimeMinutes += day.OvertimeMinutes;
                foreach (var session in new[] { day.Morning, day.Afternoon })
                {
                    switch (session)
                    {
                        case SessionStatus.OnTime: worked += 0.5m; break;
                        case SessionStatus.Late: worked += 0.5m; late++; break;
                        case SessionStatus.OnLeave: leave += 0.5m; break;
                        case SessionStatus.Absent: absent += 0.5m; break;
                    }
                }
            }

            rows.Add(new TimesheetRowDto
            {
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = NotificationText.Name(employee),
                DepartmentName = employee.Department?.DepartmentName ?? string.Empty,
                WorkedDays = worked,
                LeaveDays = leave,
                AbsentDays = absent,
                LateSessions = late,
                OvertimeHours = Math.Round(overtimeMinutes / 60m, 2)
            });
        }

        var period = await _periodRepository.GetAsync(year, month);
        return new TimesheetDto
        {
            Year = year,
            Month = month,
            StandardDays = DateRangeHelper.CountWeekdays(start, end),
            IsClosed = period is not null,
            ClosedAt = period?.ClosedAt,
            ClosedByName = period?.Closer is null ? null : NotificationText.Name(period.Closer),
            Rows = rows
        };
    }

    public async Task<byte[]> ExportCsvAsync(int year, int month, string? departmentCode)
    {
        var sheet = await GetAsync(year, month, departmentCode);
        var csv = new StringBuilder();
        csv.AppendLine($"Bảng công tháng {month:00}/{year}{(sheet.IsClosed ? " (đã chốt)" : " (chưa chốt)")} - công chuẩn {sheet.StandardDays} ngày");
        csv.AppendLine("Mã NV,Họ tên,Phòng ban,Ngày công đi làm,Ngày nghỉ phép,Ngày vắng,Số buổi đi muộn,Giờ làm thêm");
        foreach (var row in sheet.Rows)
        {
            csv.AppendLine(string.Join(',',
                Escape(row.EmployeeCode), Escape(row.EmployeeName), Escape(row.DepartmentName),
                row.WorkedDays.ToString(CultureInfo.InvariantCulture), row.LeaveDays.ToString(CultureInfo.InvariantCulture),
                row.AbsentDays.ToString(CultureInfo.InvariantCulture), row.LateSessions.ToString(CultureInfo.InvariantCulture),
                row.OvertimeHours.ToString(CultureInfo.InvariantCulture)));
        }

        // BOM để Excel mở đúng tiếng Việt.
        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
    }

    private static string Escape(string value) =>
        value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    public async Task<IEnumerable<AttendancePeriodDto>> GetClosedPeriodsAsync(int year) =>
        (await _periodRepository.GetByYearAsync(year)).OrderBy(p => p.Month).Select(ToDto);

    public async Task<AttendancePeriodDto> CloseAsync(int year, int month, string closerEmployeeCode)
    {
        ValidateMonth(year, month);
        var lastDay = new DateOnly(year, month, 1).AddMonths(1).AddDays(-1);
        if (lastDay >= VietnamClock.Today)
        {
            throw new InvalidOperationException("Chỉ chốt được bảng công của tháng đã kết thúc.");
        }

        if (await _periodRepository.GetAsync(year, month) is not null)
        {
            throw new InvalidOperationException($"Bảng công tháng {month:00}/{year} đã được chốt rồi.");
        }

        var closer = await _employeeRepository.GetByEmployeeCodeAsync(closerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{closerEmployeeCode}'.");

        var period = new AttendancePeriod { Year = year, Month = month, ClosedBy = closer.Id, Closer = closer, ClosedAt = VietnamClock.Now };
        await _periodRepository.AddAsync(period);
        await _periodRepository.SaveChangesAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "Close", "AttendancePeriod", period.Id, _currentUserService.IpAddress);
        return ToDto(period);
    }

    public async Task ReopenAsync(int year, int month)
    {
        ValidateMonth(year, month);
        var period = await _periodRepository.GetAsync(year, month)
            ?? throw new InvalidOperationException($"Bảng công tháng {month:00}/{year} chưa được chốt.");

        _periodRepository.Remove(period);
        await _periodRepository.SaveChangesAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "Reopen", "AttendancePeriod", period.Id, _currentUserService.IpAddress);
    }

    private static AttendancePeriodDto ToDto(AttendancePeriod p) => new()
    {
        Year = p.Year,
        Month = p.Month,
        ClosedAt = p.ClosedAt,
        ClosedByName = p.Closer is null ? string.Empty : NotificationText.Name(p.Closer)
    };
}

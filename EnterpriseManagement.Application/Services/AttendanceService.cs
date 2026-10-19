using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class AttendanceService : IAttendanceService
{
    // Giờ chuẩn check-in buổi sáng: đúng/trước 8h15 tính Present, sau đó tính Late — mỗi phút
    // nghỉ ngắn đã được duyệt trong buổi sáng cùng ngày cộng dồn thêm vào mốc này (xem
    // GetApprovedShortLeaveMinutesAsync). Quá 2 tiếng so với giờ chuẩn (8h) mà vẫn chưa
    // check-in và không có phép thì coi như bỏ nguyên buổi sáng (HalfDayAbsent), không tính
    // Late nữa. Giờ chuẩn buổi chiều dùng khi nhân viên đã được duyệt nghỉ buổi sáng — nghĩa
    // vụ thật của họ chỉ bắt đầu từ chiều nên so theo mốc này thay vì mốc buổi sáng.
    private static readonly TimeOnly StandardCheckInTime = new(8, 0);
    private static readonly TimeOnly StandardCheckInDeadline = new(8, 15);
    private static readonly TimeOnly HalfDayAbsentDeadline = StandardCheckInTime.AddHours(2);
    private static readonly TimeOnly StandardAfternoonDeadline = new(13, 15);

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IAttendancePhotoStorage _photoStorage;
    private readonly AttendanceLocationOptions _office;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IEffectiveAttendanceService effective,
        IAttendancePhotoStorage photoStorage,
        AttendanceLocationOptions office)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _effective = effective;
        _photoStorage = photoStorage;
        _office = office;
    }

    public OfficeLocationDto GetOfficeLocation() => new()
    {
        Name = _office.OfficeName,
        Latitude = _office.OfficeLatitude,
        Longitude = _office.OfficeLongitude,
        AllowedRadiusMeters = _office.AllowedRadiusMeters
    };

    public async Task<AttendanceRecordDto> PunchAsync(string employeeCode, PunchRequest request)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        if (!employee.TracksAttendance)
        {
            throw new InvalidOperationException(SystemAccount.NotTrackedMessage);
        }

        // Quá giờ khoá (22:00) thì không chấm công nữa, kiểm tra đầu tiên để không để lại ảnh hay dữ liệu nào.
        if (EffectiveDayCalculator.IsPunchLocked(VietnamClock.Now))
        {
            throw new InvalidOperationException(
                "Đã quá 22:00 nên hệ thống khoá chấm công. Nếu quên chấm công, hãy gửi yêu cầu điều chỉnh chấm công.");
        }

        // Kiểm tra vị trí trước khi lưu bất cứ thứ gì (kể cả ảnh), để lần chấm công bị từ chối không để lại dữ liệu.
        ValidateLocation(request);

        var today = VietnamClock.Today;
        var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today);
        var now = VietnamClock.Now;

        // Check-in = chưa có bản ghi hoặc bản ghi chưa có giờ vào thật. Cả check-in lẫn check-out đều cần chụp ảnh.
        var isCheckIn = record is null || record.CheckInTime is null;
        string? photoPath = null;
        if (isCheckIn)
        {
            // Đang trong buổi nghỉ phép đã duyệt thì không chấm công được: muốn đi làm phải gửi yêu cầu đi làm lại
            // (Manager duyệt, HR xác nhận nếu cần), để đi làm trong ngày nghỉ luôn có người chịu trách nhiệm xác nhận.
            var currentSession = now.TimeOfDay < new TimeSpan(12, 0, 0) ? DaySession.Morning : DaySession.Afternoon;
            if (await _effective.GetSessionStatusAsync(employee.Id, today, currentSession) == SessionStatus.OnLeave)
            {
                throw new InvalidOperationException(
                    "Bạn đang trong thời gian nghỉ phép đã được duyệt nên chưa thể chấm công. Nếu muốn đi làm, hãy gửi yêu cầu rút ngắn nghỉ phép.");
            }

            if (request.Photo is null)
            {
                throw new InvalidOperationException("Vui lòng chụp ảnh để check-in.");
            }

            photoPath = await _photoStorage.SaveAsync(request.Photo, request.PhotoExtension);
        }
        else
        {
            if (request.Photo is null)
            {
                throw new InvalidOperationException("Vui lòng chụp ảnh để check-out.");
            }

            photoPath = await _photoStorage.SaveAsync(request.Photo, request.PhotoExtension);
        }

        if (record is null)
        {
            // Lần punch đầu tiên trong ngày = check-in.
            record = new AttendanceRecord
            {
                EmployeeId = employee.Id,
                AttendanceDate = today,
                CheckInTime = now,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                CheckInPhotoPath = photoPath,
                Status = await DetermineCheckInStatusAsync(employee.Id, today, now),
                CreatedAt = now
            };
            await _attendanceRepository.AddAsync(record);
        }
        else if (record.CheckInTime is null)
        {
            // Bản ghi hôm nay đã tồn tại nhưng chưa có check-in thật (vd ngày đang bị đánh dấu
            // Vắng/Nghỉ phép) — coi lần bấm này là check-in thật, không phải check-out.
            record.CheckInTime = now;
            record.CheckInLatitude = request.Latitude;
            record.CheckInLongitude = request.Longitude;
            record.CheckInPhotoPath = photoPath;
            record.CheckOutTime = null;
            record.WorkingHours = null;
            record.Status = await DetermineCheckInStatusAsync(employee.Id, today, now);
            record.Note = null;
            record.UpdatedAt = now;
        }
        else
        {
            // Các lần punch sau trong cùng ngày = check-out, đè lên checkout trước đó nếu có
            // (vị trí và ảnh của lần check-out trước cũng bị thay bằng lần mới nhất).
            if (record.CheckOutPhotoPath is not null) _photoStorage.Delete(record.CheckOutPhotoPath);
            record.CheckOutTime = now;
            record.CheckOutLatitude = request.Latitude;
            record.CheckOutLongitude = request.Longitude;
            record.CheckOutPhotoPath = photoPath;
            record.WorkingHours = (decimal)(now - record.CheckInTime.Value).TotalHours;
            record.UpdatedAt = now;
        }

        await _attendanceRepository.SaveChangesAsync();

        // Trả về trạng thái hiệu lực (tách sáng/chiều) thay vì trạng thái thô của bản ghi vừa lưu.
        return AttendanceMapper.ToRecordDto(await _effective.GetDayAsync(employee.Id, today), employee);
    }

    private void ValidateLocation(PunchRequest request)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            throw new InvalidOperationException("Toạ độ vị trí không hợp lệ.");
        }

        if (request.IsMockLocation)
        {
            throw new InvalidOperationException("Phát hiện vị trí giả lập. Vui lòng tắt ứng dụng giả vị trí để chấm công.");
        }

        var distance = GeoDistance.Meters(
            request.Latitude, request.Longitude, _office.OfficeLatitude, _office.OfficeLongitude);
        if (distance > _office.AllowedRadiusMeters)
        {
            throw new InvalidOperationException(
                $"Bạn đang cách {_office.OfficeName} khoảng {Math.Round(distance)} m, vượt quá bán kính cho phép {_office.AllowedRadiusMeters:0} m.");
        }
    }

    public async Task<AttendanceStatus> DetermineCheckInStatusAsync(long employeeId, DateOnly date, DateTime checkInTime)
    {
        var checkInTimeOnly = TimeOnly.FromDateTime(checkInTime);
        var approvedLeavesToday = await GetApprovedLeavesForDateAsync(employeeId, date);

        // Đã được duyệt nghỉ buổi sáng -> nghĩa vụ thật của ngày hôm nay chỉ bắt đầu từ chiều,
        // so giờ check-in với giờ chuẩn buổi chiều thay vì buổi sáng (vd checkin 10h55 vẫn tính
        // Present vì còn sớm hơn nhiều so với 13h15, không phải "đến muộn buổi sáng").
        // Dùng phạm vi nghỉ hiệu lực (đã trừ các buổi đi làm lại), không đọc thẳng đơn nghỉ gốc.
        var hasApprovedMorningLeave =
            await _effective.GetSessionStatusAsync(employeeId, date, DaySession.Morning) == SessionStatus.OnLeave;
        if (hasApprovedMorningLeave)
        {
            return checkInTimeOnly <= StandardAfternoonDeadline ? AttendanceStatus.Present : AttendanceStatus.Late;
        }

        // Không có phép mà quá 2 tiếng so với giờ chuẩn (8h) vẫn chưa check-in -> coi như bỏ
        // nguyên buổi sáng không xin phép, cần trạng thái riêng để quản lý thấy rõ thay vì lẫn
        // với "đến trễ vài phút".
        if (checkInTimeOnly >= HalfDayAbsentDeadline)
        {
            return AttendanceStatus.HalfDayAbsent;
        }

        // Nghỉ ngắn đã được duyệt vào buổi sáng (trước 12h) cùng ngày cộng dồn vào giờ chuẩn
        // check-in — xin nghỉ ngắn 30 phút thì được đi trễ thêm 30 phút mà vẫn tính Present.
        var shortLeaveMinutes = approvedLeavesToday
            .Where(l => l.LeaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset && l.StartDate.TimeOfDay < new TimeSpan(12, 0, 0))
            .Sum(l => l.TotalTime) * 60;
        var deadline = shortLeaveMinutes > 0 ? StandardCheckInDeadline.AddMinutes((double)shortLeaveMinutes) : StandardCheckInDeadline;

        return checkInTimeOnly <= deadline ? AttendanceStatus.Present : AttendanceStatus.Late;
    }

    private async Task<List<LeaveRequest>> GetApprovedLeavesForDateAsync(long employeeId, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);
        var requests = await _leaveRequestRepository.GetActiveByEmployeeAndRangeAsync(employeeId, dayStart, dayEnd);

        return requests.Where(l => l.Status == LeaveRequestStatus.Approved).ToList();
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetHistoryAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        // Lịch sử gồm mọi ngày có dữ liệu (chấm công thật, nghỉ phép, ghi chú), đã gộp theo trạng thái hiệu lực.
        var days = await _effective.GetDaysWithDataAsync(employee.Id);
        return days.Select(d => AttendanceMapper.ToRecordDto(d, employee));
    }

    public async Task<IEnumerable<AttendanceCalendarDayDto>> GetCalendarAsync(string employeeCode, DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            throw new InvalidOperationException("Khoảng ngày tối đa là 366 ngày.");
        }

        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        var days = await _effective.GetRangeAsync(employee.Id, from, to);
        return days.Select(AttendanceMapper.ToCalendarDto);
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetAllAsync(DateOnly startDate, DateOnly endDate, string? departmentCode)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }

        // Bảng công chỉ gồm người có chấm công (bỏ tài khoản hệ thống như admin).
        var employees = (await _employeeRepository.GetAllAsync()).Where(e => e.TracksAttendance).ToList();
        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var department = await _departmentRepository.GetByCodeAsync(departmentCode)
                ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{departmentCode}'.");
            employees = employees.Where(e => e.DepartmentId == department.Id).ToList();
        }

        var byId = employees.ToDictionary(e => e.Id);
        var days = await _effective.GetDaysWithDataAsync(byId.Keys.ToList(), startDate, endDate);
        return days.OrderBy(d => d.Date).ThenBy(d => d.EmployeeId).Select(d => AttendanceMapper.ToRecordDto(d, byId[d.EmployeeId]));
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetByDepartmentAsync(string departmentCode, DateOnly startDate, DateOnly endDate)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban có mã '{departmentCode}'.");

        var employees = (await _employeeRepository.GetAllAsync())
            .Where(e => e.DepartmentId == department.Id && e.TracksAttendance)
            .ToList();
        var byId = employees.ToDictionary(e => e.Id);

        var days = await _effective.GetDaysWithDataAsync(byId.Keys.ToList(), startDate, endDate);
        return days.OrderBy(d => d.Date).ThenBy(d => d.EmployeeId).Select(d => AttendanceMapper.ToRecordDto(d, byId[d.EmployeeId]));
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetByTeamAsync(string managerEmployeeCode, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }

        var all = (await _employeeRepository.GetAllAsync()).ToList();
        var manager = all.FirstOrDefault(e => e.EmployeeCode == managerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerEmployeeCode}'.");

        // Đi xuống cây quản lý (ManagerId) lấy mọi cấp dưới; HashSet chặn vòng lặp nếu dữ liệu quản lý bị khép kín.
        var childrenByManagerId = all.Where(e => e.ManagerId.HasValue).ToLookup(e => e.ManagerId!.Value);
        var team = new List<Employee>();
        var visited = new HashSet<long> { manager.Id };
        var queue = new Queue<long>();
        queue.Enqueue(manager.Id);
        while (queue.Count > 0)
        {
            foreach (var child in childrenByManagerId[queue.Dequeue()])
            {
                if (!visited.Add(child.Id)) continue;
                // Người đã nghỉ việc không còn trong bảng công, nhưng vẫn đi tiếp xuống cấp dưới của họ.
                // Tài khoản hệ thống (không chấm công) cũng không nằm trong bảng công.
                if (child.TracksAttendance && child.EmploymentStatus is not (EmploymentStatus.Resigned or EmploymentStatus.Terminated)) team.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        // Khoảng quá dài sẽ ra hàng nghìn dòng (mỗi người mỗi ngày); bảng công cả tháng đã là đủ dùng.
        if (endDate.DayNumber - startDate.DayNumber > 93)
        {
            throw new InvalidOperationException("Chỉ xem được tối đa khoảng 3 tháng mỗi lần.");
        }

        // Mỗi cấp dưới một dòng mỗi ngày làm việc, kể cả người chưa chấm công, để quản lý thấy cả người vắng.
        var byId = team.ToDictionary(e => e.Id);
        var days = await _effective.GetAllWorkingDaysAsync(byId.Keys.ToList(), startDate, endDate);
        return days.OrderBy(d => d.Date).ThenBy(d => byId[d.EmployeeId].EmployeeCode).Select(d => AttendanceMapper.ToRecordDto(d, byId[d.EmployeeId]));
    }
}

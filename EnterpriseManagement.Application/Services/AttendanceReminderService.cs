using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Nhắc chấm công theo giờ:
//  - Sắp đến giờ vào ca: nhân viên chưa chấm công vào và buổi sáng không nghỉ phép.
//  - Hết giờ làm việc: nhân viên đã chấm công vào nhưng chưa chấm công ra (và buổi chiều không nghỉ phép).
// Chỉ gửi ngày làm việc; mỗi nhân viên mỗi loại một lần mỗi ngày (chống gửi trùng khi backend khởi động lại).
public class AttendanceReminderService : IAttendanceReminderService
{
    public const string CheckInType = "CheckInReminder";
    public const string CheckOutType = "CheckOutReminder";

    private readonly INotificationRepository _repository;
    private readonly IEffectiveAttendanceRepository _effectiveRepository;
    private readonly INotificationService _notificationService;
    private readonly RemindersOptions _options;

    public AttendanceReminderService(
        INotificationRepository repository,
        IEffectiveAttendanceRepository effectiveRepository,
        INotificationService notificationService,
        RemindersOptions options)
    {
        _repository = repository;
        _effectiveRepository = effectiveRepository;
        _notificationService = notificationService;
        _options = options;
    }

    public async Task<int> RunAsync(DateTime now)
    {
        if (!_options.Enabled) return 0;

        var today = DateOnly.FromDateTime(now);
        if (EffectiveDayCalculator.IsNonWorkingDay(today)) return 0;

        var checkIn = InWindow(now, _options.CheckInTime);
        var checkOut = InWindow(now, _options.CheckOutTime);
        if (!checkIn && !checkOut) return 0;

        var employeeIds = await _repository.GetActiveEmployeeIdsWithAccountAsync();
        if (employeeIds.Count == 0) return 0;

        var inputs = await _effectiveRepository.LoadAsync(employeeIds, today, today);
        var startOfDay = today.ToDateTime(TimeOnly.MinValue);
        var sent = 0;

        foreach (var employeeId in employeeIds)
        {
            var day = EffectiveDayCalculator.Compute(employeeId, today, inputs, today);

            if (checkIn && day.CheckIn is null && day.Morning != SessionStatus.OnLeave
                && !await _repository.ExistsSinceAsync(employeeId, CheckInType, startOfDay))
            {
                await _notificationService.NotifyAsync(
                    employeeId, CheckInType, "Sắp đến giờ chấm công vào",
                    "Sắp đến giờ làm việc (08:00). Bạn nhớ chấm công vào ca nhé.", "/employee/attendance");
                sent++;
            }

            if (checkOut && day.CheckIn is not null && day.CheckOut is null && day.Afternoon != SessionStatus.OnLeave
                && !await _repository.ExistsSinceAsync(employeeId, CheckOutType, startOfDay))
            {
                await _notificationService.NotifyAsync(
                    employeeId, CheckOutType, "Hết giờ làm việc",
                    "Đã hết giờ làm việc (17:00). Bạn nhớ chấm công ra trước khi về nhé.", "/employee/attendance");
                sent++;
            }
        }

        return sent;
    }

    // Đang trong khung [giờ nhắc, giờ nhắc + WindowMinutes).
    private bool InWindow(DateTime now, string time)
    {
        if (!TimeOnly.TryParse(time, out var at)) return false;

        var start = now.Date + at.ToTimeSpan();
        return now >= start && now < start.AddMinutes(Math.Max(1, _options.WindowMinutes));
    }
}

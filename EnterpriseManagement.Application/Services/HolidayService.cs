using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;

namespace EnterpriseManagement.Application.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _repository;
    private readonly IPeriodLockService _periodLock;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public HolidayService(
        IHolidayRepository repository, IPeriodLockService periodLock, IAuditLogService auditLogService, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _periodLock = periodLock;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    public async Task ReloadAsync() =>
        WorkCalendar.Load((await _repository.GetAllAsync()).Select(h => (h.Date, h.Name)));

    public async Task<IEnumerable<HolidayDto>> GetByYearAsync(int year) =>
        (await _repository.GetByYearAsync(year)).Select(ToDto);

    public async Task<HolidayDto> CreateAsync(CreateHolidayRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) throw new InvalidOperationException("Vui lòng nhập tên ngày lễ.");
        if (name.Length > 200) throw new InvalidOperationException("Tên ngày lễ tối đa 200 ký tự.");
        if (request.Date.Year is < 2000 or > 2100) throw new InvalidOperationException("Ngày lễ không hợp lệ.");
        if (await _repository.ExistsOnDateAsync(request.Date)) throw new InvalidOperationException("Ngày này đã được khai báo là ngày lễ.");

        await _periodLock.EnsureOpenAsync(request.Date);

        var holiday = new Holiday { Date = request.Date, Name = name, CreatedAt = VietnamClock.Now };
        await _repository.AddAsync(holiday);
        await _repository.SaveChangesAsync();
        await ReloadAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "Create", "Holiday", holiday.Id, _currentUserService.IpAddress);
        return ToDto(holiday);
    }

    public async Task DeleteAsync(long id)
    {
        var holiday = await _repository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Không tìm thấy ngày lễ {id}.");
        await _periodLock.EnsureOpenAsync(holiday.Date);

        _repository.Remove(holiday);
        await _repository.SaveChangesAsync();
        await ReloadAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "Delete", "Holiday", id, _currentUserService.IpAddress);
    }

    private static HolidayDto ToDto(Holiday h) => new() { Id = h.Id, Date = h.Date, Name = h.Name };
}

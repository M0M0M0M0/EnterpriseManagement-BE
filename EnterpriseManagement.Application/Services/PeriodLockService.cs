using EnterpriseManagement.Application.Interfaces;

namespace EnterpriseManagement.Application.Services;

public class PeriodLockService : IPeriodLockService
{
    private readonly IAttendancePeriodRepository _repository;

    public PeriodLockService(IAttendancePeriodRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsClosedAsync(DateOnly date) => await _repository.GetAsync(date.Year, date.Month) is not null;

    public async Task EnsureOpenAsync(DateOnly date)
    {
        if (await IsClosedAsync(date))
        {
            throw new InvalidOperationException($"Bảng công tháng {date.Month:00}/{date.Year} đã được chốt, không thể thêm hoặc sửa dữ liệu của tháng này.");
        }
    }

    public async Task EnsureOpenAsync(DateOnly from, DateOnly to)
    {
        for (var cursor = new DateOnly(from.Year, from.Month, 1); cursor <= to; cursor = cursor.AddMonths(1))
        {
            await EnsureOpenAsync(cursor);
        }
    }
}

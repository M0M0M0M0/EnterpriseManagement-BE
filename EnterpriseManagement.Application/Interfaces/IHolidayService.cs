using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IHolidayService
{
    // Nạp lại lịch ngày lễ vào bộ nhớ (WorkCalendar); gọi lúc khởi động và sau mỗi lần thêm/xoá.
    Task ReloadAsync();
    Task<IEnumerable<HolidayDto>> GetByYearAsync(int year);
    Task<HolidayDto> CreateAsync(CreateHolidayRequest request);
    Task DeleteAsync(long id);
}

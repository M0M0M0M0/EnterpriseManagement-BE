namespace EnterpriseManagement.Application.Interfaces;

// Chặn thêm/sửa dữ liệu công của tháng đã chốt bảng công. Mọi nghiệp vụ làm thay đổi công của một ngày (điều chỉnh, ghi nhận,
// nghỉ phép, đi làm lại, OT, ngày lễ) phải gọi trước khi ghi.
public interface IPeriodLockService
{
    Task<bool> IsClosedAsync(DateOnly date);

    // Ném InvalidOperationException nếu tháng chứa ngày này đã chốt.
    Task EnsureOpenAsync(DateOnly date);

    // Như trên cho mọi tháng nằm trong khoảng.
    Task EnsureOpenAsync(DateOnly from, DateOnly to);
}

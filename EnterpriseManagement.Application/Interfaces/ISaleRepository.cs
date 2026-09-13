using EnterpriseManagement.Domain.Entities.Sales;

namespace EnterpriseManagement.Application.Interfaces;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(long id);
    Task<IEnumerable<Sale>> GetPendingAsync();
    Task<IEnumerable<Sale>> GetAllAsync();
    Task<Sale?> GetByCodeAsync(string saleCode);
    Task<IEnumerable<Sale>> GetByEmployeeIdAsync(long employeeId);
    Task AddAsync(Sale sale);
    Task<int> SaveChangesAsync();

    // Sale Confirmed của 1 nhân viên trong 1 khoảng ngày (theo OrderDate) — dùng để tính tổng
    // doanh số làm căn cứ tính hoa hồng theo kỳ.
    Task<IEnumerable<Sale>> GetConfirmedByEmployeeAndRangeAsync(long employeeId, DateOnly periodStart, DateOnly periodEnd);
}

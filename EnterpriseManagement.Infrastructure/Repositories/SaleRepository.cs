using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly ApplicationDbContext _context;

    public SaleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(long id) =>
        await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Sale>> GetPendingAsync() =>
        await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Employee)
            .Where(s => s.Status == SaleStatus.Pending)
            .ToListAsync();

    public async Task<IEnumerable<Sale>> GetAllAsync() =>
        await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .OrderByDescending(s => s.OrderDate)
            .ToListAsync();

    public async Task<Sale?> GetByCodeAsync(string saleCode) =>
        await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .FirstOrDefaultAsync(s => s.SaleCode == saleCode);

    public async Task<IEnumerable<Sale>> GetByEmployeeIdAsync(long employeeId) =>
        await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.OrderDate)
            .ToListAsync();

    public async Task AddAsync(Sale sale) =>
        await _context.Sales.AddAsync(sale);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();

    public async Task<IEnumerable<Sale>> GetConfirmedByEmployeeAndRangeAsync(
        long employeeId, DateOnly periodStart, DateOnly periodEnd)
    {
        var startDateTime = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDateTime = periodEnd.ToDateTime(TimeOnly.MaxValue);

        return await _context.Sales
            .Where(s => s.EmployeeId == employeeId
                && s.Status == SaleStatus.Confirmed
                && s.OrderDate >= startDateTime
                && s.OrderDate <= endDateTime)
            .ToListAsync();
    }
}

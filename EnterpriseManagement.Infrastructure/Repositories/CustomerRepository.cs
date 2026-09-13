using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByCodeAsync(string customerCode) =>
        await _context.Customers
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.CustomerCode == customerCode);

    public async Task<Customer?> GetByPhoneAsync(string phone) =>
        await _context.Customers
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.Phone == phone);

    public async Task<IEnumerable<Customer>> GetAllAsync() =>
        await _context.Customers
            .Include(c => c.AssignedEmployee)
            .ToListAsync();

    public async Task AddAsync(Customer customer) =>
        await _context.Customers.AddAsync(customer);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}

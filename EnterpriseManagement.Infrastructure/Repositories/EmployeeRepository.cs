using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(long id) =>
        await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<Employee>> GetAllAsync() =>
        await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .ToListAsync();

    public async Task<Employee?> GetByEmployeeCodeAsync(string employeeCode) =>
        await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

    public async Task<IEnumerable<Employee>> GetByManagerIdAsync(long managerId) =>
        await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Where(e => e.ManagerId == managerId)
            .ToListAsync();

    public async Task AddAsync(Employee employee) =>
        await _context.Employees.AddAsync(employee);

    public void Update(Employee employee) =>
        _context.Employees.Update(employee);

    public void Delete(Employee employee) =>
        _context.Employees.Remove(employee);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}

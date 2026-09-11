using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly ApplicationDbContext _context;

    public MenuRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Menu?> GetByCodeAsync(string menuCode) =>
        await _context.Menus
            .Include(m => m.MenuPermissions)
            .ThenInclude(mp => mp.Permission)
            .FirstOrDefaultAsync(m => m.MenuCode == menuCode);

    public async Task<IEnumerable<Menu>> GetAllAsync() =>
        await _context.Menus
            .Include(m => m.MenuPermissions)
            .ThenInclude(mp => mp.Permission)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

    public async Task AddAsync(Menu menu) =>
        await _context.Menus.AddAsync(menu);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}

using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByCodeAsync(string permissionCode) =>
        await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == permissionCode);

    public async Task<IEnumerable<Permission>> GetAllAsync() =>
        await _context.Permissions.ToListAsync();

    public async Task<IEnumerable<Permission>> GetByCodesAsync(IEnumerable<string> permissionCodes) =>
        await _context.Permissions.Where(p => permissionCodes.Contains(p.PermissionCode)).ToListAsync();

    public async Task AddAsync(Permission permission) =>
        await _context.Permissions.AddAsync(permission);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}

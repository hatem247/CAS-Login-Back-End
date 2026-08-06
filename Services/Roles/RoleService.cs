using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Exceptions;
using CAS_Login_Back_End.Models.Responses;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CAS_Login_Back_End.Services.Roles;

/// <summary>
/// Implementation of IRoleService for role management.
/// </summary>
public class RoleService : IRoleService
{
    private readonly CasDbContext _dbContext;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        CasDbContext dbContext,
        ILogger<RoleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new RoleResponse
            {
                RoleId = r.RoleId,
                Name = r.Name,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<RoleResponse> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == roleId && r.IsActive, cancellationToken);

        if (role is null)
        {
            throw new NotFoundException($"Role with ID {roleId} not found.");
        }

        return new RoleResponse
        {
            RoleId = role.RoleId,
            Name = role.Name,
            Description = role.Description
        };
    }

    public async Task<RoleResponse> GetAccountRoleAsync(
        int accountId,
        int businessEntityId,
        CancellationToken cancellationToken = default)
    {
        var accountRole = await _dbContext.AccountRoles
            .AsNoTracking()
            .Include(ar => ar.Role)
            .FirstOrDefaultAsync(
                ar => ar.AccountId == accountId && ar.BusinessEntityId == businessEntityId,
                cancellationToken);

        if (accountRole is null || accountRole.Role is null)
        {
            throw new NotFoundException(
                $"No role found for account {accountId} in business entity {businessEntityId}.");
        }

        return new RoleResponse
        {
            RoleId = accountRole.Role.RoleId,
            Name = accountRole.Role.Name,
            Description = accountRole.Role.Description
        };
    }
}

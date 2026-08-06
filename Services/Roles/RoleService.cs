using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Data.Entities;
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
            .Select(r => new RoleResponse
            {
                RoleId = checked((int)r.Id),
                Name = r.RoleName,
                Description = r.BusinessEntity ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<RoleResponse> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            throw new NotFoundException($"Role with ID {roleId} not found.");
        }

        return new RoleResponse
        {
            RoleId = checked((int)role.Id),
            Name = role.RoleName,
            Description = role.BusinessEntity ?? string.Empty
        };
    }

    public async Task<RoleResponse> GetAccountRoleAsync(
        int accountId,
        string businessEntityName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessEntityName))
        {
            throw new NotFoundException("Business entity name is required.");
        }

        var accountRole = await _dbContext.AccountRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ar => ar.AccountId == accountId && ar.BusinessEntityName == businessEntityName,
                cancellationToken);

        if (accountRole is null || accountRole.RoleId is null)
        {
            throw new NotFoundException(
                $"No role found for account {accountId} in business entity '{businessEntityName}'.");
        }

        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == accountRole.RoleId.Value, cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                $"No role found for account {accountId} in business entity '{businessEntityName}'.");
        }

        return new RoleResponse
        {
            RoleId = checked((int)role.Id),
            Name = role.RoleName,
            Description = role.BusinessEntity ?? string.Empty
        };
    }
}

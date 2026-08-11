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
        long businessEntityId,
        CancellationToken cancellationToken = default)
    {
        if (businessEntityId <= 0)
        {
            throw new NotFoundException("Business entity ID is required.");
        }

        var role = await _dbContext.Database
            .SqlQuery<AccountBusinessEntityRole>($"""
                SELECT r.[Id] AS [RoleId],
                       r.[RoleName] AS [Name],
                       r.[BusinessEntity] AS [Description]
                FROM [dbo].[AccountRoles] AS ar
                INNER JOIN [dbo].[Roles] AS r ON r.[Id] = ar.[RoleID]
                INNER JOIN [dbo].[Tbl_BusinessEntity] AS be
                    ON be.[BusinessEntity] = ar.[BusinessEntityName]
                WHERE ar.[AccountID] = {accountId}
                  AND be.[ID] = {businessEntityId}
                """)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(
                $"No role found for account {accountId} in business entity '{businessEntityId}'.");

        return new RoleResponse
        {
            RoleId = checked((int)role.RoleId),
            Name = role.Name,
            Description = role.Description ?? string.Empty
        };
    }

    private sealed class AccountBusinessEntityRole
    {
        public long RoleId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}

using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Data.Entities;
using CAS_Login_Back_End.Exceptions;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CAS_Login_Back_End.Services.BusinessEntities;

/// <summary>
/// Implementation of IBusinessEntityService for business entity management.
/// </summary>
public class BusinessEntityService : IBusinessEntityService
{
    private readonly CasDbContext _dbContext;
    private readonly ILogger<BusinessEntityService> _logger;

    public BusinessEntityService(
        CasDbContext dbContext,
        ILogger<BusinessEntityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<BusinessEntityResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => !string.IsNullOrWhiteSpace(role.BusinessEntity))
            .Select(role => new RoleBusinessEntityRow(role.Id, role.BusinessEntity!))
            .ToListAsync(cancellationToken);

        return ToBusinessEntityResponses(roles);
    }

    public async Task<BusinessEntityResponse> GetByNameAsync(string businessEntityName, CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => !string.IsNullOrWhiteSpace(role.BusinessEntity))
            .Select(role => new RoleBusinessEntityRow(role.Id, role.BusinessEntity!))
            .ToListAsync(cancellationToken);

        var entity = ToBusinessEntityResponses(roles)
            .FirstOrDefault(entity => string.Equals(entity.Name, businessEntityName, StringComparison.OrdinalIgnoreCase));

        if (entity is null)
        {
            throw new NotFoundException($"Business entity '{businessEntityName}' was not found.");
        }

        return entity;
    }

    public async Task<IEnumerable<BusinessEntityResponse>> GetAccountBusinessEntitiesAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var assignedEntityNames = await _dbContext.AccountRoles
            .AsNoTracking()
            .Where(accountRole => accountRole.AccountId == accountId &&
                                  !string.IsNullOrWhiteSpace(accountRole.BusinessEntityName))
            .Select(accountRole => accountRole.BusinessEntityName!)
            .ToListAsync(cancellationToken);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => role.BusinessEntity != null && assignedEntityNames.Contains(role.BusinessEntity))
            .Select(role => new RoleBusinessEntityRow(role.Id, role.BusinessEntity!))
            .ToListAsync(cancellationToken);

        return ToBusinessEntityResponses(roles);
    }

    private static IEnumerable<BusinessEntityResponse> ToBusinessEntityResponses(
        IEnumerable<RoleBusinessEntityRow> roles) => roles
        .GroupBy(role => role.BusinessEntity, StringComparer.OrdinalIgnoreCase)
        .Select(group => new BusinessEntityResponse
        {
            Name = group.First().BusinessEntity,
            Description = group.First().BusinessEntity,
            IsActive = true
        })
        .OrderBy(entity => entity.Name);

    private sealed record RoleBusinessEntityRow(long Id, string BusinessEntity);
}

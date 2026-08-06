using CAS_Login_Back_End.Data;
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
        var entities = await _dbContext.BusinessEntities
            .AsNoTracking()
            .Where(be => be.IsActive)
            .Select(be => new BusinessEntityResponse
            {
                BusinessEntityId = be.BusinessEntityId,
                Name = be.Name,
                Description = be.Description,
                IsActive = be.IsActive
            })
            .ToListAsync(cancellationToken);

        return entities;
    }

    public async Task<BusinessEntityResponse> GetByIdAsync(int businessEntityId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.BusinessEntities
            .AsNoTracking()
            .FirstOrDefaultAsync(be => be.BusinessEntityId == businessEntityId, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException($"Business entity with ID {businessEntityId} not found.");
        }

        return new BusinessEntityResponse
        {
            BusinessEntityId = entity.BusinessEntityId,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task<IEnumerable<BusinessEntityResponse>> GetAccountBusinessEntitiesAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AccountRoles
            .AsNoTracking()
            .Where(ar => ar.AccountId == accountId)
            .Select(ar => ar.BusinessEntity)
            .Where(be => be.IsActive)
            .Distinct()
            .Select(be => new BusinessEntityResponse
            {
                BusinessEntityId = be.BusinessEntityId,
                Name = be.Name,
                Description = be.Description,
                IsActive = be.IsActive
            })
            .ToListAsync(cancellationToken);

        return entities;
    }
}

using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Exceptions;
using CAS_Login_Back_End.Models.Responses;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CAS_Login_Back_End.Services.Authentication;

/// <summary>
/// Implementation of IAuthService for authentication operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly CasDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        CasDbContext dbContext,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        string email,
        string password,
        int businessEntityId,
        string businessEntityName,
        CancellationToken cancellationToken = default)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        // Find account by email
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        if (account is null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!account.IsActive)
        {
            _logger.LogWarning("Login attempt with inactive account: {AccountId}", account.AccountId);
            throw new UnauthorizedException("Account is inactive.");
        }

        // Get login credentials
        var login = await _dbContext.Logins
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.AccountId == account.AccountId, cancellationToken);

        if (login is null)
        {
            _logger.LogWarning("No login record found for account: {AccountId}", account.AccountId);
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, login.PasswordHash))
        {
            _logger.LogWarning("Invalid password for account: {AccountId}", account.AccountId);
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Retrieve role for this business entity
        var accountRole = await _dbContext.AccountRoles
            .AsNoTracking()
            .Include(ar => ar.Role)
            .FirstOrDefaultAsync(
                ar => ar.AccountId == account.AccountId && ar.BusinessEntityId == businessEntityId,
                cancellationToken);

        if (accountRole is null || accountRole.Role is null)
        {
            _logger.LogWarning(
                "No role found for account {AccountId} in business entity {BusinessEntityId}",
                account.AccountId,
                businessEntityId);
            throw new UnauthorizedException(
                "You do not have access to this business entity.");
        }

        // Generate tokens
        var ssoToken = _tokenService.GenerateSsoToken(account.AccountId);
        var systemToken = _tokenService.GenerateSystemToken(
            account.AccountId,
            account.Email,
            account.FullNameEn,
            account.FullNameAr,
            businessEntityId,
            businessEntityName,
            accountRole.Role.Name);

        _logger.LogInformation("Successful login for account: {AccountId}", account.AccountId);

        return new LoginResponse
        {
            SsoToken = ssoToken,
            SystemToken = systemToken,
            SsoExpiresIn = 28800, // 8 hours in seconds
            SystemExpiresIn = 3600, // 1 hour in seconds
            Profile = new ProfileResponse
            {
                AccountId = account.AccountId,
                Email = account.Email,
                FullNameEn = account.FullNameEn,
                FullNameAr = account.FullNameAr,
                IsActive = account.IsActive
            },
            Role = new RoleResponse
            {
                RoleId = accountRole.Role.RoleId,
                Name = accountRole.Role.Name,
                Description = accountRole.Role.Description
            }
        };
    }

    public async Task<ExchangeTokenResponse> ExchangeTokenAsync(
        string ssoToken,
        int businessEntityId,
        string businessEntityName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssoToken))
        {
            throw new ValidationException("SSO token is required.");
        }

        // Validate SSO token
        if (!_tokenService.ValidateToken(ssoToken))
        {
            throw new UnauthorizedException("Invalid or expired SSO token.");
        }

        // Read token type
        var tokenType = _tokenService.ReadTokenType(ssoToken);
        if (tokenType != "SSO")
        {
            throw new UnauthorizedException("Token is not an SSO token.");
        }

        // Read AccountId from token
        var accountId = _tokenService.ReadAccountId(ssoToken);
        if (!accountId.HasValue)
        {
            throw new UnauthorizedException("Invalid token claims.");
        }

        // Retrieve account
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId.Value, cancellationToken);

        if (account is null || !account.IsActive)
        {
            throw new UnauthorizedException("Account not found or inactive.");
        }

        // Retrieve role for business entity
        var accountRole = await _dbContext.AccountRoles
            .AsNoTracking()
            .Include(ar => ar.Role)
            .FirstOrDefaultAsync(
                ar => ar.AccountId == accountId.Value && ar.BusinessEntityId == businessEntityId,
                cancellationToken);

        if (accountRole is null || accountRole.Role is null)
        {
            throw new UnauthorizedException(
                "You do not have access to this business entity.");
        }

        // Generate System token
        var systemToken = _tokenService.GenerateSystemToken(
            account.AccountId,
            account.Email,
            account.FullNameEn,
            account.FullNameAr,
            businessEntityId,
            businessEntityName,
            accountRole.Role.Name);

        _logger.LogInformation(
            "Token exchanged for account {AccountId} in business entity {BusinessEntityId}",
            accountId,
            businessEntityId);

        return new ExchangeTokenResponse
        {
            SystemToken = systemToken,
            ExpiresIn = 3600 // 1 hour in seconds
        };
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ValidateTokenResponse { IsValid = false };
        }

        if (!_tokenService.ValidateToken(token))
        {
            return new ValidateTokenResponse { IsValid = false };
        }

        var accountId = _tokenService.ReadAccountId(token);
        var tokenType = _tokenService.ReadTokenType(token);
        var expiration = _tokenService.ReadExpiration(token);

        // Verify account exists and is active
        if (accountId.HasValue)
        {
            var account = await _dbContext.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId.Value, cancellationToken);

            if (account is null || !account.IsActive)
            {
                return new ValidateTokenResponse { IsValid = false };
            }
        }

        return new ValidateTokenResponse
        {
            IsValid = true,
            AccountId = accountId,
            TokenType = tokenType,
            ExpiresAt = expiration
        };
    }
}

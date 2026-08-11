using System;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Services.Interfaces;
using CAS_Login_Back_End.Models.Responses;
using CAS_Login_Back_End.Models.Authentication;
using CAS_Login_Back_End.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace CAS_Login_Back_End.Services.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly CasDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IAccountIdentityService _accountIdentityService;

        public AuthService(
            CasDbContext dbContext,
            ITokenService tokenService,
            IAccountIdentityService accountIdentityService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _accountIdentityService = accountIdentityService ?? throw new ArgumentNullException(nameof(accountIdentityService));
        }

        public async Task<LoginResponse> LoginAsync(
            string email,
            string password,
            long businessEntityId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Password is required.");

            if (businessEntityId <= 0)
                throw new ValidationException("Business entity ID is required.");

            // Login is the single credential source. Profile data is resolved
            // from Account_Info by the Login.AccountId after authentication.
            var login = await _dbContext.Logins
                .AsNoTracking()
                .SingleOrDefaultAsync(l => l.Email == email, cancellationToken);

            if (login is null)
                throw new UnauthorizedException("Invalid email or password.");

            var account = await GetActiveAccountInfoAsync(login.AccountId, cancellationToken);

            if (!account.IsActive)
                throw new UnauthorizedException("Account is inactive.");

            if (!VerifyPassword(password, login.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var businessEntity = await GetAuthorizedBusinessEntityAsync(
                account.Id, businessEntityId, cancellationToken);

            var ssoToken = _tokenService.GenerateSsoToken(account.Id, account.NationalId);

            var jwtCreatedAt = DateTime.UtcNow;
            var jwtToken = _tokenService.GenerateSystemToken(
                new SystemTokenDescriptor
                {
                    AccountId = account.Id,
                    Email = account.Email,
                    NationalId = account.NationalId,
                    Phone = account.Phone,
                    City = account.City,
                    FullNameEn = account.FullNameEn ?? string.Empty,
                    FullNameAr = account.FullNameAr ?? string.Empty,
                    AccountCreatedAt = account.CreatedAt,
                    CreatedAt = jwtCreatedAt,
                    IsActive = account.IsActive,
                    StatusId = account.StatusId,
                    GovernoratesId = account.GovernoratesId,
                    BusinessEntityId = businessEntity.Id,
                    BusinessEntityName = businessEntity.Name,
                    Role = businessEntity.RoleName
                });

            return new LoginResponse
            {
                SsoToken = ssoToken,
                JwtToken = jwtToken,

                AccountId = account.Id,
                Email = account.Email,
                FullNameEn = account.FullNameEn ?? string.Empty,
                FullNameAr = account.FullNameAr ?? string.Empty,

                Role = businessEntity.RoleName,

                BusinessEntityId = businessEntity.Id,
                BusinessEntityName = businessEntity.Name,

                SsoExpiresAt = DateTime.UtcNow.AddHours(8),
                JwtCreatedAt = jwtCreatedAt,
                JwtExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<ExchangeTokenResponse> ExchangeTokenAsync(
            string ssoToken,
            long businessEntityId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ssoToken))
                throw new ValidationException("SSO token is required.");

            if (businessEntityId <= 0)
                throw new ValidationException("Business entity ID is required.");

            ClaimsPrincipal principal;

            try
            {
                principal = _tokenService.GetPrincipal(ssoToken);
            }
            catch (Exception)
            {
                throw new UnauthorizedException("Invalid or expired SSO token.");
            }

            if (!string.Equals(principal.FindFirst("TokenType")?.Value, "SSO", StringComparison.Ordinal))
                throw new UnauthorizedException("Token is not an SSO token.");

            var accountId = await _accountIdentityService.ResolveAccountIdAsync(principal, cancellationToken);
            if (!accountId.HasValue)
                throw new UnauthorizedException("Invalid SSO token claims.");

            var login = await _dbContext.Logins
                .AsNoTracking()
                .SingleOrDefaultAsync(login => login.AccountId == accountId.Value, cancellationToken)
                ?? throw new UnauthorizedException("Login record not found.");

            var account = await GetActiveAccountInfoAsync(login.AccountId, cancellationToken);

            var businessEntity = await GetAuthorizedBusinessEntityAsync(
                account.Id, businessEntityId, cancellationToken);

            var jwtCreatedAt = DateTime.UtcNow;
            var jwtToken = _tokenService.GenerateSystemToken(new SystemTokenDescriptor
            {
                AccountId = account.Id,
                Email = account.Email,
                NationalId = account.NationalId,
                Phone = account.Phone,
                City = account.City,
                FullNameEn = account.FullNameEn ?? string.Empty,
                FullNameAr = account.FullNameAr ?? string.Empty,
                AccountCreatedAt = account.CreatedAt,
                CreatedAt = jwtCreatedAt,
                IsActive = account.IsActive,
                StatusId = account.StatusId,
                GovernoratesId = account.GovernoratesId,
                BusinessEntityId = businessEntity.Id,
                BusinessEntityName = businessEntity.Name,
                Role = businessEntity.RoleName
            });

            return new ExchangeTokenResponse
            {
                JwtToken = jwtToken,
                Role = businessEntity.RoleName,
                BusinessEntityId = businessEntity.Id,
                BusinessEntityName = businessEntity.Name,
                JwtCreatedAt = jwtCreatedAt,
                JwtExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<ValidateTokenResponse> ValidateTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new ValidateTokenResponse { IsValid = false };

            ClaimsPrincipal principal;

            try
            {
                principal = _tokenService.GetPrincipal(token);
            }
            catch (SecurityTokenExpiredException)
            {
                return new ValidateTokenResponse { IsValid = false, IsExpired = true };
            }
            catch (Exception)
            {
                return new ValidateTokenResponse { IsValid = false };
            }

            var accountId = await _accountIdentityService.ResolveAccountIdAsync(principal, cancellationToken);
            if (!accountId.HasValue)
                return new ValidateTokenResponse { IsValid = false };

            return new ValidateTokenResponse
            {
                IsValid = true,
                IsExpired = false,
                TokenType = principal.FindFirst("TokenType")?.Value ?? string.Empty,
                AccountId = accountId.Value,
                CreatedAt = ReadCreatedAt(principal)
            };
        }

        private static bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            try
            {
                if (BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash))
                    return true;
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Non-BCrypt legacy credentials are compared directly below.
            }

            var enteredValue = Encoding.UTF8.GetBytes(enteredPassword);
            var storedValue = Encoding.UTF8.GetBytes(storedPasswordHash);

            return enteredValue.Length == storedValue.Length &&
                CryptographicOperations.FixedTimeEquals(enteredValue, storedValue);
        }

        private async Task<Data.Entities.AccountInfo> GetActiveAccountInfoAsync(
            long accountId,
            CancellationToken cancellationToken)
        {
            var account = await _dbContext.AccountInfos
                .AsNoTracking()
                .SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken)
                ?? throw new UnauthorizedException("Account profile not found.");

            if (!account.IsActive)
                throw new UnauthorizedException("Account is inactive.");

            return account;
        }

        private async Task<AuthorizedBusinessEntity> GetAuthorizedBusinessEntityAsync(
            long accountId,
            long businessEntityId,
            CancellationToken cancellationToken)
        {
            var businessEntity = await _dbContext.Database
                .SqlQuery<AuthorizedBusinessEntity>($"""
                    SELECT be.[ID] AS [Id],
                           be.[BusinessEntity] AS [Name],
                           ISNULL(r.[RoleName], '') AS [RoleName]
                    FROM [dbo].[Tbl_BusinessEntity] AS be
                    INNER JOIN [dbo].[AccountRoles] AS ar
                        ON ar.[BusinessEntityName] = be.[BusinessEntity]
                    LEFT JOIN [dbo].[Roles] AS r ON r.[Id] = ar.[RoleID]
                    WHERE ar.[AccountID] = {accountId}
                      AND be.[ID] = {businessEntityId}
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            return businessEntity
                ?? throw new UnauthorizedException("You do not have access to this business entity.");
        }

        private sealed class AuthorizedBusinessEntity
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string RoleName { get; init; } = string.Empty;
        }

        private static DateTime? ReadCreatedAt(ClaimsPrincipal principal)
        {
            var createdAtValue = principal.FindFirst("CreatedAt")?.Value;

            return DateTime.TryParse(
                createdAtValue,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var createdAt)
                ? createdAt
                : null;
        }
    }
}

using System;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Data.Entities;
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

        public AuthService(CasDbContext dbContext, ITokenService tokenService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<LoginResponse> LoginAsync(
            string email,
            string password,
            string businessEntityName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Password is required.");

            var login = await _dbContext.Logins
                .AsNoTracking()
                .Include(l => l.Account)
                    .ThenInclude(a => a.StudentExtension)
                .SingleOrDefaultAsync(l => l.Email == email, cancellationToken);

            Account account;
            string credentialEmail;
            string storedPasswordHash;
            string credentialSource;

            if (login is not null)
            {
                // Student and other Login-backed accounts authenticate using Login.
                account = login.Account;
                credentialEmail = login.Email;
                storedPasswordHash = login.PasswordHash;
                credentialSource = "Login";
            }
            else
            {
                // Legacy accounts without a Login row authenticate using Account.
                account = await _dbContext.Accounts
                    .AsNoTracking()
                    .SingleOrDefaultAsync(a => a.Email == email, cancellationToken)
                    ?? throw new UnauthorizedException("Invalid email or password.");
                credentialEmail = account.Email;
                storedPasswordHash = account.PasswordHash;
                credentialSource = "Account";
            }

            if (!account.IsActive)
                throw new UnauthorizedException("Account is inactive.");

            if (!VerifyPassword(password, storedPasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var accountRole = await _dbContext.AccountRoles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    ar => ar.AccountId == account.Id &&
                          ar.BusinessEntityName == businessEntityName,
                    cancellationToken)
                ?? throw new UnauthorizedException("You do not have access to this business entity.");

            string roleName = string.Empty;

            if (accountRole.RoleId.HasValue)
            {
                var roleEntity = await _dbContext.Roles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(r => r.Id == accountRole.RoleId.Value, cancellationToken);

                roleName = roleEntity?.RoleName ?? string.Empty;
            }

            // Fallback to account.Role if no role found via AccountRole
            if (string.IsNullOrWhiteSpace(roleName))
            {
                var accountWithRole = await _dbContext.Accounts
                    .AsNoTracking()
                    .Include(a => a.Role)
                    .SingleOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

                roleName = accountWithRole?.Role?.RoleName ?? string.Empty;
            }

            var ssoToken = _tokenService.GenerateSsoToken(account.Id, credentialSource);

            var jwtToken = _tokenService.GenerateSystemToken(
                new SystemTokenDescriptor
                {
                    AccountId = account.Id,
                    Email = credentialEmail,
                    FullNameEn = account.FullNameEn,
                    FullNameAr = account.FullNameAr,
                    BusinessEntityName = businessEntityName,
                    Role = roleName,
                    CredentialSource = credentialSource
                });

            return new LoginResponse
            {
                SsoToken = ssoToken,
                JwtToken = jwtToken,

                AccountId = account.Id,
                Email = credentialEmail,
                FullNameEn = account.FullNameEn,
                FullNameAr = account.FullNameAr,

                Role = roleName,

                BusinessEntityName = businessEntityName,

                SsoExpiresAt = DateTime.UtcNow.AddHours(8),
                JwtExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<ExchangeTokenResponse> ExchangeTokenAsync(
            string ssoToken,
            string businessEntityName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ssoToken))
                throw new ValidationException("SSO token is required.");

            if (string.IsNullOrWhiteSpace(businessEntityName))
                throw new ValidationException("Business entity name is required.");

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

            var accountIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(accountIdClaim, out var accountId))
                throw new UnauthorizedException("Invalid SSO token claims.");

            var account = await _dbContext.Accounts
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken)
                ?? throw new UnauthorizedException("Account not found or inactive.");

            if (!account.IsActive)
                throw new UnauthorizedException("Account not found or inactive.");

            var accountRole = await _dbContext.AccountRoles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    ar => ar.AccountId == account.Id && ar.BusinessEntityName == businessEntityName,
                    cancellationToken)
                ?? throw new UnauthorizedException("You do not have access to this business entity.");

            var roleName = string.Empty;

            if (accountRole.RoleId.HasValue)
            {
                roleName = await _dbContext.Roles
                    .AsNoTracking()
                    .Where(role => role.Id == accountRole.RoleId.Value)
                    .Select(role => role.RoleName)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                roleName = await _dbContext.Accounts
                    .AsNoTracking()
                    .Where(a => a.Id == account.Id)
                    .Select(a => a.Role.RoleName)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? string.Empty;
            }

            var credentialSource = string.Equals(
                principal.FindFirst("CredentialSource")?.Value,
                "Account",
                StringComparison.Ordinal)
                ? "Account"
                : "Login";

            var email = credentialSource == "Account"
                ? account.Email
                : await _dbContext.Logins
                    .AsNoTracking()
                    .Where(login => login.AccountId == account.Id)
                    .Select(login => login.Email)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? account.Email;

            var jwtToken = _tokenService.GenerateSystemToken(new SystemTokenDescriptor
            {
                AccountId = account.Id,
                Email = email,
                FullNameEn = account.FullNameEn,
                FullNameAr = account.FullNameAr,
                BusinessEntityName = businessEntityName,
                Role = roleName,
                CredentialSource = credentialSource
            });

            return new ExchangeTokenResponse
            {
                JwtToken = jwtToken,
                Role = roleName,
                BusinessEntityName = businessEntityName,
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

            var accountIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(accountIdClaim, out var accountId))
                return new ValidateTokenResponse { IsValid = false };

            var accountIsActive = await _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(account => account.Id == accountId && account.IsActive, cancellationToken);

            if (!accountIsActive)
                return new ValidateTokenResponse { IsValid = false };

            return new ValidateTokenResponse
            {
                IsValid = true,
                IsExpired = false,
                TokenType = principal.FindFirst("TokenType")?.Value ?? string.Empty,
                AccountId = accountId
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
    }
}

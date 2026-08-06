using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Data.Entities;
using CAS_Login_Back_End.Services.Interfaces;
using CAS_Login_Back_End.Models.Responses;
using CAS_Login_Back_End.Models.Authentication;
using CAS_Login_Back_End.Exceptions;

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

            var account = await _dbContext.Accounts
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Email == email, cancellationToken)
                ?? throw new UnauthorizedException("Invalid email or password.");

            if (!account.IsActive)
                throw new UnauthorizedException("Account is inactive.");

            var login = await _dbContext.Logins
                .AsNoTracking()
                .SingleOrDefaultAsync(l => l.AccountId == account.Id, cancellationToken)
                ?? throw new UnauthorizedException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(password, login.PasswordHash))
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

            var ssoToken = _tokenService.GenerateSsoToken(account.Id);

            var jwtToken = _tokenService.GenerateSystemToken(
                new SystemTokenDescriptor
                {
                    AccountId = account.Id,
                    Email = account.Email,
                    FullNameEn = account.FullNameEn,
                    FullNameAr = account.FullNameAr,
                    BusinessEntityName = businessEntityName,
                    Role = roleName
                });

            return new LoginResponse
            {
                SsoToken = ssoToken,
                JwtToken = jwtToken,

                AccountId = account.Id,
                Email = account.Email,
                FullNameEn = account.FullNameEn,
                FullNameAr = account.FullNameAr,

                Role = roleName,

                BusinessEntityName = businessEntityName,

                SsoExpiresAt = DateTime.UtcNow.AddHours(8),
                JwtExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public Task<ExchangeTokenResponse> ExchangeTokenAsync(string ssoToken, string businessEntityName, CancellationToken cancellationToken = default)
        {
            // Keep existing behavior in TokenService; delegate if implemented.
            throw new NotImplementedException();
        }

        public Task<ValidateTokenResponse> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

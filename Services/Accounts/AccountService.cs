using CAS_Login_Back_End.Data;
using CAS_Login_Back_End.Data.Entities;
using CAS_Login_Back_End.Exceptions;
using CAS_Login_Back_End.Models.Requests;
using CAS_Login_Back_End.Models.Responses;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CAS_Login_Back_End.Services.Accounts;

/// <summary>
/// Implementation of IAccountService for account management.
/// </summary>
public class AccountService : IAccountService
{
    private readonly CasDbContext _dbContext;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        CasDbContext dbContext,
        ILogger<AccountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ProfileResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        ValidateRegisterRequest(request);

        // Check if email already exists
        var existingAccount = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

        if (existingAccount is not null)
        {
            throw new ValidationException("Email is already registered.");
        }

        // Create account
        var account = new Account
        {
            Email = request.Email,
            FullNameEn = request.FullNameEn,
            FullNameAr = request.FullNameAr,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create login record with hashed password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var login = new Login
        {
            AccountId = account.AccountId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Logins.Add(login);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New account registered: {AccountId} ({Email})", account.AccountId, account.Email);

        return new ProfileResponse
        {
            AccountId = account.AccountId,
            Email = account.Email,
            FullNameEn = account.FullNameEn,
            FullNameAr = account.FullNameAr,
            IsActive = account.IsActive
        };
    }

    public async Task<ProfileResponse> GetProfileAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account with ID {accountId} not found.");
        }

        return new ProfileResponse
        {
            AccountId = account.AccountId,
            Email = account.Email,
            FullNameEn = account.FullNameEn,
            FullNameAr = account.FullNameAr,
            IsActive = account.IsActive
        };
    }

    public async Task<ProfileResponse> UpdateProfileAsync(
        int accountId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.FullNameEn))
        {
            throw new ValidationException("FullNameEn is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullNameAr))
        {
            throw new ValidationException("FullNameAr is required.");
        }

        // Get account
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account with ID {accountId} not found.");
        }

        // Update profile
        account.FullNameEn = request.FullNameEn;
        account.FullNameAr = request.FullNameAr;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated for account: {AccountId}", accountId);

        return new ProfileResponse
        {
            AccountId = account.AccountId,
            Email = account.Email,
            FullNameEn = account.FullNameEn,
            FullNameAr = account.FullNameAr,
            IsActive = account.IsActive
        };
    }

    public async Task ChangePasswordAsync(
        int accountId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ValidationException("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("New password is required.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationException("New password and confirmation do not match.");
        }

        // Get login record
        var login = await _dbContext.Logins
            .FirstOrDefaultAsync(l => l.AccountId == accountId, cancellationToken);

        if (login is null)
        {
            throw new NotFoundException($"Login record for account {accountId} not found.");
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, login.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        // Update password
        login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        login.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for account: {AccountId}", accountId);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

        if (account is null)
        {
            // For security, don't reveal if email exists
            _logger.LogWarning("Forgot password requested for non-existent email: {Email}", request.Email);
            return;
        }

        // TODO: Implement password reset token generation and email sending
        // For now, this is a placeholder
        _logger.LogInformation("Forgot password initiated for account: {AccountId}", account.AccountId);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("New password is required.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationException("New password and confirmation do not match.");
        }

        // TODO: Implement password reset token validation
        // For now, this is a placeholder

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account with email {request.Email} not found.");
        }

        var login = await _dbContext.Logins
            .FirstOrDefaultAsync(l => l.AccountId == account.AccountId, cancellationToken);

        if (login is null)
        {
            throw new NotFoundException($"Login record for account {account.AccountId} not found.");
        }

        login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        login.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset for account: {AccountId}", account.AccountId);
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            errors.Add("Password and confirmation do not match.");
        }

        if (string.IsNullOrWhiteSpace(request.FullNameEn))
        {
            errors.Add("FullNameEn is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullNameAr))
        {
            errors.Add("FullNameAr is required.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Registration validation failed.", errors);
        }
    }
}

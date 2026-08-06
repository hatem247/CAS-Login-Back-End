using CAS_Login_Back_End.Models.Common;
using CAS_Login_Back_End.Models.Requests;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CAS_Login_Back_End.Controllers;

/// <summary>
/// Account controller for user registration, profile management, and password operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <remarks>
    /// POST /api/account/register
    /// </remarks>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<dynamic>>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _accountService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProfileAsync), new { }, 
                ApiResponse<dynamic>.SuccessResponse(result, "Account registered successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            throw;
        }
    }

    /// <summary>
    /// Retrieves current user profile.
    /// </summary>
    /// <remarks>
    /// GET /api/account/profile
    /// Authorization: Bearer {TOKEN}
    /// </remarks>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<dynamic>>> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (accountIdClaim is null || !int.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(ApiResponse<dynamic>.FailureResponse("Unauthorized."));
            }

            var result = await _accountService.GetProfileAsync(accountId, cancellationToken);
            return Ok(ApiResponse<dynamic>.SuccessResponse(result, "Profile retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get profile error");
            throw;
        }
    }

    /// <summary>
    /// Updates current user profile.
    /// </summary>
    /// <remarks>
    /// PUT /api/account/profile
    /// Authorization: Bearer {TOKEN}
    /// </remarks>
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<dynamic>>> UpdateProfileAsync(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (accountIdClaim is null || !int.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(ApiResponse<dynamic>.FailureResponse("Unauthorized."));
            }

            var result = await _accountService.UpdateProfileAsync(accountId, request, cancellationToken);
            return Ok(ApiResponse<dynamic>.SuccessResponse(result, "Profile updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update profile error");
            throw;
        }
    }

    /// <summary>
    /// Changes password for current user.
    /// </summary>
    /// <remarks>
    /// POST /api/account/change-password
    /// Authorization: Bearer {TOKEN}
    /// </remarks>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<dynamic>>> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (accountIdClaim is null || !int.TryParse(accountIdClaim.Value, out var accountId))
            {
                return Unauthorized(ApiResponse<dynamic>.FailureResponse("Unauthorized."));
            }

            await _accountService.ChangePasswordAsync(accountId, request, cancellationToken);
            return Ok(ApiResponse<dynamic>.SuccessResponse(null, "Password changed successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password error");
            throw;
        }
    }

    /// <summary>
    /// Initiates forgot password flow.
    /// </summary>
    /// <remarks>
    /// POST /api/account/forgot-password
    /// </remarks>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<dynamic>>> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _accountService.ForgotPasswordAsync(request, cancellationToken);
            return Ok(ApiResponse<dynamic>.SuccessResponse(null, "If account exists, reset link has been sent."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password error");
            throw;
        }
    }

    /// <summary>
    /// Resets password using reset token.
    /// </summary>
    /// <remarks>
    /// POST /api/account/reset-password
    /// </remarks>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<dynamic>>> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _accountService.ResetPasswordAsync(request, cancellationToken);
            return Ok(ApiResponse<dynamic>.SuccessResponse(null, "Password reset successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error");
            throw;
        }
    }
}

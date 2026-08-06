using CAS_Login_Back_End.Models.Common;
using CAS_Login_Back_End.Models.Requests;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CAS_Login_Back_End.Controllers;

/// <summary>
/// Authentication controller for login, token exchange, and validation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user with email and password, returns SSO and System tokens.
    /// </summary>
    /// <remarks>
    /// POST /api/auth/login
    /// </remarks>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<dynamic>>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.LoginAsync(
                request.Email,
                request.Password,
                request.BusinessEntityId,
                request.BusinessEntityName,
                cancellationToken);

            return Ok(ApiResponse<dynamic>.SuccessResponse(result, "Login successful."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            throw;
        }
    }

    /// <summary>
    /// Exchanges SSO token for a System token for another business entity.
    /// </summary>
    /// <remarks>
    /// POST /api/auth/exchange
    /// Authorization: Bearer {SSO_TOKEN}
    /// </remarks>
    [HttpPost("exchange")]
    public async Task<ActionResult<ApiResponse<dynamic>>> ExchangeTokenAsync(
        [FromBody] ExchangeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var token = authHeader.Replace("Bearer ", "");

            var result = await _authService.ExchangeTokenAsync(
                token,
                request.BusinessEntityId,
                request.BusinessEntityName,
                cancellationToken);

            return Ok(ApiResponse<dynamic>.SuccessResponse(result, "Token exchanged successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token exchange error");
            throw;
        }
    }

    /// <summary>
    /// Validates a token.
    /// </summary>
    /// <remarks>
    /// POST /api/auth/validate
    /// Authorization: Bearer {TOKEN}
    /// </remarks>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<dynamic>>> ValidateTokenAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var token = authHeader.Replace("Bearer ", "");

            var result = await _authService.ValidateTokenAsync(token, cancellationToken);

            return Ok(ApiResponse<dynamic>.SuccessResponse(result, "Token validation complete."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation error");
            throw;
        }
    }
}

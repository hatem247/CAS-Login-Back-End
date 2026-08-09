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
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns both the SSO token and the System JWT.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<dynamic>>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.LoginAsync(
            request.Email,
            request.Password,
            request.BusinessEntityName,
            cancellationToken);

        return Ok(ApiResponse<dynamic>.SuccessResponse(
            result,
            "Login successful."));
    }

    /// <summary>
    /// Exchanges a valid SSO token for a System JWT belonging to another Business Entity.
    /// </summary>
    [HttpPost("switch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<dynamic>>> ExchangeTokenAsync(
        [FromBody] ExchangeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = GetBearerToken();

        var result = await _authService.ExchangeTokenAsync(
            token,
            request.BusinessEntityName,
            cancellationToken);

        return Ok(ApiResponse<dynamic>.SuccessResponse(
            result,
            "Token exchanged successfully."));
    }

    /// <summary>
    /// Validates an SSO or System JWT.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<dynamic>>> ValidateTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var token = GetBearerToken();

        var result = await _authService.ValidateTokenAsync(
            token,
            cancellationToken);

        return Ok(ApiResponse<dynamic>.SuccessResponse(
            result,
            "Token validation completed successfully."));
    }

    /// <summary>
    /// Extracts the Bearer token from the Authorization header.
    /// </summary>
    private string GetBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            throw new UnauthorizedAccessException("Authorization header is missing.");

        const string bearerPrefix = "Bearer ";

        var header = authorizationHeader.ToString();

        if (!header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid Authorization header.");

        var token = header[bearerPrefix.Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("Bearer token is missing.");

        return token;
    }
}

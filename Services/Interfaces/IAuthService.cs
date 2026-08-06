using CAS_Login_Back_End.Models.Responses;

namespace CAS_Login_Back_End.Services.Interfaces;

/// <summary>
/// Service for authentication operations.
/// Handles login, token exchange, and validation flows.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates user with email/password and generates both SSO and System tokens.
    /// </summary>
    Task<LoginResponse> LoginAsync(
        string email,
        string password,
        int businessEntityId,
        string businessEntityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges SSO token for a System token for a different business entity.
    /// </summary>
    Task<ExchangeTokenResponse> ExchangeTokenAsync(
        string ssoToken,
        int businessEntityId,
        string businessEntityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a token is valid.
    /// </summary>
    Task<ValidateTokenResponse> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

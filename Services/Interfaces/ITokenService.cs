using System.Security.Claims;

namespace CAS_Login_Back_End.Services.Interfaces;

/// <summary>
/// Service for JWT token generation, validation, and claim reading.
/// Handles both SSO tokens and System tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an SSO token (identity only, 8 hours expiration).
    /// Contains: AccountId, TokenType
    /// </summary>
    string GenerateSsoToken(int accountId);

    /// <summary>
    /// Generates a System JWT token (1 hour expiration, for ONE business entity).
    /// Contains: AccountId, Email, FullNameEn, FullNameAr, BusinessEntityId, BusinessEntityName, Role, TokenType
    /// </summary>
    string GenerateSystemToken(
        int accountId,
        string email,
        string fullNameEn,
        string fullNameAr,
        int businessEntityId,
        string businessEntityName,
        string roleName);

    /// <summary>
    /// Validates a token and returns whether it's valid.
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Reads all claims from a token.
    /// </summary>
    IEnumerable<Claim> ReadClaims(string token);

    /// <summary>
    /// Reads AccountId claim from token.
    /// </summary>
    int? ReadAccountId(string token);

    /// <summary>
    /// Reads TokenType claim from token.
    /// </summary>
    string? ReadTokenType(string token);

    /// <summary>
    /// Reads expiration from token.
    /// </summary>
    DateTime? ReadExpiration(string token);
}

namespace CAS_Login_Back_End.Models.Responses;

public sealed class ExchangeTokenResponse
{
    public string JwtToken { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string BusinessEntityName { get; init; } = string.Empty;

    public DateTime JwtExpiresAt { get; init; }
}

namespace CAS_Login_Back_End.Models.Responses;

public sealed class LoginResponse
{
    public string SsoToken { get; init; } = string.Empty;

    public string JwtToken { get; init; } = string.Empty;

    public long AccountId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullNameEn { get; init; } = string.Empty;

    public string FullNameAr { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string BusinessEntityName { get; init; } = string.Empty;

    public DateTime SsoExpiresAt { get; init; }

    public DateTime JwtExpiresAt { get; init; }
}

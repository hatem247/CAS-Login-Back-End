namespace CAS_Login_Back_End.Models.Responses;

public sealed class ValidateTokenResponse
{
    public bool IsValid { get; init; }

    public bool IsExpired { get; init; }

    public string TokenType { get; init; } = string.Empty;

    public long AccountId { get; init; }
}
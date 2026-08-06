namespace CAS_Login_Back_End.Models.Responses;

/// <summary>
/// Response model for token validation.
/// </summary>
public class ValidateTokenResponse
{
    public bool IsValid { get; set; }

    public int? AccountId { get; set; }

    public string? TokenType { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

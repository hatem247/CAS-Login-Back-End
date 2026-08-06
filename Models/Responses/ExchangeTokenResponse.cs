namespace CAS_Login_Back_End.Models.Responses;

/// <summary>
/// Response model for exchange token operation.
/// </summary>
public class ExchangeTokenResponse
{
    public string SystemToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }
}

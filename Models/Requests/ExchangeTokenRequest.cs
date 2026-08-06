namespace CAS_Login_Back_End.Models.Requests;

/// <summary>
/// Request model for token exchange using SSO token.
/// </summary>
public class ExchangeTokenRequest
{
    public int BusinessEntityId { get; set; }

    public string BusinessEntityName { get; set; } = string.Empty;
}

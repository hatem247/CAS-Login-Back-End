namespace CAS_Login_Back_End.Models.Responses;

/// <summary>
/// Response model for login operation.
/// Contains both SSO token and System JWT token.
/// </summary>
public class LoginResponse
{
    public string SsoToken { get; set; } = string.Empty;

    public string SystemToken { get; set; } = string.Empty;

    public ProfileResponse Profile { get; set; } = new();

    public RoleResponse Role { get; set; } = new();

    public int SsoExpiresIn { get; set; }

    public int SystemExpiresIn { get; set; }
}

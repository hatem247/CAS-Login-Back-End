namespace CAS_Login_Back_End.Data.Entities;

/// <summary>
/// Represents login credentials for an account.
/// </summary>
public class Login
{
    public int LoginId { get; set; }

    public int AccountId { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
}

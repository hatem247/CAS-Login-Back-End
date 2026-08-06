namespace CAS_Login_Back_End.Data.Entities;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public class Account
{
    public int AccountId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullNameEn { get; set; } = string.Empty;

    public string FullNameAr { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Login> Logins { get; set; } = new List<Login>();

    public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}

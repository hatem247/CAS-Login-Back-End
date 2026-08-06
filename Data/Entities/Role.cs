namespace CAS_Login_Back_End.Data.Entities;

/// <summary>
/// Represents a role that can be assigned to accounts.
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}

namespace CAS_Login_Back_End.Data.Entities;

/// <summary>
/// Represents the assignment of a role to an account within a specific business entity.
/// Every user has EXACTLY ONE role inside every Business Entity.
/// </summary>
public class AccountRole
{
    public int AccountRoleId { get; set; }

    public int AccountId { get; set; }

    public int RoleId { get; set; }

    public int BusinessEntityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public BusinessEntity BusinessEntity { get; set; } = null!;
}

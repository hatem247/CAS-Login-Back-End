namespace CAS_Login_Back_End.Models.Responses;

/// <summary>
/// Response model for user profile information.
/// </summary>
public class ProfileResponse
{
    public int AccountId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullNameEn { get; set; } = string.Empty;

    public string FullNameAr { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

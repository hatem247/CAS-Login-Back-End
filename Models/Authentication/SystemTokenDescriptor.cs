namespace CAS_Login_Back_End.Models.Authentication;

public sealed class SystemTokenDescriptor
{
    public long AccountId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullNameEn { get; init; } = string.Empty;

    public string FullNameAr { get; init; } = string.Empty;

    public string BusinessEntityName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string CredentialSource { get; init; } = string.Empty;
}

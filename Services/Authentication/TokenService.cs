using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CAS_Login_Back_End.Models.Authentication;
using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace CAS_Login_Back_End.Services.Authentication;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateSsoToken(long accountId, string nationalId)
    {
        var claims = new List<Claim>
        {
            new("AccountId", accountId.ToString()),
            new("NationalId", nationalId),
            new("TokenType", "SSO")
        };

        return GenerateToken(claims, DateTime.UtcNow.AddHours(8));
    }

    public string GenerateSystemToken(SystemTokenDescriptor descriptor)
    {
        var createdAt = descriptor.CreatedAt == default ? DateTime.UtcNow : descriptor.CreatedAt;

        var claims = new List<Claim>
        {
            new("AccountId", descriptor.AccountId.ToString()),
            new("Email", descriptor.Email),
            new("NationalId", descriptor.NationalId),
            new("Phone", descriptor.Phone ?? string.Empty),
            new("City", descriptor.City ?? string.Empty),

            new("FullNameEn", descriptor.FullNameEn),
            new("FullNameAr", descriptor.FullNameAr),
            new("CreatedAt", createdAt.ToString("O")),
            new("AccountCreatedAt", descriptor.AccountCreatedAt?.ToString("O") ?? string.Empty),
            new("IsActive", descriptor.IsActive.ToString()),
            new("StatusId", descriptor.StatusId.ToString()),
            new("GovernoratesId", descriptor.GovernoratesId?.ToString() ?? string.Empty),

            new("BusinessEntityId", descriptor.BusinessEntityId.ToString()),
            new("BusinessEntityName", descriptor.BusinessEntityName),

            new("Role", descriptor.Role),

            new("TokenType", "System")
        };

        return GenerateToken(claims, DateTime.UtcNow.AddHours(1));
    }

    public bool ValidateToken(string token)
    {
        try
        {
            GetPrincipal(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ClaimsPrincipal GetPrincipal(string token)
    {
        return _tokenHandler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),

                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],

                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            },
            out _);
    }

    public long? ReadAccountId(string token)
    {
        var claim = GetPrincipal(token)
            .FindFirst("AccountId");

        if (claim is null)
            return null;

        return long.TryParse(claim.Value, out var accountId)
            ? accountId
            : null;
    }

    public string? ReadTokenType(string token)
    {
        return GetPrincipal(token)
            .FindFirst("TokenType")
            ?.Value;
    }

    public DateTime? ReadExpiration(string token)
    {
        var jwt = _tokenHandler.ReadJwtToken(token);
        return jwt.ValidTo;
    }

    private string GenerateToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                GetSigningKey(),
                SecurityAlgorithms.HmacSha256));

        return _tokenHandler.WriteToken(token);
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var key = _configuration["JWT:Key"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT:Key is missing.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}

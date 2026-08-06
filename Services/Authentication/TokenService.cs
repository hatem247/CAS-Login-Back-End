using CAS_Login_Back_End.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CAS_Login_Back_End.Services.Authentication;

/// <summary>
/// Implementation of ITokenService for JWT token generation and validation.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateSsoToken(int accountId)
    {
        var key = GetSecurityKey();
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new("TokenType", "SSO")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8), // SSO expires in 8 hours
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateSystemToken(
        int accountId,
        string email,
        string fullNameEn,
        string fullNameAr,
        int businessEntityId,
        string businessEntityName,
        string roleName)
    {
        var key = GetSecurityKey();
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new(ClaimTypes.Email, email),
            new("FullNameEn", fullNameEn),
            new("FullNameAr", fullNameAr),
            new("BusinessEntityId", businessEntityId.ToString()),
            new("BusinessEntityName", businessEntityName),
            new(ClaimTypes.Role, roleName),
            new("TokenType", "System")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1), // System token expires in 1 hour
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var key = GetSecurityKey();
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<Claim> ReadClaims(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }

    public int? ReadAccountId(string token)
    {
        var claims = ReadClaims(token);
        var accountIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        return int.TryParse(accountIdClaim?.Value, out var accountId) ? accountId : null;
    }

    public string? ReadTokenType(string token)
    {
        var claims = ReadClaims(token);
        return claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
    }

    public DateTime? ReadExpiration(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    private SecurityKey GetSecurityKey()
    {
        var key = _configuration["JWT:Secret"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("JWT Secret is not configured.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}

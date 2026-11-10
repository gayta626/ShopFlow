using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public (string AccessToken, DateTime ExpiresAt) CreateToken(User user)
    {
        string? issuer = _configuration["Jwt:Issuer"];
        string? audience = _configuration["Jwt:Audience"];
        string? key = _configuration["Jwt:Key"];
        string? expirationMinutes = _configuration["Jwt:ExpirationMinutes"];


        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience va Jwt:Key phai co gia tri.");
        }

        if (!int.TryParse(expirationMinutes, out int minutes) || minutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes phai la so nguyen duong.");
        }
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new[]
        {
          new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
          new Claim(ClaimTypes.Role, user.Role.ToString()),
          new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };


        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Jwt:Key phai la chuoi Base64 hop le.");
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key phai co it nhat 32 byte sau khi giai ma Base64.");
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

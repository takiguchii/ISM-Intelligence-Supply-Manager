using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ISM.Application.Services;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyString = _configuration["Jwt:Key"] ?? "super_secret_key_that_must_be_at_least_32_characters_long_123456";
        var key = Encoding.UTF8.GetBytes(keyString);
        var issuer = _configuration["Jwt:Issuer"] ?? "ism-api";
        var audience = _configuration["Jwt:Audience"] ?? "ism-app";
        var expireHoursString = _configuration["Jwt:ExpireHours"] ?? "8";
        var expireHours = double.Parse(expireHoursString);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expireHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

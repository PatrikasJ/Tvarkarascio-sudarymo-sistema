using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Scheduler.Api.Domain;

namespace Scheduler.Api.Security;

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime Expiration) CreateToken(User user)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer missing");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience missing");
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing");
        
        if(Encoding.UTF8.GetByteCount(key)<32) throw new InvalidOperationException("JWT key must contain atleast 32 bytes");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiration,signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }
}
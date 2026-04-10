using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Project.Services.Security;

public interface ITokenProvider
{
    string CreateToken(User user);
}

public sealed class TokenProvider : ITokenProvider
{
    private readonly IConfiguration _configuration;

    public TokenProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(User user)
    {
        var secretKey = _configuration["JwtToken"]
            ?? throw new InvalidOperationException("Missing configuration value 'JwtToken'.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing configuration value 'Jwt:Issuer'.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Missing configuration value 'Jwt:Audience'.");
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]
            ?? throw new InvalidOperationException("Missing configuration value 'Jwt:ExpiryMinutes'."));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };


        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

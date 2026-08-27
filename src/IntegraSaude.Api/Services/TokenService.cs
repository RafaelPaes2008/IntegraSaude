using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IntegraSaude.Api.Data;
using IntegraSaude.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace IntegraSaude.Api.Services;

public class TokenService(IConfiguration config, AppDbContext db)
{
    public async Task<(string AccessToken, string RefreshToken, int ExpiresInMinutes)> CreateAsync(
        ApplicationUser user,
        IList<string> roles,
        bool rememberMe)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ausente.");
        var minutes = rememberMe
            ? config.GetValue("Jwt:RememberMeDays", 7) * 24 * 60
            : config.GetValue("Jwt:ExpiresMinutes", 120);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Id),
            new("nome", user.NomeCompleto),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Sha256(refresh),
            ExpiraEm = DateTime.UtcNow.AddDays(rememberMe ? 14 : 1)
        });
        await db.SaveChangesAsync();
        return (access, refresh, minutes);
    }

    public static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using laoyu_blog_backend.Models;
using laoyu_blog_backend.Options;
using Microsoft.IdentityModel.Tokens;

namespace laoyu_blog_backend.Services.Auth;

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public JwtTokenResult CreateToken(
        ApplicationUser user,
        IList<string> roles)
    {
        var issuedAtUtc = DateTime.UtcNow;

        var expiresAtUtc = issuedAtUtc.AddMinutes(
            _options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),
            new(
                JwtRegisteredClaimNames.UniqueName,
                user.UserName ?? user.Email ?? user.Id),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(
            accessToken,
            expiresAtUtc);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PrestexaAPI.Services
{
    public class AuthTokenService : IAuthTokenService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        private const int FinalTokenHours = 8;

        public AuthTokenService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string> CreateFinalJwtTokenAsync(User user, string portal)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var normalizedPortal = NormalizePortal(portal);

            var now = DateTime.UtcNow;

            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.Id,
                Portal = normalizedPortal,
                CreatedAt = now,
                ExpiresAt = now.AddHours(FinalTokenHours),
                RevokedAt = null,
                RevocationReason = null
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim("SessionId", session.SessionId.ToString()),
                new Claim("Portal", normalizedPortal),
                new Claim("CompanyNmlsNumber", user.CompanyNmlsNumber ?? ""),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: session.ExpiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string NormalizePortal(string portal)
        {
            var normalized = portal.Trim().ToLowerInvariant();

            return normalized switch
            {
                "borrower" => "borrower",
                "rea" => "rea",
                "los" => "los",
                _ => "los"
            };
        }
    }
}
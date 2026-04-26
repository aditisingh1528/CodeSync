using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services
{
    // JwtService creates signed JWT tokens for authenticated users.
    // A JWT has 3 parts: Header.Payload.Signature
    public class JwtService : IJwtService
    {
        // Cache config values at construction time — no need to re-read
        // appsettings.json on every single token generation call
        private readonly string             _key;
        private readonly string             _issuer;
        private readonly string             _audience;
        private readonly int                _expiryMinutes;
        private readonly SymmetricSecurityKey _signingKey;

        public JwtService(IConfiguration config)
        {
            _key           = config["Jwt:Key"]!;
            _issuer        = config["Jwt:Issuer"]!;
            _audience      = config["Jwt:Audience"]!;
            _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"]!);

            // Build the signing key once and reuse it — same result, less work
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        }

        public string GenerateToken(User user)
        {
            // 1. CLAIMS — data embedded inside the token
            //    Anyone can READ claims from a token (they are not secret)
            //    but they CANNOT forge them — the signature would break
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),      // user's DB id
                new Claim(ClaimTypes.Name,           user.Username),           // username
                new Claim(ClaimTypes.Email,          user.Email),              // email
                new Claim(ClaimTypes.Role,           user.Role),               // "User" or "Admin"
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // unique token id
            };

            // 2. SIGNING CREDENTIALS — signs the token with our cached key
            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            // 3. BUILD and RETURN the token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                Issuer             = _issuer,
                Audience           = _audience,
                SigningCredentials = credentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token   = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);   // "xxxxx.yyyyy.zzzzz"
        }
    }
}

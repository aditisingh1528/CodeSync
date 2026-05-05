using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services
{
    public class JwtService : IJwtService
    {
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

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        }

        public string GenerateToken(User user)
        {
            // CLAIMS 
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),      // user's DB id
                new Claim(ClaimTypes.Name,           user.Username),           // username
                new Claim(ClaimTypes.Email,          user.Email),              // email
                new Claim(ClaimTypes.Role,           user.Role),               // "User" or "Admin"
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // unique token id
            };

            // SIGNING CREDENTIALS
            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            // BUILD and RETURN the token
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
            return handler.WriteToken(token); 
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace AuthService.Tests
{
    // ---------------------------------------------------------------
    // Unit tests for JwtService
    //
    // We build a real IConfiguration in-memory so we can test the
    // actual token generation without needing appsettings.json files.
    // ---------------------------------------------------------------
    [TestFixture]
    public class JwtServiceTests
    {
        private JwtService    _jwtService = null!;
        private IConfiguration _config    = null!;

        // These mirror what we put in appsettings.json
        private const string TestKey      = "ThisIsATestSecretKeyForJwtTokenTesting@2024!!";
        private const string TestIssuer   = "AuthService";
        private const string TestAudience = "AuthServiceClients";
        private const int    TestExpiry   = 60;

        [SetUp]
        public void SetUp()
        {
            // Build an in-memory config that looks exactly like appsettings.json
            var configData = new Dictionary<string, string?>
            {
                { "Jwt:Key",           TestKey           },
                { "Jwt:Issuer",        TestIssuer        },
                { "Jwt:Audience",      TestAudience      },
                { "Jwt:ExpiryMinutes", TestExpiry.ToString() }
            };

            _config     = new ConfigurationBuilder()
                              .AddInMemoryCollection(configData)
                              .Build();
            _jwtService = new JwtService(_config);
        }

        // Helper: decode a token string back into a JwtSecurityToken so we can inspect claims
        private JwtSecurityToken DecodeToken(string tokenString)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(tokenString);
        }

        // Helper: fully VALIDATE (verify signature + claims) and return the ClaimsPrincipal
        private ClaimsPrincipal ValidateToken(string tokenString)
        {
            var handler = new JwtSecurityTokenHandler();
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = TestIssuer,
                ValidAudience            = TestAudience,
                IssuerSigningKey         = key,
                ClockSkew                = TimeSpan.Zero
            };

            return handler.ValidateToken(tokenString, validationParams, out _);
        }

        // -----------------------------------------------------------

        [Test]
        public void GenerateToken_ShouldReturnNonEmptyString()
        {
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);

            Assert.That(token, Is.Not.Null.And.Not.Empty, "Token should be a non-empty string");
        }

        [Test]
        public void GenerateToken_ShouldReturnValidJwtFormat()
        {
            // A JWT always has exactly 3 parts separated by dots: header.payload.signature
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);
            var parts = token.Split('.');

            Assert.That(parts.Length, Is.EqualTo(3), "JWT must have exactly 3 dot-separated parts");
        }

        [Test]
        public void GenerateToken_ShouldContainCorrectUserId()
        {
            var user  = new User { Id = 42, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);
            var jwt   = DecodeToken(token);

            var nameIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "nameid" ||
                              c.Type == ClaimTypes.NameIdentifier ||
                              c.Type == JwtRegisteredClaimNames.NameId);

            Assert.That(nameIdClaim?.Value, Is.EqualTo("42"), "Token should embed the user's Id");
        }

        [Test]
        public void GenerateToken_ShouldContainCorrectEmail()
        {
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);
            var jwt   = DecodeToken(token);

            var emailClaim = jwt.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email || c.Type == "email");

            Assert.That(emailClaim?.Value, Is.EqualTo("alice@example.com"));
        }

        [Test]
        public void GenerateToken_ShouldContainCorrectRole()
        {
            var adminUser = new User { Id = 2, Username = "admin", Email = "admin@example.com", Role = "Admin" };
            var token     = _jwtService.GenerateToken(adminUser);
            var jwt       = DecodeToken(token);

            // Role claim can appear as ClaimTypes.Role or the short "role" name
            var roleClaim = jwt.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role || c.Type == "role");

            Assert.That(roleClaim?.Value, Is.EqualTo("Admin"), "Token should embed the user's role");
        }

        [Test]
        public void GenerateToken_ShouldBeVerifiableWithCorrectKey()
        {
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);

            // This throws if the signature is invalid - so if it doesn't throw, the token is valid
            Assert.DoesNotThrow(() => ValidateToken(token),
                "Token should pass full validation with the correct signing key");
        }

        [Test]
        public void GenerateToken_ShouldFailValidation_WithWrongKey()
        {
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);

            // Try to validate with a DIFFERENT key - should fail
            var wrongKey    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WrongKeyWrongKeyWrongKeyWrongKey!"));
            var wrongParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = wrongKey,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = false
            };

            var handler = new JwtSecurityTokenHandler();
            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
                () => handler.ValidateToken(token, wrongParams, out _),
                "Token should be rejected when validated with the wrong key");
        }

        [Test]
        public void GenerateToken_ShouldExpireAfterConfiguredMinutes()
        {
            var user  = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token = _jwtService.GenerateToken(user);
            var jwt   = DecodeToken(token);

            var expectedExpiry = DateTime.UtcNow.AddMinutes(TestExpiry);

            // Allow 10 seconds tolerance for test execution time
            Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(10)),
                $"Token should expire in {TestExpiry} minutes");
        }

        [Test]
        public void GenerateToken_ShouldHaveUniqueJtiForEachCall()
        {
            // jti = JWT ID, a unique identifier per token - important to prevent replay attacks
            var user   = new User { Id = 1, Username = "alice", Email = "alice@example.com", Role = "User" };
            var token1 = _jwtService.GenerateToken(user);
            var token2 = _jwtService.GenerateToken(user);

            var jti1 = DecodeToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = DecodeToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            Assert.That(jti1, Is.Not.EqualTo(jti2),
                "Every token should have a unique jti claim");
        }

        [Test]
        public void GenerateToken_UserRoleToken_ShouldDifferFromAdminRoleToken()
        {
            var regularUser = new User { Id = 1, Email = "u@x.com", Username = "user",  Role = "User"  };
            var adminUser   = new User { Id = 2, Email = "a@x.com", Username = "admin", Role = "Admin" };

            var userToken  = _jwtService.GenerateToken(regularUser);
            var adminToken = _jwtService.GenerateToken(adminUser);

            // Tokens for different users should always be different strings
            Assert.That(userToken, Is.Not.EqualTo(adminToken));
        }
    }
}

using System.ComponentModel.DataAnnotations;
using AuthService.DTOs;
using NUnit.Framework;

namespace AuthService.Tests
{
    // ---------------------------------------------------------------
    // Tests for RegisterDto validation rules
    // ---------------------------------------------------------------
    [TestFixture]
    public class RegisterDtoTests
    {
        // Helper: runs DataAnnotations validation and returns any errors
        private static List<ValidationResult> Validate(object dto)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(dto);
            Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
            return results;
        }

        [Test]
        public void ValidDto_ShouldPassValidation()
        {
            var dto = new RegisterDto
            {
                Username = "alice",
                Email    = "alice@example.com",
                Password = "secret123"
            };

            var errors = Validate(dto);
            Assert.That(errors, Is.Empty, "A valid DTO should have no validation errors");
        }

        [Test]
        public void EmptyUsername_ShouldFailValidation()
        {
            var dto = new RegisterDto { Username = "", Email = "a@b.com", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Count, Is.GreaterThan(0), "Empty username should fail");
        }

        [Test]
        public void ShortUsername_ShouldFailValidation()
        {
            var dto = new RegisterDto { Username = "ab", Email = "a@b.com", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Username")), Is.True,
                "Username shorter than 3 chars should fail");
        }

        [Test]
        public void InvalidEmail_ShouldFailValidation()
        {
            var dto = new RegisterDto { Username = "alice", Email = "not-an-email", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Email")), Is.True,
                "Bad email format should fail");
        }

        [Test]
        public void ShortPassword_ShouldFailValidation()
        {
            var dto = new RegisterDto { Username = "alice", Email = "a@b.com", Password = "abc" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Password")), Is.True,
                "Password shorter than 6 chars should fail");
        }

        [Test]
        public void MissingEmail_ShouldFailValidation()
        {
            var dto = new RegisterDto { Username = "alice", Email = "", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Email")), Is.True,
                "Empty email should fail");
        }
    }

    // ---------------------------------------------------------------
    // Tests for LoginDto validation rules
    // ---------------------------------------------------------------
    [TestFixture]
    public class LoginDtoTests
    {
        private static List<ValidationResult> Validate(object dto)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(dto);
            Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
            return results;
        }

        [Test]
        public void ValidDto_ShouldPassValidation()
        {
            var dto = new LoginDto { Email = "alice@example.com", Password = "secret123" };
            var errors = Validate(dto);
            Assert.That(errors, Is.Empty, "Valid login DTO should have no errors");
        }

        [Test]
        public void MissingEmail_ShouldFailValidation()
        {
            var dto = new LoginDto { Email = "", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void InvalidEmailFormat_ShouldFailValidation()
        {
            var dto = new LoginDto { Email = "noatsign", Password = "pass123" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Email")), Is.True);
        }

        [Test]
        public void MissingPassword_ShouldFailValidation()
        {
            var dto = new LoginDto { Email = "alice@example.com", Password = "" };
            var errors = Validate(dto);
            Assert.That(errors.Any(e => e.MemberNames.Contains("Password")), Is.True);
        }
    }
}

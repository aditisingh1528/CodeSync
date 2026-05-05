using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class LoginDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}

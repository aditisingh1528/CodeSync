using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    // Data Transfer Object for login - what the user sends us when logging in
    public class LoginDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    // Used by Admin to change a user's role
    // POST /api/admin/update-role
    public class UpdateRoleDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Role is required")]
        [RegularExpression("^(User|Admin)$", ErrorMessage = "Role must be either 'User' or 'Admin'")]
        public string Role { get; set; } = string.Empty;
    }
}

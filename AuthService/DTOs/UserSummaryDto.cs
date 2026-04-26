namespace AuthService.DTOs
{
    // Safe user summary returned to Admin from GET /api/admin/users
    // Does NOT include PasswordHash — that must NEVER leave the server
    public class UserSummaryDto
    {
        public int    Id        { get; set; }
        public string Username  { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string Role      { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

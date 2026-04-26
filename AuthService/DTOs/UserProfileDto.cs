namespace AuthService.DTOs
{
    // Returned by GET /api/auth/profile
    // Read from the JWT claims - no DB call needed
    public class UserProfileDto
    {
        public string UserId   { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
    }
}

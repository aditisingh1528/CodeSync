namespace AuthService.DTOs
{
    // What we send BACK to the client after register/login
    // UC-3: now includes Token and Role
    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;

        // UC-3: The JWT token the client must send with every secured request
        // Send this in the Authorization header: "Bearer <token>"
        public string Token { get; set; } = string.Empty;
    }
}

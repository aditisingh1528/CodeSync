namespace AuthService.DTOs
{
    // What we send BACK to the client after register/login
    // We never send PasswordHash back - that would be a security risk!
    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

namespace SM_BE.Dto
{
    public class RegisterDto
    {
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
    }

    public class LoginDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

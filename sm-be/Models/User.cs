namespace SM_BE.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? PasswordHash { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
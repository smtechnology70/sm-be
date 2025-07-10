using Microsoft.IdentityModel.Tokens;
using SM_BE.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SM_BE.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "30");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("userName", user.Username ?? ""), // This will be the username
                new Claim("name", user.Name ?? ""), // This remains as display name
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                var issuer = jwtSettings["Issuer"];
                var audience = jwtSettings["Audience"];

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "userId");
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

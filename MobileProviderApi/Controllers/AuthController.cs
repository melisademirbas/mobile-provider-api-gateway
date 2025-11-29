using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Basit bir Login endpoint'i. Gerçek dünyada DB kontrolü yapılır.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Basit Varsayım: Kullanıcı adı 'testUser' ve şifre 'password' olmalıdır.
        if (request.Username != "testUser" || request.Password != "password")
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        // Başarılı doğrulama: JWT Token oluşturma
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Claim'ler: Kullanıcı bilgilerini token içine ekleriz.
            Subject = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.Name, request.Username),
                new Claim("SubscriberNo", "123456") // Örnek Abone No, API'ler bunu kullanabilir.
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Token 1 saat geçerli olacak
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return Ok(new 
        {
            Token = tokenHandler.WriteToken(token),
            Message = "Login successful"
        });
    }
}

// LoginRequest modeli (AuthController'ın altına veya ayrı bir Models klasörüne eklenebilir)
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
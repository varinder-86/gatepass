using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using Gatepaswebapi.Model;
using Microsoft.AspNetCore.Http;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("login")]
    [Consumes("application/json", "application/xml")] // Accepts both JSON and XML
    [Produces("application/json", "application/xml")] // Responds with JSON or XML
    public IActionResult Login(Login user)
    {
        if (user.username == "admin" && user.password == "admin")
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("MySecretKey12345678901234567890123456");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.username) }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            if (_httpContextAccessor.HttpContext.Session != null)
            {
                _httpContextAccessor.HttpContext.Session.SetString("JWT_Token", tokenString);
            }
            else
            {
                return StatusCode(500, "Session not available.");
            }

            //  HttpContextAccessor.Session.SetString("JWT_Token", tokenString);
            return Ok(new { Token = tokenString });
        }

        return Unauthorized();
    }
}



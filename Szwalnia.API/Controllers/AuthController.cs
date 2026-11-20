using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.DTO;

namespace Szwalnia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDb db, PasswordHasher<User> hasher) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var name = r.Name.Trim();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Name == name);

        var target = user ?? new User();
        var hash = user?.PasswordHash ?? hasher.HashPassword(target, Guid.NewGuid().ToString());
        var result = hasher.VerifyHashedPassword(target, hash, r.Password);


        if (user is null || result == PasswordVerificationResult.Failed) return Unauthorized(new ProblemDetails { Title = "Niepoprawny login lub hasło.", Status = 401 });
        
        if (result == PasswordVerificationResult.SuccessRehashNeeded) { user.PasswordHash = hasher.HashPassword(user, r.Password); await db.SaveChangesAsync(); }
        
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Role, user.Role)], CookieAuthenticationDefaults.AuthenticationScheme);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        
        return Ok(new { user.Id, user.Name, user.Role });
    }
    
    [HttpGet("me"), Authorize]
    public object Me() => new
    {
        id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), 
        name = User.Identity!.Name, role = User.FindFirstValue(ClaimTypes.Role)
    };

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(); return NoContent();
    }
    
    [HttpGet("users"), Authorize(Roles = "Admin")]
    public async Task<object> Users() => await db.Users.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Role }).ToListAsync();
    
    [HttpPost("users"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(UserRequest r)
    {
        var name = r.Name.Trim();
        if (name.Length < 3)
            return BadRequest(new ProblemDetails { Title = "Login musi mieć przynajmniej 3 znaki.", Status = 400 });

        var user = new User { Name = name, Role = r.Role };
        
        user.PasswordHash = hasher.HashPassword(user, r.Password);
        
        db.Users.Add(user); 
        
        await db.SaveChangesAsync(); 
        
        return StatusCode(201, new { user.Id, user.Name, user.Role });
    }
}

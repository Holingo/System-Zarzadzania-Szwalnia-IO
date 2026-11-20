using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;

namespace Szwalnia.API.Services;

public class SessionCookieEvents(AppDb db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
        if (role is UserRole.Admin or UserRole.Cutting or UserRole.Sewing &&
            int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            await db.Users.AnyAsync(user => user.Id == id && user.Role == role, context.HttpContext.RequestAborted))
        {
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}

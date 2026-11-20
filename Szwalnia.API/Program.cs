using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.Services;

var builder = WebApplication.CreateBuilder(args);
var secureCookies = builder.Configuration.GetValue("Security:SecureCookies", true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<AppDb>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Ustaw ConnectionStrings__Default.");
    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.PasswordHasher<Szwalnia.API.User>>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<ProductionQueries>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<SessionCookieEvents>();
builder.Services.AddHostedService<ReportWorker>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Szwalnia.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.EventsType = typeof(SessionCookieEvents);
    });
var keysPath = builder.Configuration["Security:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.UseRequestInterceptor(
        """
        (request) => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method?.toUpperCase())) {
                request.headers['X-Requested-With'] = 'Szwalnia';
            }
            return request;
        }
        """));
}

app.UseRouting();
app.UseRateLimiter();
// Uwierzytelnianie ciasteczkiem wymaga dodatkowej ochrony żądań zmieniających dane.
app.Use(async (context, next) =>
{
    var request = context.Request;
    var requestedWith = request.Headers["X-Requested-With"];
    if (request.Path.StartsWithSegments("/api") &&
        (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) ||
         HttpMethods.IsDelete(request.Method) || HttpMethods.IsPatch(request.Method)) &&
        requestedWith != "Szwalnia" && requestedWith != "SzwalniaFlow")
    {
        await Results.Problem(statusCode: 400, title: "Brakuje nagłówka X-Requested-With.")
            .ExecuteAsync(context);
        return;
    }

    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (AppDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok" })
        : Results.Problem(statusCode: 503, title: "Baza danych jest niedostępna."));

await DbStartup.Initialize(app.Services, app.Configuration);
await app.RunAsync();

public partial class Program;

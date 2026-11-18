var builder = WebApplication.CreateBuilder(args);

// Rejestracja usług wykorzystywanych przez aplikacje
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();

// Utworzenie aplikacji na podstawie konfiguracji
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Podlaczenie endpointow zdefiniowanych w kontrolerach
app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new { status = "ok" })
);

// Uruchomienie serwera.
app.Run();

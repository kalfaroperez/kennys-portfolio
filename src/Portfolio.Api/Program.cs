using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Data;
using Portfolio.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPortfolioRepository>(_ => JsonPortfolioRepository.FromEmbeddedResource());
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Protect the public API from abuse. The limit can be tuned per environment:
//   RateLimit__PermitLimit=300
var permitLimit = builder.Configuration.GetValue("RateLimit:PermitLimit", 120);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = permitLimit;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// The GitHub Pages site (a different origin) calls this API from the browser, so allow it.
// Override with Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ... on the host.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://kalfaroperez.github.io"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET")
        .AllowAnyHeader()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// The bundled frontend is also served from this API (same origin when run locally or with Docker).
app.UseDefaultFiles();
app.UseStaticFiles();

// CORS goes before the rate limiter so that 429 responses also carry the CORS headers.
app.UseCors();
app.UseRateLimiter();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapPortfolioEndpoints();

app.Run();

// Exposed so the integration tests can use WebApplicationFactory<Program>.
public partial class Program;

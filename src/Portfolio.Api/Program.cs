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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// The frontend is served from the same origin as the API, so no CORS configuration is needed.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapPortfolioEndpoints();

app.Run();

// Exposed so the integration tests can use WebApplicationFactory<Program>.
public partial class Program;

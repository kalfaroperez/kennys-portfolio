using System.Text.Json;
using Portfolio.Api.Models;

namespace Portfolio.Api.Data;

public interface IPortfolioRepository
{
    PortfolioData Get();
}

/// <summary>
/// Reads the portfolio from a JSON document once and serves it from memory.
/// Swap this class for a MongoDB/EF Core implementation without touching the endpoints.
/// </summary>
public sealed class JsonPortfolioRepository : IPortfolioRepository
{
    private const string ResourceName = "portfolio.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private readonly PortfolioData _data;

    private JsonPortfolioRepository(PortfolioData data) => _data = data;

    public static JsonPortfolioRepository FromEmbeddedResource()
    {
        using var stream = typeof(JsonPortfolioRepository).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        return FromStream(stream);
    }

    public static JsonPortfolioRepository FromStream(Stream json)
    {
        var data = JsonSerializer.Deserialize<PortfolioData>(json, Options)
            ?? throw new InvalidOperationException("Portfolio data is empty.");

        Validate(data);
        return new JsonPortfolioRepository(data);
    }

    public PortfolioData Get() => _data;

    private static void Validate(PortfolioData data)
    {
        if (data.Profile is null)
        {
            throw new InvalidOperationException("Portfolio data is missing the profile section.");
        }

        if (data.Experience is null || data.Skills is null || data.Projects is null ||
            data.Education is null || data.Certifications is null ||
            data.Courses is null || data.Languages is null)
        {
            throw new InvalidOperationException("Portfolio data is missing one or more required sections.");
        }
    }
}

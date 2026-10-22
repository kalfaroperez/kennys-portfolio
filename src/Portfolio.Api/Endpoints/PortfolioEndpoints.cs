using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Data;
using Portfolio.Api.Models;

namespace Portfolio.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api")
            .WithTags("Portfolio")
            .RequireRateLimiting("api");

        api.MapGet("/portfolio", (IPortfolioRepository repo) => Results.Ok(repo.Get()))
            .WithName("GetPortfolio")
            .WithSummary("The whole portfolio in a single payload.")
            .Produces<PortfolioData>();

        api.MapGet("/profile", (IPortfolioRepository repo) => Results.Ok(repo.Get().Profile))
            .WithName("GetProfile")
            .WithSummary("Name, headline, summary and contact links.")
            .Produces<Profile>();

        api.MapGet("/experience", (IPortfolioRepository repo, string? technology) =>
            {
                IEnumerable<ExperienceEntry> items = repo.Get().Experience;

                if (!string.IsNullOrWhiteSpace(technology))
                {
                    items = items.Where(e => ContainsTerm(e.Technologies, technology));
                }

                return Results.Ok(items.ToList());
            })
            .WithName("ListExperience")
            .WithSummary("Work history, optionally filtered by technology (case-insensitive, partial match).")
            .Produces<List<ExperienceEntry>>();

        api.MapGet("/experience/{id}", (string id, IPortfolioRepository repo) =>
            {
                var entry = repo.Get().Experience
                    .FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

                return entry is null ? NotFoundProblem("experience entry", id) : Results.Ok(entry);
            })
            .WithName("GetExperience")
            .WithSummary("A single work-history entry by id.")
            .Produces<ExperienceEntry>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapGet("/projects", (IPortfolioRepository repo, string? technology) =>
            {
                IEnumerable<Project> items = repo.Get().Projects;

                if (!string.IsNullOrWhiteSpace(technology))
                {
                    items = items.Where(p => ContainsTerm(p.Technologies, technology));
                }

                return Results.Ok(items.ToList());
            })
            .WithName("ListProjects")
            .WithSummary("Case studies, optionally filtered by technology (case-insensitive, partial match).")
            .Produces<List<Project>>();

        api.MapGet("/projects/{id}", (string id, IPortfolioRepository repo) =>
            {
                var project = repo.Get().Projects
                    .FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

                return project is null ? NotFoundProblem("project", id) : Results.Ok(project);
            })
            .WithName("GetProject")
            .WithSummary("A single case study by id.")
            .Produces<Project>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapGet("/skills", (IPortfolioRepository repo, string? category) =>
            {
                IEnumerable<SkillGroup> groups = repo.Get().Skills;

                if (!string.IsNullOrWhiteSpace(category))
                {
                    groups = groups.Where(g => g.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
                }

                return Results.Ok(groups.ToList());
            })
            .WithName("ListSkills")
            .WithSummary("Skills grouped by area, optionally filtered by category.")
            .Produces<List<SkillGroup>>();

        api.MapGet("/education", (IPortfolioRepository repo) => Results.Ok(repo.Get().Education))
            .WithName("ListEducation")
            .WithSummary("Academic background.")
            .Produces<IReadOnlyList<EducationEntry>>();

        api.MapGet("/certifications", (IPortfolioRepository repo) => Results.Ok(repo.Get().Certifications))
            .WithName("ListCertifications")
            .WithSummary("Professional certifications.")
            .Produces<IReadOnlyList<Certification>>();

        api.MapGet("/courses", (IPortfolioRepository repo) => Results.Ok(repo.Get().Courses))
            .WithName("ListCourses")
            .WithSummary("Courses completed.")
            .Produces<IReadOnlyList<string>>();

        api.MapGet("/languages", (IPortfolioRepository repo) => Results.Ok(repo.Get().Languages))
            .WithName("ListLanguages")
            .WithSummary("Spoken languages and proficiency.")
            .Produces<IReadOnlyList<LanguageSkill>>();

        return app;
    }

    private static bool ContainsTerm(IEnumerable<string> values, string term) =>
        values.Any(v => v.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static IResult NotFoundProblem(string resource, string id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: $"No {resource} with id '{id}' exists.");
}

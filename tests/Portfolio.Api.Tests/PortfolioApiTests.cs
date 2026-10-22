using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Portfolio.Api.Models;

namespace Portfolio.Api.Tests;

public class PortfolioApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PortfolioApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/portfolio")]
    [InlineData("/api/profile")]
    [InlineData("/api/experience")]
    [InlineData("/api/projects")]
    [InlineData("/api/skills")]
    [InlineData("/api/education")]
    [InlineData("/api/certifications")]
    [InlineData("/api/courses")]
    [InlineData("/api/languages")]
    [InlineData("/openapi/v1.json")]
    public async Task Endpoint_returns_success(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_serves_the_frontend()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Profile_has_name_headline_and_links()
    {
        var profile = await _client.GetFromJsonAsync<Profile>("/api/profile");

        Assert.NotNull(profile);
        Assert.Equal("Kennys Alfaro", profile.Name);
        Assert.False(string.IsNullOrWhiteSpace(profile.Headline));
        Assert.NotEmpty(profile.Links);
    }

    [Fact]
    public async Task Experience_can_be_filtered_by_technology()
    {
        var entries = await _client.GetFromJsonAsync<List<ExperienceEntry>>("/api/experience?technology=Hangfire");

        Assert.NotNull(entries);
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Contains(e.Technologies, t => t.Contains("Hangfire", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Technology_filter_is_case_insensitive()
    {
        var lower = await _client.GetFromJsonAsync<List<ExperienceEntry>>("/api/experience?technology=hangfire");
        var upper = await _client.GetFromJsonAsync<List<ExperienceEntry>>("/api/experience?technology=HANGFIRE");

        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.Equal(lower.Select(e => e.Id), upper.Select(e => e.Id));
    }

    [Fact]
    public async Task Filter_with_no_matches_returns_an_empty_list()
    {
        var projects = await _client.GetFromJsonAsync<List<Project>>("/api/projects?technology=cobol");

        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task Experience_by_id_returns_the_entry()
    {
        var entry = await _client.GetFromJsonAsync<ExperienceEntry>("/api/experience/cbtw-trip-arc");

        Assert.NotNull(entry);
        Assert.Equal("cbtw-trip-arc", entry.Id);
    }

    [Fact]
    public async Task Unknown_experience_id_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/experience/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Project_by_id_returns_the_case_study()
    {
        var project = await _client.GetFromJsonAsync<Project>("/api/projects/hotel-gateway");

        Assert.NotNull(project);
        Assert.Equal("Hotel Gateway / Hotel Inventory", project.Title);
    }

    [Fact]
    public async Task Unknown_project_id_returns_404()
    {
        var response = await _client.GetAsync("/api/projects/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Skills_can_be_filtered_by_category()
    {
        var groups = await _client.GetFromJsonAsync<List<SkillGroup>>("/api/skills?category=cloud");

        Assert.NotNull(groups);
        var group = Assert.Single(groups);
        Assert.Contains("Cloud", group.Category, StringComparison.OrdinalIgnoreCase);
    }
}

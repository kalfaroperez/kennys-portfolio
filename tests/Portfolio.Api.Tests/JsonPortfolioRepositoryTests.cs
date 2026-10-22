using System.Text;
using Portfolio.Api.Data;

namespace Portfolio.Api.Tests;

public class JsonPortfolioRepositoryTests
{
    private readonly IPortfolioRepository _repository = JsonPortfolioRepository.FromEmbeddedResource();

    [Fact]
    public void Embedded_data_loads_every_section()
    {
        var data = _repository.Get();

        Assert.False(string.IsNullOrWhiteSpace(data.Profile.Name));
        Assert.NotEmpty(data.Experience);
        Assert.NotEmpty(data.Skills);
        Assert.NotEmpty(data.Projects);
        Assert.NotEmpty(data.Education);
        Assert.NotEmpty(data.Certifications);
        Assert.NotEmpty(data.Courses);
        Assert.NotEmpty(data.Languages);
    }

    [Fact]
    public void Experience_ids_are_unique()
    {
        var ids = _repository.Get().Experience.Select(e => e.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Project_ids_are_unique()
    {
        var ids = _repository.Get().Projects.Select(p => p.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_project_lists_its_technologies_and_highlights()
    {
        Assert.All(_repository.Get().Projects, p =>
        {
            Assert.NotEmpty(p.Technologies);
            Assert.NotEmpty(p.Highlights);
        });
    }

    [Fact]
    public void Incomplete_json_is_rejected()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        Assert.Throws<InvalidOperationException>(() => JsonPortfolioRepository.FromStream(stream));
    }
}

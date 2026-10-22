namespace Portfolio.Api.Models;

public sealed record Link(string Label, string Url);

public sealed record Profile(
    string Name,
    string FullName,
    string Headline,
    string Location,
    string Summary,
    string Email,
    IReadOnlyList<Link> Links);

public sealed record ExperienceEntry(
    string Id,
    string Company,
    string Role,
    string? ProjectName,
    string Period,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Technologies);

public sealed record SkillGroup(string Category, IReadOnlyList<string> Items);

public sealed record Project(
    string Id,
    string Title,
    string Context,
    string Problem,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Technologies);

public sealed record EducationEntry(string Program, string Institution, string Period);

public sealed record Certification(string Name, string Issuer, int Year);

public sealed record LanguageSkill(string Language, string Level);

public sealed record PortfolioData(
    Profile Profile,
    IReadOnlyList<ExperienceEntry> Experience,
    IReadOnlyList<SkillGroup> Skills,
    IReadOnlyList<Project> Projects,
    IReadOnlyList<EducationEntry> Education,
    IReadOnlyList<Certification> Certifications,
    IReadOnlyList<string> Courses,
    IReadOnlyList<LanguageSkill> Languages);

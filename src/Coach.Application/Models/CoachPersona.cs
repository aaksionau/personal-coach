namespace Coach.Application.Models;

/// <summary>
/// A coach persona. <see cref="Name"/> is the full label ("Career Coach"); <see cref="ShortName"/>
/// is the one-word domain used in headings, page titles, and the header switcher ("Career").
/// </summary>
public sealed record CoachPersona(string Slug, string Name, string SystemPrompt, string Tone)
{
    private const string CoachSuffix = " Coach";

    /// <summary>The domain word on its own -- <see cref="Name"/> without its trailing "Coach".</summary>
    public string ShortName =>
        Name.EndsWith(CoachSuffix, StringComparison.Ordinal) ? Name[..^CoachSuffix.Length] : Name;
}

using System.Text.RegularExpressions;
using Transmiti.Session;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP863: Sony's marks only where they say which consoles this works with.
///
/// In a title the mark reads as the product's identity rather than a description of what it
/// connects to, which is the use most likely to draw a complaint or a store refusal. So every
/// place that names or heads something - the site's title and hero, a bold lead-in, the social
/// card, the README's headings, the installer's name - is held to carry none of them. Body text
/// still names the consoles, descriptively.
/// </summary>
public partial class SonyMarksTests
{
    [GeneratedRegex(@"PlayStation|\bPS[45]\b")]
    private static partial Regex Mark();

    private static string? Text(string relative)
        => SanitizerSource.LocateRelative(relative) is { } path ? File.ReadAllText(path) : null;

    private static void AssertNoMark(string where, IEnumerable<string> titles)
    {
        string[] offending = titles.Where(t => Mark().IsMatch(t)).ToArray();
        Assert.True(offending.Length == 0, $"{where} carries a Sony mark in a title: " + string.Join(" | ", offending));
    }

    /// <summary>PP863: the site's title, its social title, the hero and every bold lead-in.</summary>
    [Fact]
    public void TheSiteTitlesNameNoConsole()
    {
        if (Text(@"site\src\lib\site-content.ts") is not { } content)
            return;

        var titles = new List<string>();
        foreach (Match m in Regex.Matches(content, @"^\s*(title|titleLead|titleAccent|heading|badge)\s*:\s*""(?<t>[^""]*)""", RegexOptions.Multiline))
            titles.Add(m.Groups["t"].Value);
        foreach (Match m in Regex.Matches(content, @"\{\s*b:\s*""(?<t>[^""]*)""\s*\}"))
            titles.Add(m.Groups["t"].Value);

        Assert.NotEmpty(titles);
        AssertNoMark("site-content.ts", titles);
    }

    /// <summary>PP863: the page template's title and the social card's text.</summary>
    [Fact]
    public void TheTemplateAndTheCardNameNoConsole()
    {
        if (Text(@"site\index.html") is { } html)
            AssertNoMark("index.html", Regex.Matches(html, "<title>(.*?)</title>").Select(m => m.Groups[1].Value));

        if (Text(@"site\public\og.svg") is { } svg)
            AssertNoMark("og.svg", Regex.Matches(svg, "<text[^>]*>(.*?)</text>").Select(m => m.Groups[1].Value));
    }

    /// <summary>PP863: the README's headings and the installer's name.</summary>
    [Fact]
    public void TheReadmeHeadingsAndTheInstallerNameNoConsole()
    {
        if (Text("README.md") is { } readme)
            AssertNoMark("README.md", Regex.Matches(readme, @"^#+ .*$", RegexOptions.Multiline).Select(m => m.Value));

        if (Text(@"scripts\transmiti.iss") is { } iss)
            AssertNoMark("transmiti.iss", Regex.Matches(iss, @"^#define MyAppName ""(.*)""", RegexOptions.Multiline).Select(m => m.Groups[1].Value));
    }
}

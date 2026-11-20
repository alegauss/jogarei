using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP862: the repository's README is this project's, and chiaki-ng appears in it only as credited
/// origin.
///
/// The README was chiaki-ng's until 2026-11-20: its logo, its name as the title, its Discord and
/// its site. A visitor learned about the upstream and nothing about this port.
/// </summary>
public class ReadmeTests
{
    private static string? Readme()
        => SanitizerSource.LocateRelative("README.md") is { } path ? File.ReadAllText(path) : null;

    /// <summary>PP862: the title is the product's name, and the logo is the site's.</summary>
    [Fact]
    public void TheTitleAndLogoAreThisProjects()
    {
        if (Readme() is not { } readme)
            return;

        Assert.Contains("\n# Jogarei\n", readme.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Contains("site/public/logo.svg", readme);
        Assert.Contains("https://alegauss.com/jogarei/", readme);
    }

    /// <summary>
    /// PP862: none of the upstream's own channels - its logo, its Discord, its site - and chiaki-ng
    /// linked once, from the credits.
    /// </summary>
    [Fact]
    public void TheUpstreamIsOnlyTheCreditedOrigin()
    {
        if (Readme() is not { } readme)
            return;

        Assert.DoesNotContain("chiaking-logo", readme);
        Assert.DoesNotContain("discord.gg", readme);
        Assert.DoesNotContain("streetpea.github.io", readme);
        Assert.Contains("## Credits", readme);
        Assert.Contains("https://github.com/streetpea/chiaki-ng", readme);
    }
}

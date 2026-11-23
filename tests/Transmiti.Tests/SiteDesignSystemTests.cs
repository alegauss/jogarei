using Transmiti.Session;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP860: the site takes the template the product sites share from the design system, rather
/// than keeping its own copy.
///
/// A local copy is how the shared Nav, Hero, Footer and sponsor slot drifted apart across eight
/// sites. So the files the package's ./site and ./vite provide must not come back under site\,
/// and site\package.json must name the package they now come from.
/// </summary>
public class SiteDesignSystemTests
{
    private static readonly string[] ProvidedByThePackage =
    [
        @"site\src\components\Nav.tsx",
        @"site\src\components\Footer.tsx",
        @"site\src\components\sections\Hero.tsx",
        @"site\src\components\sections\FeatureIndex.tsx",
        @"site\src\components\ui\Rich.tsx",
        @"site\src\components\ui\ThemeToggle.tsx",
        @"site\src\lib\theme.ts",
        @"site\scripts\og-image.mjs",
    ];

    /// <summary>
    /// PP860: none of the files the package provides is back as a local copy.
    /// </summary>
    [Fact]
    public void NoLocalCopyOfTheSharedTemplate()
    {
        if (SanitizerSource.LocateRelative(@"site\package.json") is null)
            return;

        foreach (string relative in ProvidedByThePackage)
            Assert.Null(SanitizerSource.LocateRelative(relative));
    }

    /// <summary>
    /// PP860: the site, and its docs area, depend on the design system.
    /// </summary>
    [Fact]
    public void BothBuildsDependOnTheDesignSystem()
    {
        foreach (string relative in new[] { @"site\package.json", @"site\docs\package.json" })
        {
            if (SanitizerSource.LocateRelative(relative) is not { } path)
                continue;

            Assert.Contains("\"@viglet/viglet-design-system\"", File.ReadAllText(path));
        }
    }
}

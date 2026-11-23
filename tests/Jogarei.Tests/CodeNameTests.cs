using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP865: the code's own names follow the product.
///
/// PP861 renamed what a user sees; this holds the rest - the projects, the solution, the folders
/// and the namespace - to the same name, so a stack trace, a path or a project file never says
/// what the product stopped being called. The old token is assembled from two halves, so this
/// file is not the one occurrence it finds.
/// </summary>
public class CodeNameTests
{
    private static readonly string OldName = "Chiaki" + "Ng";

    private static readonly string[] Extensions =
        [".cs", ".csproj", ".slnx", ".cmd", ".ps1", ".sh", ".yml", ".xaml", ".props", ".targets"];

    /// <summary>
    /// PP865: the solution and the three projects carry the new name, and the old files are gone.
    /// </summary>
    [Fact]
    public void TheSolutionAndProjectsAreNamedJogarei()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.True(File.Exists(Path.Combine(root, "Jogarei.slnx")));
        Assert.True(File.Exists(Path.Combine(root, "app", "Jogarei.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "tests", "Jogarei.Tests", "Jogarei.Tests.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "tests", "Jogarei.Cases", "Jogarei.Cases.csproj")));
        Assert.False(File.Exists(Path.Combine(root, OldName + ".slnx")));
    }

    /// <summary>
    /// PP874: the product became Transmiti once Jogarei turned out to be the owner's own site
    /// about Brazilian games. The window, the exe, the installer, the site's base and the README
    /// all say the new name; the previous one is assembled from halves for the reason above.
    /// </summary>
    [Fact]
    public void WhatAReaderMeetsSaysTransmiti()
    {
        Assert.Equal("Transmiti", App.ProductName);
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        string previous = "Joga" + "rei";
        string[] read =
        [
            "README.md", "CONTRIBUTOR_GUIDE.md",
            Path.Combine("scripts", "chiaki-ng.iss"), Path.Combine("site", "vite.config.ts"),
            Path.Combine("site", "public", "llms.txt"), Path.Combine("site", "src", "lib", "site-content.ts"),
            Path.Combine("site", "docs", "astro.config.mjs"),
        ];
        // The project and namespace names are PP875's; what a reader meets is the exe, the
        // installer, the site path and the name standing alone.
        var visible = new System.Text.RegularExpressions.Regex(
            previous + @"(\.exe|-windows-installer|/|\b(?!\.(Tests|Cases|csproj|slnx)))",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var found = read.Where(r => visible.IsMatch(File.ReadAllText(Path.Combine(root, r)))).ToList();

        Assert.True(found.Count == 0, "still says the previous name: " + string.Join(", ", found));
        Assert.True(File.Exists(Path.Combine(root, "assets", "transmiti.ico")));
    }

    /// <summary>
    /// PP865: no source, project, script or workflow says the old name. Build output is skipped:
    /// a bin\ or obj\ left by an earlier build is not the tree. PP871 deleted gate\ and spike\, so
    /// they are no longer walked.
    /// </summary>
    [Fact]
    public void NoSourceSaysTheOldName()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        var found = new List<string>();
        foreach (string top in new[] { "app", "tests", "tools", "scripts", "shim", ".github" })
        {
            string dir = Path.Combine(root, top);
            if (!Directory.Exists(dir))
                continue;

            foreach (string path in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, path);
                if (relative.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj" or "TestResults" or "node_modules"))
                    continue;
                if (!Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    continue;
                if (File.ReadAllText(path).Contains(OldName, StringComparison.Ordinal))
                    found.Add(relative);
            }
        }

        foreach (string file in Directory.EnumerateFiles(root, "*.cmd"))
        {
            if (File.ReadAllText(file).Contains(OldName, StringComparison.Ordinal))
                found.Add(Path.GetFileName(file));
        }

        Assert.True(found.Count == 0, "still named the old way: " + string.Join(", ", found));
    }
}

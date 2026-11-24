using Transmiti.Session;
using Xunit;

namespace Transmiti.Tests;

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

    /// <summary>PP875: the name between the two, which a reader met for three days.</summary>
    private static readonly string PreviousName = "Joga" + "rei";

    private static bool SaysAnEarlierName(string text)
        => text.Contains(OldName, StringComparison.Ordinal)
            || text.Contains(PreviousName, StringComparison.OrdinalIgnoreCase);

    private static readonly string[] Extensions =
        [".cs", ".csproj", ".slnx", ".cmd", ".ps1", ".sh", ".yml", ".xaml", ".props", ".targets"];

    /// <summary>
    /// PP865, PP875: the solution and the three projects carry the new name, and the old files are gone.
    /// </summary>
    [Fact]
    public void TheSolutionAndProjectsAreNamedTransmiti()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.True(File.Exists(Path.Combine(root, "Transmiti.slnx")));
        Assert.True(File.Exists(Path.Combine(root, "app", "Transmiti.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "tests", "Transmiti.Tests", "Transmiti.Tests.csproj")));
        Assert.True(File.Exists(Path.Combine(root, "tests", "Transmiti.Cases", "Transmiti.Cases.csproj")));
        Assert.False(File.Exists(Path.Combine(root, OldName + ".slnx")));
        Assert.False(File.Exists(Path.Combine(root, PreviousName + ".slnx")));
    }

    /// <summary>
    /// PP874: the product became Transmiti once its previous name turned out to be the owner's own
    /// site about Brazilian games. The window, the exe, the installer, the site's base and the
    /// README all say the new name; PP875 then carried it into the code, so the files a reader
    /// meets hold neither earlier name in any form.
    /// </summary>
    [Fact]
    public void WhatAReaderMeetsSaysTransmiti()
    {
        Assert.Equal("Transmiti", App.ProductName);
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        string[] read =
        [
            "README.md", "CONTRIBUTOR_GUIDE.md",
            Path.Combine("scripts", "transmiti.iss"), Path.Combine("site", "vite.config.ts"),
            Path.Combine("site", "public", "llms.txt"), Path.Combine("site", "src", "lib", "site-content.ts"),
            Path.Combine("site", "docs", "astro.config.mjs"),
        ];
        var found = read.Where(r => SaysAnEarlierName(File.ReadAllText(Path.Combine(root, r)))).ToList();

        Assert.True(found.Count == 0, "still says the previous name: " + string.Join(", ", found));
        Assert.True(File.Exists(Path.Combine(root, "assets", "transmiti.ico")));
    }

    /// <summary>
    /// PP865, PP875: no source, project, script or workflow says either earlier name. Build output is skipped:
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
                if (SaysAnEarlierName(File.ReadAllText(path)))
                    found.Add(relative);
            }
        }

        foreach (string file in Directory.EnumerateFiles(root, "*.cmd"))
        {
            if (SaysAnEarlierName(File.ReadAllText(file)))
                found.Add(Path.GetFileName(file));
        }

        Assert.True(found.Count == 0, "still named the old way: " + string.Join(", ", found));
    }
}

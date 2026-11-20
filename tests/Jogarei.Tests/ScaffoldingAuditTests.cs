using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP866: what the cleanup audit found unused stays gone.
///
/// The audit deleted only what nothing built, ran or read: upstream's artwork, its release notes
/// and meson file, two CMake modules for dependencies PP33 removed, a patch for a dropped
/// platform, the jerasure and gf-complete submodules PP30 made dead, a committed test result, and
/// the frame-generation spike once its answer was in docs\HARDWARE-CONTRACT.md. Everything it kept
/// is named by an open line with the trigger that removes it.
/// </summary>
public class ScaffoldingAuditTests
{
    private static readonly string[] Gone =
    [
        "meson.ini",
        @"doc\release_notes.md",
        @"cmake\FindCURL.cmake",
        @"cmake\JsoncExternalProject.cmake",
        @"scripts\patches",
        @"third-party\jerasure",
        @"third-party\gf-complete",
        @"spike\frame-generation",
        @"assets\official-artwork",
    ];

    /// <summary>PP866: none of the deleted paths is back.</summary>
    [Fact]
    public void TheDeletedScaffoldingStaysGone()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        string[] back = Gone.Where(p => File.Exists(Path.Combine(root, p)) || Directory.Exists(Path.Combine(root, p))).ToArray();
        Assert.True(back.Length == 0, "deleted scaffolding is back: " + string.Join(", ", back));
    }

    /// <summary>PP866: the two dead FEC submodules are not declared either.</summary>
    [Fact]
    public void TheDeadSubmodulesAreNotDeclared()
    {
        if (SanitizerSource.LocateRelative(".gitmodules") is not { } path)
            return;

        string modules = File.ReadAllText(path);
        Assert.DoesNotContain("jerasure", modules);
        Assert.DoesNotContain("gf-complete", modules);
    }

    /// <summary>PP866: the frame-generation answer the deleted spike held is recorded.</summary>
    [Fact]
    public void TheDeletedSpikesAnswerIsRecorded()
    {
        if (SanitizerSource.LocateRelative(@"docs\HARDWARE-CONTRACT.md") is not { } path)
            return;

        Assert.Contains("What the driver offers for frame generation", File.ReadAllText(path));
    }
}

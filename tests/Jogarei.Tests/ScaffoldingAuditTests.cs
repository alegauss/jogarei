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
///
/// PP871 removed the rest: spike\ and gate\ entirely. Each committed reading a test reads moved,
/// byte for byte, to tests\Jogarei.Tests\fixtures\spikes; alloc-budget's README moved into
/// AllocBudgetTests' summary; the spikes' findings moved into docs\HARDWARE-CONTRACT.md.
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
        "spike",
        "gate",
    ];

    /// <summary>PP866, PP871: none of the deleted paths is back.</summary>
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

    /// <summary>
    /// PP871: every reading a test reads is found where its reader looks.
    ///
    /// Each reader returns early on a missing file, which is right outside a checkout and wrong
    /// inside one: a move that lost a fixture would turn every test over it into a silent pass.
    /// </summary>
    [Fact]
    public void EveryMovedSpikeReadingIsFound()
    {
        if (SanitizerSource.RepositoryRoot() is null)
            return;

        string[] readings =
        [
            EchoCancellation.ReadingRelativePath,
            OverlayDraw.ReadingRelativePath,
            .. SpikeRunQuality.BoundRuns,
            .. SpikeRunQuality.Excluded,
        ];

        Assert.Equal(7, readings.Length);

        string[] missing = readings.Where(r => SanitizerSource.LocateRelative(r) is null).ToArray();
        Assert.True(missing.Length == 0, "a moved spike reading is not found: " + string.Join(", ", missing));

        Assert.All(readings, r => Assert.StartsWith(SpikeRunQuality.FixtureDirectory + @"\", r, StringComparison.Ordinal));
    }
}

using Transmiti.Session;
using Xunit;
using Xunit.Abstractions;

namespace Transmiti.Tests;

/// <summary>
/// PP435: no executable for another platform under the source this port is responsible for.
///
/// scripts/holepunch/ carried two Linux x86-64 binaries, 14.4 MB together, that nothing in the tree
/// named. A .gitignore stops those two returning by name; this is the general case.
/// </summary>
public class ForeignBinariesTests(ITestOutputHelper output)
{
    /// <summary>
    /// THE RULE. Windows-only is a binding non-goal, so an ELF file here cannot be the answer to
    /// anything.
    /// </summary>
    [Fact]
    public void NoSourceDirectoryHoldsAnElfBinary()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        IReadOnlyList<string> walked = ForeignBinaries.Walk(root);
        output.WriteLine($"{walked.Count} files under {ForeignBinaries.SourceRelativeDirectories.Count} directories");

        // PP271: a walk that read nothing would satisfy the claim below by finding nothing.
        Assert.True(
            walked.Count >= 200,
            $"the walk examined only {walked.Count} files - it is not reading the tree");

        IReadOnlyList<string> foreign = ForeignBinaries.Foreign(root);

        Assert.True(
            foreign.Count == 0,
            "built for a platform this port does not ship to, under its own source: "
                + string.Join(", ", foreign));
    }

    /// <summary>
    /// And the two PP435 removed are gone, named so the removal is legible in the suite rather than
    /// only in a diff.
    /// </summary>
    [Theory]
    [InlineData(@"scripts\holepunch\holepunch-go\holepunch")]
    [InlineData(@"scripts\holepunch\refresh-go\refresh-token")]
    [InlineData(@"scripts\holepunch\holepunch-go\hell.txt")]
    public void WhatPP435RemovedIsNotBack(string relative)
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.False(
            File.Exists(Path.Combine(root, relative)),
            $"{relative} is back - PP435 removed it, and PP870 removed the folder it lived in");
    }

    /// <summary>
    /// PP870: the Go source PP435 kept beside them is gone too.
    ///
    /// It stayed because it documented the flow lib/src/holepunch.c implemented, and PP33 deleted
    /// holepunch.c; the flow is managed code now, held against PP297's recorded exchange. Before the
    /// folder went, every endpoint and the client id the Go named were checked to be in app\, so the
    /// PSN token flow is not dropped with it - which is what this asserts in its place.
    /// </summary>
    [Fact]
    public void TheGoReferenceIsGoneAndItsEndpointsAreInTheApp()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.False(Directory.Exists(Path.Combine(root, "scripts", "holepunch")), "scripts\\holepunch is back");

        string app = string.Concat(
            Directory.EnumerateFiles(Path.Combine(root, "app"), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        Assert.Contains("ba495a24-818c-472b-b12d-ff231c1b5745", app);
        Assert.Contains("/2.0/oauth/token", app);
        Assert.Contains("remoteplay.dl.playstation.net/remoteplay/redirect", app);
    }

    /// <summary>
    /// The detector reads the magic and nothing else - not an extension, which these two did not
    /// have, being Go binaries named after their module.
    /// </summary>
    [Fact]
    public void TheMagicIsWhatIsRead()
    {
        Assert.True(ForeignBinaries.IsElf([0x7F, (byte)'E', (byte)'L', (byte)'F', 0x02, 0x01]));

        // A PE is not this rule's business: it is what the port ships.
        Assert.False(ForeignBinaries.IsElf([(byte)'M', (byte)'Z', 0x90, 0x00]));

        // Off by one in the magic itself.
        Assert.False(ForeignBinaries.IsElf([0x7F, (byte)'E', (byte)'L', (byte)'G']));
    }

    /// <summary>
    /// PP272: and too few bytes to be a header answers false rather than throwing. An empty file in
    /// a tree this does not control is ordinary, and a gate that crashed on one would be worse than
    /// the omission.
    /// </summary>
    [Fact]
    public void TooFewBytesIsNotAnElf()
    {
        Assert.False(ForeignBinaries.IsElf([]));
        Assert.False(ForeignBinaries.IsElf([0x7F]));
        Assert.False(ForeignBinaries.IsElf([0x7F, (byte)'E', (byte)'L']));
    }

    /// <summary>
    /// The build output is skipped, which is what keeps the rule about source. app/bin and tests/obj
    /// hold real ELF-free PE output and a few thousand files nobody is asking about.
    /// </summary>
    [Fact]
    public void TheBuildOutputIsNotWalked()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        foreach (string path in ForeignBinaries.Walk(root))
        {
            string relative = Path.GetRelativePath(root, path);
            string[] parts = relative.Split(Path.DirectorySeparatorChar);

            Assert.DoesNotContain(parts, part => ForeignBinaries.SkippedDirectoryNames.Contains(part));
        }
    }

    /// <summary>
    /// third-party/ is outside the rule, and it is a decision rather than an oversight: vendored
    /// source ships helpers for platforms its own upstream supports.
    /// </summary>
    [Fact]
    public void VendoredSourceIsNotJudged()
    {
        Assert.DoesNotContain("third-party", ForeignBinaries.SourceRelativeDirectories);

        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.DoesNotContain(
            ForeignBinaries.Walk(root),
            path => Path.GetRelativePath(root, path).StartsWith("third-party", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// PP435: and a submodule inside a walked directory is not judged either.
    ///
    /// test/munit is the case that found this. "Not third-party/" was not the whole exclusion, and
    /// its gitlink is a FILE, so a boundary test looking for a .git DIRECTORY walked straight in.
    /// </summary>
    [Fact]
    public void ASubmoduleInsideAWalkedDirectoryIsNotJudged()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        // The exclusion is tied to a submodule git actually declares, not to a path spelled here.
        string modules = Path.Combine(root, ".gitmodules");
        if (File.Exists(modules))
        {
            Assert.Contains(
                "test/munit", File.ReadAllText(modules), StringComparison.Ordinal);
        }

        string munit = Path.Combine(root, "test", "munit");
        if (!Directory.Exists(munit))
            return;

        // Checked out, so the boundary has to be recognised - and by the file shape, not a name.
        Assert.True(
            ForeignBinaries.IsNestedCheckout(munit),
            "test/munit is a submodule and this did not see its boundary");

        Assert.DoesNotContain(
            ForeignBinaries.Walk(root),
            path => Path.GetRelativePath(root, path)
                .StartsWith(Path.Combine("test", "munit"), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// And a directory with no .git is not a boundary, so the rule does not exclude the whole tree
    /// by accident.
    /// </summary>
    [Fact]
    public void AnOrdinaryDirectoryIsNotABoundary()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.False(ForeignBinaries.IsNestedCheckout(Path.Combine(root, "app", "Session")));

        // The repository root itself IS one, which is what the walk starts below rather than at.
        Assert.True(ForeignBinaries.IsNestedCheckout(root));
    }
}

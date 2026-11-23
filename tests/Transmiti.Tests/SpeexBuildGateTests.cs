using Transmiti.Session;
using Xunit;
using Xunit.Abstractions;

namespace Transmiti.Tests;

/// <summary>
/// PP32 and PP867: speexdsp was the Qt client's, and it left with it.
///
/// §PP32's claims, held against the tree: lib/ references speex nowhere, and gui/ was the only
/// thing that linked it, so the probe ran only where gui/ was built. PP867 deleted gui/, so the end
/// state is held instead - nothing links it and nothing probes for it.
/// </summary>
public class SpeexBuildGateTests(ITestOutputHelper output)
{
    private static string? RootCMake()
        => SpeexBuildGate.Locate(SpeexBuildGate.RootCMakeRelativePath) is { } path
            ? File.ReadAllText(path)
            : null;

    /// <summary>
    /// PP867: a configure does not go looking for it at all.
    ///
    /// PP32 gated the probe on the client, because PP632 had turned the client's build off and the
    /// probe went on announcing a feature nothing linked. With the client deleted the gate has
    /// nothing to follow, so the probe went too.
    /// </summary>
    [Fact]
    public void TheBuildNeverProbesForIt()
    {
        if (RootCMake() is not { } cmake)
            return;

        Assert.False(
            SpeexBuildGate.Probes(cmake),
            "CMakeLists.txt probes for speexdsp again, and nothing it builds links it");
    }

    /// <summary>
    /// PP867: and nothing links it, which is what made removing the probe correct.
    ///
    /// A target linking speex now would need the probe back, and the failure would arrive at link
    /// time on somebody else's machine.
    /// </summary>
    [Fact]
    public void NothingLinksIt()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        IReadOnlyList<string> linking = SpeexBuildGate.FilesLinkingSpeex(root);
        output.WriteLine(string.Join(", ", linking));

        Assert.Empty(linking);
    }

    /// <summary>
    /// And the library references speex nowhere, which is §PP32's correction made checkable.
    ///
    /// The first draft of that section placed the speex stages beside Opus in the library and on the
    /// playback path. Both were wrong: they are the microphone's and they are the client's. That
    /// correction lived in a rationale file, and a ship deletes those - so it lives here.
    /// </summary>
    [Fact]
    public void TheLibraryReferencesSpeexNowhere()
    {
        IReadOnlyList<string> mentioning = SpeexBuildGate.LibFilesMentioningSpeex();

        Assert.True(
            mentioning.Count == 0,
            "lib/ now mentions speex, so the audio path is no longer split the way PP32 measured: "
                + string.Join(", ", mentioning));
    }

    /// <summary>
    /// PP867: the probe reader finds a probe in code, and not one a comment names - the paragraph
    /// recording why the probe went names it, and must not read as the probe being back.
    /// </summary>
    [Theory]
    [InlineData("if(X)\n find_package(SpeexDSP QUIET)\nendif()", true)]
    [InlineData("# find_package(SpeexDSP QUIET) stood here\nproject(chiaki)", false)]
    [InlineData("project(chiaki)", false)]
    public void AProbeIsFoundInCodeOnly(string cmake, bool probes)
        => Assert.Equal(probes, SpeexBuildGate.Probes(cmake));

    /// <summary>
    /// A comment naming the target is not a place the target is linked.
    ///
    /// Both readers learned this from the root CMakeLists on their first run: the paragraph
    /// explaining that speex is linked in exactly one place names it, so scanning raw text reported
    /// the file that documents the rule as a file that breaks it.
    /// </summary>
    [Fact]
    public void AMentionInACommentIsNotALink()
    {
        const string cmake = """
            # PkgConfig::SpeexDSP is linked in exactly one place, and this is not it.
            target_link_libraries(chiaki SDL2::SDL2) # not PkgConfig::SpeexDSP either
            """;

        Assert.DoesNotContain(
            SpeexBuildGate.LinkedTarget, SpeexBuildGate.Code(cmake), StringComparison.Ordinal);
    }

    /// <summary>And code on a line that also carries a comment survives the cut.</summary>
    [Fact]
    public void TheCodeBeforeATrailingCommentIsKept()
    {
        Assert.Contains(
            "target_link_libraries(chiaki PkgConfig::SpeexDSP)",
            SpeexBuildGate.Code("target_link_libraries(chiaki PkgConfig::SpeexDSP) # the one place"),
            StringComparison.Ordinal);
    }
}

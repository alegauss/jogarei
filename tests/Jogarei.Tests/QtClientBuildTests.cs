using Jogarei.Protocol;
using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP598: the Qt client's build retires in one piece, whenever it retires.
///
/// The decision was made by PP598 - gui/ stops being a build target, because PP597 showed that
/// PP33's deletion and a compilable client cannot both continue. PP632 made the change, in the same
/// commit that stopped session.c asking, which is what "rides in PP33's own commit" meant.
///
/// SO THESE HAVE TURNED OVER, the way PP591's did when the harness it described was deleted. They
/// held that the affordance was real and must not go early; they hold that it is gone and must not
/// come back. The expensive mistake was never doing it late - it was doing half of it.
///
/// PP867 deleted gui/ itself, the CHIAKI_ENABLE_GUI option and GuiFreshness. These keep the
/// argument from coming back, and say the source is gone.
/// </summary>
public class QtClientBuildTests
{
    /// <summary>
    /// compile.cmd no longer offers the argument, which is the piece no compiler reads.
    ///
    /// PP867: the other two pieces are deleted - GuiFreshness and the option the argument set - so
    /// this is the one left that could come back silently.
    /// </summary>
    [Fact]
    public void TheGateNoLongerOffersTheArgument()
    {
        if (QtClientBuild.LocateCompile() is not { } path)
            return;

        // PP632: turned over. The argument went in the commit that stopped session.c asking, which
        // is what PP598 meant by "rides in PP33's own commit" - gui/ calls eleven holepunch
        // exports, so it stopped compiling in the same instant.
        Assert.False(
            QtClientBuild.CompileStillBuildsTheClient(File.ReadAllText(path)),
            $"compile.cmd sets {QtClientBuild.EnableFlag} again, and gui/ does not compile - the "
                + "argument would produce a wall of errors rather than a client");
    }

    /// <summary>
    /// And the reader wants the assignment, not the word.
    ///
    /// compile.cmd's own comments quote the flag while explaining PP529, so a plain Contains over
    /// the file is satisfied by the paragraph that describes the affordance rather than by the line
    /// that provides it - PP587's finding, one file over.
    /// </summary>
    [Fact]
    public void ACommentDescribingTheFlagIsNotTheFlag()
    {
        Assert.False(QtClientBuild.CompileStillBuildsTheClient(
            "rem PP529: pass gui to set CHIAKI_ENABLE_GUI=ON, which nothing else does\n"));

        Assert.True(QtClientBuild.CompileStillBuildsTheClient(
            "    if /I \"%%~a\"==\"gui\"       set \"CHIAKI_ENABLE_GUI=ON\"\n"));

        Assert.False(QtClientBuild.CompileStillBuildsTheClient("echo nothing here\n"));
    }

    /// <summary>
    /// PP867: the record and the store's ownership check mean one binary, the one `compile.cmd gui`
    /// used to build - GuiFreshness owned it until it went with gui/.
    /// </summary>
    [Fact]
    public void TheRecordAndTheStoreCheckMeanOneBinary()
    {
        Assert.Equal(@"build\gui\chiaki.exe", QtClientBuild.ClientRelativePath);
        Assert.Equal(
            Path.GetFileNameWithoutExtension(QtClientBuild.ClientRelativePath),
            Jogarei.Settings.StoreOwnership.BuiltClientProcess);
    }

    /// <summary>
    /// PP599 and PP867: the client this retired was the file that drove the C session's PSN path,
    /// and nothing drives it now.
    ///
    /// PP596 held that the Qt client was the only thing putting a holepunch handle into a
    /// ChiakiSession; PP599 held that the same file was the one whose build was going, which is why
    /// the nine asks were removed rather than converted to the five results PP533 designed. PP867
    /// deleted that file's tree: the PSN driver is a recording now, and it is not among the callers
    /// of chiaki_session_init. A second PSN driver arriving would put the conversion back on the
    /// table - and the caller list is what would say so.
    /// </summary>
    [Fact]
    public void TheClientThatDrovePsnIsARecordingAndNotACaller()
    {
        Assert.Equal(
            HolepunchSessionOwnership.QtClientRelativePath,
            QtClientBuild.PsnDriverRelativePath);

        Assert.StartsWith(@"tests\oracles\gui\", QtClientBuild.PsnDriverRelativePath, StringComparison.Ordinal);

        Assert.DoesNotContain(
            QtClientBuild.PsnDriverRelativePath,
            HolepunchSessionOwnership.SessionInitCallers);
    }

    /// <summary>
    /// PP867: gui/ is gone from the checkout, which is the end state the retirement was heading for.
    ///
    /// A gui/ arriving back - a merge from upstream, a restore - would be a Qt client this tree has
    /// no option to build and no check that reads it, since every drift check reads the recording.
    /// </summary>
    [Fact]
    public void TheQtClientSourceIsGone()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.False(
            Directory.Exists(Path.Combine(root, "gui")),
            "gui/ is back in the checkout. PP867 deleted it after recording what the drift checks "
                + @"read under tests\oracles\gui; those checks do not read gui/ any more.");
    }

    /// <summary>
    /// PP632: the argument is not parsed any more, which is the half no compiler reads.
    ///
    /// Kept as an assertion rather than deleted with the affordance, and for PP591's reason: what
    /// a removal leaves behind is nothing to check unless somebody writes the absence down. An
    /// argument arriving back - a port of gui/, a merge from upstream - would be a build target for
    /// source that calls eleven exports the library no longer reaches from session.c.
    ///
    /// The NAME is still here on purpose. It is what a returning argument would be spelled, and it
    /// is what the message above has to be able to say.
    /// </summary>
    [Fact]
    public void TheAffordanceIsGoneAndTheNameRemembersIt()
    {
        Assert.Equal("gui", QtClientBuild.CompileArgument);

        if (QtClientBuild.LocateCompile() is not { } path)
            return;

        // The token-by-token match compile.cmd used to make. Its absence is the retirement.
        string text = File.ReadAllText(path);
        Assert.DoesNotContain(
            $"\"%%~a\"==\"{QtClientBuild.CompileArgument}\"", text, StringComparison.Ordinal);
    }
}

using Jogarei.Session;
using Xunit;
using Xunit.Abstractions;

namespace Jogarei.Tests;

/// <summary>
/// PP430: an option the build declares and never reads is a promise it does not keep.
///
/// CHIAKI_ENABLE_RUDP said "Enable Remote Play over Internet" and appeared once in the whole build.
/// Turning it off changed nothing, and nothing said so.
/// </summary>
public class BuildOptionsAreReadTests(ITestOutputHelper output)
{
    /// <summary>
    /// THE RULE. Every option the build declares, something reads.
    ///
    /// The count is what makes it checkable: a declaration plus at least one reader is two mentions,
    /// and one mention means nothing reads it. That is a number rather than a judgement, and it is
    /// the floor - it cannot know whether a gate is CORRECT, only that one exists.
    /// </summary>
    [Fact]
    public void EveryDeclaredOptionIsRead()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        IReadOnlyList<BuildOption> all = BuildOptionsAreRead.All(root);

        foreach (BuildOption one in all.OrderBy(o => o.Mentions))
            output.WriteLine($"{one.Mentions,3}  {one.Name}");

        // A sweep that finds nothing passes for PP271's reason.
        Assert.True(all.Count >= 6, $"only {all.Count} options found - the scan is not working");

        IReadOnlyList<BuildOption> unread = BuildOptionsAreRead.Unread(root);

        Assert.True(
            unread.Count == 0,
            "the build declares these and reads none of them, so setting one changes nothing and "
                + "nothing says so:\n  "
                + string.Join(
                    "\n  ", unread.Select(one => $"{one.Name} - \"{one.Description}\"")));
    }

    /// <summary>
    /// And the one PP430 removed is gone rather than merely unread.
    ///
    /// Named, so a revert is legible: putting the declaration back without a reader would fail the
    /// rule above, and putting it back WITH a reader is PP340's work rather than a revert.
    /// </summary>
    [Fact]
    public void TheInertRemoteOptionIsGone()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.DoesNotContain(
            BuildOptionsAreRead.All(root), one => one.Name == "CHIAKI_ENABLE_RUDP");
    }

    /// <summary>
    /// The options that ARE read are still read, so the rule did not pass by finding nothing.
    ///
    /// These gate real things - the decoder, the tests and the TLS library - and each is referenced
    /// past the floor. PP867: the Qt client and the echo canceller were here and left with gui/.
    /// </summary>
    [Theory]
    [InlineData("CHIAKI_ENABLE_FFMPEG_DECODER")]
    [InlineData("CHIAKI_ENABLE_TESTS")]
    [InlineData("CHIAKI_LIB_ENABLE_MBEDTLS")]
    public void TheLoadBearingOptionsAreStillRead(string name)
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        BuildOption one = Assert.Single(
            BuildOptionsAreRead.All(root), option => option.Name == name);

        Assert.True(
            one.Mentions >= BuildOptionsAreRead.Floor,
            $"{name} is declared and no longer read");
    }

    /// <summary>
    /// PP867: the options only the Qt client read are gone with it, rather than left declared.
    ///
    /// Each was read in gui\CMakeLists.txt or in a block that existed only to serve it, so deleting
    /// gui\ would have left them declared and unread - the rule above would have said so, and this
    /// names them so a revert is legible.
    /// </summary>
    [Theory]
    [InlineData("CHIAKI_ENABLE_GUI")]
    [InlineData("CHIAKI_ENABLE_SPEEX")]
    [InlineData("CHIAKI_GUI_ENABLE_SDL_GAMECONTROLLER")]
    [InlineData("CHIAKI_ENABLE_STEAM_SHORTCUT")]
    public void TheQtClientsOptionsAreGone(string name)
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.DoesNotContain(BuildOptionsAreRead.All(root), one => one.Name == name);
    }

    /// <summary>
    /// The reader finds both declaration forms, and refuses a commented one.
    ///
    /// PP400's rule, and it earns its place here: PP430's own removal left a comment naming the
    /// option it took out, so a reader that counted comments would report the thing it removed.
    /// </summary>
    [Fact]
    public void BothFormsAreFoundAndACommentIsNot()
    {
        Assert.Equal(1, BuildOptionsAreRead.Mentions("option(A \"x\" ON)", "A"));

        // Two mentions: a declaration and a use.
        Assert.Equal(2, BuildOptionsAreRead.Mentions("option(A \"x\" ON)\nif(A)\nendif()", "A"));

        // On an identifier boundary, so a longer name is not a mention of a shorter one.
        Assert.Equal(0, BuildOptionsAreRead.Mentions("CHIAKI_ENABLE_RUDP_EXTRA", "CHIAKI_ENABLE_RUDP"));
    }

    /// <summary>PP272: and an empty tree yields nothing rather than a pass about nothing.</summary>
    [Fact]
    public void AnEmptyTreeYieldsNoOptions()
    {
        string root = Path.Combine(
            Path.GetTempPath(), "pportal-options-" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(root);

            Assert.Empty(BuildOptionsAreRead.All(root));
            Assert.Empty(BuildOptionsAreRead.Unread(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

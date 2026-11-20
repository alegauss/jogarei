using System.Text.Json;
using ChiakiNg.Session;
using Xunit;

namespace ChiakiNg.Tests;

/// <summary>
/// PP46: the claim that dropping the bundled browser shrinks the build, held against two rows
/// measure-startup wrote rather than against a sentence.
///
/// The before is PP63's reference build - the Qt client with QtWebEngine. The after is the WPF
/// payload package.cmd stages, which is what the installer ships. Both rows sit beside the tool
/// and were taken in one session under one declared cache state, which is what makes them
/// comparable at all: measure-startup refuses to call two undeclared states the same.
/// </summary>
public class StartupBeforeAfterTests
{
    private const string Before = @"tools\measure-startup\qt-build-webengine-present.json";
    private const string After = @"tools\measure-startup\wpf-build.json";

    private static (JsonElement Before, JsonElement After)? Rows()
    {
        if (SanitizerSource.LocateRelative(Before) is not { } before
            || SanitizerSource.LocateRelative(After) is not { } after)
            return null;

        return (JsonDocument.Parse(File.ReadAllText(before)).RootElement,
                JsonDocument.Parse(File.ReadAllText(after)).RootElement);
    }

    /// <summary>
    /// PP46: the before carries Chromium and the after does not - otherwise the pair measures
    /// something other than the browser leaving.
    /// </summary>
    [Fact]
    public void TheBeforeCarriesChromiumAndTheAfterDoesNot()
    {
        if (Rows() is not var (before, after))
            return;

        Assert.True(before.GetProperty("webengine_present").GetBoolean());
        Assert.False(after.GetProperty("webengine_present").GetBoolean());
    }

    /// <summary>
    /// PP46: one declared cache state for both, and a declared one - "unknown" compares with
    /// nothing, not even with another unknown.
    /// </summary>
    [Fact]
    public void BothRowsDeclareTheSameCacheState()
    {
        if (Rows() is not var (before, after))
            return;

        string state = before.GetProperty("cache_state").GetString()!;

        Assert.NotEqual("unknown", state);
        Assert.Equal(state, after.GetProperty("cache_state").GetString());
        Assert.True(before.GetProperty("cold_is_comparable").GetBoolean());
        Assert.True(after.GetProperty("cold_is_comparable").GetBoolean());
    }

    /// <summary>
    /// PP46: the after is smaller on disk and idles lower. These are the two numbers no cache
    /// state moves; the start-up times are recorded in the rows and not asserted here, because a
    /// warm run on one afternoon is not a release-note figure.
    /// </summary>
    [Fact]
    public void TheAfterIsSmallerOnDiskAndIdlesLower()
    {
        if (Rows() is not var (before, after))
            return;

        Assert.True(
            after.GetProperty("tree_bytes").GetInt64() < before.GetProperty("tree_bytes").GetInt64(),
            "the WPF payload is not smaller than the Qt+WebEngine tree");
        Assert.True(
            after.GetProperty("working_set_bytes_median").GetInt64()
                < before.GetProperty("working_set_bytes_median").GetInt64(),
            "the WPF build does not idle lower than the Qt+WebEngine build");
    }
}

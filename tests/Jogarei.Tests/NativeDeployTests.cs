using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP492 and PP867: the deploy path, and what it has to leave in the portable tree.
///
/// None of this can be checked by looking at the tree: the closure is twenty-five files here, a
/// different set on a runner, and nothing at all on a machine with no MSYS2. What a checkout can
/// answer is whether the branch calls the walk, which is the line PP269 was missing. PP867 removed
/// the Qt deploy that sat beside it, with gui/.
/// </summary>
public class NativeDeployTests
{
    /// <summary>PP867: the two scripts this joins together are on disk, and the Qt deploy is not.</summary>
    [Fact]
    public void TheTwoScriptsExist()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.NotNull(NativeDeploy.LocateBuild());
        Assert.NotNull(NativeDeploy.LocateClosure());
        Assert.False(File.Exists(Path.Combine(root, "scripts", "deploy-windows-msys2.sh")));
    }

    /// <summary>
    /// THE REPAIR: the native-only branch hands the two libraries to the closure walk.
    ///
    /// PP269's version copied them and stopped, which is correct on every incremental build and
    /// leaves a tree nothing can load from after a clean. The assertion names both the pair and
    /// the call, because a check for the copy loop alone would pass on the broken version.
    /// </summary>
    [Fact]
    public void TheNativeOnlyPathCollectsWhatTheLibrariesImport()
    {
        if (NativeDeploy.LocateBuild() is not { } path)
            return;

        Assert.True(NativeDeploy.TheNativeOnlyPathCollectsTheClosure(File.ReadAllText(path)));
    }

    /// <summary>
    /// PP867: and the build no longer calls the Qt deploy, which packaged a client gui/ no longer
    /// holds the source of.
    /// </summary>
    [Fact]
    public void TheBuildNoLongerCallsTheQtDeploy()
    {
        if (NativeDeploy.LocateBuild() is not { } path)
            return;

        Assert.False(NativeDeploy.CallsTheQtDeploy(File.ReadAllText(path)));
        Assert.True(NativeDeploy.CallsTheQtDeploy("./scripts/deploy-windows-msys2.sh \"$DEPLOY_DIR\""));
        Assert.False(NativeDeploy.CallsTheQtDeploy("# deploy-windows-msys2.sh went with gui/"));
    }

    /// <summary>
    /// The walk is transitive, and it does not bundle Windows' own DLLs.
    ///
    /// One level would carry the shim's imports and miss libplacebo's, which shows up as this
    /// machine working and another one not. Bundling a system DLL is the opposite failure and is
    /// quieter still - nothing is missing, so nothing reports it.
    /// </summary>
    [Fact]
    public void TheWalkIsTransitiveAndSkipsSystemLibraries()
    {
        if (NativeDeploy.LocateClosure() is not { } path)
            return;

        string script = File.ReadAllText(path);

        Assert.True(NativeDeploy.TheWalkIsTransitive(script));
        Assert.True(NativeDeploy.SystemLibrariesAreNotBundled(script));
    }

    /// <summary>
    /// Every library the resolver loads from that tree is owed by one of the two sources, and the
    /// split between them is not a preference.
    ///
    /// Two are built here and copied out of the build directory. SDL2 is not: the host opens it by
    /// name at runtime, so nothing imports it and no ldd walk will ever report it - which is why a
    /// GUI-off build lost it and package.cmd is what said so. The set has to equal the resolver's
    /// table, because a library in the table and in neither source is one nothing puts there.
    /// </summary>
    [Fact]
    public void EveryLibraryTheResolverLoadsIsStagedBySomething()
    {
        Assert.Equal(
            Jogarei.Native.ChiakiNative.NativeLibraries.Values.Order(),
            NativeDeploy.HostLibraries.Order());

        Assert.Empty(NativeDeploy.BuiltHere.Intersect(NativeDeploy.StagedFromMsys2));
    }
}

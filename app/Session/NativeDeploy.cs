namespace Jogarei.Session;

/// <summary>
/// PP492: the step that fills the portable tree, and the branch of it a GUI-off build takes.
///
/// The .NET host loads chiaki-shim.dll and chiaki-render.dll out of <c>build\chiaki-ng-Win</c> -
/// <see cref="Jogarei.Native.ChiakiNative"/> looks there before it looks at the build directory,
/// because whatever is on PATH is not this build's. What puts them there is the deploy, and the
/// deploy has two paths: the Qt one, which walks the client's whole dependency closure with ldd,
/// and the native-only one PP269 added for builds with CHIAKI_ENABLE_GUI off.
///
/// PP269's path copied the two libraries and nothing else. That is invisible on an incremental
/// build - the closure is already in the tree from whenever the client was last built with the GUI
/// on - and produces an unloadable tree after a clean, where the two land beside nothing. The
/// resolver then finds the file, TryLoad fails on a missing import, and the error says the DLL "was
/// not found" while listing the path it is sitting at.
///
/// SO THE WALK IS A SCRIPT NOW. What is asserted here is the call to it, because the tree it
/// produces cannot be checked at all from a machine with no MSYS2: there is no ldd, no /mingw64,
/// and the closure is a different set of files on a runner than it is here. A predicate over the
/// scripts is what a checkout can answer.
///
/// PP867: ONE PATH NOW. The Qt deploy, scripts/deploy-windows-msys2.sh, went with gui/, so the
/// native-only deploy is the deploy and the branch that chose between the two is gone.
/// </summary>
public static class NativeDeploy
{
    /// <summary>The build driver, which owns the deploy.</summary>
    public const string BuildRelativePath = @"scripts\build-windows.sh";

    /// <summary>The closure walk itself.</summary>
    public const string ClosureRelativePath = @"scripts\deploy-native-deps.sh";

    /// <summary>The two this repository builds, copied out of the build directory.</summary>
    public static IReadOnlyList<string> BuiltHere { get; } =
        ["chiaki-shim.dll", "chiaki-render.dll"];

    /// <summary>
    /// The one that is staged from MSYS2 instead, because no walk can find it.
    ///
    /// The host opens SDL2 by name at runtime, so nothing in the tree imports it and ldd never
    /// reports it. A Qt build got it as a dependency of chiaki.exe; without the client it has to be
    /// staged by name.
    /// </summary>
    public static IReadOnlyList<string> StagedFromMsys2 { get; } = ["SDL2.dll"];

    /// <summary>Everything the portable tree owes the host, from both sources.</summary>
    public static IReadOnlyList<string> HostLibraries { get; } = [.. BuiltHere, .. StagedFromMsys2];

    /// <summary>The build driver, or null outside a checkout.</summary>
    public static string? LocateBuild() => SanitizerSource.LocateRelative(BuildRelativePath);

    /// <summary>The closure walk, or null outside a checkout.</summary>
    public static string? LocateClosure() => SanitizerSource.LocateRelative(ClosureRelativePath);

    /// <summary>The script's own name, as a caller spells it.</summary>
    private const string ScriptName = "deploy-native-deps.sh";

    /// <summary>
    /// Whether the deploy branch collects the closure as well as the two libraries.
    ///
    /// The assertion is that it both stages the pair and hands them to the walk. Staging alone is
    /// what PP269 shipped and what PP492 repaired, so a check for the copy loop would pass on the
    /// broken version. PP867: the branch is <c>do_deploy</c> itself now - the native-only one used
    /// to sit beside a Qt deploy and was keyed on <c>deploy_native_only</c>.
    /// </summary>
    public static bool TheNativeOnlyPathCollectsTheClosure(string buildScript)
    {
        ArgumentNullException.ThrowIfNull(buildScript);

        string text = buildScript.Replace("\r\n", "\n", StringComparison.Ordinal);

        int branch = text.IndexOf("if [[ $do_deploy -eq 1 ]]; then", StringComparison.Ordinal);
        if (branch < 0)
            return false;

        // To the end of the file: the deploy is the last thing the script does.
        string body = text[branch..];

        return body.Contains(ScriptName, StringComparison.Ordinal)
            && HostLibraries.All(dll => body.Contains(dll, StringComparison.Ordinal));
    }

    /// <summary>
    /// PP867: whether the build script still calls the Qt deploy, which went with gui/. Comment
    /// lines are skipped - the paragraph recording the removal names what was removed.
    /// </summary>
    public static bool CallsTheQtDeploy(string buildScript)
    {
        ArgumentNullException.ThrowIfNull(buildScript);

        return buildScript.ReplaceLineEndings("\n").Split('\n')
            .Where(line => !line.TrimStart().StartsWith('#'))
            .Any(line => line.Contains("deploy-windows-msys2", StringComparison.Ordinal)
                || line.Contains("windeployqt", StringComparison.Ordinal));
    }

    /// <summary>
    /// Whether the walk is transitive - it queues what it copies and scans that too.
    ///
    /// One level would be enough for the shim and would miss libplacebo's, which is the kind of
    /// difference that shows up as one machine working and another not.
    /// </summary>
    public static bool TheWalkIsTransitive(string closureScript)
    {
        ArgumentNullException.ThrowIfNull(closureScript);

        return closureScript.Contains("queue+=(\"$dependency\")", StringComparison.Ordinal)
            && closureScript.Contains("while [[ ${#queue[@]} -gt 0 ]]", StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether the walk still refuses to bundle a Windows system DLL.
    ///
    /// Copying one is not a missing file, so nothing downstream reports it - the application ships
    /// a kernel DLL from this machine and loads it in preference to the one the user's has.
    /// </summary>
    public static bool SystemLibrariesAreNotBundled(string closureScript)
    {
        ArgumentNullException.ThrowIfNull(closureScript);

        return closureScript.Contains("grep -iv \"system32\"", StringComparison.Ordinal)
            && closureScript.Contains("grep -iv \"windows\"", StringComparison.Ordinal);
    }
}

using System.Text.RegularExpressions;
using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP869: cpp-steam-tools is built only with the Qt client that links it.
///
/// The option defaulted to on and every default configure added the submodule, so each build
/// compiled a library nothing it produced loaded. The managed host's Steam shortcut is its own code.
/// </summary>
public class SteamToolsBuildTests
{
    /// <summary>PP869: the submodule is added only when the GUI is.</summary>
    [Fact]
    public void TheSubmoduleIsAddedOnlyWithTheGui()
    {
        if (SanitizerSource.LocateRelative(@"third-party\CMakeLists.txt") is not { } path)
            return;

        string cmake = Regex.Replace(File.ReadAllText(path), @"(?m)#.*$", "");
        Match guard = Regex.Match(cmake, @"if\((?<cond>[^)]*)\)\s*add_subdirectory\(cpp-steam-tools\)");

        Assert.True(guard.Success, "add_subdirectory(cpp-steam-tools) is no longer inside an if");
        Assert.Contains("CHIAKI_ENABLE_GUI", guard.Groups["cond"].Value);
    }
}

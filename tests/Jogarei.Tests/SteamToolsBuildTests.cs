using System.Text.RegularExpressions;
using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP869 and PP867: cpp-steam-tools was built only with the Qt client that linked it, and left the
/// tree with it.
///
/// PP869 gated the submodule on the GUI, because every default configure compiled a library nothing
/// it produced loaded. PP867 deleted the client, so the submodule, its .gitmodules entry and the
/// gate went too. The managed host's Steam shortcut is its own code.
/// </summary>
public class SteamToolsBuildTests
{
    /// <summary>PP867: nothing adds the submodule, and it is not in the checkout.</summary>
    [Fact]
    public void TheSubmoduleIsGone()
    {
        if (SanitizerSource.LocateRelative(@"third-party\CMakeLists.txt") is not { } path)
            return;

        string cmake = Regex.Replace(File.ReadAllText(path), @"(?m)#.*$", "");
        Assert.DoesNotContain("cpp-steam-tools", cmake, StringComparison.Ordinal);

        string root = SanitizerSource.RepositoryRoot()!;
        Assert.False(Directory.Exists(Path.Combine(root, "third-party", "cpp-steam-tools")));

        string gitmodules = Path.Combine(root, ".gitmodules");
        if (File.Exists(gitmodules))
            Assert.DoesNotContain("cpp-steam-tools", File.ReadAllText(gitmodules), StringComparison.Ordinal);
    }
}

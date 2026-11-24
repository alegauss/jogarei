using System.Text.RegularExpressions;
using Transmiti.Session;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP877: every file that opens a discovery service has run chiaki_lib_init by then.
///
/// The defect cannot be shown by running it here. chiaki_lib_init is WSAStartup now, and a test
/// process has long since initialised Winsock through something else, so a ConsoleDiscovery built
/// in this suite sweeps whether or not it called LibInit. The installed host is the process that had
/// touched no socket first, and there the front door reported "no network interface" on a machine
/// with three. So the call is held in the source, which is the only place it can be told apart.
/// </summary>
public class DiscoveryLibInitTests
{
    private static readonly string[] Openers =
    [
        @"app\Session\ConsoleListSources.cs",
        @"app\Session\ExchangeCapture.cs",
    ];

    /// <summary>The file with its // comments removed, so a comment naming the call cannot satisfy it.</summary>
    private static string? Code(string relative)
        => SanitizerSource.LocateRelative(relative) is { } path
            ? Regex.Replace(File.ReadAllText(path), @"//.*$", "", RegexOptions.Multiline)
            : null;

    /// <summary>
    /// PP877: ConsoleDiscovery inits before its first service. It is the one opener whose caller is
    /// a screen rather than a flag, so nothing upstream of it had run LibInit. ExchangeCapture is not
    /// held the same way: its Find is private and reached only through Run, which calls LibInit, and
    /// FindAndWake, whose one caller StreamRun calls it first - a source order there means nothing.
    /// </summary>
    [Fact]
    public void ConsoleDiscoveryInitsBeforeItsFirstService()
    {
        if (Code(Openers[0]) is not { } code)
            return;

        int init = code.IndexOf("ChiakiSession.LibInit()", StringComparison.Ordinal);
        int service = code.IndexOf("new DiscoveryService(", StringComparison.Ordinal);

        Assert.True(service >= 0, $"{Openers[0]} no longer opens a DiscoveryService");
        Assert.True(init >= 0 && init < service,
            $"{Openers[0]} opens a DiscoveryService without chiaki_lib_init before it");
    }

    /// <summary>
    /// PP877: and the list is the whole of them, so a third opener cannot arrive without somebody
    /// asking where its LibInit is. The self-test is left out: it calls LibInit as its own check.
    /// </summary>
    [Fact]
    public void TheListNamesEveryFileThatOpensOne()
    {
        // LocateDirectory, not LocateRelative: that one ends in File.Exists and would return null
        // here, which this rule would read as "outside a checkout" and pass without opening a file.
        if (SanitizerSource.LocateDirectory("app") is not { } app)
            return;

        string[] found =
        [
            .. Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !f.EndsWith("SelfTest.cs", StringComparison.Ordinal))
                .Where(f => Regex.Replace(File.ReadAllText(f), @"//.*$", "", RegexOptions.Multiline)
                    .Contains("new DiscoveryService(", StringComparison.Ordinal))
                .Select(f => @"app\" + Path.GetRelativePath(app, f))
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(Openers.Order(StringComparer.Ordinal), found);
    }
}

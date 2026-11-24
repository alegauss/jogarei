using Microsoft.Win32;
using Transmiti.Session;
using Transmiti.Settings;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP879: adding a console by address - QmlBackend.addManualHost, and the list that reads it.
///
/// The combo's index is the whole of the rule: an index into the registered consoles names the
/// registration the address belongs to, and the one past the end is an unregistered entry. Written
/// through a store of this test's own and read back the way the console list reads it.
/// </summary>
public class ManualHostAddingTests : IDisposable
{
    private readonly string keyPath = $@"SOFTWARE\TransmitiTests\{Guid.NewGuid():N}";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        GC.SuppressFinalize(this);
    }

    private static readonly RegisteredHost Ps5 = new()
    {
        ServerNickname = "PS5-385",
        ServerMac = [0x90, 0x47, 0x48, 0x82, 0xfc, 0x29],
        Target = (int)ConsoleTarget.Ps5,
    };

    /// <summary>PP879: an index into the registrations carries that console's MAC and is registered.</summary>
    [Fact]
    public void ARegisteredChoiceCarriesItsMac()
    {
        ManualHost added = Assert.Single(RegistrationRecord.ManualHostsAdding([], [Ps5], 0, " 10.0.0.9 "));

        Assert.Equal("10.0.0.9", added.Host);
        Assert.True(added.Registered);
        Assert.Equal(Ps5.ServerMac, added.RegisteredMac);
        Assert.Equal(0, added.Id);
    }

    /// <summary>PP879: the entry past the end is "register on first connection" - unregistered, no MAC.</summary>
    [Fact]
    public void ThePastTheEndChoiceIsUnregistered()
    {
        IReadOnlyList<ManualHost> after = RegistrationRecord.ManualHostsAdding(
            [new ManualHost(4, "10.0.0.1", false, null)], [Ps5], 1, "10.0.0.9");

        Assert.Equal([4, 5], after.Select(h => h.Id));
        Assert.False(after[^1].Registered);
        Assert.Null(after[^1].RegisteredMac);
    }

    /// <summary>
    /// PP879: what is written reaches the list - the manual source the console list merges shows
    /// the address, and a registered one is keyed by its MAC so it joins its registration.
    /// </summary>
    [Fact]
    public void AnAddedHostReachesTheListsManualSource()
    {
        var store = new QSettingsStore(keyPath);

        store.WriteManualHosts(RegistrationRecord.ManualHostsAdding(store.ManualHosts(), [Ps5], 0, "10.0.0.9"));
        store.WriteManualHosts(RegistrationRecord.ManualHostsAdding(store.ManualHosts(), [Ps5], 1, "10.0.0.10"));

        IReadOnlyList<ManualConsole> listed = ConsoleListSources.Manual(store.ManualHosts());
        Assert.Equal(["10.0.0.9", "10.0.0.10"], listed.Select(c => c.Address));
        Assert.True(listed[0].Registered);
        Assert.Equal("90474882fc29", listed[0].Mac);
        Assert.False(listed[1].Registered);
    }

    /// <summary>PP879: a blank address is refused where it is built, not written as an empty row.</summary>
    [Fact]
    public void ABlankAddressIsRefused()
        => Assert.ThrowsAny<ArgumentException>(() => RegistrationRecord.ManualHostsAdding([], [], 0, "  "));
}

using Microsoft.Win32;
using Transmiti.Session;
using Transmiti.Settings;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP878: registration, end to end below the window - the native run, the store write, and the
/// dialog's choice of console generation.
///
/// PP115 proved the request byte for byte and nothing sent it. What is held here is that a run
/// starts, answers once and can be stopped; that what it pairs is written so the reader reads it
/// back; and that a console typed in by address stays on the list after the dialog closes.
/// </summary>
public class ConsoleRegistrationTests : IDisposable
{
    /// <summary>A store nobody else has - never the Qt client's key, which holds real registrations.</summary>
    private readonly string keyPath = $@"SOFTWARE\TransmitiTests\{Guid.NewGuid():N}";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        GC.SuppressFinalize(this);
    }

    private static readonly byte[] Mac = [0x90, 0x47, 0x48, 0x82, 0xfc, 0x29];

    private static PairedConsole Paired(string nickname = "PS5-385", byte[]? mac = null) => new(
        new RegisteredHost
        {
            ServerNickname = nickname,
            ServerMac = mac ?? Mac,
            Target = (int)ConsoleTarget.Ps5,
            // Eight characters and eight NULs, which is the shape that has to go through the
            // registry's binary form rather than a string.
            RpRegistKey = [.. System.Text.Encoding.ASCII.GetBytes("3e91107c"), .. new byte[8]],
            RpKey = [.. Enumerable.Repeat((byte)7, 16)],
            RpKeyType = 0,
            ConsolePin = "1234",
            ApName = "PS5-385",
        },
        "ssid", "bssid", "apkey");

    private static RegistrationRequest Request(string host = "192.168.1.224", bool broadcast = false)
        => new(host, ConsoleTarget.Ps5, broadcast, 12345678, 0, null, new byte[8]);

    /// <summary>PP878: a paired console reads back as the registered host it was, every field.</summary>
    [Fact]
    public void APairedConsoleReadsBackAsItWasWritten()
    {
        var store = new QSettingsStore(keyPath);
        PairedConsole paired = Paired();

        store.SaveRegisteredHost(paired.Host, paired.ApSsid, paired.ApBssid, paired.ApKey);

        RegisteredHost back = Assert.Single(store.RegisteredHosts());
        Assert.Equal(paired.Host.ServerNickname, back.ServerNickname);
        Assert.Equal(paired.Host.ServerMac, back.ServerMac);
        Assert.Equal(paired.Host.Target, back.Target);
        Assert.Equal(paired.Host.RpRegistKey, back.RpRegistKey);
        Assert.Equal(paired.Host.RpKey, back.RpKey);
        Assert.Equal("1234", back.ConsolePin);
        Assert.Equal("PS5-385", back.ApName);
    }

    /// <summary>
    /// PP878: registering the same console again replaces its entry, and a second console is added
    /// after it - the array grows by consoles, never by registrations.
    /// </summary>
    [Fact]
    public void ReRegisteringReplacesAndAnotherConsoleAppends()
    {
        var store = new QSettingsStore(keyPath);

        store.SaveRegisteredHost(Paired("first").Host);
        store.SaveRegisteredHost(Paired("renamed").Host);
        store.SaveRegisteredHost(Paired("other", [1, 2, 3, 4, 5, 6]).Host);

        Assert.Equal(["renamed", "other"], store.RegisteredHosts().Select(h => h.ServerNickname));
    }

    /// <summary>
    /// PP878: a console typed in by address is kept as a registered manual host, or it registers and
    /// then vanishes from the list the moment the dialog closes. A discovered one is not, because
    /// discovery will show it again.
    /// </summary>
    [Fact]
    public void ATypedConsoleBecomesARegisteredManualHostAndADiscoveredOneDoesNot()
    {
        var store = new QSettingsStore(keyPath);

        RegistrationRecord.Apply(store, Request(), Paired(), discovered: true);
        Assert.Empty(store.ManualHosts());

        RegistrationRecord.Apply(store, Request(), Paired(), discovered: false);
        ManualHost manual = Assert.Single(store.ManualHosts());
        Assert.Equal("192.168.1.224", manual.Host);
        Assert.True(manual.Registered);
        Assert.Equal(Mac, manual.RegisteredMac);

        // Again, for the same address: marked, not duplicated.
        RegistrationRecord.Apply(store, Request(), Paired(), discovered: false);
        Assert.Single(store.ManualHosts());
    }

    /// <summary>PP878: a broadcast registration has no address to keep.</summary>
    [Fact]
    public void ABroadcastRegistrationKeepsNoManualHost()
    {
        var store = new QSettingsStore(keyPath);

        RegistrationRecord.Apply(store, Request(Registration.BroadcastHost, broadcast: true), Paired(), discovered: false);

        Assert.Empty(store.ManualHosts());
        Assert.Single(store.RegisteredHosts());
    }

    /// <summary>PP878: a new manual host takes the next id after the highest, as the Qt client picks it.</summary>
    [Fact]
    public void ANewManualHostTakesTheNextId()
    {
        IReadOnlyList<ManualHost> after = RegistrationRecord.ManualHostsAfter(
            [new ManualHost(3, "10.0.0.1", false, null), new ManualHost(7, "10.0.0.2", false, null)],
            "10.0.0.9", Mac);

        Assert.Equal([3, 7, 8], after.Select(h => h.Id));
        Assert.True(after[^1].Registered);
    }

    /// <summary>
    /// PP878: choosing the generation decides which identifier is asked for, and a running
    /// registration holds the button off without touching PP37's CanRegister.
    /// </summary>
    [Fact]
    public void TheTargetPicksTheIdentifierAndBusyHoldsTheButton()
    {
        var model = new RegistViewModel { Host = "10.0.0.5", RemotePlayPin = "12345678" };

        model.Target = ConsoleTarget.Ps4Below7;
        Assert.True(model.OnlineIdVisible);
        Assert.False(model.AccountIdVisible);

        model.Target = ConsoleTarget.Ps5;
        Assert.False(model.OnlineIdVisible);
        Assert.True(model.AccountIdVisible);
        Assert.False(model.CanSubmit);

        model.AccountId = "AAAAAAAAAAA=";
        Assert.Equal("AAAAAAAAAAA=", model.Identifier);
        Assert.True(model.CanSubmit);

        model.Busy = true;
        Assert.True(model.CanRegister);
        Assert.False(model.CanSubmit);
    }

    /// <summary>
    /// PP878: the native run starts, and answers exactly once with no pairing when nothing is there
    /// to pair with - here the loopback, where no console listens. Stopping it is what the dialog's
    /// close does, so both ends of the run are exercised against the real shim.
    /// </summary>
    [Fact]
    public void ARunAgainstNothingAnswersOnceWithoutAPairing()
    {
        var answered = new List<(RegistrationOutcome, PairedConsole?)>();
        using var done = new ManualResetEventSlim();

        using (var run = new ConsoleRegistration(Request("127.0.0.1"), (outcome, paired) =>
               {
                   lock (answered)
                       answered.Add((outcome, paired));
                   done.Set();
               }))
        {
            if (!done.Wait(TimeSpan.FromSeconds(10)))
                run.Stop();
        }

        (RegistrationOutcome outcome, PairedConsole? paired) = Assert.Single(answered);
        Assert.NotEqual(RegistrationOutcome.Succeeded, outcome);
        Assert.Null(paired);
    }
}

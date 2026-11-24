using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Transmiti.Native;
using Transmiti.Settings;

namespace Transmiti.Session;

/// <summary>How a registration ended: ChiakiRegistEventType, in its own order.</summary>
public enum RegistrationOutcome { Canceled = 0, Failed = 1, Succeeded = 2 }

/// <summary>
/// PP878: what a console hands back when it accepts this machine, in the shape the store writes.
///
/// The three access-point strings ride beside <see cref="RegisteredHost"/> rather than in it, because
/// nothing on the read side uses them and widening that record would change every construction of it
/// - while dropping them on the write would leave a store the Qt client reads back as a console with
/// half a registration.
/// </summary>
public sealed record PairedConsole(
    RegisteredHost Host, string? ApSsid, string? ApBssid, string? ApKey);

/// <summary>
/// PP878: what a successful registration leaves in the store - the registered host, and for a
/// console that was typed in rather than discovered, the manual host that keeps it on the list.
/// </summary>
public static class RegistrationRecord
{
    /// <summary>Writes both halves. <paramref name="discovered"/> is whether the list's row was a discovery.</summary>
    public static void Apply(
        QSettingsStore store, RegistrationRequest request, PairedConsole paired, bool discovered)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(paired);

        store.SaveRegisteredHost(paired.Host, paired.ApSsid, paired.ApBssid, paired.ApKey);

        IReadOnlyList<ManualHost> manual = store.ManualHosts();
        string? existing = manual.FirstOrDefault(m => string.Equals(m.Host, request.Host.Trim(), StringComparison.OrdinalIgnoreCase))?.Host;

        // A broadcast registration has no address worth keeping as a manual host.
        if (request.Broadcast
            || Registration.SettleManualHost(request, discovered, existing) is not { } address)
            return;

        store.WriteManualHosts(ManualHostsAfter(manual, address, paired.Host.ServerMac));
    }

    /// <summary>
    /// The manual list with <paramref name="address"/> marked registered to <paramref name="mac"/>:
    /// the existing entry for that address if there is one, so registering it again does not
    /// duplicate it, or a new entry under the next id the Qt client would also pick.
    /// </summary>
    public static IReadOnlyList<ManualHost> ManualHostsAfter(
        IReadOnlyList<ManualHost> hosts, string address, byte[] mac)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(mac);

        var after = new List<ManualHost>(hosts.Count + 1);
        var found = false;
        foreach (ManualHost host in hosts)
        {
            if (!found && string.Equals(host.Host, address, StringComparison.OrdinalIgnoreCase))
            {
                after.Add(host with { Registered = true, RegisteredMac = mac });
                found = true;
            }
            else
            {
                after.Add(host);
            }
        }

        if (!found)
            after.Add(new ManualHost(NextId(hosts), address, true, mac));

        return after;
    }

    /// <summary>
    /// PP879: the manual list with one more console typed in by address - QmlBackend.addManualHost.
    ///
    /// <paramref name="index"/> is the dialog combo's: an index into <paramref name="registered"/>
    /// names the registration this address belongs to, and the one past the end is "register on
    /// first connection", an unregistered entry. Always a NEW entry, as Settings::SetManualHost
    /// makes one for an id below zero - the Qt client lets the same address be added twice too.
    /// </summary>
    public static IReadOnlyList<ManualHost> ManualHostsAdding(
        IReadOnlyList<ManualHost> hosts, IReadOnlyList<RegisteredHost> registered, int index, string address)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        ArgumentNullException.ThrowIfNull(registered);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        bool isRegistered = index >= 0 && index < registered.Count;
        return
        [
            .. hosts,
            new ManualHost(NextId(hosts), address.Trim(), isRegistered,
                isRegistered ? registered[index].ServerMac : null),
        ];
    }

    /// <summary>The id the Qt client's manual_hosts_id_next would hand out: one past the highest.</summary>
    private static int NextId(IReadOnlyList<ManualHost> hosts)
        => hosts.Count == 0 ? 0 : hosts.Max(h => h.Id) + 1;
}

/// <summary>
/// PP878: one registration, run by libchiaki's own thread through the shim.
///
/// PP115 proved the request byte for byte and nothing ever sent it; chiaki_regist_start owns the
/// socket, the console's reply and its decryption. The callback arrives once, on that thread, and
/// the registered host it carries is read out field by field while the call lasts - its layout stays
/// on the C side of the boundary.
/// </summary>
public sealed unsafe class ConsoleRegistration : IDisposable
{
    private readonly Action<RegistrationOutcome, PairedConsole?> finished;
    private readonly ChiakiLog log;
    private GCHandle self;
    private IntPtr handle;

    /// <summary>Starts registering. <paramref name="finished"/> is called once, on libchiaki's thread.</summary>
    /// <param name="logged">Each line libchiaki logs, so a failure can say why.</param>
    public ConsoleRegistration(
        RegistrationRequest request,
        Action<RegistrationOutcome, PairedConsole?> finished,
        Action<string>? logged = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(finished);
        this.finished = finished;

        // The registration opens its own socket, so the same WSAStartup the discovery needs (PP877).
        ChiakiSession.LibInit();

        log = new ChiakiLog(
            ChiakiLogLevel.All & ~ChiakiLogLevel.Verbose,
            (_, text) => logged?.Invoke(text));

        self = GCHandle.Alloc(this);

        byte[]? account = request.AccountId;
        fixed (byte* accountBytes = account)
        {
            handle = RegistStart(
                log.Handle, (int)request.Target, request.Host, request.Broadcast,
                request.OnlineId, account is null ? null : accountBytes,
                request.Pin, request.ConsolePin, &Dispatch, GCHandle.ToIntPtr(self));
        }

        if (handle == IntPtr.Zero)
        {
            self.Free();
            log.Dispose();
            throw new InvalidOperationException("chiaki_regist_start failed.");
        }
    }

    /// <summary>Asks a registration still waiting on the console to give up.</summary>
    public void Stop()
    {
        if (handle != IntPtr.Zero)
            RegistStop(handle);
    }

    public void Dispose()
    {
        if (handle != IntPtr.Zero)
        {
            // Stops and joins libchiaki's thread before the handle that thread calls back on goes.
            RegistFree(handle);
            handle = IntPtr.Zero;
        }

        if (self.IsAllocated)
            self.Free();

        log.Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Dispatch(int type, IntPtr host, IntPtr user)
    {
        try
        {
            if (user == IntPtr.Zero || GCHandle.FromIntPtr(user).Target is not ConsoleRegistration me)
                return;

            var outcome = (RegistrationOutcome)type;
            PairedConsole? paired = outcome == RegistrationOutcome.Succeeded && host != IntPtr.Zero
                ? Read(host)
                : null;

            // A success the host could not be read from is not a success anybody can use.
            me.finished(paired is null && outcome == RegistrationOutcome.Succeeded
                ? RegistrationOutcome.Failed
                : outcome, paired);
        }
        catch
        {
            // Nothing may escape into C, on a thread libchiaki owns.
        }
    }

    /// <summary>The registered host, copied out while the callback still owns it.</summary>
    private static PairedConsole? Read(IntPtr host)
    {
        byte[]? mac = Bytes(host, 0);
        string? nickname = Text(host, 1);
        if (mac is not { Length: 6 } || string.IsNullOrEmpty(nickname))
            return null;

        long consolePin = RegisteredHostInt(host, 2);

        return new PairedConsole(
            new RegisteredHost
            {
                ServerNickname = nickname,
                ServerMac = mac,
                Target = (int)RegisteredHostInt(host, 0),
                RpRegistKey = Bytes(host, 2),
                RpKey = Bytes(host, 3),
                RpKeyType = (int)RegisteredHostInt(host, 1),
                ConsolePin = consolePin > 0 ? consolePin.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                ApName = Text(host, 7),
            },
            Text(host, 4), Text(host, 5), Text(host, 6));
    }

    private static byte[]? Bytes(IntPtr host, int field)
    {
        int size = RegisteredHostBytes(host, field, null, 0);
        if (size < 0)
            return null;

        var buffer = new byte[size];
        fixed (byte* out_ = buffer)
            RegisteredHostBytes(host, field, out_, size);
        return buffer;
    }

    private static string? Text(IntPtr host, int field)
        => Bytes(host, field) is { } bytes ? System.Text.Encoding.UTF8.GetString(bytes) : null;

    [DllImport(ChiakiNative.Library, EntryPoint = "chiaki_shim_regist_start",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr RegistStart(
        IntPtr log, int target, [MarshalAs(UnmanagedType.LPUTF8Str)] string host,
        [MarshalAs(UnmanagedType.U1)] bool broadcast,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? onlineId, byte* accountId,
        uint pin, uint consolePin,
        delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void> cb, IntPtr user);

    [DllImport(ChiakiNative.Library, EntryPoint = "chiaki_shim_regist_stop",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void RegistStop(IntPtr regist);

    [DllImport(ChiakiNative.Library, EntryPoint = "chiaki_shim_regist_free",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void RegistFree(IntPtr regist);

    [DllImport(ChiakiNative.Library, EntryPoint = "chiaki_shim_registered_host_bytes",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern int RegisteredHostBytes(IntPtr host, int field, byte* out_, int outSize);

    [DllImport(ChiakiNative.Library, EntryPoint = "chiaki_shim_registered_host_int",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern long RegisteredHostInt(IntPtr host, int field);
}

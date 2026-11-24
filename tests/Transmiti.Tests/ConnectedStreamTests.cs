using System.Text.RegularExpressions;
using Transmiti.Session;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP883: Connect on the console list draws the picture, through the same window and loop as
/// --show-stream, with the software decoder PP882 made whole.
///
/// A session needs a console, so the wiring is held in the source - the order the handle stops
/// things in is the part a run cannot show until it crashes on a decoder freed under the phase.
/// </summary>
public class ConnectedStreamTests
{
    private static string? Code(string relative)
        => SanitizerSource.LocateRelative(relative) is { } path
            ? Regex.Replace(File.ReadAllText(path), @"//.*$", "", RegexOptions.Multiline)
            : null;

    /// <summary>PP883: the front door's starter gives every session a stream by default.</summary>
    [Fact]
    public void TheFrontDoorsStarterOpensAStream()
    {
        if (Code(@"app\Session\ConsoleSession.cs") is not { } code)
            return;

        Assert.Contains(": this(ConnectedStream.TryCreate)", code, StringComparison.Ordinal);
        Assert.Contains("stream is null ? TakenAndDropped : stream.Sample", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// PP883: the stream is disposed AFTER the phase, because the phase feeds its decoder - and the
    /// pad before the phase, because the pad pushes into it.
    /// </summary>
    [Fact]
    public void TheHandleStopsThePictureLast()
    {
        if (Code(@"app\Session\ConsoleSession.cs") is not { } code)
            return;

        int stop = code.IndexOf("going.Stop();", StringComparison.Ordinal);
        int pad = code.IndexOf("pad.Dispose();", StringComparison.Ordinal);
        int phase = code.IndexOf("phase.Dispose();\n            baseline.Dispose();\n\n            stream?.Dispose();", StringComparison.Ordinal);
        if (phase < 0)
            phase = code.IndexOf("phase.Dispose();\r\n            baseline.Dispose();\r\n\r\n            stream?.Dispose();", StringComparison.Ordinal);

        Assert.True(stop >= 0 && pad > stop, "the pad is not disposed after the session's stop");
        Assert.True(phase > pad, "the handle does not dispose pad, then phase, then the stream");
    }

    /// <summary>
    /// PP883: the decoder is software (PP882) - FFmpeg's DXVA path keeps 32 of a PS5 keyframe's
    /// 51 slices, so a hardware name here would bring the broken bottom half back.
    /// </summary>
    [Fact]
    public void TheDecoderIsSoftware()
    {
        if (Code(@"app\Session\ConnectedStream.cs") is not { } code)
            return;

        Assert.Contains("hardwareName: \"\"", code, StringComparison.Ordinal);
        Assert.Contains("StreamRun.OpenWindow(", code, StringComparison.Ordinal);
        Assert.Contains("StreamRun.Draw(", code, StringComparison.Ordinal);
    }

    private static ConsoleRow Living => new(
        "Living room", "10.0.0.5", Discovered: true, Manual: false, Registered: true, Display: true);

    private static Settings.RegisteredHost Registration => new()
    {
        ServerNickname = "Living room",
        ServerMac = [0x90, 0x47, 0x48, 0x82, 0xfc, 0x29],
        Target = 1_000_100,
        RpRegistKey = "12ab34cd"u8.ToArray(),
        RpKey = [.. Enumerable.Repeat((byte)7, 16)],
    };

    /// <summary>
    /// PP883: a stopped session's late Quit says nothing. Closing the stream window disconnects,
    /// and the session's own Quit then arrived with the error the stop left - live, it replaced
    /// "The session ended." with "Unknown Error in Stream Connection".
    /// </summary>
    [Fact]
    public void AStoppedSessionsLateQuitIsIgnored()
    {
        var starter = new ConsoleConnectTests.FakeStarter();
        var model = new ConsoleListViewModel(starter, () => [Registration]);

        model.Connect(Living);
        Action<ConsoleSessionEvent> stale = starter.Report!;
        model.Disconnect();

        stale(new(ConsoleSessionState.Ended, "The session has quit: Unknown Error in Stream Connection"));

        Assert.Equal(QuitSentence.Ended, model.Status);
    }

    /// <summary>PP883: nor does it end the session a second Connect opened after it.</summary>
    [Fact]
    public void AStoppedSessionsLateQuitDoesNotEndTheNextOne()
    {
        var starter = new ConsoleConnectTests.FakeStarter();
        var model = new ConsoleListViewModel(starter, () => [Registration]);

        model.Connect(Living);
        Action<ConsoleSessionEvent> stale = starter.Report!;
        model.Connect(Living);

        stale(new(ConsoleSessionState.Ended, null));

        Assert.True(model.HasSession);
        Assert.False(starter.Released);
    }

    /// <summary>PP883: a starter handed no stream still builds, which is the no-D3D11 machine's door.</summary>
    [Fact]
    public void AStarterWithNoStreamBuilds()
        => Assert.NotNull(new NativeConsoleSessionStarter(static () => null));
}

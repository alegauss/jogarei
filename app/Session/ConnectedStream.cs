using System.Windows;
using Transmiti.Native;
using Transmiti.Protocol;
using Transmiti.Views;

namespace Transmiti.Session;

/// <summary>
/// PP883: the picture behind Connect on the console list - a decoder, the stream window and the
/// loop that draws one into the other, held for as long as the session is.
///
/// PP801 gave the front door a phase with nowhere to put a frame, which was right while the only
/// picture on offer was the one PP881 found broken. PP882 made software decoding whole, and this is
/// --show-stream's composition reached from the list: the same window
/// (<see cref="StreamRun.OpenWindow"/>), the same loop (<see cref="StreamRun.Draw"/>), and a
/// SOFTWARE decoder, because FFmpeg's DXVA path keeps 32 of a PS5 keyframe's 51 slices.
///
/// BUILT ON THE UI THREAD, and before the session starts: the phase is handed
/// <see cref="Sample"/> when it is built, so the decoder has to exist first, and the window is a
/// DispatcherObject.
///
/// WHO ENDS WHAT. Closing the window is a person's Disconnect and is reported as one; it does not
/// stop anything itself, because the session's handle owns the order things stop in. Disposing
/// this is the last step of that order - after the phase, which is what feeds the decoder.
/// </summary>
public sealed class ConnectedStream : IDisposable
{
    private readonly RenderDevice renderer;
    private readonly SessionDecoder decoder;
    private readonly Window window;
    private readonly CancellationTokenSource stopping = new();
    private readonly ManualResetEventSlim neverQuit = new(false);
    private Thread? drawing;
    private Action? closedByPerson;
    private int disposed;

    private ConnectedStream(RenderDevice renderer, SessionDecoder decoder)
    {
        this.renderer = renderer;
        this.decoder = decoder;

        var model = new StreamOverlayViewModel { Loading = true };
        window = StreamRun.OpenWindow(model);

        window.Closed += (_, _) => Interlocked.Exchange(ref closedByPerson, null)?.Invoke();

        // Armed before the session starts, for StreamRun's reason: a frame decoded while the
        // reader is still asleep is one the drain throws away without counting.
        decoder.SignalWhenReady();

        drawing = new Thread(() =>
        {
            try
            {
                StreamRun.Draw(decoder, renderer, model, window, neverQuit, stopping.Token);
            }
            finally
            {
                // The draw thread frees what it drew with, because only it knows when it has
                // stopped pulling - a dispose from the UI thread would wait on a thread that may be
                // waiting on the UI thread to present.
                decoder.Dispose();
                renderer.Dispose();
            }
        })
        {
            IsBackground = true,
            Name = "stream",
        };
    }

    /// <summary>
    /// Opens the renderer and a software decoder on it, or answers null where this machine has no
    /// D3D11 device - which leaves the session to run as PP801's door did, with nothing drawn.
    /// </summary>
    public static ConnectedStream? TryCreate()
    {
        RenderDevice? renderer = ChiakiRender.CreateD3d11();
        if (renderer is null)
            return null;

        try
        {
            var decoder = new SessionDecoder(
                IntPtr.Zero, codec: (int)ChiakiCodec.H264, maxFps: 60, hardwareName: "", renderer.NativeDevice);

            return new ConnectedStream(renderer, decoder);
        }
        catch (InvalidOperationException)
        {
            renderer.Dispose();
            return null;
        }
    }

    /// <summary>The phase's video handler: every frame goes to the decoder the window draws from.</summary>
    public bool Sample(ReadOnlySpan<byte> frame, int framesLost, bool recovered)
        => decoder.Sample(frame, framesLost, recovered);

    /// <summary>
    /// Shows the window and starts drawing, once the session has started.
    /// </summary>
    /// <param name="closed">Run when a person closes the window - their way of saying Disconnect.</param>
    public void Show(Action closed)
    {
        ArgumentNullException.ThrowIfNull(closed);

        closedByPerson = closed;
        window.Show();
        drawing?.Start();
    }

    /// <summary>
    /// Stops the loop and closes the window. Called by the session's handle AFTER the phase is
    /// disposed, so nothing is still feeding the decoder the draw thread is about to free.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        // Not a person's close any more: the session is already going, and reporting it again
        // would end a session that a later Connect may have opened.
        closedByPerson = null;
        stopping.Cancel();

        if (window.IsVisible)
            window.Close();

        // Never started - a session that failed to start - so nothing else will free these.
        if (drawing is { ThreadState: ThreadState.Unstarted })
        {
            drawing = null;
            decoder.Dispose();
            renderer.Dispose();
        }
    }
}

using Transmiti.Protocol;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP882: the repack that lets the presenter take a software frame.
///
/// FFmpeg's DXVA path keeps 32 slices of the PS5's 51-slice keyframes, so software decoding is the
/// path that draws a whole picture - and a software H.264 decoder hands back yuv420p, three planes,
/// where the presenter takes NV12's two. The shim interleaves the chroma; these hold its byte order
/// through the same export the pull calls, against the real shim.
/// </summary>
public class ChromaInterleaveTests
{
    /// <summary>PP882: U then V, alternating, one pair per chroma sample - NV12's order, not NV21's.</summary>
    [Fact]
    public void UAndVAlternateUFirst()
    {
        byte[] u = [1, 2, 3];
        byte[] v = [11, 12, 13];
        var output = new byte[6];

        Assert.True(SessionDecoder.InterleaveChroma(u, 3, v, 3, output, 6, chromaWidth: 3, chromaHeight: 1));
        Assert.Equal([1, 11, 2, 12, 3, 13], output);
    }

    /// <summary>
    /// PP882: strides are honoured on every plane, because a decoder pads its rows - libavcodec's
    /// linesize is wider than the picture - and reading width-packed rows would shear the colour.
    /// </summary>
    [Fact]
    public void PaddedRowsAreReadAndWrittenByStride()
    {
        // Two rows of two samples, each source row padded to four bytes; the output to six.
        byte[] u = [1, 2, 0xEE, 0xEE, 3, 4, 0xEE, 0xEE];
        byte[] v = [11, 12, 0xEE, 0xEE, 13, 14, 0xEE, 0xEE];
        var output = new byte[12];

        Assert.True(SessionDecoder.InterleaveChroma(u, 4, v, 4, output, 6, chromaWidth: 2, chromaHeight: 2));
        Assert.Equal([1, 11, 2, 12, 0, 0, 3, 13, 4, 14, 0, 0], output);
    }

    /// <summary>PP882: an output row too short for twice the width is refused with nothing written.</summary>
    [Fact]
    public void AShortOutputStrideIsRefused()
    {
        byte[] u = [1, 2];
        byte[] v = [3, 4];
        var output = new byte[8];

        Assert.False(SessionDecoder.InterleaveChroma(u, 2, v, 2, output, 3, chromaWidth: 2, chromaHeight: 1));
        Assert.All(output, b => Assert.Equal(0, b));
    }
}

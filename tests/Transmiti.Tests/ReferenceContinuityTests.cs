using Transmiti.Protocol;
using Xunit;

namespace Transmiti.Tests;

/// <summary>
/// PP881: on a stream that loses nothing, the port's reference ring never calls a reference lost.
///
/// PP881 asked whether ManagedVideoReceiver dropped P-frames as reference-lost where the C delivered
/// them, because the picture smeared and the run reported frames "lost by the network". The answer
/// was no - a live trace on every reference choice fired zero times, and the bytes handed to the
/// decoder decoded clean in stock ffmpeg; the smear is FFmpeg's DXVA path keeping 32 of the PS5
/// keyframe's 51 slices (docs/HARDWARE-CONTRACT.md). What is held here is the half of that answer
/// the port owns: delivered in order, every P-frame finds the frame it names, and the 16-bit wrap
/// the frame index takes after 65535 frames is not a gap.
/// </summary>
public class ReferenceContinuityTests
{
    /// <summary>PP881: each frame naming the one before it finds it, from a cold ring onwards.</summary>
    [Fact]
    public void ConsecutiveFramesAlwaysFindTheirReference()
    {
        var ring = new ReferenceFrames();
        ring.Add(0);

        for (int frame = 1; frame < 500; frame++)
        {
            ReferenceChoice choice = ring.Choose(frame, referenceFrame: 0);
            Assert.True(choice.Present, $"frame {frame} lost the frame before it");
            Assert.False(choice.Lost);
            ring.Add(frame);
        }
    }

    /// <summary>
    /// PP881: a reference further back than the previous frame, as long as it is inside the sixteen
    /// the ring keeps, is found too - the console names older references after a scene holds still.
    /// </summary>
    [Fact]
    public void AnyOfTheLastSixteenIsFound()
    {
        var ring = new ReferenceFrames();
        for (int frame = 0; frame < 40; frame++)
            ring.Add(frame);

        for (int back = 0; back < ReferenceFrames.Capacity; back++)
            Assert.True(ring.Choose(40, referenceFrame: back).Present, $"{back + 1} back was not held");
    }

    /// <summary>PP881: the index wraps at 16 bits, and the frame after 65535 still finds 65535.</summary>
    [Fact]
    public void TheSixteenBitWrapIsNotAGap()
    {
        var ring = new ReferenceFrames();
        for (int frame = 65530; frame <= 65535; frame++)
            ring.Add(frame);

        ReferenceChoice choice = ring.Choose(0, referenceFrame: 0);

        Assert.True(choice.Present);
        Assert.False(choice.Lost);
    }
}

using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class AmbientMusicTests
{
    private static readonly float[] Loop = AmbientMusic.Samples();

    [Fact]
    public void The_loop_lasts_four_chords()
    {
        Assert.Equal((int)(AmbientMusic.LoopSeconds * AmbientMusic.SampleRate), Loop.Length);
        Assert.InRange(AmbientMusic.LoopSeconds, 30, 40);
    }

    [Fact]
    public void It_is_quiet_and_never_clips()
    {
        Assert.All(Loop, s => Assert.False(float.IsNaN(s)));
        var peak = Loop.Max(Math.Abs);
        Assert.InRange(peak, 0.02f, 0.8f);
    }

    [Fact]
    public void The_end_flows_into_the_start()
    {
        // No click at the seam: the jump from the last sample to the first is as small as an ordinary step.
        var seam = Math.Abs(Loop[0] - Loop[^1]);
        var typical = Enumerable.Range(1, 2000).Average(i => Math.Abs(Loop[i] - Loop[i - 1]));
        Assert.True(seam <= typical * 6 + 0.002, $"seam {seam}, typical step {typical}");
    }

    [Fact]
    public void The_wav_has_a_header_and_every_sample()
    {
        var wav = AmbientMusic.Wav();
        Assert.Equal("RIFF"u8.ToArray(), wav[..4]);
        Assert.Equal(44 + Loop.Length * 2, wav.Length);
    }
}

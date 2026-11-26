namespace ConsoleMode.Services;

/// <summary>
/// The music under Home, like a console's: a slow, warm pad over four chords with a few bell notes drifting on top,
/// made here (no file to ship). The loop is seamless: every voice fades in and out on a circle of time, so the end
/// flows into the start. Pure, so it is tested; <see cref="HomeMusic"/> plays it.
/// </summary>
public static class AmbientMusic
{
    public const int SampleRate = 22050;

    /// <summary>Seconds per chord; four chords make the loop.</summary>
    public const double ChordSeconds = 9.0;

    // Cmaj9, Am9, Fmaj7(#11), G6/9: open voicings, low and soft.
    private static readonly double[][] Chords =
    [
        [130.81, 196.00, 246.94, 293.66, 329.63],
        [110.00, 164.81, 196.00, 246.94, 261.63],
        [87.31, 130.81, 164.81, 220.00, 246.94],
        [98.00, 146.83, 196.00, 220.00, 293.66]
    ];

    // The bells: a pentatonic line, one note every chord and a half, high and quiet.
    private static readonly double[] Bells = [783.99, 659.25, 587.33, 523.25, 659.25, 783.99, 880.00, 587.33];

    public static double LoopSeconds => Chords.Length * ChordSeconds;

    public static float[] Samples()
    {
        var total = (int)(LoopSeconds * SampleRate);
        var samples = new float[total];
        var chordLength = ChordSeconds * SampleRate;

        for (var i = 0; i < total; i++)
        {
            var t = (double)i / SampleRate;
            double pad = 0;
            for (var c = 0; c < Chords.Length; c++)
            {
                // Each chord swells in and out over one and a half chords, centred on its slot (wrapping around).
                var weight = Swell(i, c * chordLength + chordLength / 2, chordLength * 1.5, total);
                if (weight <= 0) continue;
                foreach (var f in Chords[c])
                {
                    // Two slightly detuned sines per note, with a slow shimmer: wide and calm.
                    pad += weight * (Math.Sin(2 * Math.PI * f * t) + 0.6 * Math.Sin(2 * Math.PI * f * 1.003 * t + 1.1))
                           * (0.85 + 0.15 * Math.Sin(2 * Math.PI * 0.11 * t + f));
                }
            }

            double bell = 0;
            var bellLength = chordLength * 1.5;
            for (var b = 0; b < Bells.Length; b++)
            {
                var start = (b * bellLength) % total;
                var age = (i - start + total) % total;
                if (age > SampleRate * 4) continue;
                var since = age / (double)SampleRate;
                bell += Math.Sin(2 * Math.PI * Bells[b] * since) * Math.Exp(-1.6 * since) * Math.Min(1, since * 200)
                        + 0.25 * Math.Sin(2 * Math.PI * Bells[b] * 2.01 * since) * Math.Exp(-3 * since) * Math.Min(1, since * 200);
            }

            samples[i] = (float)(pad * 0.045 + bell * 0.06);
        }

        // A gentle low-pass, run twice round the loop so the filter's memory at the start matches the end.
        var smoothed = 0.0;
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < total; i++)
            {
                smoothed += (samples[i] - smoothed) * 0.35;
                if (pass == 1) samples[i] = (float)smoothed;
            }
        return samples;
    }

    /// <summary>A raised cosine of the given width centred at <paramref name="centre"/>, measured round the loop.</summary>
    private static double Swell(int i, double centre, double width, int total)
    {
        var distance = Math.Abs(i - centre);
        distance = Math.Min(distance, total - distance);
        if (distance >= width / 2) return 0;
        return 0.5 + 0.5 * Math.Cos(Math.PI * distance / (width / 2));
    }

    /// <summary>The loop as a 16-bit mono WAV.</summary>
    public static byte[] Wav()
    {
        var samples = Samples();
        var dataLength = samples.Length * 2;
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataLength);
        foreach (var sample in samples)
            writer.Write((short)Math.Round(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
        writer.Flush();
        return stream.ToArray();
    }
}

namespace ConsoleMode.Services;

/// <summary>
/// The colour a picture "feels like", for tinting the glass and the background of its card. Pure (it reads the
/// pixels it is handed), so it is tested; the view model decodes the picture small and calls it.
/// </summary>
public static class DominantColor
{
    /// <summary>What a card without a picture (or with one that has no colour) uses.</summary>
    public const uint Neutral = 0xFF3A4250;

    /// <summary>
    /// The average of the picture's pixels weighted by how colourful they are (a grey sky does not wash out a red
    /// roof), then lifted to a colour that is neither washed out nor too dark to glow on glass. Opaque ARGB.
    /// <paramref name="bgra"/> is Bgra8, four bytes a pixel.
    /// </summary>
    public static uint FromBgra(ReadOnlySpan<byte> bgra)
    {
        double r = 0, g = 0, b = 0, weight = 0;
        for (var i = 0; i + 3 < bgra.Length; i += 4)
        {
            if (bgra[i + 3] < 128) continue;
            double pb = bgra[i] / 255.0, pg = bgra[i + 1] / 255.0, pr = bgra[i + 2] / 255.0;
            var max = Math.Max(pr, Math.Max(pg, pb));
            var min = Math.Min(pr, Math.Min(pg, pb));
            var saturation = max <= 0 ? 0 : (max - min) / max;
            // Very dark and very bright pixels (black borders, white outlines) say little about the picture.
            var midtone = 1 - Math.Abs(max - 0.55) * 1.2;
            var w = (0.05 + saturation * saturation) * Math.Max(midtone, 0.05);
            r += pr * w; g += pg * w; b += pb * w; weight += w;
        }
        if (weight <= 0) return Neutral;
        return Lift(r / weight, g / weight, b / weight);
    }

    /// <summary>Keeps the hue; saturation at least 0.45 and brightness between 0.55 and 0.85.</summary>
    public static uint Lift(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double h = 0;
        if (delta > 1e-9)
        {
            if (max == r) h = ((g - b) / delta) % 6;
            else if (max == g) h = (b - r) / delta + 2;
            else h = (r - g) / delta + 4;
            h *= 60;
            if (h < 0) h += 360;
        }
        var s = max <= 0 ? 0 : delta / max;
        // A grey picture stays grey instead of being forced to a hue it never had.
        s = s < 0.08 ? s : Math.Max(s, 0.45);
        var v = Math.Clamp(max, 0.55, 0.85);
        return FromHsv(h, Math.Min(s, 1), v);
    }

    /// <summary>Blends two colours (RGB of each, alpha ignored) by <paramref name="amount"/> from the first to the second.</summary>
    public static uint Blend(uint from, uint to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        static byte Mix(uint a, uint b, int shift, double t) =>
            (byte)Math.Round(((a >> shift) & 0xFF) * (1 - t) + ((b >> shift) & 0xFF) * t);
        return 0xFF000000u | ((uint)Mix(from, to, 16, amount) << 16) | ((uint)Mix(from, to, 8, amount) << 8) | Mix(from, to, 0, amount);
    }

    private static uint FromHsv(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        var (r, g, b) = (int)(h / 60) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };
        static uint Byte(double value) => (uint)Math.Clamp(Math.Round((value) * 255), 0, 255);
        return 0xFF000000u | (Byte(r + m) << 16) | (Byte(g + m) << 8) | Byte(b + m);
    }
}

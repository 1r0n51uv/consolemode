using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class DominantColorTests
{
    private static byte[] Solid(int pixels, byte b, byte g, byte r, byte a = 255)
    {
        var data = new byte[pixels * 4];
        for (var i = 0; i < pixels; i++) { data[i * 4] = b; data[i * 4 + 1] = g; data[i * 4 + 2] = r; data[i * 4 + 3] = a; }
        return data;
    }

    private static (int R, int G, int B) Parts(uint argb) => ((int)(argb >> 16 & 0xFF), (int)(argb >> 8 & 0xFF), (int)(argb & 0xFF));

    [Fact]
    public void A_red_picture_gives_a_red_colour()
    {
        var (r, g, b) = Parts(DominantColor.FromBgra(Solid(64, 20, 20, 220)));
        Assert.True(r > g && r > b, $"{r},{g},{b}");
    }

    [Fact]
    public void A_green_picture_gives_a_green_colour()
    {
        var (r, g, b) = Parts(DominantColor.FromBgra(Solid(64, 40, 200, 60)));
        Assert.True(g > r && g > b, $"{r},{g},{b}");
    }

    [Fact]
    public void The_colourful_part_wins_over_a_grey_one()
    {
        var data = Solid(100, 128, 128, 128).Concat(Solid(10, 30, 30, 230)).ToArray();
        var (r, g, b) = Parts(DominantColor.FromBgra(data));
        Assert.True(r > g + 40 && r > b + 40, $"{r},{g},{b}");
    }

    [Fact]
    public void Nothing_to_read_gives_the_neutral_colour()
    {
        Assert.Equal(DominantColor.Neutral, DominantColor.FromBgra([]));
        Assert.Equal(DominantColor.Neutral, DominantColor.FromBgra(Solid(16, 10, 20, 30, a: 0)));
    }

    [Fact]
    public void The_colour_is_always_opaque_and_bright_enough_to_glow()
    {
        foreach (var (b, g, r) in new (byte, byte, byte)[] { (5, 5, 90), (250, 250, 250), (200, 30, 30), (10, 10, 10) })
        {
            var color = DominantColor.FromBgra(Solid(32, b, g, r));
            Assert.Equal(0xFFu, color >> 24);
            var (cr, cg, cb) = Parts(color);
            Assert.True(Math.Max(cr, Math.Max(cg, cb)) >= 0.55 * 255 - 2, $"{cr},{cg},{cb}");
        }
    }

    [Fact]
    public void Blend_goes_from_one_colour_to_the_other()
    {
        Assert.Equal(0xFF000000u, DominantColor.Blend(0xFF000000, 0xFFFFFFFF, 0));
        Assert.Equal(0xFFFFFFFFu, DominantColor.Blend(0xFF000000, 0xFFFFFFFF, 1));
        Assert.Equal(0xFF808080u, DominantColor.Blend(0xFF000000, 0xFFFFFFFF, 0.5));
    }

    [Fact]
    public void The_glass_takes_the_colour_of_the_card()
    {
        var plain = CardMaterial.Look(CardMaterial.Dark);
        var red = CardMaterial.Look(CardMaterial.Dark, 0xFFFF0000);
        Assert.NotEqual(plain.Tint, red.Tint);
        Assert.Equal(0xFFFF0000u, CardMaterial.Look(CardMaterial.Accent, 0xFFFF0000).Tint);
    }
}

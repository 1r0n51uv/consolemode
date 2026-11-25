using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class CardMaterialTests
{
    [Theory]
    [InlineData(null, "dark")]
    [InlineData("", "dark")]
    [InlineData("neon", "dark")]
    [InlineData("light", "light")]
    [InlineData("accent", "accent")]
    public void An_unknown_material_reads_as_dark(string? saved, string expected) =>
        Assert.Equal(expected, CardMaterial.Normalize(saved));

    [Fact]
    public void Cycling_visits_every_material_and_comes_back()
    {
        var seen = new List<string>();
        var current = CardMaterial.Dark;
        for (var i = 0; i < CardMaterial.All.Length; i++)
        {
            seen.Add(current);
            current = CardMaterial.Next(current);
        }
        Assert.Equal(CardMaterial.All, seen);
        Assert.Equal(CardMaterial.Dark, current);
    }

    [Fact]
    public void Every_material_has_a_look_and_a_name()
    {
        foreach (var material in CardMaterial.All)
        {
            var (_, opacity, luminosity) = CardMaterial.Look(material);
            Assert.InRange(opacity, 0.0, 1.0);
            Assert.InRange(luminosity, 0.0, 1.0);
            Assert.StartsWith("Material", CardMaterial.NameKey(material));
        }
    }
}

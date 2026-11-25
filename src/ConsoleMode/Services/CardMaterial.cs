namespace ConsoleMode.Services;

/// <summary>
/// The glass the cards, rows and bars are made of: a tint over the blurred background. Pure, so it is tested;
/// the view (ConsoleHomeView.ApplyCardMaterial) pours the numbers into the shared "GlassFill" acrylic brush.
/// </summary>
public static class CardMaterial
{
    public const string Dark = "dark";
    public const string Light = "light";
    public const string Clear = "clear";
    public const string Accent = "accent";

    public static readonly string[] All = [Dark, Light, Clear, Accent];

    /// <summary>A saved value that is not one of ours (or none) reads as the dark glass.</summary>
    public static string Normalize(string? material) =>
        Array.IndexOf(All, material) >= 0 ? material! : Dark;

    /// <summary>Dark → light → clear → accent → dark (the console row cycles with A).</summary>
    public static string Next(string? material) =>
        All[(Array.IndexOf(All, Normalize(material)) + 1) % All.Length];

    /// <summary>Tint colour (ARGB, the alpha is ignored), the tint's opacity and how much of the backdrop's luminosity shows through.</summary>
    public static (uint Tint, double Opacity, double Luminosity) Look(string? material) => Normalize(material) switch
    {
        Light => (0xFFFFFFFF, 0.26, 0.45),
        Clear => (0xFF000000, 0.10, 0.20),
        Accent => (0xFF2FB88C, 0.42, 0.55),
        _ => (0xFF0D141D, 0.42, 0.60)
    };

    /// <summary>
    /// The look with the colour of the card in focus mixed in: the glass takes some of it (all of it for the accent
    /// material), so a green card and a red one do not sit on the same grey glass.
    /// </summary>
    public static (uint Tint, double Opacity, double Luminosity) Look(string? material, uint cardColor)
    {
        var (tint, opacity, luminosity) = Look(material);
        var amount = Normalize(material) switch { Accent => 1.0, Clear => 0.35, Light => 0.25, _ => 0.30 };
        return (DominantColor.Blend(tint, cardColor, amount), opacity, luminosity);
    }

    /// <summary>The key of the name shown for a material.</summary>
    public static string NameKey(string? material) => Normalize(material) switch
    {
        Light => "MaterialLight",
        Clear => "MaterialClear",
        Accent => "MaterialAccent",
        _ => "MaterialDark"
    };
}

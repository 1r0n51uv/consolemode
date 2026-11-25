using System.Security.Cryptography;
using System.Text;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace ConsoleMode.ViewModels;

// Each launcher card can have its own picture (the card) and its own background (the whole screen while the card is
// open). Both are copied into the data folder, so moving or deleting the original does not break the card. The colours
// of the glass, the glow and the Play pill come from them (DominantColor).
public partial class MainViewModel
{
    public const string ImageKindCard = "card";
    public const string ImageKindBackground = "background";

    /// <summary>Picture paths by launcher value ("playnite", "customApp:&lt;id&gt;").</summary>
    private Dictionary<string, LaunchImageSet> _launchImages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The window asks for a file picker: the card and the kind of picture ("card" or "background").</summary>
    public event Action<LaunchTile, string>? PickLaunchImageRequested;

    /// <summary>The background of the card in focus, over the usual one; null when it has none.</summary>
    public ImageSource? AmbientBackground => CurrentAmbientTile?.BackgroundImage;
    public bool HasAmbientBackground => AmbientBackground is not null;

    private void LoadLaunchImages(AppConfig config) =>
        _launchImages = new Dictionary<string, LaunchImageSet>(
            (config.LaunchImages ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Key))
                .ToDictionary(p => p.Key, p => new LaunchImageSet { Card = p.Value?.Card ?? "", Background = p.Value?.Background ?? "" }),
            StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, LaunchImageSet> LaunchImagesToSave() =>
        _launchImages.Where(p => !p.Value.IsEmpty)
            .ToDictionary(p => p.Key, p => new LaunchImageSet { Card = p.Value.Card, Background = p.Value.Background }, StringComparer.OrdinalIgnoreCase);

    /// <summary>The card in focus, or the picked launcher's while none is.</summary>
    private LaunchTile? CurrentAmbientTile => AmbientTile ?? LaunchTiles.FirstOrDefault(t => t.IsSelected);

    private void NotifyAmbient()
    {
        OnPropertyChanged(nameof(AmbientColor));
        OnPropertyChanged(nameof(AmbientFade));
        OnPropertyChanged(nameof(AmbientBackground));
        OnPropertyChanged(nameof(HasAmbientBackground));
    }

    /// <summary>Loads the pictures of every launcher card and works out its colour.</summary>
    private void ApplyLaunchImages()
    {
        foreach (var tile in LaunchTiles.Where(t => t.IsLauncher)) _ = ApplyLaunchImagesAsync(tile);
    }

    private async Task ApplyLaunchImagesAsync(LaunchTile tile)
    {
        try
        {
            _launchImages.TryGetValue(tile.Option.Value, out var set);
            var card = set is not null && File.Exists(set.Card) ? set.Card : "";
            var background = set is not null && File.Exists(set.Background) ? set.Background : "";
            tile.CardImage = card.Length > 0 ? new BitmapImage(new Uri(card)) { DecodePixelWidth = 720 } : null;
            tile.BackgroundImage = background.Length > 0 ? new BitmapImage(new Uri(background)) { DecodePixelWidth = 1920 } : null;
            // The colour comes from the background when there is one (it is what fills the screen), else from the card.
            var source = background.Length > 0 ? background : card;
            var color = source.Length > 0
                ? await ReadDominantAsync(source) ?? DominantColor.Neutral
                : LaunchArt.For(tile.Option.Value).From;
            tile.Accent = Argb(color);
            if (ReferenceEquals(CurrentAmbientTile, tile)) NotifyAmbient();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: imagens de \"{tile.Text}\": {ex.Message}");
        }
    }

    /// <summary>The picture decoded to 32 × 32 and read for its colour; null when it can't be read.</summary>
    private static async Task<uint?> ReadDominantAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var transform = new BitmapTransform { ScaledWidth = 32, ScaledHeight = 32 };
            var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform,
                ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            return DominantColor.FromBgra(data.DetachPixelData());
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: cor da imagem {path}: {ex.Message}");
            return null;
        }
    }

    private static string LaunchImagesDir => Path.Combine(AppPaths.DataDir, "images");

    /// <summary>Copies the picked picture into the data folder and puts it on the card.</summary>
    public void SetLaunchImage(LaunchTile tile, string kind, string sourcePath)
    {
        if (!tile.IsLauncher || !SteamArt.IsSupportedImage(sourcePath)) return;
        try
        {
            Directory.CreateDirectory(LaunchImagesDir);
            var key = tile.Option.Value;
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key)))[..12].ToLowerInvariant();
            var target = Path.Combine(LaunchImagesDir, $"{hash}-{kind}-{DateTime.UtcNow.Ticks}{Path.GetExtension(sourcePath).ToLowerInvariant()}");
            File.Copy(sourcePath, target, overwrite: true);
            if (!_launchImages.TryGetValue(key, out var set)) _launchImages[key] = set = new LaunchImageSet();
            var old = kind == ImageKindBackground ? set.Background : set.Card;
            if (kind == ImageKindBackground) set.Background = target; else set.Card = target;
            DeleteQuietly(old);
            SaveQuietly();
            _ = ApplyLaunchImagesAsync(tile);
            AppLog.Write($"Início: imagem ({kind}) de \"{tile.Text}\" definida");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: imagem de \"{tile.Text}\": {ex.Message}");
            SetStatus(LocalizationService.Get("PlaynitePathPickError", ex.Message), InfoBarSeverity.Error);
        }
    }

    private void ClearLaunchImages(LaunchTile tile)
    {
        if (!_launchImages.Remove(tile.Option.Value, out var set)) return;
        DeleteQuietly(set.Card);
        DeleteQuietly(set.Background);
        SaveQuietly();
        _ = ApplyLaunchImagesAsync(tile);
    }

    private static void DeleteQuietly(string? path)
    {
        try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
        catch { /* a picture left behind is harmless */ }
    }

    private bool HasLaunchImages(LaunchTile tile) =>
        _launchImages.TryGetValue(tile.Option.Value, out var set) && !set.IsEmpty;
}

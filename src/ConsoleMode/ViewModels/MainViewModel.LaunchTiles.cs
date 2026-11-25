using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

/// <summary>One launcher in the console Home row (Steam, Playnite, Xbox, custom app).</summary>
public sealed partial class LaunchTile : ObservableObject
{
    public required ComboOption Option { get; init; }
    /// <summary>The "+" tile after the launchers: A on it adds a custom app; the focus on it picks nothing.</summary>
    public bool IsAdd { get; init; }
    public string Text => Option.Text;
    public string Glyph { get; init; } = "";
    public Brush Background { get; init; } = null!;
    /// <summary>The launcher's own logo (Playnite's, the custom app's); until then, or without it, the glyph.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasLogo), nameof(ShowGlyph))] private ImageSource? _logo;
    public bool HasLogo => Logo is not null;
    /// <summary>The app whose logo this tile is waiting for (exe path or Store app id).</summary>
    public string? LogoKey { get; set; }
    public bool ShowGlyph => Logo is null;
    /// <summary>The "Settings" card at the end of the dock: A on it opens every setting.</summary>
    public bool IsSettings { get; init; }
    /// <summary>A real launcher (not "+" or Settings): it has Play, a menu and pictures.</summary>
    public bool IsLauncher => !IsAdd && !IsSettings;
    /// <summary>What an action card says it does, under its name (launchers show where and what changes instead).</summary>
    public string Info { get; init; } = "";
    public bool HasInfo => Info.Length > 0;
    /// <summary>The label of the pill in the open card: Play now, Add, Settings.</summary>
    [ObservableProperty] private string _actionText = "";
    public string ActionGlyph => IsSettings ? "\uE713" : IsAdd ? "\uE710" : "\uE768";
    /// <summary>The card's own picture; null shows the glass tinted with its colour.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCardImage))] private ImageSource? _cardImage;
    public bool HasCardImage => CardImage is not null;
    /// <summary>A background for the whole screen while this card is the one in focus; null keeps the usual background.</summary>
    [ObservableProperty] private ImageSource? _backgroundImage;
    /// <summary>The card's colour: from its background or card picture when it has one, else the launcher's own.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(AccentBrush), nameof(TintBrush))] private Windows.UI.Color _accent;
    public Brush AccentBrush => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xCC, Accent.R, Accent.G, Accent.B));
    public Brush TintBrush => new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(0x73, Accent.R, Accent.G, Accent.B), Offset = 0 },
            new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(0x26, Accent.R, Accent.G, Accent.B), Offset = 1 }
        }
    };

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Opacity))] private bool _isSelected;
    /// <summary>The other launchers fade back, so the one that opens stands out.</summary>
    public double Opacity => IsSelected ? 1 : 0.6;
}

// The console Home is a row of launchers over the game art, like a console's game row:
// moving the focus onto a tile picks what opens (see ConsoleHomeView), and A on it plays it.
public partial class MainViewModel
{
    public ObservableCollection<LaunchTile> LaunchTiles { get; } = [];

    /// <summary>The line under the launcher name: audio, FPS, HDR, VRR (the launcher itself is the title).</summary>
    [ObservableProperty] private string _homeMetaText = "";

    private void RebuildLaunchTiles()
    {
        LaunchTiles.Clear();
        foreach (var option in LaunchOptions)
        {
            var art = LaunchArt.For(option.Value);
            LaunchTiles.Add(new LaunchTile
            {
                Option = option,
                Glyph = art.Glyph,
                Background = TileGradient(art.From, art.To),
                Accent = Argb(art.From),
                ActionText = LocalizationService.Get("StartNow"),
                IsSelected = ReferenceEquals(option, SelectedLaunch)
            });
        }
        var add = LaunchArt.For(LaunchArt.AddCustomApp);
        LaunchTiles.Add(new LaunchTile
        {
            Option = new ComboOption { Text = LocalizationService.Get("AddCustomAppTile"), Value = LaunchArt.AddCustomApp },
            Glyph = add.Glyph,
            Background = TileGradient(add.From, add.To),
            Accent = Argb(0xFF3A4250),
            Info = LocalizationService.Get("AddCustomAppDescription"),
            ActionText = LocalizationService.Get("AddCustomAppCard"),
            IsAdd = true
        });
        LaunchTiles.Add(new LaunchTile
        {
            Option = new ComboOption { Text = LocalizationService.Get("Settings"), Value = "settings" },
            Glyph = "\uE713",
            Background = TileGradient(0xFF3A4250, 0xFF1A1E25),
            Accent = Argb(0xFF3A4250),
            Info = LocalizationService.Get("SettingsTileInfo"),
            ActionText = LocalizationService.Get("Settings"),
            IsSettings = true
        });
        foreach (var tile in LaunchTiles.Where(t => t.IsLauncher)) _ = LoadLaunchLogoAsync(tile);
    }

    /// <summary>Read at this size (pixels), enough for a 4K TV; each tile then gets it shrunk to what it draws.</summary>
    private const int LaunchLogoSize = 256;

    /// <summary>The logo's size on a tile (ConsoleHomeView.xaml), grown by the focused tile's 1.14 (ConsoleHomeView.ScaleTile).</summary>
    private const double LaunchLogoShown = 80 * 1.14;

    /// <summary>The screen's pixels per interface pixel (Windows' scale times the interface's, UiScaleHost), from the window.</summary>
    private double _logoScale = 1;

    // The logos are read once per app: the tiles are rebuilt on every language switch.
    // A Store app's logo is a file of its package; a program's, the icon pixels of its exe. Both end up as pixels.
    private readonly Dictionary<string, IconBitmap?> _launchLogos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The window moved to a screen with another scale: the logos are shrunk again for it.</summary>
    public void SetLogoScale(double scale)
    {
        if (scale <= 0 || Math.Abs(scale - _logoScale) < 0.01) return;
        _logoScale = scale;
        foreach (var tile in LaunchTiles)
            if (tile.LogoKey is { } key && _launchLogos.TryGetValue(key, out var logo) && logo is not null) ShowLaunchLogo(tile, logo);
    }

    /// <summary>
    /// The logo shrunk here, averaging every pixel, to the pixels the tile shows: left to the screen, a 256-pixel
    /// logo drawn at 64 comes out with jagged edges.
    /// </summary>
    private void ShowLaunchLogo(LaunchTile tile, IconBitmap logo)
    {
        var (width, height) = IconPixels.FitInside(logo.Width, logo.Height, (int)Math.Ceiling(LaunchLogoShown * _logoScale));
        var pixels = IconPixels.Shrink(logo.Pixels, logo.Width, logo.Height, width, height);
        var image = new WriteableBitmap(width, height);
        using (var stream = image.PixelBuffer.AsStream()) stream.Write(pixels);
        tile.Logo = image;
    }

    /// <summary>A package's logo file as premultiplied BGRA, no larger than <see cref="LaunchLogoSize"/>.</summary>
    private static async Task<IconBitmap> ReadLogoFileAsync(string file)
    {
        var storage = await Windows.Storage.StorageFile.GetFileFromPathAsync(file);
        using var stream = await storage.OpenReadAsync();
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var (width, height) = IconPixels.FitInside((int)decoder.PixelWidth, (int)decoder.PixelHeight, LaunchLogoSize);
        var transform = new Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = (uint)width,
            ScaledHeight = (uint)height,
            InterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant
        };
        var data = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, transform,
            Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        return new IconBitmap(width, height, data.DetachPixelData());
    }

    /// <summary>A launcher's path changed: its tile shows the new app's logo (or the glyph).</summary>
    private void RefreshLaunchLogo(string mode)
    {
        foreach (var tile in LaunchTiles.Where(t => t.Option.Value == mode))
        {
            tile.Logo = null;
            _ = LoadLaunchLogoAsync(tile);
        }
    }

    private async Task LoadLaunchLogoAsync(LaunchTile tile)
    {
        var custom = CustomApp.EntryId(tile.Option.Value) is { } id ? _customApps.FirstOrDefault(a => a.Id == id) : null;
        var (exe, appId) = tile.Option.Value switch
        {
            "playnite" => (Engine.Launch.GetPlaynitePath(), (string?)null),
            _ when custom is not null => (CustomApp.CleanPath(custom.Path), null),
            _ => (null, null)
        };
        var key = appId ?? exe;
        // The app this tile now waits for: a slower load for a previous path must not land after this one.
        tile.LogoKey = key;
        if (string.IsNullOrWhiteSpace(key)) return;
        try
        {
            if (!_launchLogos.TryGetValue(key, out var logo))
            {
                var file = appId is null ? null : await Task.Run(() => PackageLogos.Find(appId));
                logo = file is not null
                    ? await Task.Run(() => ReadLogoFileAsync(file))
                    : await Task.Run(() => WindowIcons.ForApp(exe, appId, LaunchLogoSize));
                _launchLogos[key] = logo;
            }
            if (!string.Equals(tile.LogoKey, key, StringComparison.OrdinalIgnoreCase) || logo is null) return;
            ShowLaunchLogo(tile, logo);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: ícone de \"{key}\": {ex.Message}");
        }
    }

    private void SyncLaunchTiles()
    {
        foreach (var tile in LaunchTiles) tile.IsSelected = tile.IsLauncher && ReferenceEquals(tile.Option, SelectedLaunch);
    }

    private void SelectLaunchTile(LaunchTile? tile)
    {
        if (tile is { IsLauncher: true } && !ReferenceEquals(SelectedLaunch, tile.Option)) SelectedLaunch = tile.Option;
    }

    /// <summary>
    /// A (or a click) on a card of the dock: a launcher picks itself and plays, "+" adds a custom app, Settings opens
    /// every setting.
    /// </summary>
    [RelayCommand]
    private async Task ChooseLaunchTile(LaunchTile? tile)
    {
        if (tile is null) return;
        if (tile.IsSettings) { OpenFullSettings(); return; }
        if (tile.IsAdd) { await AddCustomAppAsync(); return; }
        SelectLaunchTile(tile);
        if (CanStart) StartCommand.Execute(null);
    }

    /// <summary>The card the focus or the pointer is on: its colour tints the background and the glass.</summary>
    [ObservableProperty] private LaunchTile? _ambientTile;

    partial void OnAmbientTileChanged(LaunchTile? value)
    {
        OnPropertyChanged(nameof(AmbientColor));
        OnPropertyChanged(nameof(AmbientFade));
    }

    private void UpdateHomeMeta()
    {
        // SummaryItems starts with the launcher, which is already the title. The resolution comes
        // before this text in the view, so every item here is led by its separator.
        HomeMetaText = string.Concat(SummaryItems.Skip(1).Select(item => "  ·  " + item));
    }

    /// <summary>The selected launcher's colour, faint, for the glow over the background; and the same colour fully clear.</summary>
    public Windows.UI.Color AmbientColor => Tint(0x70);
    public Windows.UI.Color AmbientFade => Tint(0x00);

    private Windows.UI.Color Tint(byte alpha)
    {
        var tile = AmbientTile ?? LaunchTiles.FirstOrDefault(t => t.IsSelected);
        var color = tile?.Accent ?? Argb(LaunchArt.For(SelectedLaunch?.Value).From);
        return Microsoft.UI.ColorHelper.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>The tile's colours, see-through enough for the glass under them to show.</summary>
    private static uint Glassy(uint argb, byte alpha) => (argb & 0x00FFFFFF) | ((uint)alpha << 24);

    private static LinearGradientBrush TileGradient(uint from, uint to) => new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Argb(Glassy(from, 0x99)), Offset = 0 },
            new GradientStop { Color = Argb(Glassy(to, 0x66)), Offset = 1 }
        }
    };

    private static Windows.UI.Color Argb(uint v) =>
        Microsoft.UI.ColorHelper.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
}

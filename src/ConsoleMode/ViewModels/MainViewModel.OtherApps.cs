using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

// ControlFS's logo, shown on the session menu's file explorer row.
public partial class MainViewModel
{
    private ImageSource? _controlFsLogo;

    public ImageSource? ControlFsLogo => _controlFsLogo ??= LoadLogo("controlfs.png");
    /// <summary>Embedded, so the installed and portable builds need no loose files.</summary>
    private static BitmapImage? LoadLogo(string file)
    {
        try
        {
            using var resource = typeof(MainViewModel).Assembly.GetManifestResourceStream($"ConsoleMode.Assets.OtherApps.{file}");
            if (resource is null) return null;
            // Copied: the image may decode after this method returns.
            var copy = new MemoryStream();
            resource.CopyTo(copy);
            copy.Position = 0;
            var image = new BitmapImage();
            image.SetSource(copy.AsRandomAccessStream());
            return image;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Outros apps: logo {file}: {ex.Message}");
            return null;
        }
    }
}

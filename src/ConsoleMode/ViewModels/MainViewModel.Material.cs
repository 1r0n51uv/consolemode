using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The glass of the console interface (System → App): dark, light, clear or tinted with the accent.
public partial class MainViewModel
{
    [ObservableProperty] private string _glassMaterial = CardMaterial.Dark;

    public string GlassMaterialText => LocalizationService.Get(CardMaterial.NameKey(GlassMaterial));

    partial void OnGlassMaterialChanged(string value)
    {
        OnPropertyChanged(nameof(GlassMaterialText));
        if (_applying || IsLoading) return;
        SaveQuietly();
    }

    public void CycleCardMaterial() => GlassMaterial = CardMaterial.Next(GlassMaterial);
}

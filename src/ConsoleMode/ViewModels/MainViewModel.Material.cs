using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The glass of the console interface (System → App): dark, light, clear or tinted with the accent.
public partial class MainViewModel
{
    [ObservableProperty] private string _cardMaterial = CardMaterial.Dark;

    public string CardMaterialText => LocalizationService.Get(CardMaterial.NameKey(CardMaterialValue));

    private string CardMaterialValue => CardMaterial.Normalize(_cardMaterial);

    partial void OnCardMaterialChanged(string value)
    {
        OnPropertyChanged(nameof(CardMaterialText));
        if (_applying || IsLoading) return;
        SaveQuietly();
    }

    public void CycleCardMaterial() => CardMaterial = Services.CardMaterial.Next(_cardMaterial);
}

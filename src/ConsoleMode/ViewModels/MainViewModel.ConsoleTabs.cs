using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ConsoleMode.ViewModels;

// The console interface is Home (the dock of launchers) and, behind its Settings card, System (every setting, by
// category; the screens are there too). The Session page and the tab bar are gone: no LB / RB.
public partial class MainViewModel
{
    public const int HomeTabIndex = 0;
    public const int SessionTabIndex = 1;
    public const int SystemTabIndex = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeTab), nameof(IsSessionTab), nameof(IsSystemTab))]
    private int _consoleTabIndex;

    public bool IsHomeTab => ConsoleTabIndex == HomeTabIndex;
    public bool IsSessionTab => ConsoleTabIndex == SessionTabIndex;
    public bool IsSystemTab => ConsoleTabIndex == SystemTabIndex;

    public string HintLb => IsPlayStationHints ? "L1" : "LB";
    public string HintRb => IsPlayStationHints ? "R1" : "RB";

    /// <summary>The System tab's category on the left; only its options show on the right.</summary>
    [ObservableProperty] private int _systemCategory;

    /// <summary>x:Bind helper: the options of one System category show only while it is the chosen one.</summary>
    public Microsoft.UI.Xaml.Visibility CategoryVisibility(int current, int category) =>
        current == category ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>x:Bind helper: the accent bar shows only next to the chosen System category (it fades, so it's an opacity).</summary>
    public double IndicatorOpacity(int current, int category) => current == category ? 1.0 : 0.0;

    /// <summary>x:Bind helper: a veil that shows (fading) only while the flag is on.</summary>
    public double ShownOpacity(bool shown) => shown ? 1.0 : 0.0;

    /// <summary>x:Bind helper: the active tab is fully lit, the others dimmed.</summary>
    public double TabOpacity(int current, int tab) => current == tab ? 1.0 : 0.55;

    // The System tab is the old "all settings" list: the flag stays in step with it.
    partial void OnConsoleTabIndexChanged(int value) => IsConsoleSettingsOpen = value == SystemTabIndex;

    [RelayCommand]
    private void SelectConsoleTab(string index)
    {
        if (int.TryParse(index, out var value)) ConsoleTabIndex = value == SystemTabIndex ? SystemTabIndex : HomeTabIndex;
    }

    /// <summary>LB / RB used to switch tabs; there is one page now, so they do nothing.</summary>
    public bool StepConsoleTab(int step) => false;
}

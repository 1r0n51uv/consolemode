using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ConsoleMode.Controls;

/// <summary>
/// One tile of the Home row. The focus selects it (the page shows its game and background); A plays it. With the
/// mouse, the first click selects and a click on the selected tile plays.
/// </summary>
public sealed partial class HomeTile : UserControl
{
    public static readonly DependencyProperty TileProperty =
        DependencyProperty.Register(nameof(Tile), typeof(LaunchTile), typeof(HomeTile), new PropertyMetadata(null, (d, _) => ((HomeTile)d).Bindings.Update()));

    private static readonly Windows.UI.ViewManagement.UISettings Ui = new();

    private bool _focused;
    /// <summary>The tile had the focus already when the pointer pressed it: the click plays instead of selecting.</summary>
    private bool _wasFocusedOnPress = true;

    public HomeTile()
    {
        InitializeComponent();
        Body.AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => _wasFocusedOnPress = _focused), true);
    }

    public LaunchTile? Tile
    {
        get => (LaunchTile?)GetValue(TileProperty);
        set => SetValue(TileProperty, value);
    }

    /// <summary>The tile's button: the one that takes the focus.</summary>
    public Button BodyButton => Body;

    /// <summary>A, Enter, or a click on the tile that was already selected.</summary>
    public event RoutedEventHandler? Invoked;

    /// <summary>A right click: the tile's menu.</summary>
    public event RightTappedEventHandler? MenuRequested;

    /// <summary>The tile took the focus: the page shows it.</summary>
    public event EventHandler? Selected;

    private void OnBodyClick(object sender, RoutedEventArgs e)
    {
        var wasFocused = _wasFocusedOnPress;
        _wasFocusedOnPress = true;
        if (wasFocused) Invoked?.Invoke(this, e);
    }

    private void OnBodyRightTapped(object sender, RightTappedRoutedEventArgs e) => MenuRequested?.Invoke(this, e);

    private void OnBodyGotFocus(object sender, RoutedEventArgs e)
    {
        _focused = true;
        VisualStateManager.GoToState(this, "Selected", Ui.AnimationsEnabled);
        Selected?.Invoke(this, EventArgs.Empty);
    }

    private void OnBodyLostFocus(object sender, RoutedEventArgs e)
    {
        _focused = false;
        VisualStateManager.GoToState(this, "Rest", Ui.AnimationsEnabled);
    }
}

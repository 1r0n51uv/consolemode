using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ConsoleMode.Controls;

/// <summary>
/// One card of the Home dock. It opens (grows and shows its details and Play) while it holds the focus or the
/// pointer is over it, and closes again when both are gone.
/// </summary>
public sealed partial class DockCard : UserControl
{
    public static readonly DependencyProperty TileProperty =
        DependencyProperty.Register(nameof(Tile), typeof(LaunchTile), typeof(DockCard), new PropertyMetadata(null, (d, _) => ((DockCard)d).Bindings.Update()));

    public static readonly DependencyProperty HostProperty =
        DependencyProperty.Register(nameof(Host), typeof(MainViewModel), typeof(DockCard), new PropertyMetadata(null, (d, _) => ((DockCard)d).Bindings.Update()));

    private static readonly Windows.UI.ViewManagement.UISettings Ui = new();

    private bool _focused;
    private bool _pointerOver;

    public DockCard()
    {
        InitializeComponent();
    }

    public LaunchTile? Tile
    {
        get => (LaunchTile?)GetValue(TileProperty);
        set => SetValue(TileProperty, value);
    }

    public MainViewModel? Host
    {
        get => (MainViewModel?)GetValue(HostProperty);
        set => SetValue(HostProperty, value);
    }

    /// <summary>The card's button: the one that takes the focus.</summary>
    public Button BodyButton => Body;

    /// <summary>A, Enter or a click on the card.</summary>
    public event RoutedEventHandler? Invoked;

    /// <summary>A right click on the card: its menu.</summary>
    public event RightTappedEventHandler? MenuRequested;

    private void OnBodyClick(object sender, RoutedEventArgs e) => Invoked?.Invoke(this, e);

    private void OnBodyRightTapped(object sender, RightTappedRoutedEventArgs e) => MenuRequested?.Invoke(this, e);

    private void OnBodyGotFocus(object sender, RoutedEventArgs e)
    {
        _focused = true;
        Refresh();
    }

    private void OnBodyLostFocus(object sender, RoutedEventArgs e)
    {
        _focused = false;
        Refresh();
    }

    private void OnBodyPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _pointerOver = true;
        Refresh();
    }

    private void OnBodyPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _pointerOver = false;
        Refresh();
    }

    /// <summary>The card just opened (focus or pointer): the view tints the background with its colour.</summary>
    public event EventHandler? Opened;

    private void Refresh()
    {
        var open = _focused || _pointerOver;
        VisualStateManager.GoToState(this, open ? "Expanded" : "Compact", Ui.AnimationsEnabled);
        if (open) Opened?.Invoke(this, EventArgs.Empty);
    }
}

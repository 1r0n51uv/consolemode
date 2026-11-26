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

    // The pointer has to rest a moment before the card opens, and has to be gone a moment before it closes, so
    // sweeping across the dock does not make every card open and shut. The focus acts at once.
    private static readonly TimeSpan PointerOpenDelay = TimeSpan.FromMilliseconds(140);
    private static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(320);

    private bool _focused;
    private bool _pointerOver;
    private bool _open;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _timer;

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
        Decide(immediate: true);
    }

    private void OnBodyLostFocus(object sender, RoutedEventArgs e)
    {
        _focused = false;
        Decide(immediate: false);
    }

    private void OnBodyPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _pointerOver = true;
        Decide(immediate: false);
    }

    private void OnBodyPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _pointerOver = false;
        Decide(immediate: false);
    }

    /// <summary>The card opened (focus or pointer): the view tints the background with its colour.</summary>
    public event EventHandler? Opened;

    /// <summary>The card closed again.</summary>
    public event EventHandler? Closed;

    private void Decide(bool immediate)
    {
        _timer?.Stop();
        var wanted = _focused || _pointerOver;
        if (wanted == _open) return;
        if (immediate || !Ui.AnimationsEnabled) { Apply(wanted); return; }
        _timer ??= DispatcherQueue.CreateTimer();
        _timer.IsRepeating = false;
        _timer.Interval = wanted ? PointerOpenDelay : CloseDelay;
        _timer.Tick -= OnTimer;
        _timer.Tick += OnTimer;
        _timer.Start();
    }

    private void OnTimer(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        Apply(_focused || _pointerOver);
    }

    private void Apply(bool open)
    {
        if (open == _open) return;
        _open = open;
        VisualStateManager.GoToState(this, open ? "Expanded" : "Compact", Ui.AnimationsEnabled);
        if (open) Opened?.Invoke(this, EventArgs.Empty);
        else Closed?.Invoke(this, EventArgs.Empty);
    }
}

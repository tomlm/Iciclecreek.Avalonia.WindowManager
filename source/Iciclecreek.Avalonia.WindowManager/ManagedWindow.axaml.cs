using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Iciclecreek.Avalonia.WindowManager;

[TemplatePart(PART_TitleBar, typeof(Control))]
[TemplatePart(PART_Title, typeof(TextBlock))]
[TemplatePart(PART_SystemMenu, typeof(Menu))]
[TemplatePart(PART_SystemMenuItem, typeof(MenuItem))]
[TemplatePart(PART_MinimizeButton, typeof(Button))]
[TemplatePart(PART_MaximizeButton, typeof(Button))]
[TemplatePart(PART_RestoreButton, typeof(Button))]
[TemplatePart(PART_CloseButton, typeof(Button))]
[TemplatePart(PART_WindowBorder, typeof(Border))]
[TemplatePart(PART_ContentPresenter, typeof(Control))]
[PseudoClasses(
    CLASS_Minimized, // window is minimized
    CLASS_Maximized, // window is maximized
    CLASS_Normal,    // window is normal
    CLASS_Dragging,  // window is being dragged by mouse
    CLASS_Active,    // window is active
    CLASS_HasModal,  // window has a modal dialog open
    CLASS_IsModal,   // window is a modal dialog
    CLASS_NoBorder,  // window has no border
    CLASS_NoTitle,   // window has no title bar
    CLASS_FixedSize, // window should not be resizable
    CLASS_Sizing,    // window is being resized by keyboard
    CLASS_Moving)]   // window is being moved by keyboard
public class ManagedWindow : ContentControl
{
    public const string PART_ContentPresenter = "PART_ContentPresenter";
    public const string PART_TitleBar = "PART_TitleBar";
    public const string PART_Title = "PART_Title";
    public const string PART_SystemMenu = "PART_SystemMenu";
    public const string PART_SystemMenuItem = "PART_SystemMenuItem";
    public const string PART_MinimizeButton = "PART_MinimizeButton";
    public const string PART_MaximizeButton = "PART_MaximizeButton";
    public const string PART_RestoreButton = "PART_RestoreButton";
    public const string PART_CloseButton = "PART_CloseButton";
    public const string PART_WindowBorder = "PART_WindowBorder";
    public const string PART_ModalOverlay = "PART_ModalOverlay";

    private const string CLASS_Minimized = ":minimized";
    private const string CLASS_Maximized = ":maximized";
    private const string CLASS_Normal = ":normal";
    private const string CLASS_Dragging = ":dragging";
    private const string CLASS_Active = ":active";
    private const string CLASS_HasModal = ":hasmodal";
    private const string CLASS_IsModal = ":ismodal";
    private const string CLASS_NoBorder = ":noborder";
    private const string CLASS_NoTitle = ":notitle";
    private const string CLASS_Sizing = ":sizing";
    private const string CLASS_Moving = ":moving";
    private const string CLASS_FixedSize = ":fixedsize";

    // used to track MRU windows globally
    private static List<ManagedWindow> s_MRU = null;
    private static bool s_isAnyWindowActivating = false;  // Global guard against activation loops

    private PixelPoint _minimizedPosition = new PixelPoint(int.MinValue, int.MinValue);
    private Rect _normalRect;
    private BoxShadows _normalBoxShadow;
    private Thickness _normalMargin;
    private ContentPresenter _content;
    private Border? _windowBorder;
    private ManagedWindow? _owner;
    private bool _loaded;
    private bool _isActive;
    private object? _dialogResult;
    private Control? _title;
    private Control? _titleBar;
    private Control? _focus;
    private Menu? _systemMenu;
    private MenuItem? _systemMenuItem;
    private Panel? _modalOverlay;
    private bool _keyboardMoving;
    private bool _keyboardSizing;
    private ManagedWindow? _modalDialog;
    private readonly List<(ManagedWindow Child, bool IsDialog)> _children = new List<(ManagedWindow, bool)>();
    private bool _isActivating;  // Guard against re-entrant activation

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreCommand { get; }
    public ReactiveCommand<Unit, Unit> MinimizeCommand { get; }
    public ReactiveCommand<Unit, Unit> MaximizeCommand { get; }
    public ReactiveCommand<Unit, Unit> SizeCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSystemMenuCommand { get; }


    /// <summary>
    /// Defines the <see cref="IsActive"/> property.
    /// </summary>
    public static readonly DirectProperty<ManagedWindow, bool> IsActiveProperty =
        AvaloniaProperty.RegisterDirect<ManagedWindow, bool>(nameof(IsActive), o => o.IsActive);

    /// <summary>
    /// Defines the <see cref="Owner"/> property.
    /// </summary>
    public static readonly DirectProperty<ManagedWindow, ManagedWindow?> OwnerProperty =
        AvaloniaProperty.RegisterDirect<ManagedWindow, ManagedWindow?>(nameof(Owner), o => o.Owner);

    public static readonly StyledProperty<bool> TopmostProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(Topmost));

    /// <summary>
    /// Defines the <see cref="SizeToContent"/> property.
    /// </summary>
    public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
        AvaloniaProperty.Register<ManagedWindow, SizeToContent>(nameof(SizeToContent));

    /// <summary>
    /// Defines the <see cref="ShowActivated"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowActivatedProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(ShowActivated), true);

    /// <summary>
    /// Defines the <see cref="AnimateWindow"/> property. If True, animations will be used for transitions.
    /// </summary>
    public static readonly StyledProperty<bool> AnimateWindowProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(AnimateWindow), true);

    /// <summary>
    /// Represents the current window state (normal, minimized, maximized)
    /// </summary>
    public static readonly StyledProperty<WindowState> WindowStateProperty =
        AvaloniaProperty.Register<ManagedWindow, WindowState>(nameof(WindowState));

    /// <summary>
    /// Defines the <see cref="ClosingBehavior"/> property.
    /// </summary>
    public static readonly StyledProperty<WindowClosingBehavior> ClosingBehaviorProperty =
        AvaloniaProperty.Register<Window, WindowClosingBehavior>(nameof(ClosingBehavior));

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ManagedWindow, string?>(nameof(Title), "Window");

    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<ManagedWindow, object?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="WindowStartupLocation"/> property.
    /// </summary>
    public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty =
        AvaloniaProperty.Register<ManagedWindow, WindowStartupLocation>(nameof(WindowStartupLocation));

    public static readonly StyledProperty<PixelPoint> PositionProperty =
    AvaloniaProperty.Register<ManagedWindow, PixelPoint>(nameof(Position));

    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(CanResize), true);

    /// <summary>
    /// Defines the <see cref="BoxShadow"/> property.
    /// </summary>
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        AvaloniaProperty.Register<ManagedWindow, BoxShadows>(nameof(BoxShadow));

    /// <summary>
    /// Defines the <see cref="SystemDecorations"/> property.
    /// </summary>
    public static readonly StyledProperty<SystemDecorations> SystemDecorationsProperty =
        AvaloniaProperty.Register<ManagedWindow, SystemDecorations>(nameof(SystemDecorations), SystemDecorations.Full);

    /// <summary>
    /// Routed event that can be used for global tracking of window destruction
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> WindowClosedEvent =
        RoutedEvent.Register<ManagedWindow, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);

    /// <summary>
    /// Routed event that can be used for global tracking of opening windows
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> WindowOpenedEvent =
        RoutedEvent.Register<ManagedWindow, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);

    static ManagedWindow()
    {
        AffectsRender<ManagedWindow>(
            SystemDecorationsProperty,
            WindowStateProperty);
        AffectsMeasure<ManagedWindow>(
            SystemDecorationsProperty,
            WindowStateProperty);
        
        // ManagedWindow should be focusable to properly receive keyboard input
        FocusableProperty.OverrideDefaultValue<ManagedWindow>(true);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ManagedWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        SetValue(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Cycle);

        CloseCommand = ReactiveCommand.Create(() => Close(), outputScheduler: AvaloniaScheduler.Instance);

        RestoreCommand = ReactiveCommand.Create(() => { WindowState = WindowState.Normal; },
            // NOTE: There appears to be a focus bug in avalonia when the first MenuItem is disabled. So for now we always enable Restore. 
            canExecute: this.WhenAnyValue(win => win.CanResize, win => win.WindowState, (canResize, windowState) => true), // canResize && windowState != WindowState.Normal),
            outputScheduler: AvaloniaScheduler.Instance);

        MaximizeCommand = ReactiveCommand.Create(() => { WindowState = WindowState.Maximized; },
            canExecute: this.WhenAnyValue(win => win.CanResize, win => win.WindowState, (canResize, windowState) => canResize && windowState != WindowState.Maximized),
            outputScheduler: AvaloniaScheduler.Instance);

        MinimizeCommand = ReactiveCommand.Create(() => { WindowState = WindowState.Minimized; },
            canExecute: this.WhenAnyValue(win => win.CanResize, win => win.WindowState, (canResize, windowState) => canResize && windowState != WindowState.Minimized),
            outputScheduler: AvaloniaScheduler.Instance);

        ShowSystemMenuCommand = ReactiveCommand.Create(() =>
            {
                if (_systemMenuItem != null)
                {
                    _systemMenuItem.IsSubMenuOpen = true;

                    // find first enabled child menu and set focus to it.
                    var firstEnabledChild = _systemMenuItem.Items
                        .OfType<MenuItem>()
                        .FirstOrDefault(mi => mi.IsEnabled);
                    firstEnabledChild?.Focus();
                }
            },
            outputScheduler: AvaloniaScheduler.Instance);

        MoveCommand = ReactiveCommand.Create(() =>
            {
                _keyboardMoving = true;
                _keyboardSizing = false;
                this.SetPsudoClasses();
            },
            canExecute: this.WhenAnyValue(win => win.WindowState).Select(state => state == WindowState.Normal),
            outputScheduler: AvaloniaScheduler.Instance);

        SizeCommand = ReactiveCommand.Create(() =>
            {
                if (CanResize)
                {
                    _keyboardSizing = true;
                    _keyboardMoving = false;
                    this.SetPsudoClasses();
                }
                _focus?.Focus();
            },
            canExecute: this.WhenAnyValue(win => win.CanResize, win => win.WindowState, (canResize, windowState) => canResize && windowState == WindowState.Normal),
            outputScheduler: AvaloniaScheduler.Instance);
        
        // Don't subscribe to GotFocus here - it causes infinite loops
        // Window activation is handled through PointerPressed events instead
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_focus == null)
        {
            _focus = _content.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Focusable);
            if (_focus != null)
                _focus.Focus();
        }

        this.WindowsPanel.SizeChanged += OnWindowsPanelSizeChanged;
    }

    private void OnWindowsPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            Width = WindowsPanel.Bounds.Width;
            Height = WindowsPanel.Bounds.Height;
        }
    }

    public void PreviousWindow()
    {
        if (s_MRU == null)
            s_MRU = GetWindows().ToList();

        var index = s_MRU.IndexOf(this);
        if (index > 0)
        {
            s_MRU[index - 1].Activate();
        }
        else if (s_MRU.Count > 0)
        {
            s_MRU.Last().Activate();
        }
    }

    public void NextWindow()
    {
        if (s_MRU == null)
            s_MRU = GetWindows().ToList();

        var index = s_MRU.IndexOf(this);
        if (index >= 0 && index < s_MRU.Count - 1)
        {
            s_MRU[index + 1].Activate();
        }
        else if (s_MRU.Count > 0)
        {
            s_MRU.First().Activate();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating how the window will size itself to fit its content.
    /// </summary>
    /// <remarks>
    /// If <see cref="SizeToContent"/> has a value other than <see cref="SizeToContent.Manual"/>,
    /// <see cref="SizeToContent"/> is automatically set to <see cref="SizeToContent.Manual"/>
    /// if a user resizes the window by using the resize grip or dragging the border.
    /// 
    /// NOTE: Because of a limitation of X11, <see cref="SizeToContent"/> will be reset on X11 to
    /// <see cref="SizeToContent.Manual"/> on any resize - including the resize that happens when
    /// the window is first shown. This is because X11 resize notifications are asynchronous and
    /// there is no way to know whether a resize came from the user or the layout system. To avoid
    /// this, consider setting <see cref="CanResize"/> to false, which will disable user resizing
    /// of the window.
    /// </remarks>
    public SizeToContent SizeToContent
    {
        get => GetValue(SizeToContentProperty);
        set => SetValue(SizeToContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the box shadow effect parameters
    /// </summary>
    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating how the <see cref="Closing"/> event behaves in the presence
    /// of child windows.
    /// </summary>
    public WindowClosingBehavior ClosingBehavior
    {
        get => GetValue(ClosingBehaviorProperty);
        set => SetValue(ClosingBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets value indicating that the window should animate when opening, closing, and resizing
    /// </summary>
    public bool AnimateWindow
    {
        get => GetValue(AnimateWindowProperty);
        set => SetValue(AnimateWindowProperty, value);
    }


    /// <summary>
    /// Gets or sets a value that indicates whether a window is activated when first shown. 
    /// </summary>
    public bool ShowActivated
    {
        get => GetValue(ShowActivatedProperty);
        set => SetValue(ShowActivatedProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimized/maximized state of the window.
    /// </summary>
    public WindowState WindowState
    {
        get => GetValue(WindowStateProperty);
        set => SetValue(WindowStateProperty, value);
    }

    /// <summary>
    /// Enables or disables resizing of the window.
    /// </summary>
    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon of the window.
    /// </summary>
    [DependsOn(nameof(ContentTemplate))]
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Sets the system decorations (title bar, border, etc)
    /// </summary>
    public SystemDecorations SystemDecorations
    {
        get => GetValue(SystemDecorationsProperty);
        set => SetValue(SystemDecorationsProperty, value);
    }

    /// <summary>
    /// Gets or sets the startup location of the window.
    /// </summary>
    public WindowStartupLocation WindowStartupLocation
    {
        get => GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    /// <summary>
    /// Gets or sets the client size of the window.
    /// </summary>
    public Size ClientSize
    {
        get => new Size(this._content.Width, this._content.Height);
        protected set
        {
            this._content.Width = value.Width;
            this._content.Height = value.Height;
        } // TODO ? (this._content.DesiredSize, value);
    }

    /// <summary>
    /// Gets or sets the window position in screen coordinates.
    /// </summary>
    public PixelPoint Position
    {
        get => GetValue(PositionProperty);
        set
        {
            SetValue(PositionProperty, value);
            Canvas.SetLeft(this, value.X);
            Canvas.SetTop(this, value.Y);
        }
    }

    /// <summary>
    /// Gets or sets the owner of the window.
    /// </summary>
    public ManagedWindow? Owner
    {
        get => _owner;
        protected set => SetAndRaise(OwnerProperty, ref _owner, value);
    }

    /// <summary>
    /// Gets or sets whether this window appears on top of all other windows
    /// </summary>
    public bool Topmost
    {
        get => GetValue(TopmostProperty);
        set => SetValue(TopmostProperty, value);
    }

    /// <summary>
    /// Gets a value that indicates whether the window is active.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        private set => SetAndRaise(IsActiveProperty, ref _isActive, value);
    }

    public ITopLevelImpl PlatformImpl => TopLevel.GetTopLevel(this)!.PlatformImpl!;

    /// <summary>
    /// File System storage service used for file pickers and bookmarks.
    /// </summary>
    public IStorageProvider StorageProvider => TopLevel.GetTopLevel(this)!.StorageProvider;

    public Screens Screens => TopLevel.GetTopLevel(this)!.Screens!;

    public IInsetsManager? InsetsManager => TopLevel.GetTopLevel(this)!.PlatformImpl!.TryGetFeature<IInsetsManager>();
    public IInputPane? InputPane => PlatformImpl!.TryGetFeature<IInputPane>();
    public ILauncher Launcher => PlatformImpl!.TryGetFeature<ILauncher>();

    /// <summary>
    /// Gets the platform's clipboard implementation
    /// </summary>
    public IClipboard? Clipboard => PlatformImpl!.TryGetFeature<IClipboard>();

    /// <inheritdoc />
    public IFocusManager? FocusManager => TopLevel.GetTopLevel(this)!.FocusManager;

    /// <summary>
    /// The WindowsPanel that this window is hosted in.
    /// </summary>
    public WindowsPanel WindowsPanel { get; private set; }

    /// <summary>
    /// When a window has a modal dialog, this property will be set to the modal dialog.
    /// </summary>
    public ManagedWindow? ModalDialog
    {
        get => _modalDialog;
        set
        {
            if (value != null && _modalDialog != null)
                throw new NotSupportedException("Already showing a modal dialog for this window");

            _modalDialog = value;
            SetPsudoClasses();
            if (_modalDialog != null)
            {
                _modalDialog.Closed += (s, e) =>
                {
                    _modalDialog = null;
                    SetPsudoClasses();
                };
            }

        }
    }

    /// <summary>
    /// Fired when the window is activated.
    /// </summary>
    public event EventHandler? Activated;

    /// <summary>
    /// Fired when the window is deactivated.
    /// </summary>
    public event EventHandler? Deactivated;

    /// <summary>
    /// Fired when the window is opened.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Fired when the window is closed.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Fired when the window position is changed.
    /// </summary>
    public event EventHandler<PixelPointEventArgs>? PositionChanged;

    /// <summary>
    /// Occurs when the window is resized.
    /// </summary>
    /// <remarks>
    /// Although this event is similar to the <see cref="Control.SizeChanged"/> event, they are
    /// conceptually different:
    /// 
    /// - <see cref="Resized"/> is a window-level event, fired when a resize notification arrives
    ///   from the platform windowing subsystem. The event args contain details of the source of
    ///   the resize event in the <see cref="WindowResizedEventArgs.Reason"/> property. This
    ///   event is raised before layout has been run on the window's content.
    /// - <see cref="Control.SizeChanged"/> is a layout-level event, fired when a layout pass
    ///   completes on a control. <see cref="Control.SizeChanged"/> is present on all controls
    ///   and is fired when the control's size changes for any reason, including a
    ///   <see cref="Resized"/> event in the case of a Window.
    /// </remarks>
    public event EventHandler<WindowResizedEventArgs>? Resized;

    /// <summary>
    /// Fired before a window is closed.
    /// </summary>
    public event EventHandler<WindowClosingEventArgs>? Closing;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        switch (change.Property.Name)
        {
            case nameof(WindowState):
                if (_loaded)
                {
                    if (change.OldValue is WindowState oldState)
                    {
                        CaptureWindowState(oldState);

                        switch (WindowState)
                        {
                            case WindowState.FullScreen:
                                OnFullscreenWindow();
                                break;
                            case WindowState.Maximized:
                                OnMaximizeWindow();
                                break;
                            case WindowState.Minimized:
                                OnMinimizeWindow();
                                break;
                            case WindowState.Normal:
                                OnNormalWindow();
                                break;
                            default:
                                break;
                        }
                    }
                }
                break;

            case nameof(SystemDecorations):
                SetPsudoClasses();
                break;

            default:
                base.OnPropertyChanged(change);
                break;
        }
    }

    private void CaptureWindowState(WindowState state)
    {
        switch (state)
        {
            case WindowState.Normal:
                var width = double.IsNaN(this.Width) ? this.Bounds.Width : this.Width;
                var height = double.IsNaN(this.Height) ? this.Bounds.Height : this.Height;
                _normalRect = new Rect((int)this.Position.X, (int)this.Position.Y, (int)width, (int)height);
                if (_windowBorder != null)
                {
                    _normalBoxShadow = _windowBorder.BoxShadow!;
                    _normalMargin = _windowBorder.Margin;
                }
                break;
            case WindowState.Minimized:
                _minimizedPosition = new PixelPoint(this.Position.X, this.Position.Y);
                break;
            case WindowState.FullScreen:
            case WindowState.Maximized:
                if (_normalRect.Width == 0 && _normalRect.Height == 0)
                {
                    _normalRect = new Rect((int)2, (int)2, (int)WindowsPanel.Bounds.Width - 4, (int)WindowsPanel.Bounds.Height - 4);
                    if (_windowBorder != null)
                    {
                        _normalBoxShadow = _windowBorder.BoxShadow!;
                        _normalMargin = _windowBorder.Margin;
                    }
                }
                break;
            default:
                break;
        }
    }

    protected virtual async void OnMaximizeWindow()
    {
        BringToTop();

        SetPsudoClasses();

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              new Rect(0, 0, WindowsPanel.Bounds.Width, WindowsPanel.Bounds.Height));

        this.Position = new PixelPoint((ushort)0, (ushort)0);
        this.Width = WindowsPanel.Bounds.Width;
        this.Height = WindowsPanel.Bounds.Height;
        if (_windowBorder != null)
        {
            _windowBorder.Margin = new Thickness(0);
            _windowBorder.BoxShadow = new BoxShadows();
        }
        _focus?.Focus();
    }

    protected virtual async void OnFullscreenWindow()
    {
        await Task.CompletedTask;
        OnMaximizeWindow();
    }

    protected virtual async void OnNormalWindow()
    {
        BringToTop();

        SetPsudoClasses();

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              _normalRect);

        this.Position = new PixelPoint((int)_normalRect.Position.X, (int)_normalRect.Position.Y);
        this.Width = _normalRect.Width;
        this.Height = _normalRect.Height;

        if (_windowBorder != null)
        {
            _windowBorder.Margin = _normalMargin;
            _windowBorder.BoxShadow = _normalBoxShadow;
        }
        _focus?.Focus();
    }

    protected virtual async void OnMinimizeWindow()
    {
        BringToTop();

        SetPsudoClasses();

        if (_minimizedPosition.X == int.MinValue && _minimizedPosition.Y == int.MinValue)
            _minimizedPosition = new PixelPoint(this.Position.X, this.Position.Y);

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              new Rect(_minimizedPosition.X, _minimizedPosition.Y, _title.Bounds.Width, _title.Bounds.Height));

        this.Position = _minimizedPosition;
        this.Width = double.NaN;
        this.Height = double.NaN;

        _systemMenu?.Focus();
    }

    /// <summary>
    /// Activates the window.
    /// </summary>
    public void Activate()
    {
        // Guard against re-entrant activation (both local and global)
        if (_isActivating || s_isAnyWindowActivating)
        {
            Debug.WriteLine($"[ManagedWindow '{Title}'] Activate: Already activating (local={_isActivating}, global={s_isAnyWindowActivating}), skipping");
            return;
        }
        
        if (!IsActive && ModalDialog == null && Parent != null)
        {
            if (WindowState == WindowState.Minimized)
            {
                return;
            }

            _isActivating = true;
            s_isAnyWindowActivating = true;
            try
            {
                // Set IsActive FIRST before any focus operations
                IsActive = true;
                
                foreach (var win in GetWindows().Where(win => win != this))
                {
                    win.Deactivate();
                }
                BringToTop();
                SetPsudoClasses();
                
                OnActivated();

                // Don't focus content here - it causes focus loops
                // Focus will be handled by:
                // 1. The control that was clicked (if activation was from a click)
                // 2. OnLoaded for new windows
                // 3. Manual calls to FocusContent when needed
            }
            finally
            {
                _isActivating = false;
                s_isAnyWindowActivating = false;
            }
        }
    }

    /// <summary>
    /// Focuses the window's content synchronously (internal implementation).
    /// </summary>
    private void FocusContentInternal()
    {
        Debug.WriteLine($"[ManagedWindow '{Title}'] FocusContentInternal: _focus={_focus?.GetType().Name}");
        
        if (_focus != null && _focus.IsVisible && _focus.IsEnabled)
        {
            Debug.WriteLine($"[ManagedWindow '{Title}'] Focusing: {_focus.GetType().Name}");
            _focus.Focus();
            return;
        }

        if (_content != null)
        {
            var focusable = _content.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(c => c.Focusable && c.IsVisible && c.IsEnabled);
            
            if (focusable != null)
            {
                Debug.WriteLine($"[ManagedWindow '{Title}'] Focusing first: {focusable.GetType().Name}");
                _focus = focusable;
                focusable.Focus();
            }
        }
    }

    /// <summary>
    /// Focuses the window's content. If a previously focused control exists, it will be focused.
    /// Otherwise, finds the first focusable control in the content area.
    /// </summary>
    public void FocusContent()
    {
        FocusContentInternal();
    }

    /// <summary>
    /// Deactivates the window
    /// </summary>
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            OnDeactivated();
            SetPsudoClasses();
        }
    }



    public void Show()
    {
        Show(null);
    }

    /// <summary>
    /// Shows the window.
    /// </summary>
    public virtual void Show(Visual? parent)
    {
        if (parent == null)
        {
            WindowsPanel = FindTopWindowsPanel();
        }
        else if (parent is WindowsPanel wh)
        {
            // parent is a windows panel
            this.WindowsPanel = wh;
        }
        else if (parent is ManagedWindow mw)
        {
            // window is a dialog, it shares the same window host and it's owner is the parent window
            this.WindowsPanel = mw.WindowsPanel;
            Owner = mw;
        }
        else
        {
            // try and find the window host as parent of this element.
            this.WindowsPanel = parent.FindAncestorOfType<WindowsPanel>(true) ??
                                parent.FindDescendantOfType<WindowsPanel>(true);
            if (this.WindowsPanel == null)
            {
                WindowsPanel = FindTopWindowsPanel();
            }
        }

        if (WindowsPanel == null)
            throw new ArgumentNullException(nameof(WindowsPanel), "To show a window you need to add a WindowsPanel to your visual hierachy.");

        //if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
        //{
        //    this.WindowsPanel = FindTopWindowsPanel();
        //}

        WindowsPanel.Windows.Add(this);

        //// Force a layout pass
        Measure(Size.Infinity);
        //Arrange(new Rect(DesiredSize));

        SetWindowStartupLocation();

        // dialogWrap.HadFocusOn = focusedElement;
        // _dialogs.Push(popupHost);

        // we capture window state for normal before we maximize/minimize/etc.
        CaptureWindowState(WindowState.Normal);

        switch (WindowState)
        {
            case WindowState.Maximized:
                OnMaximizeWindow();
                break;
            case WindowState.Minimized:
                OnMinimizeWindow();
                break;
            case WindowState.FullScreen:
                OnFullscreenWindow();
                break;
        }

        RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

        OnOpened(EventArgs.Empty);

        if (ShowActivated)
        {
            Activate();
        }
    }

    private WindowsPanel FindTopWindowsPanel()
    {
        // search from top down
        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return singleView.MainView?.FindDescendantOfType<WindowsPanel>(true);
        }
        else if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.FindDescendantOfType<WindowsPanel>(true);
        }
        return null;
    }

    /// <summary>
    /// Shows the window as a dialog.
    /// </summary>
    /// <param name="owner">The dialog's owner window. if null this will be global modal</param>
    /// <exception cref="InvalidOperationException">
    /// The window has already been closed.
    /// </exception>
    /// <returns>
    /// A task that can be used to track the lifetime of the dialog.
    /// </returns>
    public Task ShowDialog(Visual? owner = null)
    {
        return ShowDialog<object>(owner);
    }

    /// <summary>
    /// Shows the window as a dialog.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result produced by the dialog.
    /// </typeparam>
    /// <param name="owner">The dialog's owner window. if null this will be a global modal</param>
    /// <returns>.
    /// A task that can be used to retrieve the result of the dialog when it closes.
    /// </returns>
    public Task<TResult> ShowDialog<TResult>(Visual? parent = null)
    {
        if (ModalDialog != null)
            throw new NotSupportedException("Already showing a modal dialog for this window");

        var result = new TaskCompletionSource<TResult>();

        this.Show(parent);

        // NOTE: Coming out Show we should have WindowsPanel set to the panel and Owner set to the owner window if it exists.

        if (this.Owner != null)
        {
            // we have an owner, we need to set the modal dialog on the owner.
            this.Owner.ModalDialog = this;
        }
        else
        {
            this.WindowsPanel.ModalDialog = this;
        }

        this.Closed += (sender, e) =>
        {
            // when dialog closes change focus back to owner.
            if (this.Owner != null)
            {
                this.Owner.ModalDialog = null;
                this.Owner.Activate();
            }
            else
            {
                this.WindowsPanel.ModalDialog = null;
            }

            result.SetResult((TResult)(_dialogResult ?? default(TResult))!);
        };
        SetPsudoClasses();
        return result.Task;
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close()
    {
        CloseCore(WindowCloseReason.WindowClosing, true, false);
    }

    /// <summary>
    /// Closes a dialog window with the specified result.
    /// </summary>
    /// <param name="dialogResult">The dialog result.</param>
    /// <remarks>
    /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
    /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
    /// resulting task will produce the <see cref="_dialogResult"/> value when the window
    /// is closed.
    /// </remarks>
    public void Close(object? dialogResult)
    {
        _dialogResult = dialogResult;
        CloseCore(WindowCloseReason.WindowClosing, true, false);
    }

    internal void CloseCore(WindowCloseReason reason, bool isProgrammatic, bool ignoreCancel)
    {
        bool close = true;

        try
        {
            var args = ActivatorEx.CreateInstance<WindowClosingEventArgs>(reason, isProgrammatic);
            if (ShouldCancelClose(args))
            {
                close = false;
            }
        }
        finally
        {
            if (close || ignoreCancel)
            {
                CloseInternal();
            }
        }
    }

    private async void CloseInternal()
    {
        foreach (var (child, _) in _children.ToArray())
        {
            child.CloseInternal();
        }

        await CloseAnimation();

        Owner = null;
        OnClosed(new EventArgs());
        RaiseEvent(new RoutedEventArgs(WindowClosedEvent));

        this.WindowsPanel.Windows.Remove(this);

        PreviousWindow();

        s_MRU.Remove(this);
    }

    private bool ShouldCancelClose(WindowClosingEventArgs args)
    {
        switch (ClosingBehavior)
        {
            case WindowClosingBehavior.OwnerAndChildWindows:
                bool canClose = true;

                if (_children.Count > 0)
                {
                    var childArgs = args.CloseReason == WindowCloseReason.WindowClosing ?
                        ActivatorEx.CreateInstance<WindowClosingEventArgs>(WindowCloseReason.OwnerWindowClosing, args.IsProgrammatic) :
                        args;

                    foreach (var (child, _) in _children.ToArray())
                    {
                        if (child.ShouldCancelClose(childArgs))
                        {
                            canClose = false;
                        }
                    }
                }

                if (canClose)
                {
                    OnClosing(args);

                    return args.Cancel;
                }

                return true;

            case WindowClosingBehavior.OwnerWindowOnly:
                OnClosing(args);

                return args.Cancel;
        }

        return false;
    }

    /// <summary>
    /// Raies the <see cref="Activated"/> event.
    /// </summary>
    protected virtual void OnActivated()
    {
        Dispatcher.UIThread.Invoke(() => Activated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Deactivated"/> event.
    /// </summary>
    protected virtual void OnDeactivated()
    {
        Dispatcher.UIThread.Invoke(() => Deactivated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Opened"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected async virtual void OnOpened(EventArgs e)
    {
        await ShowAnimation();

        Dispatcher.UIThread.Invoke(() => Opened?.Invoke(this, e));
    }

    /// <summary>
    /// Raises the <see cref="Closing"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    /// <remarks>
    /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
    /// overridden method must call <see cref="OnClosing"/> on the base class if the
    /// <see cref="Closing"/> event needs to be raised.
    /// </remarks>
    protected virtual void OnClosing(WindowClosingEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() => Closing?.Invoke(this, e));
    }

    /// <summary>
    /// Raises the <see cref="Closed"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected virtual void OnClosed(EventArgs e)
    {
        Dispatcher.UIThread.Invoke(() => Closed?.Invoke(this, e));
    }

    protected virtual async Task ShowAnimation()
    {
        if (AnimateWindow)
        {

            var scaleTransform = new ScaleTransform(0.2, 0.2);
            RenderTransform = scaleTransform;
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 1.0),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
            this.RenderTransform = null;
        }
    }

    protected virtual async Task ResizeAnimation(Rect oldPosition, Rect newPosition)
    {
        if (AnimateWindow)
        {
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward, // Ensure the animation holds the end value
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, oldPosition.X),
                            new Setter(Canvas.TopProperty, oldPosition.Y),
                            new Setter(WidthProperty, oldPosition.Width),
                            new Setter(HeightProperty, oldPosition.Height)
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, newPosition.X),
                            new Setter(Canvas.TopProperty, newPosition.Y),
                            new Setter(WidthProperty, newPosition.Width),
                            new Setter(HeightProperty, newPosition.Height)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
        }
    }

    protected virtual async Task CloseAnimation()
    {
        if (AnimateWindow)
        {
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            RenderTransform = scaleTransform;
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, .2),
                            new Setter(ScaleTransform.ScaleYProperty, .2)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
            this.RenderTransform = null;
        }
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        
        // Handle pointer pressed to activate window when clicked anywhere
        this.AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);

        //if (this.Theme == null)
        //    this.Theme = (ControlTheme)this.FindResource("ManagedWindow");
        _title = e.NameScope.Find<TextBlock>(PART_Title);

        _titleBar = e.NameScope.Find<Control>(PART_TitleBar);
        if (_titleBar != null)
        {
            SetupDragging(_titleBar);
        }

        var partMinimizeButton = e.NameScope.Find<Button>(PART_MinimizeButton);
        if (partMinimizeButton != null)
        {
            partMinimizeButton.Command = MinimizeCommand;
        }

        var partMaximizeButton = e.NameScope.Find<Button>(PART_MaximizeButton);
        if (partMaximizeButton != null)
        {
            partMaximizeButton.Command = MaximizeCommand;
        }

        var partRestoreButton = e.NameScope.Find<Button>(PART_RestoreButton);
        if (partRestoreButton != null)
        {
            partRestoreButton.Command = RestoreCommand;
        }

        var partCloseButton = e.NameScope.Find<Button>(PART_CloseButton);
        if (partCloseButton != null)
        {
            partCloseButton.Command = CloseCommand;
        }

        _systemMenu = e.NameScope.Find<Menu>(PART_SystemMenu);

        _systemMenuItem = _systemMenu?.Items.OfType<MenuItem>().FirstOrDefault();

        if (_systemMenuItem != null)
        {
            _systemMenuItem.PropertyChanged += (sender, e) =>
            {
                if (e.Property == MenuItem.IsSubMenuOpenProperty && !_systemMenuItem.IsSubMenuOpen)
                {
                    // Menu is closing, restore focus
                    _focus?.Focus();
                }
            };
        }

        _content = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
        ArgumentNullException.ThrowIfNull(_content);

        _modalOverlay = e.NameScope.Find<Panel>(PART_ModalOverlay);
        if (_modalOverlay != null)
        {
            _modalOverlay.PointerPressed += OnModalOverlayClick;
        }

        _windowBorder = e.NameScope.Find<Border>(PART_WindowBorder);
        SetupResize(_windowBorder);

        this.Tapped += OnTapped;
        _loaded = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Debug.WriteLine($"[ManagedWindow '{Title}'] OnKeyDown: Key={e.Key}, Source={e.Source?.GetType().Name}, IsActive={IsActive}");
        
        if (_keyboardMoving || _keyboardSizing)
        {
            // TODO: Make this configurable
            var sizing = (Int32)this.FindResource("ManagedWindow_SizingVector");
            switch (e.Key)
            {
                case Key.Left:
                    if (_keyboardMoving)
                    {
                        this.Position = new PixelPoint(this.Position.X - sizing, this.Position.Y);
                    }
                    else if (_keyboardSizing)
                    {
                        if (this.Width > sizing)
                            this.Width = this.Bounds.Width - sizing;
                    }
                    e.Handled = true;
                    return;

                case Key.Right:
                    if (_keyboardMoving)
                    {
                        this.Position = new PixelPoint(this.Position.X + sizing, this.Position.Y);
                    }
                    else if (_keyboardSizing)
                    {
                        this.Width = this.Bounds.Width + sizing;
                    }
                    e.Handled = true;
                    return;

                case Key.Up:
                    if (_keyboardMoving)
                    {
                        this.Position = new PixelPoint(this.Position.X, this.Position.Y - sizing);
                    }
                    else if (_keyboardSizing)
                    {
                        if (this.Height > sizing)
                            this.Height = this.Bounds.Height - sizing;
                    }
                    e.Handled = true;
                    return;

                case Key.Down:
                    if (_keyboardMoving)
                    {
                        this.Position = new PixelPoint(this.Position.X, this.Position.Y + sizing);
                    }
                    else if (_keyboardSizing)
                    {
                        this.Height = this.Bounds.Height + sizing;
                    }
                    e.Handled = true;
                    return;

                default:
                    _keyboardMoving = false;
                    _keyboardSizing = false;
                    SetPsudoClasses();
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Escape || e.Key == Key.Enter)
        {
            var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
            var focusedElement = focusManager?.GetFocusedElement();
            bool isMenuFocused = focusedElement is Menu || focusedElement is MenuItem;
            if (!isMenuFocused)
            {
                var buttons = this.GetVisualDescendants().OfType<Button>().ToList();
                var defaultButton = buttons.FirstOrDefault(x => x.IsDefault);
                var cancelButton = buttons.FirstOrDefault(x => x.IsCancel);
                if (e.Key == Key.Escape && cancelButton != null)
                {
                    cancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && defaultButton != null)
                {
                    defaultButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
            }
        }
        else if ((e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) ||
                 (e.Key == Key.F6 && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            NextWindow();
            e.Handled = true;
            // return because we don't want to reset the MRU 
            return;
        }
        else if ((e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control)) ||
                 (e.Key == Key.F6 && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            PreviousWindow();
            e.Handled = true;
            // return because we don't want to reset the MRU 
            return;
        }
        s_MRU = null;
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsActive)
        {
            Activate();
        }
    }

    /// <summary>
    /// Handles pointer pressed on the window to activate it.
    /// This is used instead of GotFocus to avoid focus loops.
    /// </summary>
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Debug.WriteLine($"[ManagedWindow '{Title}'] OnWindowPointerPressed: IsActive={IsActive}");
        if (!IsActive && !s_isAnyWindowActivating)
        {
            Activate();
        }
    }

    private void SetupDragging(Control? partTitleBar)
    {
        if (partTitleBar == null)
            // no title bar means no draggable behavior needed.
            return;

        PixelPoint? start = null;
        var parent = this.Parent as Control;

        partTitleBar.PointerPressed += (object? sender, PointerPressedEventArgs e) =>
        {
            if (WindowState != WindowState.Maximized)
            {
                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed)
                {
                    var point = e.GetPosition(parent);
                    start = new PixelPoint((int)point.X, (int)point.Y);
                    SetPsudoClasses(true);
                }
            }

            if (!IsActive)
                Activate();

            e.Handled = true;
        };

        partTitleBar.PointerReleased += (object? sender, PointerReleasedEventArgs e) =>
        {
            if (start != null)
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    SetPsudoClasses(false);
                    start = null;
                    e.Handled = true;
                }
            }
        };

        partTitleBar.PointerMoved += (object? sender, PointerEventArgs e) =>
        {
            if (WindowState != WindowState.Maximized)
            {

                var properties = e.GetCurrentPoint(this).Properties;
                if (start != null && properties.IsLeftButtonPressed)
                {
                    var point = e.GetPosition(parent);
                    var position = new PixelPoint((int)point.X, (int)point.Y);
                    var delta = position - start.Value;
                    start = position;
                    this.Position = this.Position + delta;
                }

                e.Handled = true;
            }
        };

        partTitleBar.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs e) =>
        {
            SetPsudoClasses(false);
            if (start != null)
                e.Handled = true;

            start = null;
        };

        partTitleBar.DoubleTapped += OnTitleBarDoubleClick;
    }

    private void SetPsudoClasses(bool isDragging = false)
    {
        var classes = ((IPseudoClasses)this.Classes);
        classes.Remove(CLASS_Minimized);
        classes.Remove(CLASS_Maximized);
        classes.Remove(CLASS_Normal);
        classes.Remove(CLASS_Dragging);
        classes.Remove(CLASS_Active);
        classes.Remove(CLASS_HasModal);
        classes.Remove(CLASS_IsModal);
        classes.Remove(CLASS_NoBorder);
        classes.Remove(CLASS_NoTitle);
        classes.Remove(CLASS_Sizing);
        classes.Remove(CLASS_Moving);
        classes.Remove(CLASS_FixedSize);

        if (isDragging)
            classes.Add(CLASS_Dragging);

        if (IsActive)
            classes.Add(CLASS_Active);

        if (ModalDialog != null)
            classes.Add(CLASS_HasModal);

        if (Owner != null)
            classes.Add(CLASS_IsModal);

        if (!CanResize)
            classes.Add(CLASS_FixedSize);

        switch (WindowState)
        {
            case WindowState.Minimized:
                classes.Add(CLASS_Minimized);
                break;
            case WindowState.Maximized:
                classes.Add(CLASS_Maximized);
                break;
            case WindowState.Normal:
                classes.Add(CLASS_Normal);
                break;
            case WindowState.FullScreen:
                classes.Add(CLASS_Maximized);
                classes.Add(CLASS_NoBorder);
                classes.Add(CLASS_NoTitle);
                break;
        }

        switch (SystemDecorations)
        {
            case SystemDecorations.None:
                classes.Add(CLASS_NoBorder);
                classes.Add(CLASS_NoTitle);
                break;
            case SystemDecorations.BorderOnly:
                classes.Add(CLASS_NoTitle);
                break;
            case SystemDecorations.Full:
            default:
                break;
        }

        if (_keyboardMoving)
        {
            classes.Add(CLASS_Moving);
        }
        if (_keyboardSizing)
        {
            classes.Add(CLASS_Sizing);
        }

    }


    public void BringToTop()
    {
        foreach (var win in GetWindows().Where(win => win != this && win.WindowState == WindowState.Minimized))
        {
            win.ZIndex = 0;
        }

        var windows = GetWindows().ToList();
        int i = 1;
        foreach (var win in windows.Where(win => win != this && win.WindowState != WindowState.Minimized))
        {
            win.ZIndex = i++;
            if (win.Topmost)
                win.ZIndex = windows.Count();
        }

        // bring all parent windows to top with the active one.
        if (Owner != null)
        {
            Stack<ManagedWindow> owners = new Stack<ManagedWindow>();
            var owner = Owner;
            do
            {
                owners.Push(owner);
            } while ((owner = owner.Owner) != null);

            while (owners.Count > 0)
            {
                var ownerWindow = owners.Pop();
                ownerWindow.ZIndex = i++;
            }
        }
        this.ZIndex = i++;
    }

    void SetupResize(Border? border)
    {
        if (border == null)
            return;

        // when there is a box shadow we need to adjust the margin to allow it to be rendered.
        if (border.BoxShadow.Count > 0)
            border.Margin = new Thickness(0, 0, border.BoxShadow[0].OffsetX * 2, border.BoxShadow[0].OffsetY * 2);

        WindowEdge? edge = null;
        Point? start = null;
        border.PointerPressed += (i, e) =>
        {
            if (ModalDialog != null)
                return;

            if (!IsActive)
                Activate();

            if (CanResize && WindowState == WindowState.Normal)
            {
                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed && this?.Parent is Control parent)
                {
                    var point = e.GetPosition(this.Parent as Control);
                    edge = GetEdge(border, point);
                    border.Cursor = new Cursor(GetCursorForEdge(edge));
                    if (edge != null)
                    {
                        start = point;
                        e.Pointer.Capture(border);
                    }
                }
            }
        };
        border.PointerReleased += (i, e) =>
        {
            if (CanResize && e.InitialPressMouseButton == MouseButton.Left)
            {
                start = null;
                edge = null;
                e.Pointer.Capture(null);
                border.Cursor = new Cursor(GetCursorForEdge(edge));
            }
        };
        border.PointerCaptureLost += (i, e) =>
        {

            start = null;
            edge = null;
            e.Pointer.Capture(null);
            if (CanResize)
            {
                border.Cursor = new Cursor(GetCursorForEdge(edge));
            }
        };
        border.PointerMoved += (i, e) =>
        {
            if (CanResize && WindowState == WindowState.Normal)
            {
                var position = e.GetPosition(this.Parent as Control);

                var properties = e.GetCurrentPoint(this).Properties;
                if (edge != null &&
                    start != null &&
                    properties.IsLeftButtonPressed)
                {
                    double top = Canvas.GetTop(this);
                    double left = Canvas.GetLeft(this);
                    double width = this.Bounds.Width;
                    double height = this.Bounds.Height;

                    var deltaX = (int)position.X - (int)start.Value.X;
                    var deltaY = (int)position.Y - (int)start.Value.Y;

                    switch (edge)
                    {
                        case WindowEdge.West:
                            left += deltaX;
                            width -= deltaX;
                            break;
                        case WindowEdge.East:
                            width += deltaX;
                            break;
                        case WindowEdge.North:
                            top += deltaY;
                            height -= deltaY;
                            break;
                        case WindowEdge.South:
                            height += deltaY;
                            break;
                        case WindowEdge.NorthWest:
                            top += deltaY;
                            height -= deltaY;
                            left += deltaX;
                            width -= deltaX;
                            break;
                        case WindowEdge.NorthEast:
                            width += deltaX;
                            top += deltaY;
                            height -= deltaY;
                            break;
                        case WindowEdge.SouthWest:
                            left += deltaX;
                            width -= deltaX;
                            height += deltaY;
                            break;
                        case WindowEdge.SouthEast:
                            height += deltaY;
                            width += deltaX;
                            break;
                    }

                    if (deltaX != 0 || deltaY != 0)
                    {
                        this.Position = new PixelPoint((int)left, (int)top);
                        if (width != Width && width >= MinWidth && width < MaxWidth)
                            Width = width;
                        if (height != Height && height >= MinHeight && height < MaxHeight)
                            Height = height;

                        start = position;
                    }
                }
                else
                {
                    var edgeTemp = GetEdge(border, position);
                    border.Cursor = new Cursor(GetCursorForEdge(edgeTemp));
                }
            }
        };
    }

    private WindowEdge? GetEdge(Border border, Point? start)
    {
        if (start == null)
            return null;
        double top = Canvas.GetTop(this);
        double left = Canvas.GetLeft(this);
        double right = left + this.Bounds.Width;
        double bottom = top + this.Bounds.Height;

        var leftEdge = start.Value.X >= left &&
                       start.Value.X < left + border.BorderThickness.Left - border.Margin.Left;
        var rightEdge = start.Value.X >= right - border.BorderThickness.Right - border.Margin.Right &&
                        start.Value.X < right;
        var topEdge = start.Value.Y >= top &&
                        start.Value.Y < top + border.BorderThickness.Top - border.Margin.Top;
        var bottomEdge = start.Value.Y >= bottom - border.BorderThickness.Bottom - border.Margin.Bottom &&
                        start.Value.Y < bottom;
        if (topEdge && leftEdge)
            return WindowEdge.NorthWest;
        else if (topEdge && rightEdge)
            return WindowEdge.NorthEast;
        else if (bottomEdge && leftEdge)
            return WindowEdge.SouthWest;
        else if (bottomEdge && rightEdge)
            return WindowEdge.SouthEast;
        else if (topEdge)
            return WindowEdge.North;
        else if (bottomEdge)
            return WindowEdge.South;
        else if (leftEdge)
            return WindowEdge.West;
        else if (rightEdge)
            return WindowEdge.East;
        return null;
    }

    StandardCursorType GetCursorForEdge(WindowEdge? edge)
        => edge switch
        {
            WindowEdge.North => StandardCursorType.SizeNorthSouth,
            WindowEdge.South => StandardCursorType.SizeNorthSouth,
            WindowEdge.West => StandardCursorType.SizeWestEast,
            WindowEdge.East => StandardCursorType.SizeWestEast,
            WindowEdge.NorthWest => StandardCursorType.TopLeftCorner,
            WindowEdge.NorthEast => StandardCursorType.TopRightCorner,
            WindowEdge.SouthWest => StandardCursorType.BottomLeftCorner,
            WindowEdge.SouthEast => StandardCursorType.BottomRightCorner,
            _ => StandardCursorType.Arrow
        };

    private void OnTitleBarDoubleClick(object? sender, TappedEventArgs e)
    {
        if (CanResize)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            else if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;

            e.Handled = true;
        }
    }

    private void SetWindowStartupLocation()
    {
        PixelRect size;
        switch (this.SizeToContent)
        {
            case SizeToContent.Manual:
                size = new PixelRect(0, 0,
                                     (int)(Double.IsNaN(this.Width) ? this.DesiredSize.Width : this.Width),
                                     (int)(Double.IsNaN(this.Height) ? this.DesiredSize.Height : this.Height));
                break;
            case SizeToContent.WidthAndHeight:
                size = new PixelRect(0, 0, (int)this.DesiredSize.Width, (int)this.DesiredSize.Height);
                break;

            case SizeToContent.Width:
                size = new PixelRect(0, 0, (int)this.DesiredSize.Width, (int)this.Height);
                break;
            case SizeToContent.Height:
                size = new PixelRect(0, 0, (int)this.Width, (int)this.DesiredSize.Height);
                break;
            default:
                throw new NotImplementedException();
        }

        if (WindowStartupLocation == WindowStartupLocation.CenterOwner ||
            WindowStartupLocation == WindowStartupLocation.CenterScreen)
        {
            if (this.Owner != null)
            {
                var targetRect = new PixelRect(
                    this.Owner.Position,
                    new PixelSize((int)this.Owner.Bounds.Width, (int)this.Owner.Bounds.Height));
                var childRect = targetRect.CenterRect(size);
                this.Position = childRect.Position;
            }
            else 
            {
                var targetRect = new PixelRect(
                    new PixelPoint(0,0),
                    new PixelSize((int)this.WindowsPanel.Bounds.Width, (int)this.WindowsPanel.Bounds.Height));
                var childRect = targetRect.CenterRect(size);
                this.Position = childRect.Position;
            }
        }
    //    else if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
    //    {
    //        var classic = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
    //        var single = Application.Current.ApplicationLifetime as ISingleViewApplicationLifetime;
    //        var screenBounds = classic?.MainWindow.Bounds ?? single?.MainView?.Bounds ?? throw new ArgumentNullException("Screen");
    //        var screenSize = new PixelRect(0, 0, (int)screenBounds.Width, (int)screenBounds.Height);

    //        var childRect = screenSize.CenterRect(size);
    //        this.Position = childRect.Position;
    //    }
    }

    

    private IEnumerable<ManagedWindow> GetWindows()
    {
        if (WindowsPanel == null)
            return Array.Empty<ManagedWindow>();
        return WindowsPanel.Windows.Where(child => child is ManagedWindow).Cast<ManagedWindow>().OrderBy(win => win.ZIndex);
    }

    private void OnModalOverlayClick(object? sender, PointerPressedEventArgs e)
    {
        _keyboardSizing = false;
        _keyboardMoving = false;
        SetPsudoClasses();
    }

}

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

// ReSharper disable CheckNamespace

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    ///     A portable window abstraction that can be hosted in either a <see cref="ManagedWindowHost" />
    ///     (console/TUI) or a <see cref="SystemWindowHost" /> (desktop/system window).
    ///     Users derive from this class instead of <see cref="Window" /> or <see cref="ManagedWindow" />.
    ///     When <see cref="Show(Visual)" /> or <see cref="ShowDialog{TResult}(Visual)" /> is called,
    ///     an appropriate host window is created and the PortableWindow becomes its content.
    /// </summary>
    public class PortableWindow : ContentControl
    {
        /// <summary>
        ///     Defines the <see cref="Title" /> property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<PortableWindow, string>(nameof(Title), "Window");

        /// <summary>
        ///     Defines the <see cref="Icon" /> property.
        /// </summary>
        public static readonly StyledProperty<object> IconProperty =
            AvaloniaProperty.Register<PortableWindow, object>(nameof(Icon));

        /// <summary>
        ///     Defines the <see cref="WindowStartupLocation" /> property.
        /// </summary>
        public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty =
            AvaloniaProperty.Register<PortableWindow, WindowStartupLocation>(nameof(WindowStartupLocation));

        /// <summary>
        ///     Defines the <see cref="SizeToContent" /> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<PortableWindow, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        ///     Defines the <see cref="CanResize" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanResizeProperty =
            AvaloniaProperty.Register<PortableWindow, bool>(nameof(CanResize), true);

        /// <summary>
        ///     Defines the <see cref="WindowState" /> property.
        /// </summary>
        public static readonly StyledProperty<WindowState> WindowStateProperty =
            AvaloniaProperty.Register<PortableWindow, WindowState>(nameof(WindowState));

        /// <summary>
        ///     Defines the <see cref="ShowActivated" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowActivatedProperty =
            AvaloniaProperty.Register<PortableWindow, bool>(nameof(ShowActivated), true);

        /// <summary>
        ///     Defines the <see cref="Topmost" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<PortableWindow, bool>(nameof(Topmost));

        /// <summary>
        ///     Defines the <see cref="WindowDecorations" /> property.
        /// </summary>
        public static readonly StyledProperty<WindowDecorations> WindowDecorationsProperty =
            AvaloniaProperty.Register<PortableWindow, WindowDecorations>(nameof(WindowDecorations),
                WindowDecorations.Full);

        /// <summary>
        ///     Defines the <see cref="AnimateWindow" /> property.
        /// </summary>
        public static readonly StyledProperty<bool> AnimateWindowProperty =
            AvaloniaProperty.Register<PortableWindow, bool>(nameof(AnimateWindow), true);

        /// <summary>
        ///     Defines the <see cref="ClosingBehavior" /> property.
        /// </summary>
        public static readonly StyledProperty<WindowClosingBehavior> ClosingBehaviorProperty =
            AvaloniaProperty.Register<PortableWindow, WindowClosingBehavior>(nameof(ClosingBehavior));

        /// <summary>
        ///     Defines the <see cref="Position" /> property.
        /// </summary>
        public static readonly StyledProperty<PixelPoint> PositionProperty =
            AvaloniaProperty.Register<PortableWindow, PixelPoint>(nameof(Position));

        /// <summary>
        /// Defines the <see cref="ResizeThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> ResizeThicknessProperty =
            AvaloniaProperty.Register<PortableWindow, Thickness>(nameof(ResizeThickness), default);

        private IPortableWindowHost _host;

        /// <summary>
        ///     Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        ///     Gets or sets the icon of the window.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        ///     Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get => GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        /// <summary>
        ///     Gets or sets how the window will size itself to fit its content.
        /// </summary>
        public SizeToContent SizeToContent
        {
            get => GetValue(SizeToContentProperty);
            set => SetValue(SizeToContentProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the window can be resized.
        /// </summary>
        public bool CanResize
        {
            get => GetValue(CanResizeProperty);
            set => SetValue(CanResizeProperty, value);
        }

        /// <summary>
        ///     Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get => GetValue(WindowStateProperty);
            set => SetValue(WindowStateProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the window is activated when first shown.
        /// </summary>
        public bool ShowActivated
        {
            get => GetValue(ShowActivatedProperty);
            set => SetValue(ShowActivatedProperty, value);
        }

        /// <summary>
        ///     Gets or sets whether this window appears on top of all other windows.
        /// </summary>
        public bool Topmost
        {
            get => GetValue(TopmostProperty);
            set => SetValue(TopmostProperty, value);
        }

        /// <summary>
        ///     Gets or sets the system decorations (title bar, border, etc).
        /// </summary>
        public WindowDecorations WindowDecorations
        {
            get => GetValue(WindowDecorationsProperty);
            set => SetValue(WindowDecorationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the resize hit-test thickness. When zero (default),
        /// BorderThickness is used. Set this to a larger value to make resize
        /// edges easier to grab (e.g. for low-resolution mouse input).
        /// </summary>
        public Thickness ResizeThickness
        {
            get => GetValue(ResizeThicknessProperty);
            set => SetValue(ResizeThicknessProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether animations are used for window transitions.
        /// </summary>
        public bool AnimateWindow
        {
            get => GetValue(AnimateWindowProperty);
            set => SetValue(AnimateWindowProperty, value);
        }

        /// <summary>
        ///     Gets or sets how the <see cref="Closing" /> event behaves.
        /// </summary>
        public WindowClosingBehavior ClosingBehavior
        {
            get => GetValue(ClosingBehaviorProperty);
            set => SetValue(ClosingBehaviorProperty, value);
        }

        /// <summary>
        ///     Gets or sets the window position.
        /// </summary>
        public PixelPoint Position
        {
            get => GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        /// <summary>
        ///     Gets a value indicating whether the window is active.
        /// </summary>
        public bool IsActive => _host?.IsActive ?? false;

        /// <summary>
        ///     Gets the owner of the window.
        /// </summary>
        public PortableWindow Owner { get; private set; }

        /// <summary>
        ///     Gets the platform's clipboard implementation.
        /// </summary>
        public IClipboard Clipboard => _host?.Clipboard;

        /// <summary>
        ///     Gets the storage provider for file pickers.
        /// </summary>
        public IStorageProvider StorageProvider => _host?.StorageProvider;

        /// <summary>
        ///     Gets the focus manager.
        /// </summary>
        public IFocusManager FocusManager => _host?.FocusManager;

        /// <summary>
        ///     Gets the launcher for opening URIs.
        /// </summary>
        public ILauncher Launcher => _host?.Launcher;

        /// <summary>
        ///     Fired when the window is activated.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        ///     Fired when the window is deactivated.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        ///     Fired when the window is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        ///     Fired when the window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        ///     Fired when the window position is changed.
        /// </summary>
        public event EventHandler<PixelPointEventArgs> PositionChanged;

        /// <summary>
        ///     Fired when the window is resized.
        /// </summary>
        public event EventHandler<WindowResizedEventArgs> Resized;

        /// <summary>
        ///     Fired before the window is closed.
        /// </summary>
        public event EventHandler<WindowClosingEventArgs> Closing;

        /// <summary>
        ///     Shows the window using the appropriate host.
        /// </summary>
        public void Show()
        {
            Show(null);
        }

        /// <summary>
        ///     Shows the window using the appropriate host.
        /// </summary>
        /// <param name="parent">The parent visual or owner window.</param>
        public void Show(Visual parent)
        {
            _host = CreateHost();
            _host.ShowPortableWindow(this, parent, isDialog: false);
        }

        /// <summary>
        ///     Shows the window as a dialog.
        /// </summary>
        /// <param name="owner">The dialog's owner. If null, this will be a global modal.</param>
        /// <returns>A task that completes when the dialog is closed.</returns>
        public Task ShowDialog(Visual owner = null)
        {
            return ShowDialog<object>(owner);
        }

        /// <summary>
        ///     Shows the window as a dialog with a typed result.
        /// </summary>
        /// <typeparam name="TResult">The type of the dialog result.</typeparam>
        /// <param name="owner">The dialog's owner. If null, this will be a global modal.</param>
        /// <returns>A task that produces the dialog result when the window is closed.</returns>
        public Task<TResult> ShowDialog<TResult>(Visual owner = null)
        {
            _host = CreateHost();
            return _host.ShowPortableWindowDialog<TResult>(this, owner);
        }

        /// <summary>
        ///     Closes the window.
        /// </summary>
        public void Close()
        {
            _host?.Close();
        }

        /// <summary>
        ///     Closes the window with a dialog result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        public void Close(object dialogResult)
        {
            _host?.Close(dialogResult);
        }

        /// <summary>
        ///     Activates the window.
        /// </summary>
        public void Activate()
        {
            _host?.Activate();
        }

        /// <summary>
        ///     Creates the appropriate host for this portable window.
        ///     Override to change host selection logic.
        /// </summary>
        /// <returns>A new host window instance.</returns>
        protected virtual IPortableWindowHost CreateHost()
        {
            // currently the only environments which support native system windows are desktop (windows/macos/linux).
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                return new SystemWindowHost();
            }

            if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime)
            {
                // use the managed window host (android/ios/etc.)
                return new ManagedWindowHost();
            }

            // we don't know so use ManagedWindowHost.
            return new ManagedWindowHost();
        }

        internal void RaiseActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        internal void RaiseDeactivated()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        internal void RaiseOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        internal void RaiseClosed()
        {
            _host = null;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        internal void RaisePositionChanged(PixelPointEventArgs args)
        {
            PositionChanged?.Invoke(this, args);
        }

        internal void RaiseResized(WindowResizedEventArgs args)
        {
            Resized?.Invoke(this, args);
        }

        internal bool RaiseClosing(WindowClosingEventArgs args)
        {
            Closing?.Invoke(this, args);
            return args.Cancel;
        }

        internal void SetOwner(PortableWindow owner)
        {
            Owner = owner;
        }
    }
}
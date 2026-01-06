using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    /// This hosts a collection of windows 
    /// </summary>
    [TemplatePart(PART_Windows, typeof(Canvas))]
    [TemplatePart(PART_ModalOverlay, typeof(Panel))]
    [TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
    [PseudoClasses(CLASS_HasModal)]   

    public partial class WindowsPanel : ContentControl
    {
        public const string PART_Windows = "PART_Windows";
        public const string PART_ModalOverlay = "PART_ModalOverlay";
        public const string PART_ContentPresenter = "PART_ContentPresenter";
        private const string CLASS_HasModal = ":hasmodal";

        private Canvas _canvas;
        private Panel _modalOverlay;
        private ContentPresenter _contentPresenter;
        private ManagedWindow? _modalDialog;

        static WindowsPanel()
        {
            // Ensure WindowManagerTheme is registered
            if (Application.Current != null)
            {
                var hasTheme = Application.Current.Styles.OfType<WindowManagerTheme>().Any();
                if (!hasTheme)
                {
                    Application.Current.Styles.Insert(0, new WindowManagerTheme());
                }
            }

            // WindowsPanel should be focusable to act as a focus scope
            FocusableProperty.OverrideDefaultValue<WindowsPanel>(true);
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public WindowsPanel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            // Ensure Theme is set before any template application
            EnsureTheme();
            
            // Set keyboard navigation mode to ensure focus stays within panel
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        private void EnsureTheme()
        {
            if (Theme == null && Application.Current != null)
            {
                // Try to find the ControlTheme in Application resources
                if (Application.Current.TryFindResource(typeof(WindowsPanel), null, out var theme) && theme is ControlTheme controlTheme)
                {
                    Theme = controlTheme;
                }
            }
        }

        public ManagedWindow? ModalDialog
        {
            get => _modalDialog;
            set
            {
                if (value != null && _modalDialog != null)
                    throw new NotSupportedException("Already showing a modal dialog for this window");

                _modalDialog = value;
                if (_modalDialog != null && _modalOverlay != null)
                {
                    PseudoClasses.Add(CLASS_HasModal);
                    _modalDialog.Closed += (s, e) =>
                    {
                        _modalDialog = null;
                        PseudoClasses.Remove(CLASS_HasModal);
                    };
                }
            }
        }

        public Controls Windows => _canvas.Children;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _canvas = e.NameScope.Find<Canvas>(PART_Windows) ?? throw new ArgumentNullException(PART_Windows);
            _modalOverlay = e.NameScope.Find<Panel>(PART_ModalOverlay) ?? throw new ArgumentNullException(PART_ModalOverlay);
            _contentPresenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter) ?? throw new ArgumentNullException(PART_ContentPresenter);

            // Subscribe to focus events on the canvas to track focus changes
            _canvas.AddHandler(GotFocusEvent, OnCanvasGotFocus, RoutingStrategies.Bubble);
            
            // Add keyboard event handlers for diagnostics
            _canvas.AddHandler(KeyDownEvent, OnCanvasKeyDown, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Diagnostic handler to track keyboard events in the canvas.
        /// </summary>
        private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
        {
            var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
            var focusedElement = focusManager?.GetFocusedElement();
            
            // Find which ManagedWindow contains the focused element
            if (focusedElement is Control focusedControl)
            {
                var window = focusedControl.FindAncestorOfType<ManagedWindow>();
            }
        }

        /// <summary>
        /// Handles focus events bubbling up from windows in the canvas.
        /// Note: Window activation based on focus is handled by ManagedWindow.OnGetFocus.
        /// This handler is kept for potential future use but does not activate windows
        /// to avoid duplicate activation which can cause focus loops.
        /// </summary>
        private void OnCanvasGotFocus(object? sender, GotFocusEventArgs e)
        {
            // Window activation is handled by ManagedWindow.OnGetFocus
            // Do not activate here to avoid duplicate activation and focus loops
            Debug.WriteLine($"[WindowsPanel] OnCanvasGotFocus: Source={e.Source?.GetType().Name}");
        }

        /// <summary>
        /// Gets the currently active window, or null if no window is active.
        /// </summary>
        public ManagedWindow? ActiveWindow
        {
            get
            {
                if (_canvas == null)
                    return null;
                return _canvas.Children
                    .OfType<ManagedWindow>()
                    .FirstOrDefault(w => w.IsActive);
            }
        }

        /// <summary>
        /// Ensures there is an active window. If no window is active, activates the topmost one.
        /// </summary>
        public void EnsureActiveWindow()
        {
            if (_canvas == null)
                return;

            var activeWindow = ActiveWindow;
            if (activeWindow == null)
            {
                // Find the topmost non-minimized window and activate it
                var topWindow = _canvas.Children
                    .OfType<ManagedWindow>()
                    .Where(w => w.WindowState != WindowState.Minimized)
                    .OrderByDescending(w => w.ZIndex)
                    .FirstOrDefault();
                
                topWindow?.Activate();
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            
            Debug.WriteLine($"[WindowsPanel] OnGotFocus: Source={e.Source?.GetType().Name}");

            // If the WindowsPanel itself got focus (not a child), redirect to active window
            if (e.Source == this)
            {
                Debug.WriteLine($"[WindowsPanel] Panel itself got focus, redirecting to active window");
                Dispatcher.UIThread.Post(() =>
                {
                    EnsureActiveWindow();
                    var activeWindow = ActiveWindow;
                    if (activeWindow != null)
                    {
                        Debug.WriteLine($"[WindowsPanel] Focusing content of '{activeWindow.Title}'");
                        // Focus the active window's content
                        activeWindow.FocusContent();
                    }
                    else
                    {
                        Debug.WriteLine($"[WindowsPanel] No active window to focus");
                    }
                }, DispatcherPriority.Input);
            }
        }

        /// <summary>
        /// Show a window as overlapping window in the current WindowsPanel
        /// </summary>
        /// <param name="window"></param>
        public void Show(ManagedWindow window)
        {
            window.Show(this);
        }

        /// <summary>
        /// Show a window as a modal dialog in the current Windows Panel.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public Task ShowDialog(ManagedWindow window)
        {
            if (ModalDialog != null)
                throw new NotSupportedException("Already showing a dialog for this window");
            ModalDialog = window;
            return ModalDialog.ShowDialog(this);
        }
    }
}

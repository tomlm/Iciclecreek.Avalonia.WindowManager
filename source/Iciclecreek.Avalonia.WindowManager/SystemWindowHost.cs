using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

// ReSharper disable CheckNamespace
namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    ///     A system <see cref="Window" /> host that wraps a <see cref="PortableWindow" />
    ///     for display in a desktop/system window environment.
    /// </summary>
    public class SystemWindowHost : Window, IPortableWindowHost
    {
        private PortableWindow _portableWindow;

        bool IPortableWindowHost.IsActive => IsActive;

        IClipboard IPortableWindowHost.Clipboard => Clipboard;

        IStorageProvider IPortableWindowHost.StorageProvider => StorageProvider;

        IFocusManager IPortableWindowHost.FocusManager => FocusManager;

        ILauncher IPortableWindowHost.Launcher => Launcher;

        /// <inheritdoc />
        public void ShowPortableWindow(PortableWindow portableWindow, Visual parent, bool isDialog)
        {
            _portableWindow = portableWindow;
            BindToPortableWindow();

            if (parent is PortableWindow ownerPortable)
            {
                Window ownerWindow = FindWindowForPortableWindow(ownerPortable);
                if (ownerWindow != null)
                {
                    Show(ownerWindow);
                    return;
                }
            }
            else if (parent is Window parentWindow)
            {
                Show(parentWindow);
                return;
            }

            Show();
        }

        /// <inheritdoc />
        public Task<TResult> ShowPortableWindowDialog<TResult>(PortableWindow portableWindow, Visual owner)
        {
            _portableWindow = portableWindow;
            BindToPortableWindow();

            Window ownerWindow = null;
            if (owner is PortableWindow ownerPortable)
            {
                portableWindow.SetOwner(ownerPortable);
                ownerWindow = FindWindowForPortableWindow(ownerPortable);
            }
            else if (owner is Window window)
            {
                ownerWindow = window;
            }

            if (ownerWindow != null)
                return ShowDialog<TResult>(ownerWindow);

            // If no owner found, try main window
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (lifetime?.MainWindow != null)
                return ShowDialog<TResult>(lifetime.MainWindow);

            // Last resort: show as normal window and return a task that completes on close
            var tcs = new TaskCompletionSource<TResult>();
            Closed += (_, _) => tcs.TrySetResult(default);
            Show();
            return tcs.Task;
        }

        void IPortableWindowHost.Close()
        {
            Close();
        }

        void IPortableWindowHost.Close(object dialogResult)
        {
            Close(dialogResult);
        }

        void IPortableWindowHost.Activate()
        {
            Activate();
        }

        private void BindToPortableWindow()
        {
            Content = _portableWindow;

            // Two-way bindings for window-specific properties
            Bind(Window.TitleProperty, new Binding(nameof(PortableWindow.Title))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.WindowStartupLocationProperty, new Binding(nameof(PortableWindow.WindowStartupLocation))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.SizeToContentProperty, new Binding(nameof(PortableWindow.SizeToContent))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.CanResizeProperty, new Binding(nameof(PortableWindow.CanResize))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.WindowStateProperty, new Binding(nameof(PortableWindow.WindowState))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.ShowActivatedProperty, new Binding(nameof(PortableWindow.ShowActivated))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.TopmostProperty, new Binding(nameof(PortableWindow.Topmost))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.WindowDecorationsProperty, new Binding(nameof(PortableWindow.WindowDecorations))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(Window.ClosingBehaviorProperty, new Binding(nameof(PortableWindow.ClosingBehavior))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });

            // One-way bindings for layout / appearance properties
            Bind(WidthProperty, new Binding(nameof(Width))
            { Source = _portableWindow, Mode = BindingMode.OneWay });
            Bind(HeightProperty, new Binding(nameof(Height))
            { Source = _portableWindow, Mode = BindingMode.OneWay });
            Bind(MinWidthProperty, new Binding(nameof(MinWidth))
            { Source = _portableWindow, Mode = BindingMode.OneWay });
            Bind(MinHeightProperty, new Binding(nameof(MinHeight))
            { Source = _portableWindow, Mode = BindingMode.OneWay });
            Bind(MaxWidthProperty, new Binding(nameof(MaxWidth))
            { Source = _portableWindow, Mode = BindingMode.OneWay });
            Bind(MaxHeightProperty, new Binding(nameof(MaxHeight))
            { Source = _portableWindow, Mode = BindingMode.OneWay });

            // Wire up host events -> PortableWindow
            Activated += (_, _) => _portableWindow.RaiseActivated();
            Deactivated += (_, _) => _portableWindow.RaiseDeactivated();
            Opened += (_, _) => _portableWindow.RaiseOpened();
            Closed += (_, _) => _portableWindow.RaiseClosed();
            PositionChanged += (_, e) =>
            {
                _portableWindow.SetValue(PortableWindow.PositionProperty, Position);
                _portableWindow.RaisePositionChanged(e);
            };
            Closing += (_, e) =>
            {
                if (_portableWindow.RaiseClosing(e))
                    e.Cancel = true;
            };
        }

        private static Window FindWindowForPortableWindow(PortableWindow portableWindow)
        {
            // Walk up the visual tree from the PortableWindow to find its SystemWindowHost
            return portableWindow.FindAncestorOfType<SystemWindowHost>()
                   ?? portableWindow.FindAncestorOfType<Window>();
        }
    }
}
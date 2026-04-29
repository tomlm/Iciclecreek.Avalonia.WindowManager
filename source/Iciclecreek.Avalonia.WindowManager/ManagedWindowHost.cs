using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Iciclecreek.Avalonia.WindowManager;

// ReSharper disable CheckNamespace

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    ///     A <see cref="ManagedWindow" /> host that wraps a <see cref="PortableWindow" />
    ///     for display in a console/TUI environment.
    /// </summary>
    public class ManagedWindowHost : ManagedWindow, IPortableWindowHost
    {
        private PortableWindow _portableWindow;

        IClipboard IPortableWindowHost.Clipboard => Clipboard;

        IStorageProvider IPortableWindowHost.StorageProvider => StorageProvider;

        IFocusManager IPortableWindowHost.FocusManager => FocusManager;

        ILauncher IPortableWindowHost.Launcher => Launcher;

        /// <inheritdoc />
        public void ShowPortableWindow(PortableWindow portableWindow, Visual parent, bool isDialog)
        {
            _portableWindow = portableWindow;
            BindToPortableWindow();
            Show(parent);
        }

        /// <inheritdoc />
        public Task<TResult> ShowPortableWindowDialog<TResult>(PortableWindow portableWindow, Visual owner)
        {
            _portableWindow = portableWindow;
            BindToPortableWindow();

            if (owner is PortableWindow ownerPortable)
            {
                portableWindow.SetOwner(ownerPortable);
                // Find the ManagedWindowHost that owns the parent PortableWindow
                Visual actualOwner = FindHostForPortableWindow(ownerPortable) ?? (Visual)owner;
                return ShowDialog<TResult>(actualOwner);
            }

            return ShowDialog<TResult>(owner);
        }

        void IPortableWindowHost.Activate()
        {
            Activate();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // ManagedWindow.Position CLR setter calls Canvas.SetLeft/SetTop,
            // but Avalonia bindings bypass the CLR setter. Replicate that here.
            if (change.Property == PositionProperty)
            {
                Canvas.SetLeft(this, Position.X);
                Canvas.SetTop(this, Position.Y);
            }
        }

        private void BindToPortableWindow()
        {
            Content = _portableWindow;

            // Two-way bindings for window-specific properties
            Bind(TitleProperty, new Binding(nameof(PortableWindow.Title))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(IconProperty, new Binding(nameof(PortableWindow.Icon))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(WindowStartupLocationProperty, new Binding(nameof(PortableWindow.WindowStartupLocation))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(SizeToContentProperty, new Binding(nameof(PortableWindow.SizeToContent))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(CanResizeProperty, new Binding(nameof(PortableWindow.CanResize))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(WindowStateProperty, new Binding(nameof(PortableWindow.WindowState))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(ShowActivatedProperty, new Binding(nameof(PortableWindow.ShowActivated))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(TopmostProperty, new Binding(nameof(PortableWindow.Topmost))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(WindowDecorationsProperty, new Binding(nameof(PortableWindow.WindowDecorations))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(AnimateWindowProperty, new Binding(nameof(PortableWindow.AnimateWindow))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(ClosingBehaviorProperty, new Binding(nameof(PortableWindow.ClosingBehavior))
            { Source = _portableWindow, Mode = BindingMode.TwoWay });
            Bind(PositionProperty, new Binding(nameof(PortableWindow.Position))
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
            PositionChanged += (_, e) => _portableWindow.RaisePositionChanged(e);
            Resized += (_, e) => _portableWindow.RaiseResized(e);
            Closing += (_, e) =>
            {
                if (_portableWindow.RaiseClosing(e))
                    e.Cancel = true;
            };
        }

        private static Visual FindHostForPortableWindow(PortableWindow portableWindow)
        {
            // Walk up the visual tree from the PortableWindow to find its ManagedWindowHost
            return portableWindow.FindAncestorOfType<ManagedWindowHost>();
        }
    }
}
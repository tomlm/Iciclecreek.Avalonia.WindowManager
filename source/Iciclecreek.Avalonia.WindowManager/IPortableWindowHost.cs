using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

// ReSharper disable CheckNamespace

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    ///     Interface for host windows that can display a <see cref="PortableWindow" />.
    /// </summary>
    public interface IPortableWindowHost
    {
        /// <summary>
        ///     Gets a value indicating whether the host window is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        ///     Gets the clipboard service.
        /// </summary>
        IClipboard Clipboard { get; }

        /// <summary>
        ///     Gets the storage provider service.
        /// </summary>
        IStorageProvider StorageProvider { get; }

        /// <summary>
        ///     Gets the focus manager.
        /// </summary>
        IFocusManager FocusManager { get; }

        /// <summary>
        ///     Gets the launcher service.
        /// </summary>
        ILauncher Launcher { get; }

        /// <summary>
        ///     Shows the portable window in this host.
        /// </summary>
        void ShowPortableWindow(PortableWindow portableWindow, Visual parent, bool isDialog);

        /// <summary>
        ///     Shows the portable window as a dialog in this host.
        /// </summary>
        Task<TResult> ShowPortableWindowDialog<TResult>(PortableWindow portableWindow, Visual owner);

        /// <summary>
        ///     Closes the host window.
        /// </summary>
        void Close();

        /// <summary>
        ///     Closes the host window with a dialog result.
        /// </summary>
        void Close(object dialogResult);

        /// <summary>
        ///     Activates the host window.
        /// </summary>
        void Activate();
    }
}
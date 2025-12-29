using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using System;
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
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public WindowsPanel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            // Ensure Theme is set before any template application
            EnsureTheme();
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

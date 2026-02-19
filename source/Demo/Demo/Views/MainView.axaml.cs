using Avalonia.Controls;
using Avalonia.Interactivity;
using Demo.ViewModels;
using Iciclecreek.Avalonia.WindowManager;
using System;

namespace Demo.Views
{
    public partial class MainView : Panel
    {
        public MainView()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        private void OnAddWindowClick(object? sender, RoutedEventArgs args)
        {
            var window = new MyWindow()
            {
                WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
                SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString()),
                WindowState = Enum.Parse<WindowState>(((ComboBoxItem)WindowStateCombo.SelectedItem).Tag.ToString()),    
            };

            window.Show(this);
        }

        private void OnClick(object? sender, RoutedEventArgs args)
        {
            MainViewModel model = (MainViewModel)DataContext;
            model.Counter++;
        }

        private async void OnShowDialog(object? sender, RoutedEventArgs args)
        {
            var dialog = new MyDialog()
            {
                WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
                SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString()),
                CanResize = false,
            };
            dialog.ViewModel.Text = ViewModel.Text;

            var result = await dialog.ShowDialog<string?>(this);
            if (result != null)
                ViewModel.Text = result;
        }
    }
}
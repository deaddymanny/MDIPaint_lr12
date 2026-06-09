using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using ProgressBar = System.Windows.Controls.ProgressBar;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace PaintApp
{
    public partial class MainWindow : Window
    {
        private PluginManager _pluginManager;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
            _pluginManager = new PluginManager();
            UpdateFiltersMenu();
            UpdateUIState();
        }

        private void UpdateFiltersMenu()
        {
            FiltersMenu.Items.Clear();
            var configItem = new MenuItem { Header = "Настроить плагины...", ToolTip = "Управление загрузкой модулей" };
            configItem.Click += ConfigurePlugins_Click;
            FiltersMenu.Items.Add(configItem);
            FiltersMenu.Items.Add(new Separator());

            foreach (var plugin in _pluginManager.LoadedPlugins.Values)
            {
                MenuItem item = new MenuItem { Header = plugin.Name, Tag = plugin };
                item.Click += Plugin_Click;
                FiltersMenu.Items.Add(item);
            }
        }

        private void UpdateUIState()
        {
            bool hasDoc = DocumentTabs.SelectedItem != null;
            foreach (var item in FiltersMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Tag != null)
                {
                    menuItem.IsEnabled = hasDoc;
                }
            }
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = new DocumentView();
            TabItem tab = new TabItem { Header = "Без названия", Content = doc };
            DocumentTabs.Items.Add(tab);
            DocumentTabs.SelectedItem = tab;
            UpdateUIState();
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                DocumentView doc = new DocumentView();
                await doc.LoadImageAsync(dlg.FileName);
                TabItem tab = new TabItem { Header = Path.GetFileName(dlg.FileName), Content = doc, Tag = dlg.FileName };
                DocumentTabs.Items.Add(tab);
                DocumentTabs.SelectedItem = tab;
                UpdateUIState();
            }
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                if (tab.Tag is string path)
                {
                    await doc.SaveImageAsync(path);
                }
                else
                {
                    SaveAsFile_Click(sender, e);
                }
            }
        }

        private async void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                SaveFileDialog dlg = new SaveFileDialog { Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp" };
                if (dlg.ShowDialog() == true)
                {
                    await doc.SaveImageAsync(dlg.FileName);
                    tab.Header = Path.GetFileName(dlg.FileName);
                    tab.Tag = dlg.FileName;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                ColorPickerWindow colorPicker = new ColorPickerWindow
                {
                    Owner = this
                };

                if (colorPicker.ShowDialog() == true)
                {
                    doc.CurrentColor = colorPicker.SelectedColor;
                    StatusText.Text = $"Цвет изменен на: {doc.CurrentColor.Name}";
                }
            }
        }

        private void StarSettings_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                StarSettingsWindow settingsWindow = new StarSettingsWindow(doc.StarPoints, doc.StarInnerRatio)
                {
                    Owner = this
                };

                if (settingsWindow.ShowDialog() == true)
                {
                    doc.StarPoints = settingsWindow.Points;
                    doc.StarInnerRatio = settingsWindow.Ratio;
                    StatusText.Text = $"Звезда: {doc.StarPoints} лучей, коэффициент {doc.StarInnerRatio}";
                }
            }
        }

        private void SetTool_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                doc.CurrentTool = (string)((MenuItem)sender).Tag;
                StatusText.Text = $"Инструмент: {doc.CurrentTool}";
            }
        }

        private async void Plugin_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem tab && tab.Content is DocumentView doc)
            {
                if (!(e.OriginalSource is MenuItem item) || !(item.Tag is PluginInterface.IPlugin plugin)) return;

                _cts = new CancellationTokenSource();
                Window progressWindow = new Window
                {
                    Title = plugin.Name,
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    WindowStyle = WindowStyle.ToolWindow
                };

                ProgressBar pb = new ProgressBar { Height = 20, Margin = new Thickness(10), Minimum = 0, Maximum = 100 };
                Button cancelBtn = new Button { Content = "Отмена", Width = 80, Height = 30, Margin = new Thickness(10) };
                cancelBtn.Click += (s, args) => _cts.Cancel();

                StackPanel panel = new StackPanel();
                panel.Children.Add(new Label { Content = "Обработка...", HorizontalAlignment = HorizontalAlignment.Center });
                panel.Children.Add(pb);
                panel.Children.Add(cancelBtn);
                progressWindow.Content = panel;

                var progress = new Progress<int>(val => pb.Value = val);

                await Task.Run(() =>
                {
                    var bmp = doc.GetBitmap();
                    plugin.TransformAsync(bmp, progress, _cts.Token);
                    Application.Current.Dispatcher.Invoke(() => doc.SetBitmap(bmp));
                }, _cts.Token);

                progressWindow.Close();
                StatusText.Text = _cts.IsCancellationRequested ? "Отменено" : "Готово";
            }
        }

        private void ConfigurePlugins_Click(object sender, RoutedEventArgs e)
        {
            _pluginManager.ShowConfigDialog();
            UpdateFiltersMenu();
        }

        private void CascadeWindows_Click(object sender, RoutedEventArgs e) { }
        private void TileWindows_Click(object sender, RoutedEventArgs e) { }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MDI Paint WPF\nВерсия 1.0\nРеализация на 10 баллов + Вариант 1", "О программе");
        }

        private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUIState();
        }
    }
}
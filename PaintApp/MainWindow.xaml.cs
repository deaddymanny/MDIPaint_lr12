using AvalonDock.Layout;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using AvalonDock;

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
            DockManager.ActiveContentChanged += DockManager_ActiveContentChanged;
        }

        private void UpdateFiltersMenu()
        {
            FiltersGroup.Items.Clear();

            var configBtn = new RibbonButton { Label = "Настроить плагины...", Tag = "Config" };
            configBtn.Click += ConfigurePlugins_Click;
            FiltersGroup.Items.Add(configBtn);

            FiltersGroup.Items.Add(new RibbonSeparator());

            foreach (var plugin in _pluginManager.LoadedPlugins.Values)
            {
                var btn = new RibbonButton { Label = plugin.Name, Tag = plugin };
                btn.Click += Plugin_Click;
                FiltersGroup.Items.Add(btn);
            }
        }

        private void UpdateUIState()
        {
            bool hasDoc = GetActiveDocument() != null;
            foreach (var item in FiltersGroup.Items)
            {
                if (item is RibbonButton btn && btn.Tag is PluginInterface.IPlugin)
                {
                    btn.IsEnabled = hasDoc;
                }
            }
        }

        private DocumentView GetActiveDocument()
        {
            var activeLayoutDoc = DockManager.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive);
            return activeLayoutDoc?.Content as DocumentView;
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = new DocumentView();
            LayoutDocument layoutDoc = new LayoutDocument { Title = "Без названия", Content = doc };
            DocumentPane.Children.Add(layoutDoc);
            layoutDoc.IsActive = true;
            UpdateUIState();
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                DocumentView doc = new DocumentView();
                await doc.LoadImageAsync(dlg.FileName);
                LayoutDocument layoutDoc = new LayoutDocument { Title = Path.GetFileName(dlg.FileName), Content = doc, ToolTip = dlg.FileName };
                DocumentPane.Children.Add(layoutDoc);
                layoutDoc.IsActive = true;
                UpdateUIState();
            }
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = GetActiveDocument();
            if (doc != null && DockManager.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive) is LayoutDocument tab)
            {
                if (tab.ToolTip is string path)
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
            DocumentView doc = GetActiveDocument();
            if (doc != null && DockManager.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => d.IsActive) is LayoutDocument tab)
            {
                SaveFileDialog dlg = new SaveFileDialog { Filter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp" };
                if (dlg.ShowDialog() == true)
                {
                    await doc.SaveImageAsync(dlg.FileName);
                    tab.Title = Path.GetFileName(dlg.FileName);
                    tab.ToolTip = dlg.FileName;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = GetActiveDocument();
            if (doc != null)
            {
                ColorPickerWindow colorPicker = new ColorPickerWindow { Owner = this };
                if (colorPicker.ShowDialog() == true)
                {
                    doc.CurrentColor = colorPicker.SelectedColor;
                    StatusText.Text = $"Цвет изменен на: {doc.CurrentColor.Name}";
                }
            }
        }

        private void StarSettings_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = GetActiveDocument();
            if (doc != null)
            {
                StarSettingsWindow settingsWindow = new StarSettingsWindow(doc.StarPoints, doc.StarInnerRatio) { Owner = this };
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
            DocumentView doc = GetActiveDocument();
            if (doc != null && sender is RibbonButton btn)
            {
                doc.CurrentTool = btn.Tag as string;
                StatusText.Text = $"Инструмент: {doc.CurrentTool}";
            }
        }

        private async void Plugin_Click(object sender, RoutedEventArgs e)
        {
            DocumentView doc = GetActiveDocument();
            if (doc == null) return;

            if (!(sender is RibbonButton btn) || !(btn.Tag is PluginInterface.IPlugin plugin)) return;

            _cts = new CancellationTokenSource();
            Window progressWindow = new Window
            {
                Title = plugin.Name,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize
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

            progressWindow.Show();

            try
            {
                await Task.Run(() =>
                {
                    var bmp = doc.GetBitmap();
                    plugin.TransformAsync(bmp, progress, _cts.Token);
                    Application.Current.Dispatcher.Invoke(() => doc.SetBitmap(bmp));
                }, _cts.Token);
            }
            finally
            {
                progressWindow.Close();
                StatusText.Text = _cts.IsCancellationRequested ? "Отменено" : "Готово";
            }
        }

        private void ConfigurePlugins_Click(object sender, RoutedEventArgs e)
        {
            _pluginManager.ShowConfigDialog();
            UpdateFiltersMenu();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MDI Paint WPF\nВерсия 1.0\nРеализация на 10 баллов + Вариант 1\nИнтеграция AvalonDock для причаливающих окон", "О программе");
        }

        private void DockManager_ActiveContentChanged(object sender, EventArgs e)
        {
            UpdateUIState();
        }
    }
}
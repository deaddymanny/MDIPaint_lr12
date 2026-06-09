using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using PluginInterface;

namespace PaintApp
{
    public class PluginManager
    {
        private readonly string _configPath = "plugins.config.json";
        private readonly string _pluginsDir = "Plugins";
        public Dictionary<string, IPlugin> LoadedPlugins { get; } = new Dictionary<string, IPlugin>();

        public PluginManager()
        {
            if (!Directory.Exists(_pluginsDir))
            {
                Directory.CreateDirectory(_pluginsDir);
            }
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            PluginConfig config = LoadConfig();
            string[] files = Directory.GetFiles(_pluginsDir, "*.dll");

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                bool isEnabled = config.Plugins.FirstOrDefault(p => p.FileName == fileName)?.IsEnabled ?? true;

                if (isEnabled)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(file);
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                            {
                                IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                                LoadedPlugins[plugin.Name] = plugin;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            SaveConfig(config, files);
        }

        private PluginConfig LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
            }
            return new PluginConfig();
        }

        private void SaveConfig(PluginConfig config, string[] currentFiles)
        {
            var fileNames = currentFiles.Select(Path.GetFileName).ToList();
            config.Plugins = config.Plugins.Where(p => fileNames.Contains(p.FileName)).ToList();

            foreach (var file in fileNames)
            {
                if (config.Plugins.All(p => p.FileName != file))
                {
                    config.Plugins.Add(new PluginEntry { FileName = file, IsEnabled = true });
                }
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public void ShowConfigDialog()
        {
            Window dialog = new Window
            {
                Title = "Управление плагинами",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            System.Windows.Controls.ListBox listBox = new System.Windows.Controls.ListBox { Margin = new Thickness(10) };
            PluginConfig config = LoadConfig();
            string[] files = Directory.GetFiles(_pluginsDir, "*.dll");

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                var entry = config.Plugins.FirstOrDefault(p => p.FileName == fileName) ?? new PluginEntry { FileName = fileName, IsEnabled = true };

                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    Type type = assembly.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
                    if (type != null)
                    {
                        var versionAttr = (VersionAttribute)Attribute.GetCustomAttribute(type, typeof(VersionAttribute));
                        IPlugin tempPlugin = (IPlugin)Activator.CreateInstance(type);

                        string version = versionAttr != null ? $"v{versionAttr.Major}.{versionAttr.Minor}" : "v1.0";
                        string displayText = $"{tempPlugin.Name} ({tempPlugin.Author}, {version})";

                        System.Windows.Controls.CheckBox checkBox = new System.Windows.Controls.CheckBox
                        {
                            Content = displayText,
                            IsChecked = entry.IsEnabled,
                            Tag = fileName,
                            Margin = new Thickness(5)
                        };
                        listBox.Items.Add(checkBox);
                    }
                }
                catch { }
            }

            System.Windows.Controls.Button saveBtn = new System.Windows.Controls.Button
            {
                Content = "Сохранить и перезагрузить",
                Width = 150,
                Height = 30,
                Margin = new Thickness(10)
            };

            saveBtn.Click += (s, e) =>
            {
                config.Plugins.Clear();
                foreach (System.Windows.Controls.CheckBox cb in listBox.Items)
                {
                    config.Plugins.Add(new PluginEntry { FileName = (string)cb.Tag, IsEnabled = cb.IsChecked == true });
                }
                SaveConfig(config, files);
                LoadedPlugins.Clear();
                LoadPlugins();
                dialog.Close();
                MessageBox.Show("Конфигурация обновлена. Перезапустите приложение или обновите меню.");
            };

            System.Windows.Controls.StackPanel panel = new System.Windows.Controls.StackPanel();
            panel.Children.Add(new System.Windows.Controls.Label { Content = "Доступные плагины:", FontWeight = FontWeights.Bold, Margin = new Thickness(10, 10, 10, 0) });
            panel.Children.Add(listBox);
            panel.Children.Add(saveBtn);
            dialog.Content = panel;
            dialog.ShowDialog();
        }
    }
}

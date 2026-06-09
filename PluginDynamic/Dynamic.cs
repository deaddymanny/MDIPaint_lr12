using System;
using System.CodeDom.Compiler;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PluginInterface;
using static System.Net.Mime.MediaTypeNames;

namespace PluginDynamic
{
    [Version(1, 0)]
    public class DynamicPlugin : IPlugin
    {
        public string Name => "Динамический скрипт (C#)";
        public string Author => "Student";

        public void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken)
        {
            progress.Report(10);
            if (cancellationToken.IsCancellationRequested) return;

            string code = ShowCodeEditor();
            if (string.IsNullOrEmpty(code))
            {
                progress.Report(100);
                return;
            }

            progress.Report(30);
            if (cancellationToken.IsCancellationRequested) return;

            CompilerResults results = CompileCode(code);
            if (results.Errors.HasErrors)
            {
                MessageBox.Show("Ошибка компиляции: " + results.Errors[0].ErrorText, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Report(100);
                return;
            }

            progress.Report(60);
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                Assembly assembly = results.CompiledAssembly;
                Type scriptType = assembly.GetType("UserScript");
                MethodInfo method = scriptType.GetMethod("Process");

                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
                byte[] pixels = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, bytes);

                progress.Report(80);
                method.Invoke(null, new object[] { pixels, bitmap.Width, bitmap.Height });

                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, bytes);
                bitmap.UnlockBits(bmpData);
                progress.Report(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка выполнения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Report(100);
            }
        }

        private string ShowCodeEditor()
        {
            Window window = new Window
            {
                Title = "Введите C# код плагина",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            TextBox textBox = new TextBox
            {
                Text = "using System;\npublic class UserScript {\n    public static void Process(byte[] pixels, int width, int height) {\n        for(int i=0; i<pixels.Length; i+=3) {\n            pixels[i] = (byte)(255 - pixels[i]);\n        }\n    }\n}",
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };

            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };

            Button btnOk = new Button { Content = "Применить", Width = 100, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
            Button btnCancel = new Button { Content = "Отмена", Width = 100, Height = 30 };

            panel.Children.Add(btnOk);
            panel.Children.Add(btnCancel);

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(textBox, 0);
            Grid.SetRow(panel, 1);

            grid.Children.Add(textBox);
            grid.Children.Add(panel);
            window.Content = grid;

            string result = null;
            btnOk.Click += (s, e) => { result = textBox.Text; window.DialogResult = true; window.Close(); };
            btnCancel.Click += (s, e) => { window.DialogResult = false; window.Close(); };

            bool? dialogResult = window.ShowDialog();
            return dialogResult == true ? result : null;
        }

        private CompilerResults CompileCode(string code)
        {
            using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
            {
                CompilerParameters parameters = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false
                };
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Drawing.dll");
                return provider.CompileAssemblyFromSource(parameters, code);
            }
        }
    }
}
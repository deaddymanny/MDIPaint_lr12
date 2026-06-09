using System;
using System.Windows;
using System.Windows.Controls;

namespace PaintApp
{
    public partial class ColorPickerWindow : Window
    {
        public System.Drawing.Color SelectedColor { get; private set; } = System.Drawing.Color.Black;

        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorName)
            {
                SelectedColor = System.Drawing.Color.FromName(colorName);
                DialogResult = true;
                Close();
            }
        }
    }
}
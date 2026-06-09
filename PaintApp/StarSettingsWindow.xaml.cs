using System;
using System.Windows;

namespace PaintApp
{
    public partial class StarSettingsWindow : Window
    {
        public int Points { get; private set; }
        public double Ratio { get; private set; }

        public StarSettingsWindow(int currentPoints, double currentRatio)
        {
            InitializeComponent();
            PointsTextBox.Text = currentPoints.ToString();
            RatioTextBox.Text = currentRatio.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PointsTextBox.Text, out int points) && points >= 3)
            {
                Points = points;
            }
            else
            {
                MessageBox.Show("Количество лучей должно быть целым числом >= 3", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (double.TryParse(RatioTextBox.Text, out double ratio) && ratio > 0.0 && ratio < 1.0)
            {
                Ratio = ratio;
            }
            else
            {
                MessageBox.Show("Отношение радиусов должно быть числом от 0.1 до 0.9", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
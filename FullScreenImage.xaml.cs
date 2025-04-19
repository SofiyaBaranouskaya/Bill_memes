using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bill_memes
{
    public partial class FullScreenImage : Window
    {
        public FullScreenImage(BitmapImage image)
        {
            InitializeComponent();

            // Устанавливаем изображение в элемент Image
            FullScreenImageViewer.Source = image;
            FullScreenImageViewer.MouseDown += CloseWindow;

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Закрыть окно при клике на изображение
            this.Close();
        }
    }
}

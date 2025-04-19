using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Bill_memes
{
    public partial class MainWindow : Window
    {
        private (string text, string imagePath)[] memes;

        public MainWindow()
        {
            InitializeComponent();
            LoadMemes();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.WorkArea.Left;
            this.Top = SystemParameters.WorkArea.Top;
            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadMemes()
        {
            try
            {
                string allText = File.ReadAllText("memes.txt");
                var blocks = allText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                memes = new (string, string)[blocks.Length];

                for (int i = 0; i < blocks.Length; i++)
                {
                    var lines = blocks[i].Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    string imagePath = lines[^1];
                    string text = string.Join(Environment.NewLine, lines, 0, lines.Length - 1);
                    memes[i] = (text, imagePath);
                }

                Random rng = new Random();
                memes = memes.OrderBy(m => rng.Next()).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error with data loading: {ex.Message}");
            }
        }

        private void GenerateMeme_Click(object sender, RoutedEventArgs e)
        {
            if (memes != null && memes.Length > 0)
            {
                Random random = new Random();
                int index = random.Next(memes.Length);
                var selectedMeme = memes[index];

                memeTextBlock.Text = selectedMeme.text;

                try
                {
                    string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), selectedMeme.imagePath);
                    BitmapImage bitmap = new BitmapImage(new Uri(absolutePath, UriKind.Absolute));
                    imageControl.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error on image loading: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("No memes available.");
            }
        }

        private void SaveAsImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Принудительно обновляем layout
                memeContainer.UpdateLayout();

                // Создаем временный холст для корректного рендеринга
                var canvas = new Canvas
                {
                    Width = memeContainer.ActualWidth,
                    Height = memeContainer.ActualHeight,
                    Background = Brushes.White
                };

                // Создаем визуальную копию контейнера
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = new VisualBrush(memeContainer);
                    context.DrawRectangle(brush, null, new Rect(0, 0, memeContainer.ActualWidth, memeContainer.ActualHeight));
                }

                canvas.Children.Add(new DrawingVisualHost(visual));

                // Измеряем и располагаем холст
                canvas.Measure(new Size(canvas.Width, canvas.Height));
                canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));

                // Создаем RenderTargetBitmap
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)canvas.Width,
                    (int)canvas.Height,
                    96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(canvas);

                // Диалог сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                    Title = "Save Meme As Image",
                    FileName = "meme_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder = saveFileDialog.FileName.EndsWith(".jpg")
                        ? new JpegBitmapEncoder()
                        : new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBox.Show("Meme saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving meme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный класс для отображения DrawingVisual
        public class DrawingVisualHost : FrameworkElement
        {
            private readonly Visual _visual;

            public DrawingVisualHost(Visual visual)
            {
                _visual = visual;
            }

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException();
                return _visual;
            }
        }

        private void Create_meme_Click(object sender, RoutedEventArgs e) {
            CreateMeme create_meme = new CreateMeme();
            create_meme.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Profile profile = new Profile();
            profile.Show();
            this.Close();
        }
    }
}
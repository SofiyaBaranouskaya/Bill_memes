using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Bill_memes
{
    public class HalfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width * 0.5; // 50% от ширины
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class CreateMeme : Window
    {

        private string ConnectionString
        {
            get
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                return $"Host={config["PostgreSQL:Host"]};" +
                       $"Port={config["PostgreSQL:Port"]};" +
                       $"Database={config["PostgreSQL:Database"]};" +
                       $"Username={config["PostgreSQL:Username"]};" +
                       $"Password={config["PostgreSQL:Password"]};";
            }
        }

        private SolidColorBrush selectedColor = new SolidColorBrush(Colors.Black);
        private SolidColorBrush selectedBackgroundColor = new SolidColorBrush(Colors.White);
        public CreateMeme()
        {
            InitializeComponent();
            InitializeColorWheels();
            SubscribeToEvents();
            UpdatePreview();
        }

        private void InitializeColorWheels()
        {
            InitializeColorWheel(ColorWheel);
            InitializeColorWheel(ColorWheelBackground);
        }

        private void InitializeColorWheel(Rectangle target)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                for (int angle = 0; angle < 360; angle++)
                {
                    Color color = ColorFromHue(angle);
                    double radians = angle * Math.PI / 180;
                    Point startPoint = new Point(75 + Math.Cos(radians) * 75, 75 + Math.Sin(radians) * 75);

                    context.DrawLine(new Pen(new SolidColorBrush(color), 1), new Point(75, 75), startPoint);
                }

                context.DrawGeometry(new SolidColorBrush(Colors.Transparent), new Pen(Brushes.Transparent, 0),
                    new EllipseGeometry(new Point(75, 75), 75, 75));
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(150, 150, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);
            target.Fill = new ImageBrush(bmp);
        }

        private Color ColorFromHue(int angle)
        {
            // Преобразование угла в цвет
            double r, g, b;

            if (angle < 60)
            {
                r = 255;
                g = (angle / 60.0) * 255;
                b = 0;
            }
            else if (angle < 120)
            {
                r = (120 - angle) / 60.0 * 255;
                g = 255;
                b = 0;
            }
            else if (angle < 180)
            {
                r = 0;
                g = 255;
                b = (angle - 120) / 60.0 * 255;
            }
            else if (angle < 240)
            {
                r = 0;
                g = (240 - angle) / 60.0 * 255;
                b = 255;
            }
            else if (angle < 300)
            {
                r = (angle - 240) / 60.0 * 255;
                g = 0;
                b = 255;
            }
            else
            {
                r = 255;
                g = 0;
                b = (360 - angle) / 60.0 * 255;
            }

            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        private double Clamp(double min, double max, double value)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void ColorCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Canvas canvas = (Canvas)sender;
                Point position = e.GetPosition(canvas);
                double centerX = canvas.Width / 2;
                double centerY = canvas.Height / 2;
                double dx = position.X - centerX;
                double dy = position.Y - centerY;
                double radius = Math.Sqrt(dx * dx + dy * dy);

                if (radius > 75) return;

                double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
                if (angle < 0) angle += 360;

                Color selectedColorValue = ColorFromHue((int)angle);

                if (sender == ColorCanvas) // Круг для цвета текста
                {
                    selectedColor.Color = selectedColorValue;
                    PreviewText.Foreground = selectedColor;
                }
                else if (sender == ColorBackgroundCanvas) // Круг для цвета фона
                {
                    selectedBackgroundColor.Color = selectedColorValue;
                    MemePreviewGrid.Background = selectedBackgroundColor;
                }
            }
        }


        private void ColorCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            // Сброс цвета при уходе мыши
            PreviewText.Foreground = selectedColor;
        }

        private void SubscribeToEvents()
        {
            MemeText.TextChanged += (s, e) => UpdatePreview();
            FontFamilyCombo.SelectionChanged += (s, e) => UpdatePreview();
            FontSizeSlider.ValueChanged += (s, e) => UpdatePreview();
            BoldCheckBox.Checked += (s, e) => UpdatePreview();
            BoldCheckBox.Unchecked += (s, e) => UpdatePreview();
            ItalicCheckBox.Checked += (s, e) => UpdatePreview();
            ItalicCheckBox.Unchecked += (s, e) => UpdatePreview();
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

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                PreviewImage.Source = bitmap;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            // Обновляем текст
            PreviewText.Text = MemeText.Text;

            // Обновляем цвет текста
            PreviewText.Foreground = selectedColor;

            // Обновляем шрифт
            if (FontFamilyCombo.SelectedItem is ComboBoxItem fontFamilyItem)
            {
                string fontFamily = fontFamilyItem.Content.ToString();
                PreviewText.FontFamily = new FontFamily(fontFamily);
            }

            // Обновляем размер шрифта
            PreviewText.FontSize = FontSizeSlider.Value;

            // Обновляем стиль шрифта
            PreviewText.FontWeight = BoldCheckBox.IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
            PreviewText.FontStyle = ItalicCheckBox.IsChecked == true ? FontStyles.Italic : FontStyles.Normal;

            // Обновляем цвет фона
            PreviewText.Background = selectedBackgroundColor;

        }

        private string ConvertRenderToBase64(RenderTargetBitmap renderBitmap)
        {
            // Создаем PngBitmapEncoder для кодирования изображения
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var memoryStream = new System.IO.MemoryStream())
            {
                encoder.Save(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        private void SaveMemeToDatabase(string base64String)
        {
            int userId = App.CurrentUserId;

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO memes (id, user_id, image_base64)
                VALUES (gen_random_uuid(), @userId, @imageBase64)
                RETURNING id;
            ";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@imageBase64", base64String);

                        var memeId = command.ExecuteScalar(); // Получаем UUID

                        if (memeId != null)
                        {
                            string shareLink = $"meme://{memeId}";
                            Clipboard.SetText(shareLink);

                            MessageBox.Show($"Мем сохранен! Ссылка скопирована в буфер обмена:\n{shareLink}",
                                            "Поделиться мемом", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при сохранении мема.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении мема: {ex.Message}");
            }
        }


        private bool UserExists(int userId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(1) FROM users WHERE id = @id";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", userId);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void SaveMeme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                    FileName = "custom_meme.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Создаем временный контейнер для рендеринга
                    Border renderBorder = new Border
                    {
                        Background = MemePreviewGrid.Background,
                        Child = new Grid
                        {
                            Width = MemePreviewGrid.Width,
                            ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                            Children =
                    {
                        new TextBlock
                        {
                            Text = PreviewText.Text,
                            FontFamily = PreviewText.FontFamily,
                            FontSize = PreviewText.FontSize,
                            FontWeight = PreviewText.FontWeight,
                            FontStyle = PreviewText.FontStyle,
                            Foreground = PreviewText.Foreground,
                            TextWrapping = TextWrapping.Wrap,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(10),
                            Background = Brushes.Transparent
                        },
                        new Image
                        {
                            Source = PreviewImage.Source,
                            Stretch = Stretch.Uniform,
                            Width = 150,
                            Height = 150,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(10)
                        }
                    }
                        }
                    };

                    Grid.SetColumn(renderBorder.Child, 0);
                    if (renderBorder.Child is Grid grid && grid.Children.Count > 1)
                    {
                        Grid.SetColumn(grid.Children[1], 1);
                    }

                    renderBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    renderBorder.Arrange(new Rect(renderBorder.DesiredSize));

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)renderBorder.ActualWidth,
                        (int)renderBorder.ActualHeight,
                        96d, 96d, PixelFormats.Pbgra32);
                    renderBitmap.Render(renderBorder);

                    BitmapEncoder encoder = saveFileDialog.FileName.EndsWith(".jpg")
                        ? new JpegBitmapEncoder()
                        : new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBox.Show("Meme saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    string base64String = ConvertRenderToBase64(renderBitmap);

                    // Сохраняем Base64-строку в базу данных
                    SaveMemeToDatabase(base64String);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving meme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
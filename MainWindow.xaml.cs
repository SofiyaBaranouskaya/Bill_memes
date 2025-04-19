using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Npgsql;

namespace Bill_memes
{
    public partial class MainWindow : Window
    {
        private (string text, string imagePath)[] memes;

        public MainWindow()
        {
            InitializeComponent();
            LoadMemes();
            LoadUserMemesFromDatabase();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.WorkArea.Left;
            this.Top = SystemParameters.WorkArea.Top;
            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
        }

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadUserMemesFromDatabase()
        {
            try
            {
                int userId = App.CurrentUserId;

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT image_base64
                FROM generated_memes
                WHERE user_id = @userId
            ";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string base64String = reader.GetString(0);
                                AddMemeToHistoryPanel(base64String);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user memes: {ex.Message}");
            }
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

        private string ConvertRenderTargetBitmapToBase64(RenderTargetBitmap bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return Convert.ToBase64String(stream.ToArray());
            }
        }


        private void SaveRenderedMemeToDatabase()
        {
            try
            {
                memeContainer.UpdateLayout();

                var renderBitmap = new RenderTargetBitmap(
                    (int)memeContainer.ActualWidth,
                    (int)memeContainer.ActualHeight,
                    96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(memeContainer);

                string base64String = ConvertRenderTargetBitmapToBase64(renderBitmap);
                SaveMemeToDatabase(base64String);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving rendered meme: {ex.Message}");
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

                    SaveRenderedMemeToDatabase();
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


        private void AddMemeToHistoryPanel(string base64String)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);

                BitmapImage bitmap = new BitmapImage();
                using (var ms = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }

                var image = new Image
                {
                    Source = bitmap,
                    MaxWidth = 220, // ✅ максимум по ширине, адаптивная высота
                    Stretch = Stretch.Uniform, // ✅ не обрезает, просто подгоняет по ширине
                    Margin = new Thickness(0, 5, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = image
                };

                HistoryPanel.Children.Insert(0, border); // последний мем наверх
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding meme to panel: {ex.Message}");
            }
        }

        private void SaveMemeToDatabase(string base64String)
        {
            try
            {
                int userId = App.CurrentUserId;

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO generated_memes (user_id, image_base64, is_liked, created_at)
                VALUES (@userId, @imageBase64, @isLiked, @createdAt)
                RETURNING id;
            ";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);
                        command.Parameters.AddWithValue("imageBase64", base64String);
                        command.Parameters.AddWithValue("isLiked", false);
                        command.Parameters.AddWithValue("createdAt", DateTime.Now);

                        var memeId = command.ExecuteScalar();

                        if (memeId != null)
                        {
                            string shareLink = $"meme://{memeId}";
                            Clipboard.SetText(shareLink);

                            // ✅ Добавим отображение в правой панели
                            AddMemeToHistoryPanel(base64String);

                            MessageBox.Show($"Meme saved! Share link copied to clipboard:\n{shareLink}", "Share Meme", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Error saving meme.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving meme to database: {ex.Message}");
            }
        }

        private void SaveAsImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                memeContainer.UpdateLayout();

                var canvas = new Canvas
                {
                    Width = memeContainer.ActualWidth,
                    Height = memeContainer.ActualHeight,
                    Background = Brushes.White
                };

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = new VisualBrush(memeContainer);
                    context.DrawRectangle(brush, null, new Rect(0, 0, memeContainer.ActualWidth, memeContainer.ActualHeight));
                }

                canvas.Children.Add(new DrawingVisualHost(visual));

                canvas.Measure(new Size(canvas.Width, canvas.Height));
                canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)canvas.Width,
                    (int)canvas.Height,
                    96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(canvas);

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

        private void Create_meme_Click(object sender, RoutedEventArgs e)
        {
            CreateMeme createMemeWindow = new CreateMeme();
            createMemeWindow.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Profile profileWindow = new Profile();
            profileWindow.Show();
            this.Close();
        }
    }
}

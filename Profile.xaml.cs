using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Windows.Controls;

namespace Bill_memes
{
    public partial class Profile : Window
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

        public Profile()
        {
            InitializeComponent();
            int userId = App.CurrentUserId;
            LoadUserImages(userId);
        }

        public class UserMeme
        {
            public BitmapImage Image { get; set; }
            public Guid Id { get; set; }
            public bool IsLiked { get; set; }
        }

        private void LoadUserImages(int userId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT id, image_base64, is_liked FROM memes WHERE user_id = @userId ORDER BY id DESC";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            var memes = new List<UserMeme>();

                            while (reader.Read())
                            {
                                Guid id = reader.GetGuid(0);
                                string base64 = reader.GetString(1);
                                bool isLiked = !reader.IsDBNull(2) && reader.GetBoolean(2);

                                var image = ConvertBase64ToBitmap(base64);
                                memes.Add(new UserMeme { Id = id, Image = image, IsLiked = isLiked });
                            }

                            ImageList.ItemsSource = memes;
                            ImageCountText.Text = $"Количество изображений: {memes.Count}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserMeme meme)
            {
                meme.IsLiked = !meme.IsLiked;

                try
                {
                    using (var connection = new NpgsqlConnection(ConnectionString))
                    {
                        connection.Open();

                        string updateQuery = "UPDATE memes SET is_liked = @isLiked WHERE id = @id";
                        using (var command = new NpgsqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@isLiked", meme.IsLiked);
                            command.Parameters.AddWithValue("@id", meme.Id);
                            command.ExecuteNonQuery();
                        }
                    }

                    // Обновляем текст кнопки
                    button.Content = meme.IsLiked ? "Убрать лайк" : "Лайк";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении лайка: {ex.Message}");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Source is BitmapImage bitmapImage)
            {
                FullScreenImage fullScreenImage = new FullScreenImage(bitmapImage);
                fullScreenImage.ShowDialog();
            }
        }

        private BitmapImage ConvertBase64ToBitmap(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using (var stream = new MemoryStream(bytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        private void OpenMemeFromLink_Click(object sender, RoutedEventArgs e)
        {
            string inputLink = Microsoft.VisualBasic.Interaction.InputBox(
                "Вставьте ссылку (meme://UUID):", "Открыть мем", "");

            if (inputLink.StartsWith("meme://"))
            {
                string uuid = inputLink.Replace("meme://", "").Trim();

                try
                {
                    using (var connection = new NpgsqlConnection(ConnectionString))
                    {
                        connection.Open();

                        string query = "SELECT image_base64 FROM memes WHERE id = @id";
                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", Guid.Parse(uuid));

                            var result = command.ExecuteScalar();

                            if (result != null)
                            {
                                string base64 = result.ToString();
                                var image = ConvertBase64ToBitmap(base64);

                                FullScreenImage fullImageWindow = new FullScreenImage(image);
                                fullImageWindow.ShowDialog();
                            }
                            else
                            {
                                MessageBox.Show("Мем не найден.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке мема: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Неверный формат ссылки.");
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Profile.UserMeme meme)
            {
                string shareLink = $"meme://{meme.Id}";
                Clipboard.SetText(shareLink);
                MessageBox.Show("Ссылка на мем скопирована в буфер обмена!", "Поделиться", MessageBoxButton.OK, MessageBoxImage.Information);
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

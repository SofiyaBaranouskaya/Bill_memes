using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Bill_memes
{
    public partial class Registration : Window
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

        public Registration()
        {
            InitializeComponent();
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

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            if (rbRegister.IsChecked == true)
            {
                RegisterUser(login, password);
            }
            else
            {
                LoginUser(login, password);
            }
        }

        private void RegisterUser(string login, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Проверка существования пользователя
                    using (var checkCmd = new NpgsqlCommand(
                        "SELECT COUNT(1) FROM users WHERE login = @login", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        long exists = (long)checkCmd.ExecuteScalar();

                        if (exists > 0)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует");
                            return;
                        }
                    }

                    // Регистрация нового пользователя и получение его ID
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO users (login, password, created_at) VALUES (@login, @password, CURRENT_TIMESTAMP) RETURNING id",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", HashPassword(password)); // Хеширование пароля

                        // Выполняем запрос и получаем ID нового пользователя
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int userId = Convert.ToInt32(result);

                            // Сохраняем ID в глобальное свойство
                            App.CurrentUserId = userId;

                            MessageBox.Show("Регистрация успешна!");
                            OpenMainWindow();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось получить ID нового пользователя.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        private void LoginUser(string login, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand(
                        "SELECT id FROM users WHERE login = @login AND password = @password",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", HashPassword(password));

                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int userId = Convert.ToInt32(result);

                            // Обновляем глобальный ID текущего пользователя
                            App.CurrentUserId = userId;

                            MessageBox.Show("Авторизация успешна!");
                            OpenMainWindow();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}");
            }
        }


        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private void OpenMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Bill_memes
{
    /// <summary>
    /// Логика взаимодействия для Admin.xaml
    /// </summary>
    public partial class Admin : Window
    {
        private string connectionString
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

        public Admin()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers(object sender, RoutedEventArgs e)
        {
            LoadUsers(); // Вызываем основной метод загрузки
        }


        private void LoadUsers()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    var adapter = new NpgsqlDataAdapter("SELECT id, login, created_at FROM users ORDER BY id", connection);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    UsersGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "INSERT INTO users (login, password) VALUES (@login, @password)",
                        connection);

                    command.Parameters.AddWithValue("@login", txtLogin.Text);
                    command.Parameters.AddWithValue("@password", txtPassword.Password);

                    command.ExecuteNonQuery();
                    MessageBox.Show("Пользователь добавлен");
                    LoadUsers();
                }
            }
            catch (NpgsqlException ex) when (ex.SqlState == "23505")
            {
                MessageBox.Show("Пользователь с таким логином уже существует");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления");
                return;
            }

            var selectedRow = (DataRowView)UsersGrid.SelectedItem;
            var userId = selectedRow["id"];

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand("DELETE FROM users WHERE id = @id", connection);
                    command.Parameters.AddWithValue("@id", userId);

                    command.ExecuteNonQuery();
                    MessageBox.Show("Пользователь удален");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }
    }
}

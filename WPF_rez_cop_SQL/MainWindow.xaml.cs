using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_rez_cop_SQL
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cmbAuthType.SelectedIndex = 0;
            if (pnlLogin != null && pnlPassword != null)
            {
                pnlLogin.Visibility = Visibility.Collapsed;
                pnlPassword.Visibility = Visibility.Collapsed;
            }

            AddLog("Приложение запущено");
            AddLog("Выберите тип аутентификации и введите параметры подключения");
        }
        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\n");
            txtLog.ScrollToEnd();
        }
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Выберите папку для сохранения резервной копии";
            dialog.FileName = "Выберите папку";
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.ValidateNames = false;

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                txtBackupPath.Text = folderPath;
                AddLog($"Выбрана папка: {folderPath}");
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void CmbAuthType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (pnlLogin == null || pnlPassword == null)
                return;

            if (cmbAuthType.SelectedIndex == 1)
            {
                pnlLogin.Visibility = Visibility.Visible;
                pnlPassword.Visibility = Visibility.Visible;
            }
            else
            {
                pnlLogin.Visibility = Visibility.Collapsed;
                pnlPassword.Visibility = Visibility.Collapsed;
            }
        }
        private string GetConnectionString()
        {
            string server = txtServer.Text.Trim();
            string database = txtDatabase.Text.Trim();

            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = server;
            builder.InitialCatalog = database;
            builder.ConnectTimeout = 30;

            if (cmbAuthType.SelectedIndex == 0)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = txtLogin.Text.Trim();
                builder.Password = txtPassword.Text;
                builder.IntegratedSecurity = false;
            }

            return builder.ConnectionString;
        }
        private void BtnBackup_Click(object sender, System.EventArgs e)
        {
            try
            {
                string server = txtServer.Text.Trim();
                string database = txtDatabase.Text.Trim();
                string backupType = ((System.Windows.Controls.ComboBoxItem)cmbBackupType.SelectedItem).Content.ToString();
                string backupPath = txtBackupPath.Text.Trim();
                string fileName = txtFileName.Text.Trim();

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database))
                {
                    MessageBox.Show("Заполните поля 'Сервер' и 'База данных'", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cmbAuthType.SelectedIndex == 1) // SQL Server
                {
                    string login = txtLogin.Text.Trim();
                    if (string.IsNullOrEmpty(login))
                    {
                        MessageBox.Show("Введите логин для SQL Server аутентификации", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                string fullPath = System.IO.Path.Combine(backupPath, fileName);

                if (!System.IO.Directory.Exists(backupPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(backupPath);
                        AddLog($"Создана папка: {backupPath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось создать папку: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                AddLog($"Начинается резервное копирование…");
                AddLog($"Сервер: {server}");
                AddLog($"База данных: {database}");
                AddLog($"Тип копии: {backupType}");
                AddLog($"Путь: {fullPath}");

                string sqlCommand = "";

                if (backupType.Contains("Полная"))
                {
                    sqlCommand = $"BACKUP DATABASE [{database}] TO DISK = '{fullPath}' WITH FORMAT, INIT, NAME = 'Full Backup of {database}'";
                }
                else if (backupType.Contains("Разностная"))
                {
                    sqlCommand = $"BACKUP DATABASE [{database}] TO DISK = '{fullPath}' WITH DIFFERENTIAL, FORMAT, INIT, NAME = 'Differential Backup of {database}'";
                }
                else if (backupType.Contains("Журнал"))
                {
                    sqlCommand = $"BACKUP LOG [{database}] TO DISK = '{fullPath}' WITH FORMAT, INIT, NAME = 'Transaction Log Backup of {database}'";
                }

                string connectionString = GetConnectionString();

                AddLog("Подключение к серверу...");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    AddLog("Подключение установлено");

                    using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                    {
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                AddLog("Резервное копирование успешно завершено!");
                AddLog($"Файл создан: {fullPath}");

                if (File.Exists(fullPath))
                {
                    FileInfo fileInfo = new FileInfo(fullPath);
                    double sizeMB = fileInfo.Length / (1024.0 * 1024.0);
                    AddLog($"Размер файла: {sizeMB:F2} МБ");
                }
                MessageBox.Show("Резервное копирование успешно завершено!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
            }

            catch (SqlException sqlEx)
            {
                AddLog($" Ошибка SQL Server: {sqlEx.Message}");
                MessageBox.Show($"Ошибка SQL Server: {sqlEx.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                AddLog($" Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            string server = txtServer.Text.Trim();

            if (string.IsNullOrEmpty(server))
            {
                MessageBox.Show("Введите имя сервера", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                string connectionString;

                if (cmbAuthType.SelectedIndex == 0) // Windows
                {
                    connectionString = $"Server={server};Trusted_Connection=True;Connection Timeout=5";
                }
                else // SQL Server
                {
                    string login = txtLogin.Text.Trim();
                    string password = txtPassword.Text;

                    if (string.IsNullOrEmpty(login))
                    {
                        MessageBox.Show("Введите логин для SQL Server аутентификации", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    connectionString = $"Server={server};User ID={login};Password={password};Connection Timeout = 5";
                }

                AddLog("Проверка подключения...");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    string query = "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int dbCount = 0;
                        while (reader.Read())
                        {
                            dbCount++;
                        }
                        AddLog($" Подключение успешно! Найдено баз данных: {dbCount}");

                        string database = txtDatabase.Text.Trim();
                        if (!string.IsNullOrEmpty(database))
                        {
                            connection.Close();
                            connection.Open();

                            string checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{database}'";
                            using (SqlCommand checkCommand = new SqlCommand(checkDbQuery, connection))
                            {
                                int exists = (int)checkCommand.ExecuteScalar();
                                if (exists > 0)
                                {
                                    AddLog($" База данных '{database}' найдена");
                                }
                                else
                                {
                                    AddLog($" База данных '{database}' не найдена");
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                AddLog($" Ошибка SQL Server: {sqlEx.Message}");
                MessageBox.Show($"Ошибка подключения: {sqlEx.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                AddLog($" Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public class AppSettings
        {
            public string ServerName { get; set; } = "localhost";
            public string LastDatabase { get; set; } = "";
            public string BackupPath { get; set; } = "C:\\Backups\\";
            public void Save()
            {
                try
                {
                    string json = JsonConvert.SerializeObject(this);
                    File.WriteAllText("settings.json", json);
                }
                catch
                {

                }
            }
            public static AppSettings Load()
            {
                try
                {
                    if (File.Exists("settings.json"))
                    {
                        string json = File.ReadAllText("settings.json");
                        return JsonConvert.DeserializeObject<AppSettings>(json);
                    }
                }
                catch
                {

                }
                return new AppSettings();
            }
        }
    }
}

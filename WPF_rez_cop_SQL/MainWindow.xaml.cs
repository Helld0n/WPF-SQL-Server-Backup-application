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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

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
            if (pnlLogin != null || pnlPassword != null)
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
    }
}

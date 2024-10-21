using System.Windows;
using System.Windows.Media;
using System.IO;
using System;

namespace WPPSDataAgent
{
    /// <summary>
    /// ConfigWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConfigWindow : Window
    {
        MainWindow main = (MainWindow)Application.Current.MainWindow;

        bool okCheck = false;

        string configFilePath = "./Config.ini";

        double windowRatio = 450.0d / 255.0d; // Window ratio

        double preWidth = 450.0d;
        double preHeight = 255.0d;

        bool isFirstSizeChange = true;

        public ConfigWindow()
        {
            InitializeComponent();

            string[] configFileValues = main.configManager.Initialize(configFilePath);

            dbServerTextBox.Text = configFileValues[(uint)Configuration.ConfigManager.ConfigValue.DBServer];
            dbPortTextBox.Text = configFileValues[(uint)Configuration.ConfigManager.ConfigValue.DBPort];
            dbNameTextBox.Text = configFileValues[(uint)Configuration.ConfigManager.ConfigValue.DBName];
            dbUserIDTextBox.Text = configFileValues[(uint)Configuration.ConfigManager.ConfigValue.UserID];
            timeoutTextBox.Text = configFileValues[(uint)Configuration.ConfigManager.ConfigValue.Timeout];

            uint directoryStartIndex = (uint)Configuration.ConfigManager.ConfigValue./**CLASSIFIED**/;

            try
            {
                // Create exception directory
                string path = main.exceptionDirectoryPath;

                DirectoryInfo directory = new DirectoryInfo(path);

                if (!directory.Exists)
                {
                    directory.Create();
                }

                for (int index = 0; index < Configuration.ConfigManager.directoryCount; index++)
                {
                    path = configFileValues[directoryStartIndex + index];

                    directory = new DirectoryInfo(path);

                    if (!directory.Exists)
                    {
                        directory.Create();
                    }

                    main.directoryPaths[index] = path;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Directory Exception: " + e, "Error");
            }

            // Add window loaded event
            this.Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void OKConfig(object sender, RoutedEventArgs e)
        {
            main.dbServer = dbServerTextBox.Text;
            main.dbPort = dbPortTextBox.Text;
            main.dbName = dbNameTextBox.Text;
            main.userID = dbUserIDTextBox.Text;
            main.password = dbPasswordBox.Password;

            main.commandTimeout = int.Parse(timeoutTextBox.Text);

            okCheck = true;

            GetWindow(this).Close();
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
            main.configManager.EditConfigFile(configFilePath, dbServerTextBox.Text, dbPortTextBox.Text,
                dbNameTextBox.Text, dbUserIDTextBox.Text, timeoutTextBox.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ChangeSize(this.ActualWidth, this.ActualHeight);
            }

            this.SizeChanged += new SizeChangedEventHandler(Window_Changed);
        }

        private void Window_Changed(object sender, SizeChangedEventArgs e)
        {
            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            ChangeSize(newWidth, newHeight);

            preWidth = newWidth;
            preHeight = newHeight;
        }

        private void ChangeSize(double width, double height)
        {
            // Window size difference after change window
            double diffWidth = width - preWidth;
            double diffHeight = height - preHeight;

            if (isFirstSizeChange)
            {
                diffWidth -= 5;
                diffHeight -= 20;
                isFirstSizeChange = false;
            }

            // Resize root grid
            if (grid.Width + diffWidth >= 0.0d) grid.Width += diffWidth;
            if (grid.Height + diffHeight >= 0.0d) grid.Height += diffHeight;

            // Move ok button
            okButton.Margin =
                new Thickness(okButton.Margin.Left + diffWidth, okButton.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Move save button
            saveButton.Margin =
                new Thickness(saveButton.Margin.Left + diffWidth, saveButton.Margin.Top + diffHeight, 0.0d, 0.0d);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (!okCheck) GetWindow(main).Close();
        }
    }
}

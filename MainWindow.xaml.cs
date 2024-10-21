using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;

namespace WPPSDataAgent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TCPClientSync wavePowerTCPClient = null;
        Database.DBConnector wavePowerDBConnector = null;

        public TCPServerSync queryExportTCPServer = null;
        Database.DBConnector queryExportDBConnector = null;

        Database.DBConnector fileImportDBConnector = null;

        public Mutex serverReceiveMutex = null;
        public Mutex clientReceiveMutex = null;

        public Mutex dataReaderMutex = null;

        #region Boolean Values
        // Comm. Import boolean with lock
        readonly object clientConnectedLocker = new object();
        readonly object commImportDBConnectedLocker = new object();

        private bool _isClientConnected = false;
        private bool _isCommImportDBConnected = false;

        bool isClientRunning = false;

        public bool isClientConnected
        {
            get { return _isClientConnected; }
            set { lock (clientConnectedLocker) { _isClientConnected = value; } }
        }

        public bool isCommImportDBConnected
        {
            get { return _isCommImportDBConnected; }
            set { lock (commImportDBConnectedLocker) { _isCommImportDBConnected = value; } }
        }

        // Query Export boolean with lock
        readonly object serverListeningLocker = new object();
        readonly object serverAcceptedLocker = new object();
        readonly object queryExportDBConnectedLocker = new object();

        private bool _isServerListening = false;
        private bool _isServerAccepted = false;
        private bool _isQueryExportDBConnected = false;

        bool isServerRunning = false;

        public bool isServerListening
        {
            get { return _isServerListening; }
            set { lock (serverListeningLocker) { _isServerListening = value; } }
        }

        public bool isServerAccepted
        {
            get { return _isServerAccepted; }
            set { lock (serverAcceptedLocker) { _isServerAccepted = value; } }
        }

        public bool isQueryExportDBConnected
        {
            get { return _isQueryExportDBConnected; }
            set { lock (queryExportDBConnectedLocker) { _isQueryExportDBConnected = value; } }
        }

        // File Import boolean with lock
        readonly object fileImportLocker = new object();
        readonly object fileImportDBConnectedLocker = new object();

        private bool _isFileImporting = false;
        private bool _isFileImportDBConnected = false;

        public bool isFileImporting
        {
            get { return _isFileImporting; }
            set { lock (fileImportLocker) { _isFileImporting = value; } }
        }

        public bool isFileImportDBConnected
        {
            get { return _isFileImportDBConnected; }
            set { lock (fileImportDBConnectedLocker) { _isFileImportDBConnected = value; } }
        }
        #endregion

        // DB Connect
        public string dbServer = string.Empty;
        public string dbPort = string.Empty;
        public string dbName = string.Empty;
        public string userID = string.Empty;
        public string password = string.Empty;

        // File path for importing 
        string selectedFilePath = string.Empty;

        int maxLogLength = 100000;

        FileStream readFileStream, queryFileStream, queryDataFileStream;//, commFileStream, , commDataFileStream, ;
        StreamWriter readFileStreamWriter, queryStreamWriter, queryDataStreamWriter;//, commStreamWriter, , commDataStreamWriter, ;

        const float initProgressValue = 0.0f;

        public int commandTimeout = 3600;

        public Configuration.ConfigManager configManager = null;

        // Directory paths used in file import
        public string[] directoryPaths = new string[Configuration.ConfigManager.directoryCount];

        public string exceptionDirectoryPath = "/**CLASSIFIED**/";

        Thread fileImportThread = null;
        Thread remainingFileCountThread = null;

        private string previousHourForLog = DateTime.Now.ToString("HH");

        private string previousHourForInsert = DateTime.Now.ToString("HH");
        private string previousDayForInsert = DateTime.Now.ToString("dd");
        private string previousWeekForInsert = "00";
        private string previousMonthForInsert = DateTime.Now.ToString("MM");

        private bool isProgramRunning = true;

        DispatcherTimer insertStatisticsDataTimer = null;

        System.Globalization.Calendar calendar = null;
        System.Globalization.CalendarWeekRule calendarWeekRule;
        DayOfWeek firstDayOfWeek;

        public enum DirectoryIndex
        {
            /**CLASSIFIED**/
            ValueCount = 14
        }

        public enum Tab
        {
            Comm = 0,
            FileImport = 1,
            QueryExport = 2,
            TabCount = 3
        }

        public static MainWindow main;

        double windowRatio = 1280.0d / 700.0d; // Window(Grid) ratio

        double preWidth = 1280.0d;
        double preHeight = 720.0d;

        bool isFirstSizeChange = true;

        public MainWindow()
        {
            InitializeComponent();

            main = this;

            isProgramRunning = true;

            InitLogFileStreams();

            AddLog((uint)Tab.Comm, "The program starts");
            AddLog((uint)Tab.FileImport, "The program starts");
            AddLog((uint)Tab.QueryExport, "The program starts");

            configManager = new Configuration.ConfigManager();

            ConfigWindow config = new ConfigWindow();
            config.ShowDialog();

            fileImportProgress.Value = initProgressValue;

            serverReceiveMutex = new Mutex();
            clientReceiveMutex = new Mutex();

            dataReaderMutex = new Mutex();

            remainingFileCountThread = new Thread(CountRemainingFiles);
            remainingFileCountThread.Start();

            StartLogFileCommit();
            SetPathName(directoryPaths);

            // Calculate week of year
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");
            calendar = cultureInfo.Calendar;

            calendarWeekRule = cultureInfo.DateTimeFormat.CalendarWeekRule;
            firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;

            DateTime requestDate = DateTime.Now;

            previousWeekForInsert = calendar.GetWeekOfYear(requestDate, calendarWeekRule, firstDayOfWeek).ToString("00");

            // Add window loaded event
            this.Loaded += new RoutedEventHandler(Window_Loaded);
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
                diffHeight -= 20;
                isFirstSizeChange = false;
            }

            // Resize root grid
            if (grid.Width + diffWidth >= 0.0d) grid.Width += diffWidth;
            if (grid.Height + diffHeight >= 0.0d) grid.Height += diffHeight;

            // Resize file import grid
            if (fileReadGrid.Width + diffWidth >= 0.0d) fileReadGrid.Width += diffWidth;
            if (fileReadGrid.Height + diffHeight >= 0.0d) fileReadGrid.Height += diffHeight;

            // Resize query export grid
            if (exportGrid.Width + diffWidth >= 0.0d) exportGrid.Width += diffWidth;
            if (exportGrid.Height + diffHeight >= 0.0d) exportGrid.Height += diffHeight;

            #region File Import Tab
            // Move start batch import button
            fileImportStartButton.Margin =
                new Thickness(fileImportStartButton.Margin.Left + diffWidth, fileImportStartButton.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Move importing file label
            importingFileLabel.Margin =
                new Thickness(importingFileLabel.Margin.Left, importingFileLabel.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Move processing label
            processingLabel.Margin =
                new Thickness(processingLabel.Margin.Left, processingLabel.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Move remaining input files label
            importFileLabel.Margin =
                new Thickness(importFileLabel.Margin.Left + diffWidth, importFileLabel.Margin.Top, 0.0d, 0.0d);

            // Move /**CLASSIFIED**/ data name label
            /**CLASSIFIED**/

            // Move progress bar prop label
            progressProp.Margin =
                new Thickness(progressProp.Margin.Left + (diffWidth / 2), progressProp.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Resize log text box
            if (fileImportLogTextBlock.Width + diffWidth >= 0.0d) fileImportLogTextBlock.Width += diffWidth;
            if (fileImportLogTextBlock.Height + diffHeight >= 0.0d) fileImportLogTextBlock.Height += diffHeight;

            // Resize and move progress bar
            if (fileImportProgress.Width + diffWidth >= 0.0d) fileImportProgress.Width += diffWidth;
            fileImportProgress.Margin =
                new Thickness(fileImportProgress.Margin.Left, fileImportProgress.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Resize and move importing file name text box
            if (importingFileTextBox.Width + diffWidth >= 0.0d) importingFileTextBox.Width += diffWidth;
            importingFileTextBox.Margin =
                new Thickness(fileImportProgress.Margin.Left, importingFileTextBox.Margin.Top + diffHeight, 0.0d, 0.0d);
            #endregion

            #region Query Export Tab
            // Move start button
            exportStartButton.Margin =
                new Thickness(exportStartButton.Margin.Left + diffWidth, exportStartButton.Margin.Top + diffHeight, 0.0d, 0.0d);

            // Resize log text box
            if (exportLogTextBlock.Width + diffWidth >= 0.0d) exportLogTextBlock.Width += diffWidth;
            if (exportLogTextBlock.Height + (diffHeight / 2) >= 0.0d) exportLogTextBlock.Height += diffHeight / 2;

            // Resize and move query log text box
            if (queryLogTextBlock.Width + diffWidth >= 0.0d) queryLogTextBlock.Width += diffWidth;
            if (queryLogTextBlock.Height + (diffHeight / 2) >= 0.0d) queryLogTextBlock.Height += diffHeight / 2;
            queryLogTextBlock.Margin =
                new Thickness(queryLogTextBlock.Margin.Left, queryLogTextBlock.Margin.Top + (diffHeight / 2), 0.0d, 0.0d);

            // Move query result log label
            queryDataLabel.Margin =
                new Thickness(queryDataLabel.Margin.Left, queryDataLabel.Margin.Top + +(diffHeight / 2), 0.0d, 0.0d);

            // Move query request label
            clientLabel.Margin =
                new Thickness(clientLabel.Margin.Left + diffWidth, clientLabel.Margin.Top, 0.0d, 0.0d);

            // Move listen port label
            serverPortLabel.Margin =
                new Thickness(serverPortLabel.Margin.Left + diffWidth, serverPortLabel.Margin.Top, 0.0d, 0.0d);

            // Move input listen port text box
            serverPortTextBox.Margin =
                new Thickness(serverPortTextBox.Margin.Left + diffWidth, serverPortTextBox.Margin.Top, 0.0d, 0.0d);
            #endregion
        }

        public void SetTextBoxReadOnly(bool flag)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                serverPortTextBox.IsReadOnly = flag;
            }));
        }

        private void SetPathName(string[] directories)
        {
            const int directoryJump = 2;
            const int maxLength = 30;

            for (int index = 0; index < Configuration.ConfigManager.directoryCount; index += directoryJump)
            {
                if (index == 0) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 2) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 4) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 6) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 8) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 10) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
                else if (index == 12) /**CLASSIFIED**/.Content = CutPath(directoryPaths[index], maxLength);
            }
        }

        private string CutPath(string str, int length)
        {
            if (str.Length > length)
            {
                return "Path: ..." + str.Substring(str.Length - length, length);
            }
            else
            {
                return "Path: " + str;
            }
        }

        public void SetTargetFile(string fileName)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                importingFileTextBox.Text = fileName;
            }));
        }

        private void InitLogFileStreams()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss ");

            readFileStream = new FileStream("./" + timestamp + "FileImportLog.txt", FileMode.Append, FileAccess.Write);
            //commFileStream = new FileStream("./" + timestamp + "CommLog.txt", FileMode.Append, FileAccess.Write);
            queryFileStream = new FileStream("./" + timestamp + "QueryExportLog.txt", FileMode.Append, FileAccess.Write);
            //commDataFileStream = new FileStream("./" + timestamp + "CommDataLog.csv", FileMode.Append, FileAccess.Write);
            queryDataFileStream = new FileStream("./" + timestamp + "QueryDataLog.csv", FileMode.Append, FileAccess.Write);

            readFileStreamWriter = new StreamWriter(readFileStream);
            //commStreamWriter = new StreamWriter(commFileStream);
            queryStreamWriter = new StreamWriter(queryFileStream);
            //commDataStreamWriter = new StreamWriter(commDataFileStream);
            queryDataStreamWriter = new StreamWriter(queryDataFileStream);
        }

        private void LogFileCommit(object sender, EventArgs e)
        {
            if (previousHourForLog != DateTime.Now.ToString("HH"))
            {
                CloseStream(readFileStreamWriter, readFileStream);
                CloseStream(queryStreamWriter, queryFileStream);
                CloseStream(queryDataStreamWriter, queryDataFileStream);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss ");

                readFileStream = new FileStream("./" + timestamp + "FileImportLog.txt", FileMode.Append, FileAccess.Write);
                readFileStreamWriter = new StreamWriter(readFileStream);

                queryFileStream = new FileStream("./" + timestamp + "QueryExportLog.txt", FileMode.Append, FileAccess.Write);
                queryStreamWriter = new StreamWriter(queryFileStream);

                queryDataFileStream = new FileStream("./" + timestamp + "QueryDataLog.csv", FileMode.Append, FileAccess.Write);
                queryDataStreamWriter = new StreamWriter(queryDataFileStream);

                previousHourForLog = DateTime.Now.ToString("HH");
            }
        }

        private void StartLogFileCommit()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromTicks(10000000); // 1sec
            timer.Tick += new EventHandler(LogFileCommit);
            timer.Start();
        }

        // Event for insert statistics data
        private void InsertStatisticsData(object sender, EventArgs e)
        {
            string year = DateTime.Now.ToString("yyyy");
            string month = DateTime.Now.ToString("MM");

            string day = DateTime.Now.ToString("dd");
            string hour = DateTime.Now.ToString("HH");

            DateTime requestDate = DateTime.Parse(year + "-" + month + "-" + day);

            string week = calendar.GetWeekOfYear(requestDate, calendarWeekRule, firstDayOfWeek).ToString("00");

            // Synchronizing a DataReader
            // Wait
            if (dataReaderMutex != null) dataReaderMutex.WaitOne();

            if (previousHourForInsert != hour)
            {
                if (queryExportTCPServer.dataDecoder != null)
                {
                    /**CLASSIFIED**/
                    previousHourForInsert = hour;
                }
            }

            if (previousDayForInsert != day)
            {
                if (queryExportTCPServer.dataDecoder != null)
                {
                    /**CLASSIFIED**/
                    previousDayForInsert = day;
                }
            }

            if (previousWeekForInsert != week)
            {
                if (queryExportTCPServer.dataDecoder != null)
                {
                    /**CLASSIFIED**/
                    previousWeekForInsert = week;
                }
            }

            if (previousMonthForInsert != month)
            {
                if (queryExportTCPServer.dataDecoder != null)
                {
                    /**CLASSIFIED**/
                    previousMonthForInsert = month;
                }
            }

            // Synchronizing a DataReader
            // Release
            if (dataReaderMutex != null) dataReaderMutex.ReleaseMutex();
        }



        /**CLASSIFIED**/



        // If need test, change event
        private void StartInsertStatisticsData(long interval)
        {
            insertStatisticsDataTimer = new DispatcherTimer();
            insertStatisticsDataTimer.Interval = TimeSpan.FromTicks(interval);
            insertStatisticsDataTimer.Tick += new EventHandler(InsertStatisticsData);
            insertStatisticsDataTimer.Start();
        }

        private void StopInsertStatisticsData()
        {
            insertStatisticsDataTimer.Stop();
        }

        private void StartConnect(object sender, RoutedEventArgs e)
        {
            if (!isClientRunning)
            {
                isClientRunning = !isClientRunning;

                // Connect to server (TCP/IP)
                string inputIPAddress = ipAddressTextBox.GetLineText(0);
                int inputPort = int.Parse(clientPortTextBox.GetLineText(0));

                // Connect to DB
                /**CLASSIFIED**/

                if (isCommImportDBConnected)
                {
                    /**CLASSIFIED**/
                }

                if (!isClientConnected || !isCommImportDBConnected)
                {
                    AddLog((uint)Tab.Comm, "[TCP Client] Connection is falied");

                    StopConnect();
                }
            }
            else
            {
                if (/**CLASSIFIED**/ != null)
                {
                    clientReceiveMutex.WaitOne();

                    StopConnect();

                    clientReceiveMutex.ReleaseMutex();
                }
            }
        }

        private void StopConnect()
        {
            isClientRunning = !isClientRunning;

            /**CLASSIFIED**/

            insertStartButton.Content = "Start";
        }

        private void StartSelect(object sender, RoutedEventArgs e)
        {
            if (!isServerRunning) // Start Server
            {
                isServerRunning = !isServerRunning;

                int inputPort = int.Parse(serverPortTextBox.GetLineText(0));

                queryExportDBConnector = new Database.DBConnector();

                queryExportTCPServer = new QueryDataCommunicatorSync(inputPort,
                    new Decoder.QueryDataDecoder(new DataContainer.QueryDataContainer()),
                    queryExportDBConnector);

                if (queryExportTCPServer != null)
                {
                    queryExportTCPServer.StartServer();

                    serverPortTextBox.IsReadOnly = true;

                    const long TIMEINTERVAL = 6000000000; // 600sec

                    StartInsertStatisticsData(TIMEINTERVAL);
                }

                AddLog((uint)Tab.QueryExport, "[TCP Server] Start to listen");

                exportStartButton.Content = "Stop";
            }
            else // Stop
            {
                if (queryExportTCPServer != null)
                {
                    serverReceiveMutex.WaitOne();

                    isServerRunning = !isServerRunning;

                    queryExportTCPServer.CloseSocket();

                    //StopInsertStatisticsData();

                    if (queryExportDBConnector != null)
                    {
                        queryExportDBConnector.DisconnectFromDB(queryExportDBConnector.dbConnection, (uint)Tab.QueryExport);
                    }

                    exportStartButton.Content = "Start";

                    serverReceiveMutex.ReleaseMutex();
                }
            }
        }

        public bool SetDBConnector(Database.DBConnector connector, TCPServerSync server)
        {
            if (connector != null)
            {
                connector.ConnectToDB(dbServer,
                        dbPort,
                        dbName,
                        userID,
                        password);

                server.dataDecoder.SetDBConnector(connector);

                return connector.dbConnection.State == System.Data.ConnectionState.Open;
            }

            return false;
        }

        private void CountRemainingFiles()
        {
            int directoryJump = 2;

            while (isProgramRunning)
            {
                for (int index = 0; index < Configuration.ConfigManager.directoryCount; index += directoryJump)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(directoryPaths[index]);

                    int count = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).Length;

                    ChangeContent(index, count);
                }

                Thread.Sleep(100);
            }
        }

        private void ChangeContent(int index, int count)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (index == 0) /**CLASSIFIED**/.Content = count;
                else if (index == 2) /**CLASSIFIED**/.Content = count;
                else if (index == 4) /**CLASSIFIED**/.Content = count;
                else if (index == 6) /**CLASSIFIED**/.Content = count;
                else if (index == 8) /**CLASSIFIED**/.Content = count;
                else if (index == 10) /**CLASSIFIED**/.Content = count;
                else if (index == 12) /**CLASSIFIED**/.Content = count;
            }));
        }

        private void StartBatchFileImport(object sender, RoutedEventArgs e)
        {
            if (!isFileImporting && !isFileImportDBConnected)
            {
                isFileImporting = true;

                // Connect to DB
                fileImportDBConnector = new Database.DBConnector(new DataImporter.ReadFileDataImporter());
                isFileImportDBConnected = fileImportDBConnector.ConnectToDBToInsert(dbServer, dbPort,
                                dbName, userID, password, directoryPaths);

                fileImportThread = new Thread(StartImportFile);
                fileImportThread.Start();

                fileImportStartButton.Content = "Stop Batch Import";

                //fileReadStartButton.IsEnabled = false;
                //fileOpenButton.IsEnabled = false;
            }
            else
            {
                isFileImporting = false;

                if (fileImportThread != null) fileImportThread.Join();

                EndFileImporting();
            }
        }

        private void StartImportFile()
        {
            while (isFileImporting)
            {
                fileImportDBConnector.dataImporter.InsertData(fileImportDBConnector.dbConnection, directoryPaths);
            }
        }

        public void EndFileImporting()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                fileImportDBConnector.DisconnectFromDB(fileImportDBConnector.dbConnection, (uint)Tab.FileImport);
                fileImportStartButton.IsEnabled = true;
                fileImportStartButton.Content = "Start Batch Import";

                //fileOpenButton.IsEnabled = true;
            }));

            isFileImportDBConnected = false;
        }

        private void StartFileOpen(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".csv"; // Default file extension
            dialog.Filter = "CSV documents|*.csv"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open file
                selectedFilePath = dialog.FileName;

                string fileName = System.IO.Path.GetFileName(selectedFilePath);

                fileImportStartButton.IsEnabled = true;

                //selectedFileTextBox.Text = fileName;
            }
        }

        #region Log
        public void AddLog(uint tab, string log)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff  ");

            string logText = timestamp + log + "\n";

            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (tab == (uint)Tab.Comm)
                {
                    if (logTextBlock.Text.Length > maxLogLength)
                    {
                        logTextBlock.Text = RemoveLine(logTextBlock.Text);
                    }

                    //commStreamWriter.Write(logText);
                    logTextBlock.Text += logText;

                    logTextBlock.ScrollToEnd();
                }
                else if (tab == (uint)Tab.FileImport)
                {
                    if (fileImportLogTextBlock.Text.Length > maxLogLength)
                    {
                        fileImportLogTextBlock.Text = RemoveLine(fileImportLogTextBlock.Text);
                    }

                    readFileStreamWriter.Write(logText);
                    fileImportLogTextBlock.Text += logText;

                    fileImportLogTextBlock.ScrollToEnd();
                }
                else if (tab == (uint)Tab.QueryExport)
                {
                    if (exportLogTextBlock.Text.Length > maxLogLength)
                    {
                        exportLogTextBlock.Text = RemoveLine(exportLogTextBlock.Text);
                    }

                    queryStreamWriter.Write(logText);
                    exportLogTextBlock.Text += logText;

                    exportLogTextBlock.ScrollToEnd();
                }
            }));
        }

        public void AddDataLog(uint tab, string log)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff  ");

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string time = "'" + DateTime.Now.ToString("HH:mm:ss.fff");

            string logText = timestamp + log + "\n";

            string fileLogText = $"{date},{time}," + log;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (tab == (uint)Tab.Comm)
                {
                    if (dataLogTextBlock.Text.Length > maxLogLength)
                    {
                        dataLogTextBlock.Text = RemoveLine(dataLogTextBlock.Text);
                    }

                    //commDataStreamWriter.WriteLine(fileLogText);
                    dataLogTextBlock.Text += logText;

                    dataLogTextBlock.ScrollToEnd();
                }
                else if (tab == (uint)Tab.QueryExport)
                {
                    if (queryLogTextBlock.Text.Length > maxLogLength)
                    {
                        queryLogTextBlock.Text = RemoveLine(queryLogTextBlock.Text);
                    }

                    queryDataStreamWriter.WriteLine(fileLogText);
                    queryLogTextBlock.Text += logText;

                    queryLogTextBlock.ScrollToEnd();
                }
            }));
        }
        #endregion

        private void CloseStream(StreamWriter streamWriter, FileStream fileStream)
        {
            if (streamWriter != null) streamWriter.Close();
            if (fileStream != null) fileStream.Close();
        }

        private string RemoveLine(string text)
        {
            string result = text.Substring(maxLogLength,
                text.Length - maxLogLength);

            return result;
        }

        public void UpdateProgress(float value)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                fileImportProgress.Value = value;
            }));
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            isProgramRunning = false;

            CloseStream(readFileStreamWriter, readFileStream);
            //CloseStream(commStreamWriter, commFileStream);
            //CloseStream(commDataStreamWriter, commDataFileStream);
            CloseStream(queryStreamWriter, queryFileStream);
            CloseStream(queryDataStreamWriter, queryDataFileStream);
        }
    }
}

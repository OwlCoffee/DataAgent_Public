using System;
using System.Windows;
using System.Threading;
using MySql.Data.MySqlClient;

namespace WPPSDataAgent.Database
{
    public class DBConnector
    {
        public MySqlConnection dbConnection = null;

        public DataImporter.DataImporter dataImporter = null;

        public Thread dataInsertThread = null;

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        public DBConnector(DataImporter.DataImporter importer)
        {
            dataImporter = importer;
        }

        public DBConnector()
        {
        }

        public void BeginInsert(Decoder.DataDecoder decoder)
        {
            // Begin insert
            dataInsertThread = new Thread(() => dataImporter.InsertData(dbConnection, decoder));
            dataInsertThread.Start();

            main.AddLog((uint)MainWindow.Tab.Comm, "[DB] Start to insert data");
        }

        // Connect to MariaDB for Comm. Import
        public bool ConnectToDBToInsert(string server, string port, string db, string id, string pw)
        {
            string connectionCommand = $"Server={server}; Port={port}; Database={db}; user id={id}; password={pw}";

            try
            {
                dbConnection = new MySqlConnection(connectionCommand);

                if (dbConnection != null)
                {
                    dbConnection.Open(); // MariaDB connection open

                    main.isCommImportDBConnected = true;

                    main.AddLog((uint)MainWindow.Tab.Comm, "[DB] Connected to DB " + db
                        + $"\n\t\t\tDB Server: {server}  DB Port: {port}"
                        + $"\n\t\t\tDB User ID: {id}");

                    return true;
                }
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[DB] Warning Level 2! MySql Exception: " + ex);
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[DB] Warning Level 3! Exception: " + ex);
            }

            if (dataInsertThread != null)
            {
                if (dataInsertThread.IsAlive) dataInsertThread.Join();
            }

            DisconnectFromDB(dbConnection, (uint)MainWindow.Tab.Comm);

            return false;
        }

        // Connect to MariaDB for File Read
        public bool ConnectToDBToInsert(string server, string port, string db, string id, string pw, string[] directoryPaths)
        {
            string connectionCommand = $"Server={server}; Port={port}; Database={db}; user id={id}; password={pw}";

            try
            {
                dbConnection = new MySqlConnection(connectionCommand);

                if (dbConnection != null)
                {
                    dbConnection.Open(); // MariaDB connection open

                    main.AddLog((uint)MainWindow.Tab.FileImport, "[DB] Connected to DB " + db
                        + $"\n\t\t\tDB Server: {server}  DB Port: {port}"
                        + $"\n\t\t\tDB User ID: {id}");

                    main.AddLog((uint)MainWindow.Tab.FileImport, "[DB] Start to insert data");

                    return true;
                }
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.FileImport, "[DB] Warning Level 2! MySql Exception: " + ex);
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.FileImport, "[DB] Warning Level 3! Exception: " + ex);
            }

            if (dataInsertThread != null)
            {
                if (dataInsertThread.IsAlive) dataInsertThread.Join();
            }

            DisconnectFromDB(dbConnection, (uint)MainWindow.Tab.FileImport);

            return false;
        }

        // Connect to MariaDB for Query Export
        public bool ConnectToDB(string server, string port, string db, string id, string pw)
        {
            string connectionCommand = $"Server={server}; Port={port}; Database={db}; user id={id}; password={pw}";

            try
            {
                dbConnection = new MySqlConnection(connectionCommand);

                if (dbConnection != null)
                {
                    dbConnection.Open(); // MariaDB connection open

                    main.isQueryExportDBConnected = true;

                    main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Connected to DB " + db
                        + $"\n\t\t\tDB Server: {server}  DB Port: {port}"
                        + $"\n\t\t\tDB User ID: {id}");

                    return true;
                }
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);
            }

            DisconnectFromDB(dbConnection, (uint)MainWindow.Tab.QueryExport);

            return false;
        }

        public void DisconnectFromDB(MySqlConnection connection, uint tab)
        {
            if (connection != null)
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    if (tab == (uint)MainWindow.Tab.Comm)
                    {
                        main.isCommImportDBConnected = false;
                    }
                    else if (tab == (uint)MainWindow.Tab.FileImport)
                    {
                        main.isFileImportDBConnected = false;
                    }
                    else if (tab == (uint)MainWindow.Tab.QueryExport)
                    {
                        main.isQueryExportDBConnected = false;
                    }

                    if (dataInsertThread != null)
                    {
                        if (dataInsertThread.IsAlive) dataInsertThread.Join();
                    }

                    connection.Close();

                    main.AddLog(tab, "[DB] Disconnected from DB");
                }
            }
        }
    }
}
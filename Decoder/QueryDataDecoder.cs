using System;
using System.Text;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace WPPSDataAgent.Decoder
{
    public class QueryDataDecoder : DataDecoder
    {
        public QueryDataDecoder(DataContainer.DataContainer container) : base(container) { }

        Database.DBConnector dbConnector = null;

        private string latestHourTableYear = "2000";
        private string latestHourTableMonth = "00";

        private string latestDayTableYear = "2000";
        private string latestDayTableMonth = "00";

        private string latestWeekTableYear = "2000";
        private string latestWeekTableMonth = "00";

        private string latestMonthTableYear = "2000";
        private string latestMonthTableMonth = "00";

        private string latestPrimaryTableYear = "2000";
        private string latestPrimaryTableMonth = "00";

        enum QueryData
        {
            QueryNumber = 0,
            Year = 1,
            Month = 2,
            Day = 3,
            Hour = 4,
            QueryDataCount = 5
        }

        enum CreateQuery
        {
            HourTable = 0,
            DayTable = 1,
            WeekTable = 2,
            MonthTable = 3,
            PrimaryTable = 4,
            TableCount = 5
        }

        public enum SelectQuery
        {
            /**CLASSIFIED**/
        }

        public enum InsertQuery
        {
            /**CLASSIFIED**/
        }

        enum ErrorData
        {
            ErrorCode = 0,
            ErrorMessage = 1,
            Count = 2
        }

        enum ErrorCode
        {
            Timeout = 1,
            MySql = 2,
            Exception = 3,
        }

        enum WaterPressureData
        {
            /**CLASSIFIED**/
        }

        enum PreviousWaterPressureData
        {
            /**CLASSIFIED**/
        }

        string[] insertTableName = new string[] {
            /**CLASSIFIED**/
        };

        string[] createTableName = new string[] {
            /**CLASSIFIED**/
        };

        protected MySqlCommand command = null;

        protected MySqlDataReader dataReader = null;

        public override int Decode(byte[] receivedData)
        {
            mergeString += Encoding.ASCII.GetString(receivedData, 0, receivedData.Length).Trim('\0');

            if (CountNewLine(mergeString) >= 1)
            {
                string[] s_received = SubString(mergeString).Split('\n');

                for (int i = 0; i < s_received.Length; i++)
                {
                    if (s_received[i] != string.Empty)
                    {
                        // Synchronizing a DataReader
                        // Wait
                        if (main.dataReaderMutex != null) main.dataReaderMutex.WaitOne();

                        int resultDataCount = SelectQueryData(dbConnector.dbConnection, s_received[i]);

                        // Synchronizing a DataReader
                        // Release
                        if (main.dataReaderMutex != null) main.dataReaderMutex.ReleaseMutex();

                        return resultDataCount;
                    }
                }
            }

            return 0;
        }

        public override bool CreateStatisticsTable(uint queryNumber, string year, string month)
        {
            string createQuery = SetCreateQuery(queryNumber, year, month);

            MySqlDataReader checkDataReader = null;

            string checkQuery = string.Empty;

            if (queryNumber == (uint)CreateQuery.HourTable)
                checkQuery = $"SELECT * FROM stat_hour_{year}_{month}";
            else if (queryNumber == (uint)CreateQuery.DayTable)
                checkQuery = $"SELECT * FROM stat_day_{year}_{month}";
            else if (queryNumber == (uint)CreateQuery.WeekTable)
                checkQuery = $"SELECT * FROM stat_week_{year}_{month}";
            else if (queryNumber == (uint)CreateQuery.MonthTable)
                checkQuery = $"SELECT * FROM stat_month_{year}_{month}";
            else if (queryNumber == (uint)CreateQuery./**CLASSIFIED**/)
                checkQuery = $"/**CLASSIFIED**/";

            try
            {
                if (dbConnector == null) return false;
                if (createQuery == null || createQuery == string.Empty) return false;

                if (!WaitingDataReaderClose(checkDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (checkDataReader != null) if (!checkDataReader.IsClosed) checkDataReader.Close();

                MySqlCommand createCommand = new MySqlCommand(createQuery, dbConnector.dbConnection);
                createCommand.CommandTimeout = main.commandTimeout;
                createCommand.ExecuteNonQuery();

                MySqlCommand readCommand = new MySqlCommand(checkQuery, dbConnector.dbConnection);
                readCommand.CommandTimeout = main.commandTimeout;
                checkDataReader = readCommand.ExecuteReader();

                if (!checkDataReader.Read())
                {
                    if (!WaitingDataReaderClose(checkDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                    //if (checkDataReader != null) if (!checkDataReader.IsClosed) checkDataReader.Close();

                    if (!FillZeroData(queryNumber, year, month)) return false;
                }

                if (!WaitingDataReaderClose(checkDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (checkDataReader != null) if (!checkDataReader.IsClosed) checkDataReader.Close();

                main.AddLog((uint)MainWindow.Tab.QueryExport,
                    $"[DB] Created {createTableName[queryNumber]}_{year}_{month} table");

                return true;
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                if (!WaitingDataReaderClose(checkDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (checkDataReader != null) if (!checkDataReader.IsClosed) checkDataReader.Close();

                return false;
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                if (!WaitingDataReaderClose(checkDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (checkDataReader != null) if (!checkDataReader.IsClosed) checkDataReader.Close();

                return false;
            }
        }

        protected bool FillZeroData(uint queryNumber, string year, string month)
        {
            int[] dayOfMonth = null;

            string insertZeroQuery = string.Empty;

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            MySqlCommand insertCommand = null;

            if (LeapYearCheck(int.Parse(year))) dayOfMonth = leapYear;
            else dayOfMonth = normalYear;

            if (queryNumber == (uint)CreateQuery.HourTable)
            {
                for (int day = 1; day <= dayOfMonth[int.Parse(month) - 1]; day++)
                {
                    for (int hour = 0; hour < 24; hour++)
                    {
                        string date = $"{year}-{month}-{day.ToString("00")}";
                        string fHour = hour.ToString("00");

                        insertZeroQuery = $"INSERT INTO stat_hour_{year}_{month} "
                            + $"VALUES('{date}', '{fHour}', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', "
                            + $"'0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '{dateTime}')";

                        insertCommand = new MySqlCommand(insertZeroQuery, dbConnector.dbConnection);
                        insertCommand.CommandTimeout = main.commandTimeout;
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            else if (queryNumber == (uint)CreateQuery.DayTable)
            {
                for (int day = 1; day <= dayOfMonth[int.Parse(month) - 1]; day++)
                {
                    string date = $"{year}-{month}-{day.ToString("00")}";

                    insertZeroQuery = $"INSERT INTO stat_day_{year}_{month} "
                        + $"VALUES('{date}', '0', '0', '0', '0', '0', '0', '0', '0', '0', '{dateTime}')";

                    insertCommand = new MySqlCommand(insertZeroQuery, dbConnector.dbConnection);
                    insertCommand.CommandTimeout = main.commandTimeout;
                    insertCommand.ExecuteNonQuery();
                }
            }
            else if (queryNumber == (uint)CreateQuery.WeekTable)
            {
                string week = string.Empty;

                for (int day = 1; day <= dayOfMonth[int.Parse(month) - 1]; day++)
                {
                    System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");
                    System.Globalization.Calendar calendar = cultureInfo.Calendar;

                    System.Globalization.CalendarWeekRule calendarWeekRule = cultureInfo.DateTimeFormat.CalendarWeekRule;
                    DayOfWeek firstDayOfWeek = DayOfWeek.Monday;//cultureInfo.DateTimeFormat.FirstDayOfWeek; // First day of week: monday

                    DateTime requestDate = DateTime.Parse(year + "-" + month + "-" + day.ToString("00"));

                    string date = $"{year}-{month}-{day.ToString("00")}";

                    int weekOfYear = calendar.GetWeekOfYear(requestDate, calendarWeekRule, firstDayOfWeek);

                    if (CheckFirstWeekOfYear(year)) weekOfYear--;

                    string newWeek = weekOfYear.ToString("00");

                    // Insert zero data in new week
                    if (week != newWeek)
                    {
                        insertZeroQuery = $"INSERT INTO stat_week_{year}_{month} "
                            + $"VALUES('{date}', '{newWeek}', '0', '0', '0', '0', '0', '0', '0', '0', '0', '{dateTime}')";

                        insertCommand = new MySqlCommand(insertZeroQuery, dbConnector.dbConnection);
                        insertCommand.CommandTimeout = main.commandTimeout;
                        insertCommand.ExecuteNonQuery();

                        week = newWeek;
                    }
                }
            }
            else if (queryNumber == (uint)CreateQuery.MonthTable)
            {
                string date = $"{year}-{month}-01";

                insertZeroQuery = $"INSERT INTO stat_month_{year}_{month} "
                    + $"VALUES('{date}', '{int.Parse(month)}', '0', '0', '0', '0', '0', '0', '0', '0', '0', '{dateTime}')";

                insertCommand = new MySqlCommand(insertZeroQuery, dbConnector.dbConnection);
                insertCommand.CommandTimeout = main.commandTimeout;
                insertCommand.ExecuteNonQuery();
            }
            else if (queryNumber == (uint)CreateQuery./**CLASSIFIED**/)
            {
                for (int day = 1; day <= dayOfMonth[int.Parse(month) - 1]; day++)
                {
                    for (int hour = 0; hour < 24; hour++)
                    {
                        for (int min = 0; min < 60; min++)
                        {
                            for (int sec = 0; sec < 60; sec++)
                            {
                                string date = $"{year}-{month}-{day.ToString("00")}";
                                string fTime = $"{hour.ToString("00")}:{min.ToString("00")}:{sec.ToString("00")}";

                                insertZeroQuery = $"INSERT INTO /**CLASSIFIED**/_{year}_{month} "
                                    + $"VALUES('{date}', '{fTime}', '0', '0', '0', '0', '{dateTime}')";

                                insertCommand = new MySqlCommand(insertZeroQuery, dbConnector.dbConnection);
                                insertCommand.CommandTimeout = main.commandTimeout;
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            else return false;

            return true;
        }


        public override bool InsertStatisticsData(uint queryNumber, string year, string month, string day, string hour)
        {
            string insertQuery = SetInsertQuery(queryNumber, year, month, day, hour);

            try
            {
                if (dbConnector == null) return false;
                if (insertQuery == null || insertQuery == string.Empty) return false;

                MySqlCommand insertCommand = new MySqlCommand(insertQuery, dbConnector.dbConnection);
                insertCommand.CommandTimeout = main.commandTimeout;

                insertCommand.ExecuteNonQuery();

                main.AddLog((uint)MainWindow.Tab.QueryExport,
                    $"[DB] Insert, update data from {insertTableName[queryNumber - 1]}_{year}_{month}");

                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.ToString().Contains("Duplicate"))
                {
                    main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Try to insert duplicated data, please check date and time");
                    return false;
                }

                if (ex.ToString().Contains("cannot be null"))
                {
                    main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Try to insert null data, please check date and time");
                    return false;
                }

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                // No exists from table in DB
                if (ex.Code == 0)
                {
                    for (uint table = (uint)CreateQuery.HourTable; table < (uint)CreateQuery.TableCount; table++)
                        CreateStatisticsTable(table, year, month);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                return false;
            }
        }

        public override void SetDBConnector(Database.DBConnector connector)
        {
            dbConnector = connector;
        }

        public int CountNewLine(string s)
        {
            int count = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Equals('\n')) count++;
            }

            return count;
        }

        protected int SelectQueryData(MySqlConnection connection, string inputData)
        {
            int dataCount = 0;
            uint queryNumber = 0;

            string year = string.Empty;
            string month = string.Empty;
            string day = string.Empty;
            string hour = string.Empty;

            string requestTimeStamp = string.Empty;

            string[] queryData = null;

            try
            {
                queryData = inputData.Split(",");

                queryNumber = uint.Parse(queryData[(uint)QueryData.QueryNumber]);

                // Process (/**CLASSIFIED**/) ~ (/**CLASSIFIED**/) query
                if ((uint)SelectQuery./**CLASSIFIED**/ <= queryNumber && queryNumber <= (uint)SelectQuery./**CLASSIFIED**/)
                {
                    year = queryData[(uint)QueryData.Year];
                    month = queryData[(uint)QueryData.Month];
                    day = queryData[(uint)QueryData.Day];
                    hour = queryData[(uint)QueryData.Hour];

                    if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                    string selectQuery = SetSelectQuery(queryNumber, year, month, day, hour);

                    if (selectQuery != null && selectQuery != string.Empty)
                    {
                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Request query number: " + queryNumber);

                        command = new MySqlCommand(selectQuery, connection);

                        command.CommandTimeout = main.commandTimeout;

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                        dataReader = command.ExecuteReader();

                        Queue<string[]> selectedDataQueue = new Queue<string[]>();

                        if (selectedDataQueue != null)
                        {
                            while (dataReader.Read())
                            {
                                int selectFieldsCount = dataReader.FieldCount;

                                string[] selectedData = new string[selectFieldsCount];

                                int fieldIndex = 0;

                                // Example) "2020-01-01 오전 12:00:00" -> "2020-01-01"
                                selectedData[fieldIndex] = dataReader[fieldIndex++].ToString().Substring(0, 10);

                                for (; fieldIndex < selectFieldsCount; fieldIndex++)
                                {
                                    selectedData[fieldIndex] = dataReader[fieldIndex].ToString();
                                }

                                selectedDataQueue.Enqueue(selectedData);

                                dataCount++;
                            }

                            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                            string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                            dataContainer.EnqueueData(header);

                            if (dataCount == 0)
                            {
                                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] There is no data for query\n"
                                    + $"\t\t\tQuery number: {queryNumber}, Date: {year}-{month}-{day}, Hour: {hour}");
                            }

                            for (int row = 0; row < dataCount; row++)
                            {
                                dataContainer.EnqueueData(selectedDataQueue.Dequeue());
                            }
                        }
                    }
                }
                // Process (/**CLASSIFIED**/) ~ (/**CLASSIFIED**/) query
                else if ((uint)SelectQuery./**CLASSIFIED**/ <= queryNumber && queryNumber <= (uint)SelectQuery./**CLASSIFIED**/)
                {
                    year = queryData[(uint)QueryData.Year];
                    month = queryData[(uint)QueryData.Month];
                    day = queryData[(uint)QueryData.Day];
                    hour = queryData[(uint)QueryData.Hour];

                    requestTimeStamp = $"{year}-{month}-{day} {hour}";

                    if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                    string selectQuery = SetSelectQuery(queryNumber, year, month, day, hour);

                    if (selectQuery != null && selectQuery != string.Empty)
                    {
                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Request query number: " + queryNumber);

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                        Queue<string[]> selectedDataQueue = new Queue<string[]>();

                        if (selectedDataQueue != null)
                        {
                            // Update /**CLASSIFIED**/
                            if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/ || queryNumber == (uint)SelectQuery./**CLASSIFIED**/)
                            {
                                InsertStatisticsData((uint)InsertQuery./**CLASSIFIED**/, year, month, day, hour);
                            }

                            command = new MySqlCommand(selectQuery, connection);

                            command.CommandTimeout = main.commandTimeout;

                            dataReader = command.ExecuteReader();

                            int selectRowCount = 1;

                            while (dataReader.Read())
                            {
                                int selectFieldsCount = dataReader.FieldCount - 1;

                                string[] selectedData = new string[selectFieldsCount];

                                string timeStamp = string.Empty;

                                // Example) "2022-01-01 10:00:00" -> "2022-01-01 10"
                                if (dataReader[selectFieldsCount].ToString() != null &&
                                    dataReader[selectFieldsCount].ToString() != string.Empty)
                                {
                                    timeStamp = dataReader[selectFieldsCount].ToString();

                                    DateTime dateTimeStamp = DateTime.Parse(timeStamp);

                                    timeStamp = dateTimeStamp.ToString("yyyy-MM-dd HH");
                                }


                                if (timeStamp != requestTimeStamp)
                                {
                                    if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                                    //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                                    // Create statistics table and insert data
                                    InsertStatisticsData(queryNumber, year, month, day, hour);

                                    dataReader = command.ExecuteReader();
                                    for (int i = 0; i < selectRowCount; i++) dataReader.Read();
                                }

                                int fieldIndex = 0;

                                // Example) "2020-01-01 오전 12:00:00" -> "2020-01-01"
                                if (dataReader[fieldIndex].ToString() != null &&
                                    dataReader[fieldIndex].ToString() != string.Empty)
                                {
                                    selectedData[fieldIndex] = dataReader[fieldIndex++].ToString().Substring(0, 10);
                                }
                                else fieldIndex++;

                                for (; fieldIndex < selectFieldsCount; fieldIndex++)
                                {
                                    selectedData[fieldIndex] = dataReader[fieldIndex].ToString();
                                }

                                selectedDataQueue.Enqueue(selectedData);

                                selectRowCount++;

                                dataCount++;
                            }

                            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                            string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                            dataContainer.EnqueueData(header);

                            if (dataCount == 0)
                            {
                                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                                //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                                // Update /**CLASSIFIED**/
                                if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/ || queryNumber == (uint)SelectQuery./**CLASSIFIED**/)
                                {
                                    InsertStatisticsData((uint)InsertQuery./**CLASSIFIED**/, year, month, day, hour);
                                }

                                // Create statistics table and insert data
                                InsertStatisticsData(queryNumber, year, month, day, hour);
                            }

                            for (int row = 0; row < dataCount; row++)
                            {
                                dataContainer.EnqueueData(selectedDataQueue.Dequeue());
                            }
                        }
                    }
                }

                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                if (dataCount == 0)
                {
                    // Process (/**CLASSIFIED**/) ~ (/**CLASSIFIED**/) query
                    if ((uint)SelectQuery./**CLASSIFIED**/ <= queryNumber && queryNumber <= (uint)SelectQuery./**CLASSIFIED**/)
                    {
                        year = queryData[(uint)QueryData.Year];
                        month = queryData[(uint)QueryData.Month];
                        day = queryData[(uint)QueryData.Day];
                        hour = queryData[(uint)QueryData.Hour];

                        requestTimeStamp = $"{year}-{month}-{day} {hour}";

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                        string selectQuery = SetSelectQuery(queryNumber, year, month, day, hour);

                        if (selectQuery != null && selectQuery != string.Empty)
                        {
                            main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Request query number: " + queryNumber);

                            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                            //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                            Queue<string[]> selectedDataQueue = new Queue<string[]>();

                            if (selectedDataQueue != null)
                            {
                                // Update /**CLASSIFIED**/
                                if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/ || queryNumber == (uint)SelectQuery./**CLASSIFIED**/)
                                {
                                    InsertStatisticsData((uint)InsertQuery./**CLASSIFIED**/, year, month, day, hour);
                                }

                                command = new MySqlCommand(selectQuery, connection);

                                command.CommandTimeout = main.commandTimeout;

                                dataReader = command.ExecuteReader();

                                int selectRowCount = 1;

                                while (dataReader.Read())
                                {
                                    int selectFieldsCount = dataReader.FieldCount - 1;

                                    string[] selectedData = new string[selectFieldsCount];

                                    string timeStamp = string.Empty;

                                    // Example) "2022-01-01 10:00:00" -> "2022-01-01 10"
                                    if (dataReader[selectFieldsCount].ToString() != null &&
                                        dataReader[selectFieldsCount].ToString() != string.Empty)
                                    {
                                        timeStamp = dataReader[selectFieldsCount].ToString();

                                        DateTime dateTimeStamp = DateTime.Parse(timeStamp);

                                        timeStamp = dateTimeStamp.ToString("yyyy-MM-dd HH");
                                    }

                                    if (timeStamp != requestTimeStamp)
                                    {
                                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                                        // Create statistics table and insert data
                                        InsertStatisticsData(queryNumber, year, month, day, hour);

                                        dataReader = command.ExecuteReader();
                                        for (int i = 0; i < selectRowCount; i++) dataReader.Read();
                                    }

                                    int fieldIndex = 0;

                                    // Example) "2020-01-01 오전 12:00:00" -> "2020-01-01"
                                    if (dataReader[fieldIndex].ToString() != null &&
                                        dataReader[fieldIndex].ToString() != string.Empty)
                                    {
                                        selectedData[fieldIndex] = dataReader[fieldIndex++].ToString().Substring(0, 10);
                                    }
                                    else fieldIndex++;

                                    for (; fieldIndex < selectFieldsCount; fieldIndex++)
                                    {
                                        selectedData[fieldIndex] = dataReader[fieldIndex].ToString();
                                    }

                                    selectedDataQueue.Enqueue(selectedData);

                                    selectRowCount++;

                                    dataCount++;
                                }

                                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                                string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                                dataContainer.EnqueueData(header);

                                if (dataCount == 0)
                                {
                                    main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] There is no data for query\n"
                                        + $"\t\t\tQuery number: {queryNumber}, Date: {year}-{month}-{day}, Hour: {hour}");
                                }

                                for (int row = 0; row < dataCount; row++)
                                {
                                    dataContainer.EnqueueData(selectedDataQueue.Dequeue());
                                }
                            }
                        }
                    }
                }
            }
            catch (TimeoutException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 1! Timeout Exception: " + ex);

                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                dataContainer.EnqueueData(header);

                dataCount = 1;

                dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.Timeout));
            }
            catch (MySqlException ex)
            {
                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                // There is no statistics table
                if (ex.ToString().Contains("doesn't exist"))
                {
                    // Update /**CLASSIFIED**/
                    if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/ || queryNumber == (uint)SelectQuery./**CLASSIFIED**/)
                    {
                        InsertStatisticsData((uint)InsertQuery./**CLASSIFIED**/, year, month, day, hour);
                    }

                    // Create statistics table and insert data
                    InsertStatisticsData(queryNumber, year, month, day, hour);

                    // Retry to select data
                    try
                    {
                        // Process (/**CLASSIFIED**/) ~ (/**CLASSIFIED**/) query
                        if ((uint)SelectQuery./**CLASSIFIED**/ <= queryNumber && queryNumber <= (uint)SelectQuery./**CLASSIFIED**/)
                        {
                            year = queryData[(uint)QueryData.Year];
                            month = queryData[(uint)QueryData.Month];
                            day = queryData[(uint)QueryData.Day];
                            hour = queryData[(uint)QueryData.Hour];

                            requestTimeStamp = $"{year}-{month}-{day} {hour}";

                            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                            string selectQuery = SetSelectQuery(queryNumber, year, month, day, hour);

                            if (selectQuery != null && selectQuery != string.Empty)
                            {
                                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                                //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                                Queue<string[]> selectedDataQueue = new Queue<string[]>();

                                if (selectedDataQueue != null)
                                {
                                    // Update /**CLASSIFIED**/
                                    if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/ || queryNumber == (uint)SelectQuery./**CLASSIFIED**/)
                                    {
                                        InsertStatisticsData((uint)InsertQuery./**CLASSIFIED**/, year, month, day, hour);
                                    }

                                    command = new MySqlCommand(selectQuery, connection);

                                    command.CommandTimeout = main.commandTimeout;

                                    dataReader = command.ExecuteReader();

                                    int selectRowCount = 1;

                                    while (dataReader.Read())
                                    {
                                        int selectFieldsCount = dataReader.FieldCount - 1;

                                        string[] selectedData = new string[selectFieldsCount];

                                        string timeStamp = string.Empty;

                                        // Example) "2022-01-01 10:00:00" -> "2022-01-01 10"
                                        if (dataReader[selectFieldsCount].ToString() != null &&
                                            dataReader[selectFieldsCount].ToString() != string.Empty)
                                        {
                                            timeStamp = dataReader[selectFieldsCount].ToString();

                                            DateTime dateTimeStamp = DateTime.Parse(timeStamp);

                                            timeStamp = dateTimeStamp.ToString("yyyy-MM-dd HH");
                                        }

                                        if (timeStamp != requestTimeStamp)
                                        {
                                            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                                            //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                                            // Create statistics table and insert data
                                            InsertStatisticsData(queryNumber, year, month, day, hour);

                                            dataReader = command.ExecuteReader();
                                            for (int i = 0; i < selectRowCount; i++) dataReader.Read();
                                        }

                                        int fieldIndex = 0;

                                        // Example) "2020-01-01 오전 12:00:00" -> "2020-01-01"
                                        if (dataReader[fieldIndex].ToString() != null &&
                                            dataReader[fieldIndex].ToString() != string.Empty)
                                        {
                                            selectedData[fieldIndex] = dataReader[fieldIndex++].ToString().Substring(0, 10);
                                        }
                                        else fieldIndex++;

                                        for (; fieldIndex < selectFieldsCount; fieldIndex++)
                                        {
                                            selectedData[fieldIndex] = dataReader[fieldIndex].ToString();
                                        }

                                        selectedDataQueue.Enqueue(selectedData);

                                        selectRowCount++;

                                        dataCount++;
                                    }

                                    if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

                                    string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                                    dataContainer.EnqueueData(header);

                                    if (dataCount == 0)
                                    {
                                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] There is no data for query\n"
                                            + $"\t\t\tQuery number: {queryNumber}, Date: {year}-{month}-{day}, Hour: {hour}");
                                    }

                                    for (int row = 0; row < dataCount; row++)
                                    {
                                        dataContainer.EnqueueData(selectedDataQueue.Dequeue());
                                    }
                                }
                            }
                        }
                    }
                    catch (TimeoutException e)
                    {
                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 1! Timeout Exception: " + e);

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                        string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                        dataContainer.EnqueueData(header);

                        dataCount = 1;

                        dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.Timeout));
                    }
                    catch (MySqlException e)
                    {
                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + e);

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                        string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                        dataContainer.EnqueueData(header);

                        dataCount = 1;

                        dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.MySql));
                    }
                    catch (Exception e)
                    {
                        main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + e);

                        if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                        //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                        string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                        dataContainer.EnqueueData(header);

                        dataCount = 1;

                        dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.Exception));
                    }
                }
                else
                {
                    main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                    if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                    //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                    string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                    dataContainer.EnqueueData(header);

                    dataCount = 1;

                    dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.MySql));
                }
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (dataReader != null) if (!dataReader.IsClosed) dataReader.Close();

                string[] header = new string[] { queryNumber.ToString(), dataCount.ToString() };

                dataContainer.EnqueueData(header);

                dataCount = 1;

                dataContainer.EnqueueData(GetErrorMessage((uint)ErrorCode.Exception));
            }

            // If DB connection is closed, connect again
            if (dbConnector.dbConnection.State != System.Data.ConnectionState.Open)
            {
                main.SetDBConnector(dbConnector, main.queryExportTCPServer);
            }

            if (!WaitingDataReaderClose(dataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");

            return dataCount + 1; // Add header data count
        }

        protected string SetCreateQuery(uint queryNumber, string year, string month)
        {
            string queryString = string.Empty;

            if (queryNumber == (uint)CreateQuery.HourTable)
            {
                queryString = $"/**CLASSIFIED**/";
            }
            else if (queryNumber == (uint)CreateQuery.DayTable)
            {
                queryString = $"/**CLASSIFIED**/";
            }
            else if (queryNumber == (uint)CreateQuery.WeekTable)
            {
                queryString = $"/**CLASSIFIED**/";
            }
            else if (queryNumber == (uint)CreateQuery.MonthTable)
            {
                queryString = $"/**CLASSIFIED**/";
            }
            else if (queryNumber == (uint)CreateQuery./**CLASSIFIED**/)
            {
                queryString = $"/**CLASSIFIED**/";
            }
            else
            {
                queryString = string.Empty;

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[Query Export] Request wrong query");
            }

            return queryString;
        }

        protected bool CheckFirstWeekOfYear(string year)
        {
            // Get week of year at first day
            string weekCheckQuery = $"SELECT WEEK('{year}-01-01', 1)";

            MySqlCommand weekCheckCommand = null;

            MySqlDataReader weekCheckDataReader = null;

            int week = 0;

            try
            {
                if (dbConnector == null) return false;

                if (!WaitingDataReaderClose(weekCheckDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (weekCheckDataReader != null) if (!weekCheckDataReader.IsClosed) weekCheckDataReader.Close();

                weekCheckCommand = new MySqlCommand(weekCheckQuery, dbConnector.dbConnection);
                weekCheckCommand.CommandTimeout = main.commandTimeout;

                if (weekCheckCommand != null)
                {
                    weekCheckDataReader = weekCheckCommand.ExecuteReader();

                    if (weekCheckDataReader.Read())
                    {
                        week = int.Parse(weekCheckDataReader[0].ToString());
                    }
                }

                if (!WaitingDataReaderClose(weekCheckDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (weekCheckDataReader != null) if (!weekCheckDataReader.IsClosed) weekCheckDataReader.Close();
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                if (!WaitingDataReaderClose(weekCheckDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (weekCheckDataReader != null) if (!weekCheckDataReader.IsClosed) weekCheckDataReader.Close();

                return false;
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                if (!WaitingDataReaderClose(weekCheckDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (weekCheckDataReader != null) if (!weekCheckDataReader.IsClosed) weekCheckDataReader.Close();

                return false;
            }

            if (week == 0) return true;
            else return false;
        }

        protected string SetSelectQuery(uint queryNumber, string year, string month, string day, string hour)
        {
            string queryString = string.Empty;

            /**CLASSIFIED**/

            /**CLASSIFIED**/

            /**CLASSIFIED**/

            if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/) // /**CLASSIFIED**/
            {
                for (/**CLASSIFIED**/)
                {
                    queryString += $"/**CLASSIFIED**/";

                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }

                    queryString += "( ";

                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                    queryString += "/**CLASSIFIED**/";

                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                    queryString += "/**CLASSIFIED**/";

                    for (/**CLASSIFIED**/)
                    {
                        for (/**CLASSIFIED**/)
                        {
                            if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                            else queryString += $"/**CLASSIFIED**/";

                            if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                            else queryString += $"/**CLASSIFIED**/";
                        }
                    }
                    queryString += "/**CLASSIFIED**/";

                    queryString += $"/**CLASSIFIED**/"
                                 + $"/**CLASSIFIED**/"
                                 + $"/**CLASSIFIED**/"

                                 + $"/**CLASSIFIED**/";
                }

                queryString += "/**CLASSIFIED**/";

                for (/**CLASSIFIED**/)
                {
                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                    queryString += $"/**CLASSIFIED**/";
                }

                for (/**CLASSIFIED**/)
                {
                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                }
                queryString += "/**CLASSIFIED**/";

                queryString += $"/**CLASSIFIED**/"
                             + $"/**CLASSIFIED**/"
                             + $"/**CLASSIFIED**/"

                             + $"/**CLASSIFIED**/";

                queryString += "/**CLASSIFIED**/";

                for (/**CLASSIFIED**/)
                {
                    queryString += "/**CLASSIFIED**/";

                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                    queryString += "/**CLASSIFIED**/";

                    for (/**CLASSIFIED**/)
                    {
                        for (/**CLASSIFIED**/)
                        {
                            if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                            else queryString += $"/**CLASSIFIED**/";

                            if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                            else queryString += $"/**CLASSIFIED**/";
                        }
                    }

                    queryString += $"/**CLASSIFIED**/";
                }

                queryString += "( ";

                for (/**CLASSIFIED**/)
                {
                    for (/**CLASSIFIED**/)
                    {
                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";

                        if (/**CLASSIFIED**/) queryString += $"/**CLASSIFIED**/";
                        else queryString += $"/**CLASSIFIED**/";
                    }
                }

                queryString += "/**CLASSIFIED**/";

                queryString += $"/**CLASSIFIED**/"
                             + $"/**CLASSIFIED**/"
                             + $"/**CLASSIFIED**/";
            }
            else if (queryNumber == (uint)SelectQuery./**CLASSIFIED**/) // /**CLASSIFIED**/
            {
                /**CLASSIFIED**/
            }
            /**CLASSIFIED**/
            else
            {
                queryString = string.Empty;

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[Query Export] Request wrong query");
            }

            return queryString;
        }

        protected string SetInsertQuery(uint queryNumber, string year, string month, string day, string hour)
        {
            string queryString = string.Empty;

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (queryNumber == 1)
            {
                //queryString = $"INSERT ";
            }
            else if (queryNumber == 2)
            {
                //queryString = $"INSERT ";
            }
            else if (queryNumber == 3)
            {
                //queryString = $"INSERT ";
            }
            else if (queryNumber == (uint)InsertQuery./**CLASSIFIED**/) // /**CLASSIFIED**/
            {
                /**CLASSIFIED**/

                /**CLASSIFIED**/
            }
            else if (queryNumber == (uint)InsertQuery./**CLASSIFIED**/) // /**CLASSIFIED**/
            {
                /**CLASSIFIED**/
            }
            /**CLASSIFIED**/
            else
            {
                queryString = string.Empty;

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[Query Export] Request wrong query");
            }

            return queryString;
        }

        protected bool CheckExistsTable(string tableName, string year, string month)
        {
            // Count tables in DB query
            string tableCheckQuery = "SELECT COUNT(TABLE_NAME) FROM information_schema.TABLES "
                             + $"WHERE TABLE_NAME = '{tableName}_{year}_{month}'";

            MySqlCommand selectDataCommand = null;

            MySqlDataReader selectDataReader = null;

            int tableCount = 0;

            try
            {
                if (dbConnector == null) return false;

                if (!WaitingDataReaderClose(selectDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (selectDataReader != null) if (!selectDataReader.IsClosed) selectDataReader.Close();

                selectDataCommand = new MySqlCommand(tableCheckQuery, dbConnector.dbConnection);
                selectDataCommand.CommandTimeout = main.commandTimeout;

                if (selectDataCommand != null)
                {
                    selectDataReader = selectDataCommand.ExecuteReader();

                    if (selectDataReader.Read())
                    {
                        tableCount = int.Parse(selectDataReader[0].ToString());
                    }
                }

                if (!WaitingDataReaderClose(selectDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (selectDataReader != null) if (!selectDataReader.IsClosed) selectDataReader.Close();
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                if (!WaitingDataReaderClose(selectDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (selectDataReader != null) if (!selectDataReader.IsClosed) selectDataReader.Close();

                return false;
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                if (!WaitingDataReaderClose(selectDataReader)) main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! Failed to close data reader");
                //if (selectDataReader != null) if (!selectDataReader.IsClosed) selectDataReader.Close();

                return false;
            }

            if (tableCount < 1) return false;

            return true;
        }

        /**CLASSIFIED**/
        // Waiting for closing data reader
        protected bool WaitingDataReaderClose(MySqlDataReader dataReader)
        {
            try
            {
                if (dataReader != null)
                {
                    if (!dataReader.IsClosed)
                    {
                        dataReader.Close();

                        while (!dataReader.IsClosed) ;
                        //main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 4! DataReader IsClosed: " + dataReader.IsClosed); // Check IsClosed
                    }
                }
            }
            catch (MySqlException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 2! MySql Exception: " + ex);

                return false;
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[DB] Warning Level 3! Exception: " + ex);

                return false;
            }

            return true;
        }

        protected string[] GetErrorMessage(uint errorCode)
        {
            string[] data = new string[(uint)ErrorData.Count];

            data[(uint)ErrorData.ErrorCode] = errorCode.ToString();

            if (errorCode == (uint)ErrorCode.Timeout) data[(uint)ErrorData.ErrorMessage] = "DB Command Timeout";
            else if (errorCode == (uint)ErrorCode.MySql) data[(uint)ErrorData.ErrorMessage] = "MySql DB Exception";
            else if (errorCode == (uint)ErrorCode.Exception) data[(uint)ErrorData.ErrorMessage] = "Exception";

            return data;
        }
    }
}

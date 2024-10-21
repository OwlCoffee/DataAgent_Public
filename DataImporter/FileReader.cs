using System.Windows;
using System.IO;
using System;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace WPPSDataAgent.DataImporter
{
    class FileReader
    {
        protected MySqlCommand command = null;

        public string[] directoryPaths;

        public virtual void TravelSubDirectory(string path, MySqlConnection connection)
        {
        }

        protected void CloseStream(StreamReader streamReader, FileStream fileStream)
        {
            if (streamReader != null) streamReader.Close();
            if (fileStream != null) fileStream.Close();
        }

        protected bool MoveFileWithCheckDuplicate(string file, string fileName, string path)
        {
            try
            {
                string destination = Path.Combine(path, fileName);

                FileInfo fileInfo = new FileInfo(destination);

                if (fileInfo.Exists)
                {
                    bool isDuplicated = true;

                    int duplicateCount = 1;

                    while (isDuplicated)
                    {
                        destination = Path.Combine(path, fileName + $" ({duplicateCount++})");

                        fileInfo = new FileInfo(destination);

                        if (!fileInfo.Exists) isDuplicated = false;
                    }

                    File.Move(file, destination);

                    MainWindow.main.AddLog((uint)MainWindow.Tab.FileImport, "[File Import] Duplicate file exists: " + fileName);
                }
                else
                {
                    File.Move(file, destination);
                }

                return true;
            }
            catch (Exception ex)
            {
                MainWindow.main.AddLog((uint)MainWindow.Tab.FileImport, "[File Import] Warning Level 3! Exception: " + ex);

                string destination = Path.Combine(MainWindow.main.exceptionDirectoryPath, fileName);

                FileInfo fileInfo = new FileInfo(destination);

                if (fileInfo.Exists)
                {
                    bool isDuplicated = true;

                    int duplicateCount = 1;

                    while (isDuplicated)
                    {
                        destination = Path.Combine(MainWindow.main.exceptionDirectoryPath, fileName + $" ({duplicateCount++})");

                        fileInfo = new FileInfo(destination);

                        if (!fileInfo.Exists) isDuplicated = false;
                    }

                    File.Move(file, destination);

                    MainWindow.main.AddLog((uint)MainWindow.Tab.FileImport, "[File Import] Duplicate exception file exists: " + fileName);
                }
                else
                {
                    File.Move(file, destination);
                }

                return false;
            }
        }

        protected string QuotesAndCommaRemover(string data)
        {
            char[] charArray = data.ToCharArray();

            bool quoteChecker = false;

            for (int i = 0; i < charArray.Length; i++)
            {
                if (charArray[i] == '"')
                {
                    charArray[i] = 'r'; // Replace quote to 'r' to remove

                    // If character is begin quote, quoteChecker is true
                    // If character is end quote, quoteChecker is false
                    quoteChecker = !quoteChecker;
                }
                if (quoteChecker && charArray[i] == ',') charArray[i] = 'r'; // Replace comma in quotes to 'r' to remove
            }

            return new string(charArray).Replace("r", "");
        }

        protected bool IsNumber(string data)
        {
            return Regex.IsMatch(data, @"[-+]?\d*\.?\d+");
        }
    }
}

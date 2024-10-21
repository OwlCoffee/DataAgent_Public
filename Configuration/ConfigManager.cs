using System;
using System.Windows;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace WPPSDataAgent.Configuration
{
    public class ConfigManager
    {
        string filePath = string.Empty;

        FileInfo iniFile = null;

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public enum ConfigValue
        {
            DBServer = 0,
            DBPort = 1,
            DBName = 2,
            UserID = 3,
            Timeout = 4,
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            //*CLASSIFIED*
            ValueCount = 19
        }

        public const int directoryCount = 14;

        protected string[] pathValues = new string[directoryCount]
        {
            /*
                *CLASSIFIED*
            */
        };

        public string[] Initialize(string path)
        {
            filePath = path;
            iniFile = new FileInfo(filePath);

            if (iniFile != null)
            {
                // Load the config file
                if (iniFile.Exists)
                {
                    return LoadConfigFile(filePath);
                }
                // Write default values
                else
                {
                    WriteDefaultValues(filePath);

                    // Load config file after write default values
                    return LoadConfigFile(filePath);
                }
            }

            return null;
        }

        protected void WriteDefaultValues(string path)
        {
            // Write config values (path, section, key, value)
            WriteValue(path, "DBConnect", "DBServer", "127.0.0.1");
            WriteValue(path, "DBConnect", "DBPort", "3306");
            WriteValue(path, "DBConnect", "DBName", /**CLASSIFIED**/);
            WriteValue(path, "DBConnect", "UserID", "root");
            WriteValue(path, "DBConnect", "Timeout", "43200");

            int startIndex = -5;

            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
            WriteValue(path, "FileImport", "/**CLASSIFIED**/", pathValues[(uint)ConfigValue./**CLASSIFIED**/ + startIndex]);
        }

        protected void WriteValue(string filePath, string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        protected string ReadValue(string filePath, string section, string key)
        {
            var value = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", value, 255, filePath);

            return value.ToString();
        }

        protected string[] LoadConfigFile(string path)
        {
            return GetConfigFileValues(path);
        }

        public void EditConfigFile(string path, string server, string port, string name, string id, string timeout)
        {
            // Write config values (path, section, key, value)
            WriteValue(path, "DBConnect", "DBServer", server);
            WriteValue(path, "DBConnect", "DBPort", port);
            WriteValue(path, "DBConnect", "DBName", name);
            WriteValue(path, "DBConnect", "UserID", id);
            WriteValue(path, "DBConnect", "Timeout", timeout);
        }

        public string[] GetConfigFileValues(string path)
        {
            string[] values = new string[(uint)ConfigValue.ValueCount];

            values[(uint)ConfigValue.DBServer] = ReadValue(path, "DBConnect", "DBServer");
            values[(uint)ConfigValue.DBPort] = ReadValue(path, "DBConnect", "DBPort");
            values[(uint)ConfigValue.DBName] = ReadValue(path, "DBConnect", "DBName");
            values[(uint)ConfigValue.UserID] = ReadValue(path, "DBConnect", "UserID");
            values[(uint)ConfigValue.Timeout] = ReadValue(path, "DBConnect", "Timeout");

            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");
            values[(uint)ConfigValue./**CLASSIFIED**/] = ReadValue(path, "FileImport", "/**CLASSIFIED**/");

            return values;
        }
    }
}

using MySql.Data.MySqlClient;
using System.Windows;

namespace WPPSDataAgent.DataImporter
{
    public class DataImporter
    {
        protected MainWindow main;// = (MainWindow)Application.Current.MainWindow;

        protected MySqlCommand command = null;

        public virtual void InsertData(MySqlConnection connection, Decoder.DataDecoder decoder)
        {
        }

        public virtual void InsertData(MySqlConnection connection, string[] directoryPaths)
        {
        }
    }
}

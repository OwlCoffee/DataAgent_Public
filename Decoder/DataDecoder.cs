using System.Windows;

namespace WPPSDataAgent.Decoder
{
    public class DataDecoder
    {
        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        protected int[] leapYear = new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        protected int[] normalYear = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public DataContainer.DataContainer dataContainer = null;

        protected string mergeString = string.Empty;

        public DataDecoder(DataContainer.DataContainer container)
        {
            dataContainer = container;
        }

        public virtual int Decode(byte[] received)
        {
            return 0;
        }

        public virtual bool CreateStatisticsTable(uint queryNumber, string year, string month)
        {
            return false;
        }

        public virtual bool InsertStatisticsData(uint queryNumber, string year, string month, string day, string hour)
        {
            return false;
        }

        protected virtual string SubString(string s)
        {
            string result = "";
            int count = 0;
            int idx = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Equals('\n'))
                {
                    count++;
                    if (count > 0)
                    {
                        for (; idx <= i; idx++)
                        {
                            result += s[idx];
                        }
                    }
                }
            }

            mergeString = mergeString.Substring(result.Length);
            return result;
        }

        protected bool LeapYearCheck(int year)
        {
            bool isLeapYear = false;

            if (year % 4 == 0)
            {
                if (year % 100 == 0)
                {
                    if (year % 400 == 0)
                    {
                        isLeapYear = true;
                    }
                }
                else
                {
                    isLeapYear = true;
                }
            }

            return isLeapYear;
        }

        public virtual void SetDBConnector(Database.DBConnector connector)
        {
        }
    }
}

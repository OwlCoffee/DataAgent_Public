using System;
using System.Text;

namespace WPPSDataAgent.Decoder
{
    public class WavePowerDataDecoder : DataDecoder
    {
        public WavePowerDataDecoder(DataContainer.DataContainer container) : base(container) { }

        const int dataAmount = 18;

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
                        dataContainer.EnqueueData(AddDateTime(s_received[i]));
                    }
                }
            }

            return 0;
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

        public int CountComma(string s)
        {
            int count = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Equals(',')) count++;
            }

            return count;
        }

        // Add date information
        // Example) [0]: year  [1]: month  [2]: day  [3]: hour  [4]: min  [5]: sec  [6]: ms  [7]: data1 ~
        public string[] AddDateTime(string inputData)
        {
            int dateLength = 3;

            string[] splittedData = inputData.Split(',');

            string[] result = new string[splittedData.Length + dateLength];

            DateTime timestamp = DateTime.Now;

            string year = timestamp.ToString("yyyy");
            string month = timestamp.ToString("MM");
            string day = timestamp.ToString("dd");

            result[0] = year;
            result[1] = month;
            result[2] = day;

            for (int index = 0; index < splittedData.Length; index++)
            {
                result[index + dateLength] = splittedData[index];
            }

            return result;
        }
    }
}

using System.Collections.Generic;
using System.Threading;

namespace WPPSDataAgent.DataContainer
{
    public class WavePowerDataContainer : DataContainer
    {
        private Queue<string[]> wavePowerDataQueue = new Queue<string[]>();

        private string[] dequeueData;

        /*
         * Mutex for data synchronization
         * TCPClient receiving thread & Inserting to DB thread
         * Access (wavePowerDataQueue) in WavePowerDataContainer
         */
        private Mutex wavePowerDataMutex = new Mutex();

        public override void EnqueueData(string[] data)
        {
            wavePowerDataMutex.WaitOne();

            wavePowerDataQueue.Enqueue(data);

            wavePowerDataMutex.ReleaseMutex();
        }

        public override string[] DequeueData()
        {
            wavePowerDataMutex.WaitOne();

            if (wavePowerDataQueue.Count >= 1)
            {
                dequeueData = wavePowerDataQueue.Dequeue();

                wavePowerDataMutex.ReleaseMutex();

                return dequeueData;
            }
            else
            {
                wavePowerDataMutex.ReleaseMutex();

                return null;
            }
        }

        public override void ClearQueueData()
        {
            wavePowerDataMutex.WaitOne();

            wavePowerDataQueue.Clear();

            wavePowerDataMutex.ReleaseMutex();
        }

        public override int GetQueueCount()
        {
            wavePowerDataMutex.WaitOne();

            int queueCount = wavePowerDataQueue.Count;

            wavePowerDataMutex.ReleaseMutex();

            return queueCount;
        }
    }
}

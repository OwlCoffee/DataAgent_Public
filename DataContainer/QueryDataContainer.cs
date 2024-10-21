using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System;
using System.Collections;

namespace WPPSDataAgent.DataContainer
{
    public class QueryDataContainer : DataContainer
    {
        private Queue<string[]> queryDataQueue = new Queue<string[]>();

        private string[] dequeueData;

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        /*
         * Mutex for data synchronization
         * TCPServer receiving thread & Selecting from DB thread
         * Access (queryDataQueue) in queryDataContainer
         */
        private Mutex queryDataMutex = new Mutex();

        public override void EnqueueData(string[] data)
        {
            queryDataMutex.WaitOne();

            queryDataQueue.Enqueue(data);

            queryDataMutex.ReleaseMutex();
        }

        public override string[] DequeueData()
        {
            queryDataMutex.WaitOne();

            if (queryDataQueue.Count >= 1)
            {
                dequeueData = queryDataQueue.Dequeue();

                queryDataMutex.ReleaseMutex();

                return dequeueData;
            }
            else
            {
                queryDataMutex.ReleaseMutex();

                return null;
            }
        }

        public override void ClearQueueData()
        {
            queryDataMutex.WaitOne();

            queryDataQueue.Clear();

            queryDataMutex.ReleaseMutex();
        }
    }
}

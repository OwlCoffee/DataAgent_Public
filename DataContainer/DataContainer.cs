namespace WPPSDataAgent.DataContainer
{
    public class DataContainer
    {
        public virtual void EnqueueData(string[] data) { }

        public virtual string[] DequeueData()
        {
            string[] result = null;

            return result;
        }

        public virtual void ClearQueueData() { }

        public virtual int GetQueueCount()
        {
            return 0;
        }
    }
}

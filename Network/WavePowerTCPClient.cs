namespace WPPSDataAgent
{
    class WavePowerTCPClient : TCPClientSync
    {
        public WavePowerTCPClient(string inputIPAddress, int inputPort, Decoder.DataDecoder decoder)
        : base(inputIPAddress, inputPort, decoder)
        {
        }
    }
}

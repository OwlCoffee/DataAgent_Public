using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WPPSDataAgent
{
    class QueryDataCommunicatorSync : TCPServerSync
    {
        Database.DBConnector dbConnector = null;

        public QueryDataCommunicatorSync(int inputPort, Decoder.DataDecoder decoder, Database.DBConnector connector)
         : base(inputPort, decoder)
        {
            dbConnector = connector;
        }

        protected override void AcceptTcpClient(IAsyncResult result)
        {
            try
            {
                if (main.isServerListening && serverSocket != null)
                {
                    TcpClient client = serverSocket.EndAcceptTcpClient(result);

                    if (client != null)
                    {
                        if (!client.Connected)
                        {
                            // If TCP client isn't accepted, try again accept
                            serverSocket.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
                        }
                        else
                        {
                            networkStream = client.GetStream();

                            main.isServerListening = false;
                            main.isServerAccepted = true;

                            string acceptedIPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            string acceptedPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

                            main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Accepted\n"
                                + $"\t\t\tClient IP Address: {acceptedIPAddress}  Client Port: {acceptedPort}");

                            main.SetTextBoxReadOnly(true);

                            receiveBuffer = new byte[dataBufferSize];

                            main.SetDBConnector(dbConnector, this);

                            // If TCP client is accepted, start to receive and send data with thread
                            receiveThread = new Thread(ReceiveDataLoop);
                            receiveThread.Start();
                        }
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (receiveThread != null)
                {
                    if (receiveThread.IsAlive) receiveThread.Join();
                }

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Object Disposed Exception: " + ex);
            }
            catch (Exception ex)
            {
                if (receiveThread != null)
                {
                    if (receiveThread.IsAlive) receiveThread.Join();
                }

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Exception: " + ex);
            }
        }

        #region Receive
        protected override void ReceiveDataLoop()
        {
            while (socketReady)
            {
                main.serverReceiveMutex.WaitOne();
                ReturnQueryData();
                main.serverReceiveMutex.ReleaseMutex();
            }
        }

        protected void ReturnQueryData()
        {
            try
            {
                if (networkStream != null)
                {
                    do
                    {
                        int bytelength = networkStream.Read(receiveBuffer, 0, dataBufferSize);

                        // If there is no received data, close socket
                        if (bytelength <= 0)
                        {
                            CloseSocket();
                            return;
                        }

                        int dataCount = dataDecoder.Decode(receiveBuffer); // Decode received data in data decoder

                        if (dataCount >= 1)
                        {
                            for (int index = 0; index < dataCount; index++)
                            {
                                string marshalledData = MarshalData(dataDecoder.dataContainer.DequeueData());

                                if (marshalledData != null && socketReady) Send(marshalledData);
                            }
                        }

                        Array.Clear(receiveBuffer, 0, bytelength); // Clear buffer
                    } while (networkStream.DataAvailable);
                }
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Return Query Data Socket Exception: " + e);

                if (e.SocketErrorCode == SocketError.Disconnecting || e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    CloseSocket();
                }
                else
                {
                    CloseSocket();
                }
            }
            catch (Exception e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Return Query Data Exception : " + e);

                CloseSocket();
            }
        }
        #endregion

        public string MarshalData(string[] input)
        {
            string result = input[0];

            if (input.Length > 1)
            {
                for (int i = 1; i < input.Length; i++)
                {
                    result += "," + input[i];
                }
            }

            main.AddDataLog((uint)MainWindow.Tab.QueryExport, result);

            result += "\n";

            return result;
        }
    }
}

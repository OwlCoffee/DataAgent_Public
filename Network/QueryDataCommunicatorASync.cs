using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WPPSDataAgent
{
    class QueryDataCommunicatorASync : TCPServerASync
    {
        Database.DBConnector dbConnector = null;

        public QueryDataCommunicatorASync(int inputPort, Decoder.DataDecoder decoder, Database.DBConnector connector)
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

                            main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Accepted");

                            string acceptedIPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            string acceptedPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

                            main.AddLog((uint)MainWindow.Tab.QueryExport, $"IP Address: {acceptedIPAddress}  Port: {acceptedPort}");

                            receiveBuffer = new byte[dataBufferSize];

                            //main.SetDBConnector(dbConnector, this);

                            // If TCP client is accepted, start to receive and send data with thread
                            ReceiveData();
                        }
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Object Disposed Exception: " + ex);
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Exception: " + ex);
            }
        }

        #region Receive Callback
        protected override void ReceiveData()
        {
            if (networkStream != null)
            {
                isBeginRead = true;
                networkStream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(QueryDataReturnCallback), null);
            }
            else isBeginRead = false;
        }

        /*
         * Receive and send data synchronously from server
         */
        protected void QueryDataReturnCallback(IAsyncResult _result)
        {
            try
            {
                isCallbackRunning = true;

                int bytelength;

                if (networkStream != null)
                {
                    do
                    {
                        if (!isBeginRead)
                        {
                            bytelength = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                        }
                        else
                        {
                            bytelength = networkStream.EndRead(_result);
                            isBeginRead = false;
                        }

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

                                if (marshalledData != null) Send(marshalledData);
                            }
                        }

                        Array.Clear(receiveBuffer, 0, bytelength); // Clear buffer
                    } while (networkStream != null && networkStream.DataAvailable);

                    // If DataAvailable is false, start again ReceiveData method
                    ReceiveData();
                }
            }
            catch (ObjectDisposedException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Object Disposed Exception: " + ex);

                CloseSocket();
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Receive Callback Socket Exception: " + e);

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
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Receive Callback Exception: " + e);

                CloseSocket();
            }
            finally
            {
                isCallbackRunning = false;
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

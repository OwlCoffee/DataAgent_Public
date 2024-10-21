using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Generic;
using System.Windows;

namespace WPPSDataAgent
{
    public class TCPServerASync
    {
        protected int port = 0;

        protected bool socketReady = false;
        protected NetworkStream networkStream;

        protected Queue<string> stringData = new Queue<string>();

        protected TcpListener serverSocket;

        //public Thread receiveThread;

        protected bool isBeginRead = false;

        // Buffer for receiving data
        protected byte[] receiveBuffer;
        protected int dataBufferSize = 10000; // Bytes

        public Decoder.DataDecoder dataDecoder = null;

        private bool _isCallbackRunning = false;

        private Mutex isCallbackRunningMutex = new Mutex();

        public bool isCallbackRunning
        {
            get
            {
                isCallbackRunningMutex.WaitOne();
                bool result = _isCallbackRunning;
                isCallbackRunningMutex.ReleaseMutex();

                return result;
            }
            set
            {
                isCallbackRunningMutex.WaitOne();
                _isCallbackRunning = value;
                isCallbackRunningMutex.ReleaseMutex();
            }
        }

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        public TCPServerASync(int inputPort, Decoder.DataDecoder decoder)
        {
            port = inputPort;
            dataDecoder = decoder;
        }

        public bool StartServer()
        {
            try
            {
                serverSocket = new TcpListener(IPAddress.Any, port);

                socketReady = true;

                serverSocket.Start();

                main.isServerListening = true;

                // Start listening
                StartListening();

                return true;
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Socket Exception: " + e);
            }
            catch (Exception e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Start Server Exception: " + e);
            }

            CloseSocket();

            return false;
        }

        protected void StartListening()
        {
            serverSocket.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
        }

        protected virtual void AcceptTcpClient(IAsyncResult result)
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

                            // If TCP client is accepted, start receiving data
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

        #region Receive
        protected virtual void ReceiveData()
        {
            if (networkStream != null)
            {
                isBeginRead = true;
                networkStream.BeginRead(receiveBuffer, 0, dataBufferSize, new AsyncCallback(ReceiveCallback), null);
            }
            else isBeginRead = false;
        }

        /*
         * Receive data asynchronously from server
         */
        protected void ReceiveCallback(IAsyncResult _result)
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

                        dataDecoder.Decode(receiveBuffer); // Decode received data in data decoder
                        Array.Clear(receiveBuffer, 0, bytelength); // Clear buffer
                    } while (networkStream != null && networkStream.DataAvailable);

                    // If DataAvailable is false, start again ReceiveData method
                    ReceiveData();
                }
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

        #region Send
        protected void SendData(string data)
        {
            if (serverSocket == null) { return; }
            try
            {
                StreamWriter writer = new StreamWriter(networkStream);

                writer.Write(data);
                writer.Flush();
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Socket Exception: " + e);
            }

        }

        /*
         * If accepted to client, send data
         */
        public void Send(string data)
        {
            try
            {
                if (socketReady)
                {
                    if (data != null)
                    {
                        SendData(data);
                    }
                }
            }
            catch (Exception e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Exception: " + e);
            }
        }
        #endregion

        /*
         * Close socket and network stream
         */
        public void CloseSocket()
        {
            if (socketReady)
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                }
                serverSocket.Stop();
            }

            socketReady = false;

            isBeginRead = false;

            main.isServerListening = false;
            main.isServerAccepted = false;

            main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Close socket");
        }
    }
}

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
    public class TCPServerSync
    {
        protected int port = 0;

        protected bool socketReady = false;
        protected NetworkStream networkStream;

        protected Queue<string> stringData = new Queue<string>();

        protected TcpListener serverSocket;

        public Thread receiveThread;

        // Buffer for receiving data
        protected byte[] receiveBuffer;
        protected int dataBufferSize = 10000; // Bytes

        public Decoder.DataDecoder dataDecoder = null;

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        public TCPServerSync(int inputPort, Decoder.DataDecoder decoder)
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

                            string acceptedIPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            string acceptedPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

                            main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Accepted\n"
                                + $"\t\t\tClient IP Address: {acceptedIPAddress}  Client Port: {acceptedPort}");

                            receiveBuffer = new byte[dataBufferSize];

                            // If TCP client is accepted, start to receive data with thread
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
        protected virtual void ReceiveDataLoop()
        {
            while (socketReady)
            {
                main.serverReceiveMutex.WaitOne();
                ReceiveData();
                main.serverReceiveMutex.ReleaseMutex();
            }
        }

        protected virtual void ReceiveData()
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

                        dataDecoder.Decode(receiveBuffer); // Decode received data in data decoder
                        Array.Clear(receiveBuffer, 0, bytelength); // Clear buffer
                    } while (networkStream.DataAvailable);
                }
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 2! Receive Data Socket Exception: " + e);

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
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Warning Level 3! Receive Data Exception: " + e);

                CloseSocket();
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

            main.isServerListening = false;
            main.isServerAccepted = false;

            main.SetTextBoxReadOnly(false);

            main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Server] Close socket");
        }
    }
}

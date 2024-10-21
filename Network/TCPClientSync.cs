using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace WPPSDataAgent
{
    class TCPClientSync
    {
        public Decoder.DataDecoder dataDecoder = null;

        protected string ipAddress = string.Empty;
        protected int port = 0;

        public TCPClientSync(string inputIPAddress, int inputPort, Decoder.DataDecoder decoder)
        {
            ipAddress = inputIPAddress;
            port = inputPort;

            dataDecoder = decoder;
        }

        protected bool socketReady = false;

        protected NetworkStream networkStream;

        protected TcpClient clientSocket;
        public Thread receiveThread;

        // Buffer for receiving data
        protected byte[] receiveBuffer;
        protected int dataBufferSize = 10000; // Bytes

        protected bool isClientStarted = false;

        protected MainWindow main = (MainWindow)Application.Current.MainWindow;

        public bool StartClient()
        {
            isClientStarted = true;

            try
            {
                clientSocket = new TcpClient(ipAddress, port);

                socketReady = true;

                // Start receive data
                StartReceive();

                return true;
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 2! Client Socket Exception: " + e);
            }
            catch (Exception e)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 3! Client Exception: " + e);
            }

            CloseSocket();

            return false;
        }

        protected void StartReceive()
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    main.isClientConnected = true;

                    main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Connected to server\n"
                        + $"\t\t\tIP Address: {ipAddress}  Port: {port}");

                    networkStream = clientSocket.GetStream();

                    receiveBuffer = new byte[dataBufferSize];

                    // If connect to server, start to receive data with thread
                    receiveThread = new Thread(ReceiveDataLoop);
                    receiveThread.Start();

                    main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Start to receive data");
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (receiveThread != null)
                {
                    if (receiveThread.IsAlive) receiveThread.Join();
                }

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Client] Warning Level 2! Object Disposed Exception: " + ex);
            }
            catch (SocketException ex)
            {
                if (receiveThread != null)
                {
                    if (receiveThread.IsAlive) receiveThread.Join();
                }

                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 2! Client Socket Exception: " + ex);
            }
            catch (Exception ex)
            {
                if (receiveThread != null)
                {
                    if (receiveThread.IsAlive) receiveThread.Join();
                }

                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Client] Warning Level 3! Exception: " + ex);
            }
        }

        #region Receive
        protected virtual void ReceiveDataLoop()
        {
            while (socketReady)
            {
                main.clientReceiveMutex.WaitOne();
                ReceiveData();
                main.clientReceiveMutex.ReleaseMutex();
            }
        }

        protected void ReceiveData()
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
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 2! Receive Data Socket Exception: " + e);

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
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 3! Receive Data Exception: " + e);

                CloseSocket();
            }
        }
        #endregion

        /*
         * Close socket and network stream
         */
        public void CloseSocket()
        {
            if (isClientStarted)
            {
                if (socketReady)
                {
                    if (networkStream != null)
                    {
                        networkStream.Close();
                    }
                    clientSocket.Close();
                }

                socketReady = false;

                main.isClientConnected = false;

                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Disconnected from server");
            }

            isClientStarted = false;
        }
    }
}

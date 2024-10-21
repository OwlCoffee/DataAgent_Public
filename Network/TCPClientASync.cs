using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace WPPSDataAgent
{
    class TCPClientASync
    {
        public Decoder.DataDecoder dataDecoder = null;

        protected string ipAddress = string.Empty;
        protected int port = 0;

        public TCPClientASync(string inputIPAddress, int inputPort, Decoder.DataDecoder decoder)
        {
            ipAddress = inputIPAddress;
            port = inputPort;

            dataDecoder = decoder;
        }

        protected bool socketReady = false;

        protected NetworkStream networkStream;

        protected TcpClient clientSocket;

        // Buffer for receiving data
        protected byte[] receiveBuffer;
        protected int dataBufferSize = 10000; // Bytes

        protected bool isBeginRead = false;
        protected bool isClientStarted = false;

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

                    // If connect to server, start to receive data
                    ReceiveData();

                    main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Start to receive data");
                }
            }
            catch (ObjectDisposedException ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Client] Warning Level 2! Object Disposed Exception: " + ex);
            }
            catch (SocketException ex)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 2! Client Socket Exception: " + ex);
            }
            catch (Exception ex)
            {
                main.AddLog((uint)MainWindow.Tab.QueryExport, "[TCP Client] Warning Level 3! Exception: " + ex);
            }
        }

        #region Receive
        public void ReceiveData()
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
                    } while (networkStream.DataAvailable);

                    // If DataAvailable is false, start again ReceiveData method
                    ReceiveData();
                }
            }
            catch (SocketException e)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 2! Receive Callback Socket Exception: " + e);

                if (e.SocketErrorCode == SocketError.Disconnecting || e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    CloseSocket();
                    StartClient();
                }
                else
                {
                    CloseSocket();
                }
            }
            catch (Exception e)
            {
                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Warning Level 3! Receive Callback Exception: " + e);

                CloseSocket();
            }
            finally
            {
                isCallbackRunning = false;
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

                isBeginRead = false;

                main.AddLog((uint)MainWindow.Tab.Comm, "[TCP Client] Disconnected from server");
            }

            isClientStarted = false;
        }
    }
}

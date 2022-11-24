using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace KhTracker
{
    public partial class MainWindow : Window
    {
        private static DispatcherTimer networkTimer;
        private static readonly SolidColorBrush errorColor = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
        private static readonly SolidColorBrush runningColor = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00));

        private void SetStatusBar(string msg, bool isError)
        {
            NetworkStatus.Header = msg;
            NetworkStatus.Foreground = isError ? errorColor : runningColor;
        }

        private void NetworkTimerStart(EventHandler OnTick)
        {
            if (networkTimer != null)
                networkTimer.Stop();

            networkTimer = new DispatcherTimer();
            networkTimer.Tick += OnTick;
            networkTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            networkTimer.Start();
        }

        public void Join()
        {
            if (Network.MP.IsRunning() == false)
            {
                var mpJoin = new JoinMultiplayerForm();
                if (mpJoin.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    Join(Properties.Settings.Default.ServerIP, Properties.Settings.Default.ServerPort);
            }
            else
            {
                Join(null, 0);
            }
        }

        public void Host()
        {
            if (Network.MP.IsRunning() == false)
            {
                Host(Properties.Settings.Default.HostPort);
            }
            else
            {
                Host(0);
                Network.MP.Stop();
            }
        }

        public void NetworkShutdown()
        {
            if (!Network.MP.IsRunning())
                return;

            Network.MP.Stop();
            networkTimer.Stop();
        }

    }

    public class MessageEventArgs : EventArgs
    {
        public byte[] Message { get; private set; }
        public int Source { get; private set; }

        public MessageEventArgs(byte[] message, int source)
        {
            this.Message = message;
            this.Source = source;
        }
    }

    public class Network
    {

        private static NetworkInterface mp = null;
        private static readonly object padlock = new object();

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);

        Network()
        {
        }

        public static NetworkInterface MP
        {
            get
            {
                lock (padlock)
                {
                    if (mp == null)
                    {
                        mp = new NetworkInterface();
                    }
                    return mp;
                }
            }
        }

        const int bufferSize = 1048576;

        public class NetworkInterface
        {
            public Client client;
            public Server server;
            private MPServer mpServer;
            private List<MessageEventArgs> newMessages;

            bool isClient;

            public NetworkInterface()
            {
                client = new Client();
                server = new Server();
                newMessages = new List<MessageEventArgs>();

                isClient = false;
            }

            public void setServer(MPServer ser)
            {
                mpServer = ser;
            }

            public bool HasMessages()
            {
                return newMessages.Count > 0;
            }

            public void QueueMessage(MessageEventArgs msg)
            {
                lock (this)
                {
                    newMessages.Add(msg);
                }
            }

            public MessageEventArgs GetMessage()
            {
                MessageEventArgs msg = null;
                lock (this)
                {
                    msg = newMessages[0];
                    newMessages.RemoveAt(0);
                }
                return msg;
            }

            public bool UpdateItem(string itemName, string worldName, bool add, bool manual)
            {
                if (!IsRunning())
                    return false;

                if (worldName.EndsWith("Grid"))
                    worldName = worldName.Remove(worldName.Length - 4, 4);
                //build update message
                ItemFoundMSG itemMsg = new ItemFoundMSG(itemName, worldName, add, manual);
                byte[] byteItem = itemMsg.ToByte();
                byte[] msg = new byte[sizeof(NetworkMessages) + byteItem.Length];
                msg[0] = (byte)NetworkMessages.ItemFound;
                byteItem.CopyTo(msg, 1);

                //send update message
                if (isClient)
                {
                    client.send(msg, msg.Length);
                }
                else
                {
                    if (mpServer != null)
                        mpServer.AddItem(itemName, worldName);
                    server.BroadcastMessage(null, msg, msg.Length);
                }

                return true;
            }

            public void Join(string serverAddress, int port)
            {
                isClient = true;
                client.Join(serverAddress, port);
            }

            public bool Host(int port)
            {
                bool result = server.start(port);

                if (result == true)
                {
                    isClient = false;
                }

                return result;
            }

            public void Send(byte[] msg, int msgLength)
            {
                if (isClient)
                {
                    client.send(msg, msgLength);
                }
                else
                {
                    server.BroadcastMessage(null, msg, msgLength);
                }
            }

            public void Send(int clientId, byte[] msg, int msgLength)
            {
                if (!isClient)
                {
                    server.Send(clientId, msg, msgLength);
                }
                else
                {
                    client.send(msg, msgLength);
                }
            }

            public void Broadcast(int source, byte[] msg, int msgLength)
            {
                if(!isClient)
                {
                    server.BroadcastMessage(source, msg, msgLength);
                }
            }

            public void Stop()
            {
                if (IsRunning() == true)
                {
                    if (isClient)
                        client.stop();
                    else
                        server.stop();
                }
            }

            public bool IsRunning()
            {
                return client.isRunning || server.isRunning;
            }

            public bool IsClient()
            {
                return isClient;
            }
        }

        public class Server
        {
            private TcpListener tcpserver;
            private List<cWorker> clients;
            private int nextId;
            public bool isRunning;


            public bool start(int port)
            {

                if (clients == null)
                {
                    clients = new List<cWorker>();
                }
                else
                {
                    removeAllClients();
                }

                if (tcpserver != null)
                {
                    tcpserver.Stop();
                }

                tcpserver = new TcpListener(IPAddress.Any, port);
                if (tcpserver != null)
                {
                    tcpserver.Start();
                    nextId = 1;
                    isRunning = true;
                    new Thread(accept_connect).Start();
                    return true;
                }

                isRunning = false;
                return false;
            }
            public void stop()
            {
                if (tcpserver != null)
                {
                    removeAllClients();
                    tcpserver.Stop();
                }
                isRunning = false;
            }

            private void accept_connect()
            {
                try
                {
                    while (true)
                    {

                        TcpClient socket = tcpserver.AcceptTcpClient();
                        cWorker worker = new cWorker(socket, nextId);
                        nextId++;
                        addClient(worker);
                        worker.Start();

                    }
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (SocketException e)
                {
                    switch (e.NativeErrorCode)
                    {
                        case 10004:
                            // socket was closed, no need to display an error just stop the thread
                            break;
                        default:
                            MessageBox.Show(e.Message);
                            break;
                    }
                }
            }

            public void Send(int id, byte[] msg, int msgLegnth)
            {
                foreach(cWorker client in clients)
                {
                    if (client.id == id)
                    {
                        client.Send(msg, msgLegnth);
                        return;
                    }
                }
            }

            public void BroadcastMessage(cWorker from, byte[] message)
            {
                BroadcastMessage(from, message, message.Length);
            }

            public void BroadcastMessage(int from, byte[] message, int msgLength)
            {
                cWorker source = null;
                lock (this)
                {
                    if (from != 0)
                    {
                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].id == from)
                            {
                                source = clients[i];
                                break;
                            }
                        }
                    }
                }

                BroadcastMessage(source, message, msgLength);
            }

            public void BroadcastMessage(cWorker from, byte[] message, int msgLength)
            {
                lock (this)
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        cWorker worker = clients[i];
                        if (!worker.Equals(from))
                        {
                            try
                            {
                                worker.Send(message, msgLength);
                            }
                            catch (Exception)
                            {
                                clients.RemoveAt(i--);
                                worker.Close();
                            }
                        }
                    }
                }
            }

            private void onMessageReceived(object sender, MessageEventArgs e)
            {
                MP.QueueMessage(e);
            }

            private void onClientDisconnect(object sender, EventArgs e)
            {
                removeClient(sender as cWorker);
            }

            private void addClient(cWorker worker)
            {
                lock (this)
                {
                    clients.Add(worker);
                    worker.Disconnected += onClientDisconnect;
                    worker.MessageReceived += onMessageReceived;
                }
            }

            public void RemoveClient(int id)
            {
                cWorker source = null;
                lock (this)
                {
                    if (id != 0)
                    {
                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].id == id)
                            {
                                source = clients[i];
                                break;
                            }
                        }
                    }
                }

                removeClient(source);
            }
            private void removeClient(cWorker worker)
            {
                lock (this)
                {
                    worker.Disconnected -= onClientDisconnect;
                    worker.MessageReceived -= onMessageReceived;
                    clients.Remove(worker);
                    worker.Close();
                }
            }

            private void removeAllClients()
            {
                lock (this)
                {
                    foreach (cWorker c in clients)
                    {
                        c.Disconnected -= onClientDisconnect;
                        c.MessageReceived -= onMessageReceived;
                        c.Close();
                    }

                    clients.Clear();
                }
            }

            private bool GetClientStatus(cWorker source, out ClientState curState, out TimeSpan elapsed)
            {
                curState = ClientState.Unknown;
                elapsed = TimeSpan.Zero;
                if (source == null)
                    return false;

                curState = source.state;
                elapsed = source.TimeInCurrentState();
                return true;
            }
            public bool GetClientStatus(int id, out ClientState curState, out TimeSpan elapsed)
            {
                cWorker source = null;
                
                lock (this)
                {
                    if (id != 0)
                    {
                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].id == id)
                            {
                                source = clients[i];
                                break;
                            }
                        }
                    }
                }
                return GetClientStatus(source, out curState, out elapsed);
            }

            public void UpdateClientStatus(int id, ClientState newState)
            {
                cWorker source = null;
                lock (this)
                {
                    if (id != 0)
                    {
                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].id == id)
                            {
                                source = clients[i];
                                break;
                            }
                        }
                    }
                }
                
                UpdateClientStatus(source, newState);
            }

            private void UpdateClientStatus(cWorker client, ClientState newState)
            {
                if (client == null)
                    return;

                lock (this)
                {
                    client.ChangeState(newState);
                }
            }
        }

        public class Client
        {
            private TcpClient sock;
            private Stream ns;
            public event EventHandler OnDisconnect;
            public event MessageEventHandler OnMessageRecv;

            public bool isRunning;

            public void Join(string ip, int port)
            {
                isRunning = true;
                new Thread(() => Run(ip, port)).Start();
            }
            private void onMessageReceived(object sender, MessageEventArgs e)
            {
                MP.QueueMessage(e);
            }
            private void connect(string serverAddress, int port)
            {
                if (sock != null)
                    stop();
                isRunning = true;
                sock = new TcpClient(serverAddress, port);

                if (sock != null)
                {
                    ns = sock.GetStream();
                    OnMessageRecv += onMessageReceived;
                }
            }

            public bool IsConnected()
            {
                return sock != null && sock.Connected;
            }

            public void send(byte[] msg)
            {
                if (ns != null)
                    ns.Write(msg, 0, msg.Length);
            }

            public void send(byte[] msg, int length)
            {
                if (ns != null)
                    ns.Write(msg, 0, length);
            }

            private void Run(string ip, int port)
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        connect(ip, port);
                        break;
                    }
                    catch (SocketException e)
                    {
                        if (retryCount < 5)
                        {
                            retryCount++;
                            continue;
                        }
                        switch (e.NativeErrorCode)
                        {
                            case 10061:
                                MessageBox.Show("Failed to connect to server");
                                break;
                            default:
                                MessageBox.Show(e.Message);
                                break;
                        }
                        stop();
                        return;
                    }
                }

                byte[] buffer = new byte[bufferSize];
                try
                {
                    while (true)
                    {
                        int receivedBytes = ns.Read(buffer, 0, buffer.Length);
                        if (receivedBytes < 1)
                            break;
                        byte[] msg = new byte[receivedBytes];
                        Buffer.BlockCopy(buffer, 0, msg, 0, receivedBytes);
                        OnMessageRecv?.Invoke(this, new MessageEventArgs(msg, 0));
                    }
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                OnDisconnect?.Invoke(this, EventArgs.Empty);
                stop();
            }
            public void stop()
            {
                if (sock != null)
                    sock.Close();
                isRunning = false;
                OnDisconnect -= OnDisconnect;
                OnMessageRecv -= OnMessageRecv;
            }
        }

        public class cWorker
        {


            public event MessageEventHandler MessageReceived;
            public event EventHandler Disconnected;

            private readonly TcpClient sock;
            private readonly Stream ns;
            private int extra;
            private DateTime StateStart;
            public ClientState state;
            public int id;

            public cWorker(TcpClient socket, int id)
            {
                this.sock = socket;
                this.ns = socket.GetStream();
                this.id = id;
            }

            public void ChangeState(ClientState newState)
            {
                state = newState;
                StateStart = DateTime.Now;
            }

            public TimeSpan TimeInCurrentState()
            {
                return DateTime.Now - StateStart;
            }

            public void Send(byte[] buffer)
            {
                ns.Write(buffer, 0, buffer.Length);
            }

            public void Send(byte[] msg, int msgLength)
            {
                ns.Write(msg, 0, msgLength);
            }

            public void Start()
            {
                state = ClientState.Join;
                
                new Thread(Run).Start();
            }

            private void Run()
            {
                byte[] buffer = new byte[bufferSize];
                try
                {
                    while (true)
                    {
                        int receivedBytes = ns.Read(buffer, 0, buffer.Length);
                        if (receivedBytes < 1)
                            break;
                        byte[] msg = new byte[receivedBytes];
                        Buffer.BlockCopy(buffer, 0, msg, 0, receivedBytes);
                        MessageReceived?.Invoke(this, new MessageEventArgs(msg, id));
                    }
                }
                catch (ObjectDisposedException) { }
                catch (IOException) { }
                Disconnected?.Invoke(this, EventArgs.Empty);
            }

            public void Close()
            {
                sock.Close();
            }
        }


    }
}

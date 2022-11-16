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
    //class Network

    public partial class MainWindow : Window
    {
        private static DispatcherTimer networkTimer;
        private static SolidColorBrush errorColor = new SolidColorBrush(Color.FromRgb(0xff,0x00,0x00));
        private static SolidColorBrush runningColor = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00));

        private void SetStatusBar(string msg, bool isError)
        {
            NetworkStatus.Header = msg;
            NetworkStatus.Foreground = isError ? errorColor : runningColor;
        }
        private void networkUpdateItem(string itemName, string worldName, bool add, bool manual)
        {
            bool hasItem = false;
            foreach (Item child in MainWindow.data.WorldsData[worldName].worldGrid.Children)
            {
                if(child.Name.Equals(itemName))
                {
                    hasItem = true;
                    break;
                }
            }
            if(!hasItem)
                data.WorldsData[worldName].worldGrid.Add_Ghost(Data.GhostItems["Ghost_" + itemName], null);
        }

        private void ClientTick(object sender, EventArgs e)
        {
            while (Network.MP.HasMessages())
            {
                byte[] msg = Network.MP.GetMessage().Message;

                string[] smsg = Encoding.UTF8.GetString(msg, 0, msg.Length).Split(' ');

                networkUpdateItem(smsg[0], smsg[1], smsg[2].Equals("True"), smsg[3].Equals("True"));
            }
            //client disconnected/shutdown
            if(!Network.MP.IsRunning() || !Network.MP.client.IsConnected())
            {
                if (Network.MP.IsRunning())
                    Network.MP.Stop();
                MultiplayerHost.IsEnabled = true;
                MultiplayerJoin.Header = "Join Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);

                networkTimer.Stop();
            }
        }

        private void HostTick(object sender, EventArgs e)
        {
            while (Network.MP.HasMessages())
            {
                MessageEventArgs msg = Network.MP.GetMessage();

                string[] smsg = Encoding.UTF8.GetString(msg.Message, 0, msg.Message.Length).Split(' ');

                Network.MP.Send(msg.Source, msg.Message, msg.Message.Length);

                networkUpdateItem(smsg[0], smsg[1], smsg[2].Equals("True"), smsg[3].Equals("True"));
            }
            //Server shutdown
            if (!Network.MP.IsRunning())
            {
                MultiplayerJoin.IsEnabled = true;
                MultiplayerHost.Header = "Host Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);

                networkTimer.Stop();
            }
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

        public void Join(string ip, int port)
        {
            if(Network.MP.IsRunning() == false)
            {

                if (Network.MP.Join(ip, port))
                {
                    MultiplayerHost.IsEnabled = false;
                    MultiplayerJoin.Header = "Disconnect from Multiplayer";

                    SetStatusBar("NetworkStatus: Connected", false);

                    NetworkTimerStart(ClientTick);
                    GhostItemToggle(true);
                }

            }
            else
            {
                Network.MP.Stop();
                MultiplayerHost.IsEnabled = true;
                MultiplayerJoin.Header = "Join Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);

                networkTimer.Stop();
            }
        }

        public void Host(int port)
        {
            if (Network.MP.IsRunning() == false)
            {
                if (Network.MP.Host(port))
                {
                    MultiplayerJoin.IsEnabled = false;
                    MultiplayerHost.Header = "Stop Server";
                    SetStatusBar("NetworkStatus: Connected", false);

                    NetworkTimerStart(HostTick);
                    GhostItemToggle(true);
                }
            }
            else
            {
                Network.MP.Stop();
                MultiplayerJoin.IsEnabled = true;
                MultiplayerHost.Header = "Host Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);

                networkTimer.Stop();
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

        const int bufferSize = 2018;

        public class NetworkInterface
        {
            public Client client;
            public Server server;
            private List<MessageEventArgs> newMessages;

            bool isClient;
            bool isRunning;

            public NetworkInterface()
            {
                client = new Client();
                server = new Server();
                newMessages = new List<MessageEventArgs>();

                isClient = isRunning = false;
            }

            public bool HasMessages()
            {
                return newMessages.Count > 0;
            }

            public void QueueMessage(MessageEventArgs msg)
            {
                lock(this)
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
                if (!isRunning)
                    return false;

                //build update message
                byte[] msg = Encoding.Default.GetBytes(itemName+' '+ worldName.Remove(worldName.Length - 4, 4) + ' '+add+' '+manual);

                //send update message
                if(isClient)
                {
                    client.send(msg, msg.Length);
                }
                else
                {
                    server.BroadcastMessage(null, msg, msg.Length);
                }

                return true;
            }

            public bool Join(string serverAddress, int port)
            {
                bool result = client.connect(serverAddress, port);

                if (result == true)
                {
                    isClient = isRunning = true;
                }

                return result;
            }

            public bool Host(int port)
            {
                bool result = server.start(port);

                if (result == true)
                {
                    isClient = false;
                    isRunning = true;
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
                if(!isClient)
                {
                    server.Send(clientId, msg, msgLength);
                }
            }

            public void Stop()
            {
                if (isRunning == true)
                {
                    if (isClient)
                        client.stop();
                    else
                        server.stop();

                    isRunning = false;
                }
            }

            public bool IsRunning()
            {
                return isRunning;
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
            private Thread sThread;
            private int nextId;


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
                    sThread.Abort();
                    tcpserver.Stop();
                }

                tcpserver = new TcpListener(IPAddress.Any, port);
                if (tcpserver != null)
                {
                    tcpserver.Start();
                    nextId = 1;
                    sThread = new Thread(accept_connect);
                    sThread.Start();
                    return true;
                }

                return false;
            }
            public void stop()
            {
                if (tcpserver != null)
                {
                    removeAllClients();

                    sThread.Abort();
                    tcpserver.Stop();
                }
            }

            private void accept_connect()
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

            public void Send(int id, byte[] msg, int msgLegnth)
            {
                if(id >= 0 && clients.Count() > id)
                {
                    clients[id].Send(msg, msgLegnth);
                }
            }

            public void BroadcastMessage(cWorker from, byte[] message)
            {
                BroadcastMessage(from, message, message.Length);
            }

            public void BroadcastMessage(int from, byte[] message, int msgLength)
            {
                cWorker source = null;
                lock(this)
                {
                    for(int i = 0; i < clients.Count; i++)
                    {
                        if (clients[i].id == from)
                        {
                            source = clients[i];
                            break;
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
        }

        public enum ClientState
        {
            init,
            connecting,
            join,
            running,
            disconnected,
            unknown,
            last
        }

        public class Client
        {
            private TcpClient sock;
            private Stream ns;
            private ClientState state;
            public event EventHandler OnDisconnect;
            public event MessageEventHandler OnMessageRecv;

            private void onMessageReceived(object sender, MessageEventArgs e)
            {
                MP.QueueMessage(e);
            }
            public bool connect(string serverAddress, int port)
            {
                if (sock != null)
                    stop();
                try
                {
                    sock = new TcpClient(serverAddress, port);
                }
                catch (SocketException e)
                {
                    switch (e.NativeErrorCode)
                    {
                        case 10061:
                            MessageBox.Show("Failed to connect to server");
                            break;
                        default:
                            MessageBox.Show(e.Message);
                            break;
                    }
                }

                if (sock != null)
                {
                    ns = sock.GetStream();
                    OnMessageRecv += onMessageReceived;
                    new Thread(Run).Start();

                    return true;
                }

                return false;
            }

            public bool IsConnected()
            {
                return sock.Connected;
            }

            public void send(byte[] msg)
            {
                ns.Write(msg, 0, msg.Length);
            }

            public void send(byte[] msg, int length)
            {
                ns.Write(msg, 0, length);
            }

            private void Run()
            {
                byte[] buffer = new byte[2048];
                try
                {
                    while (true)
                    {
                        int receivedBytes = ns.Read(buffer, 0, buffer.Length);
                        if (receivedBytes < 1)
                            break;
                        OnMessageRecv?.Invoke(this, new MessageEventArgs(buffer, 0));
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
            private ClientState state;
            public int id;

            public cWorker(TcpClient socket, int id)
            {
                this.sock = socket;
                this.ns = socket.GetStream();
                this.id = id;
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
                state = ClientState.join;
                new Thread(Run).Start();
            }

            private void Run()
            {
                byte[] buffer = new byte[bufferSize];
                try
                {
                    while (true)
                    {
                        int recivedBytes = ns.Read(buffer, 0, bufferSize);
                        if (recivedBytes < 1)
                            break;

                        MessageReceived?.Invoke(this, new MessageEventArgs(buffer, id));
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

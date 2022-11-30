using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KhTracker
{
    public partial class MainWindow : Window
    {
        private MPServer mpServer = new MPServer();
        private const double timeOutSeconds = 30;
        private void HostUpdateStatus()
        {
            if (Network.MP.IsRunning())
            {
                MultiplayerJoin.IsEnabled = false;
                MultiplayerHost.Header = "Stop Server";
                SetNetworkStatusBar("NetworkStatus: Connected", false);
            }
            else
            {
                MultiplayerJoin.IsEnabled = true;
                MultiplayerHost.Header = "Host Multiplayer";
                SetNetworkStatusBar("NetworkStatus: Disconnected", true);
            }
        }

        private void HostTick(object sender, EventArgs e)
        {
            while (Network.MP.HasMessages())
            {
                int msgStart = 0;
                MessageEventArgs msg = Network.MP.GetMessage();

                while(msgStart < msg.Message.Length - 1)
                {
                    NetworkMessageHandler handler;
                    mpServer.MessageHandlers.TryGetValue((NetworkMessages)msg.Message[msgStart], out handler);
                    msgStart++;
                    if (handler != null)
                        msgStart = handler(msgStart, msg.Message, msg.Source);
                    else
                        msgStart = OnInvalidMSG(msgStart, msg.Message, msg.Source);
                }
            }
            //
            List<int> ClientsToDissconect = null;
            var ClientStatusList = Network.MP.server.GetAllClientStatus();
            foreach(var clientStatus in ClientStatusList)
            {
                if(clientStatus.CurrentState == ClientState.Join && clientStatus.DurationInStatus.TotalSeconds >= timeOutSeconds)
                {
                    if (ClientsToDissconect == null)
                        ClientsToDissconect = new List<int>();

                    ClientsToDissconect.Add(clientStatus.ClientID);
                }
            }

            if(ClientsToDissconect != null)
            {
                foreach(int id in ClientsToDissconect)
                {
                    Network.MP.server.RemoveClient(id);
                }
            }
            //Server shutdown
            if (!Network.MP.IsRunning())
            {
                HostUpdateStatus();
                networkTimer.Stop();
            }
        }

        public void Host(int port)
        {
            if(mpServer.MessageHandlers.Count == 0)
            {
                mpServer.MessageHandlers.Add(NetworkMessages.ItemFound, OnItemFound);
                mpServer.MessageHandlers.Add(NetworkMessages.Join, mpServer.OnJoinReq);
                mpServer.MessageHandlers.Add(NetworkMessages.SyncRequest, mpServer.OnSyncRequest);

                Network.MP.setServer(mpServer);
            }
            if (Network.MP.IsRunning() == false)
            {
                if (Network.MP.Host(port))
                {
                    HostUpdateStatus();
                    NetworkTimerStart(HostTick);
                    GhostItemToggle(true);
                }
            }
            else
            {
                Network.MP.Stop();
            }
        }

        public void NetworkHintUpdate(string hint)
        {
            if (mpServer == null)
                return;

            mpServer.JoinReqData.hintData = hint;
        }
    }

    public class MPServer
    {
        public GameMode Mode;
        public Dictionary<NetworkMessages, NetworkMessageHandler> MessageHandlers;
        public JoinRequest JoinReqData;
        public bool bAllowDownload;
        public bool bHintMatch;
        private Dictionary<ItemIDs, WorldIDs> FoundItems;

        public MPServer()
        {
            Mode = new GameMode();
            MessageHandlers = new Dictionary<NetworkMessages, NetworkMessageHandler>();
            JoinReqData = new JoinRequest();
            FoundItems = new Dictionary<ItemIDs, WorldIDs>();
            bHintMatch = true;
            bAllowDownload = true;
        }

        public void AddItem(string itemName, string worldName)
        {
            ItemIDs name = ItemIDs.Error;
            WorldIDs world = WorldIDs.Error;
            Enum.TryParse<ItemIDs>(itemName, out name);
            Enum.TryParse<WorldIDs>(worldName, out world);

            FoundItems.Add(name, world);
        }

        public int OnJoinReq(int offset, byte[] msg, int source)
        {
            JoinRequest req = new JoinRequest();
            offset = req.FromByte(msg, offset);
            byte[] resp;
            if (req.magicNumber.SequenceEqual(JoinReqData.magicNumber))
            {
                if (!bHintMatch || req.hintData.SequenceEqual(JoinReqData.hintData))
                {
                    int msgOffset = 0;
                    byte[] syncMsg = CreateSyncData();
                    resp = new byte[sizeof(NetworkMessages) + sizeof(NetworkMessages) + syncMsg.Length];

                    resp[msgOffset] = (byte)NetworkMessages.Join;
                    msgOffset += sizeof(NetworkMessages);

                    resp[msgOffset] = (byte)NetworkMessages.SyncData;
                    msgOffset += sizeof(NetworkMessages);

                    syncMsg.CopyTo(resp, msgOffset);

                    Network.MP.server.UpdateClientStatus(source, ClientState.Running);
                }
                else if(bAllowDownload)
                {
                    int msgOffset = 0;
                    byte[] hintMsg = new HintDataMSG(JoinReqData.hintData).ToByte();
                    byte[] syncMsg = CreateSyncData();
                    resp = new byte[sizeof(NetworkMessages) + sizeof(NetworkMessages) + hintMsg.Length + sizeof(NetworkMessages) + syncMsg.Length];

                    resp[msgOffset] = (byte)NetworkMessages.Join;
                    msgOffset += sizeof(NetworkMessages);

                    resp[msgOffset] = (byte)NetworkMessages.HintData;
                    msgOffset += sizeof(NetworkMessages);

                    hintMsg.CopyTo(resp, msgOffset);
                    msgOffset += hintMsg.Length;

                    resp[msgOffset] = (byte)NetworkMessages.SyncData;
                    msgOffset += sizeof(NetworkMessages);

                    syncMsg.CopyTo(resp, msgOffset);

                    Network.MP.server.UpdateClientStatus(source, ClientState.Running);
                }
                else
                {
                    resp = new byte[] { (byte)NetworkMessages.Quit };
                    Network.MP.server.Send(source, resp, resp.Length);
                    Network.MP.server.RemoveClient(source);
                }
            }
            else
            {
                resp = new byte[] {(byte)NetworkMessages.Quit};
                Network.MP.server.Send(source, resp, resp.Length);
                Network.MP.server.RemoveClient(source);
            }
            Network.MP.server.Send(source, resp, resp.Length);
            return offset;
        }

        public byte[] CreateSyncData()
        {
            SyncMsg syncData = new SyncMsg();

            foreach (KeyValuePair<ItemIDs, WorldIDs> item in FoundItems)
            {
                syncData.FoundItems.Add((item.Key, item.Value));
            }

            return syncData.ToByte();
        }

        public int OnSyncRequest(int offset, byte[] msg, int source)
        {
            byte[] sync = CreateSyncData();
            byte[] resp = new byte[sizeof(NetworkMessages) + sync.Length];
            resp[0] = (byte)NetworkMessages.SyncData;
            sync.CopyTo(resp, 1);
            Network.MP.server.Send(source, resp, resp.Length);

            return offset;
        }
    }
}

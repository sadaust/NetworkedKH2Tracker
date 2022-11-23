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
        private void HostUpdateStatus()
        {
            if (Network.MP.IsRunning())
            {
                MultiplayerJoin.IsEnabled = false;
                MultiplayerHost.Header = "Stop Server";
                SetStatusBar("NetworkStatus: Connected", false);
            }
            else
            {
                MultiplayerJoin.IsEnabled = true;
                MultiplayerHost.Header = "Host Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);
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

        public MPServer()
        {
            Mode = new GameMode();
            MessageHandlers = new Dictionary<NetworkMessages, NetworkMessageHandler>();
            JoinReqData = new JoinRequest();
            bHintMatch = true;
            bAllowDownload = true;
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
                    //TODO add sync MSG
                    resp = new byte[] { (byte)NetworkMessages.Join };
                    Network.MP.server.UpdateClientStatus(source, ClientState.Running);
                }
                else if(bAllowDownload)
                {
                    //TODO add sync MSG
                    byte[] hintMsg = new HintDataMSG(JoinReqData.hintData).ToByte();
                    resp = new byte[1 + hintMsg.Length];
                    resp[0] = (byte)NetworkMessages.HintData;
                    hintMsg.CopyTo(resp, 1);
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
    }
}

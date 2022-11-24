using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;

namespace KhTracker
{
    public partial class MainWindow : Window
    {
        private MPClient mpClient = new MPClient();

        private void ClientTick(object sender, EventArgs e)
        {
            switch (mpClient.State)
            {
                case ClientState.Connecting:
                    if (Network.MP.client.IsConnected())
                    {
                        mpClient.State = ClientState.Join;
                        ClientUpdateStatus();
                        byte[] req = new JoinRequest(data.openKHHintText).ToByte();
                        byte[] msg = new byte[1 + req.Length];
                        msg[0] = (byte)NetworkMessages.Join;
                        req.CopyTo(msg, 1);
                        Network.MP.Send(msg, msg.Length);
                    }
                    break;
                case ClientState.Join:
                    while (Network.MP.HasMessages())
                    {
                        int msgStart = 0;
                        byte[] msg = Network.MP.GetMessage().Message;
                        while (mpClient.State == ClientState.Join && msgStart < msg.Length)
                        {
                            switch ((NetworkMessages)msg[msgStart])
                            {
                                case NetworkMessages.ResendJoin:
                                    msgStart++;
                                    byte[] req = new JoinRequest(data.openKHHintText).ToByte();
                                    byte[] msgdata = new byte[1 + req.Length];
                                    msgdata[0] = (byte)NetworkMessages.Join;
                                    req.CopyTo(msgdata, 1);
                                    Network.MP.Send(msgdata, msgdata.Length);
                                    break;
                                case NetworkMessages.Join:
                                    msgStart++;
                                    mpClient.State = ClientState.Running;
                                    break;
                                case NetworkMessages.Quit:
                                    msgStart++;
                                    mpClient.State = ClientState.Disconnected;
                                    break;
                                default:
                                    msgStart = OnInvalidMSG(msgStart, msg, 0);
                                    break;
                            }

                        }

                        // Process remaining messages if we have more
                        while (mpClient.State == ClientState.Running && msgStart < msg.Length)
                        {
                            NetworkMessageHandler handler;
                            mpClient.MessageHandlers.TryGetValue((NetworkMessages)msg[msgStart], out handler);
                            msgStart++;
                            if (handler != null)
                                msgStart = handler(msgStart, msg, 0);
                            else
                                msgStart = OnInvalidMSG(msgStart, msg, 0);
                        }

                        ClientUpdateStatus();
                    }
                    break;
                case ClientState.Running:
                    while (Network.MP.HasMessages())
                    {
                        int msgStart = 0;
                        byte[] msg = Network.MP.GetMessage().Message;
                        while (msgStart < msg.Length)
                        {
                            NetworkMessageHandler handler;
                            mpClient.MessageHandlers.TryGetValue((NetworkMessages)msg[msgStart], out handler);
                            msgStart++;
                            if (handler != null)
                                msgStart = handler(msgStart, msg, 0);
                            else
                                msgStart = OnInvalidMSG(msgStart, msg, 0);
                        }
                    }
                    break;
                default:
                    break;

            }
            //client disconnected/shutdown
            if (!Network.MP.IsRunning())
            {
                if (Network.MP.IsRunning())
                    Network.MP.Stop();
                mpClient.State = ClientState.Disconnected;
                ClientUpdateStatus();
                MultiplayerHost.IsEnabled = true;
                MultiplayerJoin.Header = "Join Multiplayer";
                SetStatusBar("NetworkStatus: Disconnected", true);

                networkTimer.Stop();
            }
        }

        public int OnClientSyncMSG(int offset, byte[] msg, int source)
        {
            SyncMsg sync = new SyncMsg();
            offset = sync.FromByte(msg, offset);

            foreach ((ItemIDs itemID, WorldIDs worldID) item in sync.FoundItems)
            {
                networkUpdateItem(item.itemID.ToString(), item.worldID.ToString(), true, false);
            }
            return offset;
        }

        public int OnClientHintDataMSG(int offset, byte[] msg, int source)
        {
            HintDataMSG hMsg = new HintDataMSG();
            offset = hMsg.FromByte(msg, offset);
            data.openKHHintText = hMsg.hintData;
            //Restart hint loading
            var hintText = Encoding.UTF8.GetString(Convert.FromBase64String(data.openKHHintText));
            var hintObject = JsonSerializer.Deserialize<Dictionary<string, object>>(hintText);
            var settings = new List<string>();
            bool betterSTTon = false;

            if (hintObject.ContainsKey("settings"))
            {
                settings = JsonSerializer.Deserialize<List<string>>(hintObject["settings"].ToString());

                //set all settings to false
                {
                    PromiseCharmToggle(false);
                    SimulatedToggle(false);
                    HundredAcreWoodToggle(false);
                    AtlanticaToggle(false);
                    CavernToggle(false);
                    OCCupsToggle(false);
                    SoraHeartToggle(true);
                    SoraLevel01Toggle(true);
                    VisitLockToggle(false);
                    TerraToggle(false);
                    PuzzleToggle(false);
                    SynthToggle(false);

                    AbilitiesToggle(true);
                    //TornPagesToggle(true);
                    //CureToggle(true);
                    //FinalFormToggle(true);

                    ExtraChecksToggle(false);
                    AntiFormToggle(false);

                    SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;
                    broadcast.SimulatedTwilightTownPlus.Visibility = Visibility.Hidden;
                }

                //load settings from hints
                foreach (string setting in settings)
                {
                    Console.WriteLine("setting found = " + setting);

                    switch (setting)
                    {
                        case "PromiseCharm":
                            PromiseCharmToggle(true);
                            break;
                        case "Level":
                            {
                                SoraHeartToggle(false);
                            }
                            break;
                        case "ExcludeFrom50":
                            {
                                SoraHeartToggle(true);
                                SoraLevel50Toggle(true);
                            }
                            break;
                        case "ExcludeFrom99":
                            {
                                SoraHeartToggle(true);
                                SoraLevel99Toggle(true);
                            }
                            break;
                        case "Simulated Twilight Town":
                            SimulatedToggle(true);
                            break;
                        case "Hundred Acre Wood":
                            HundredAcreWoodToggle(true);
                            break;
                        case "Atlantica":
                            AtlanticaToggle(true);
                            break;
                        case "Cavern of Remembrance":
                            CavernToggle(true);
                            break;
                        case "Olympus Cups":
                            OCCupsToggle(true);
                            break;
                        case "visit_locking":
                            VisitLockToggle(true);
                            break;
                        case "Lingering Will (Terra)":
                            TerraToggle(true);
                            break;
                        case "Puzzle":
                            PuzzleToggle(true);
                            break;
                        case "Synthesis":
                            SynthToggle(true);
                            break;
                        case "better_stt":
                            betterSTTon = true;
                            break;
                        case "extra_ics":
                            ExtraChecksToggle(true);
                            break;
                            //DEBUG! UPDATE LATER
                            //case "Anti-Form":
                            //    AntiFormToggle(true);
                            //    break;
                    }
                    //if (setting.Key == "Second Chance & Once More ")
                    //    AbilitiesToggle(true);
                    //if (setting.Key == "Torn Pages")
                    //    TornPagesToggle(true);
                    //if (setting.Key == "Cure")
                    //    CureToggle(true);
                    //if (setting.Key == "Final Form")
                    //    FinalFormToggle(true);
                }
            }

            switch (hintObject["hintsType"].ToString())
            {
                case "Shananas":
                    {
                        SetMode(Mode.OpenKHAltHints);
                        ShanHints(hintObject);
                    }
                    break;
                case "JSmartee":
                    {
                        SetMode(Mode.OpenKHHints);
                        JsmarteeHints(hintObject);
                    }
                    break;
                case "Points":
                    {
                        SetMode(Mode.DAHints);
                        PointsHints(hintObject);
                    }
                    break;
                case "Path":
                    {
                        SetMode(Mode.PathHints);
                        PathHints(hintObject);
                    }
                    break;
                case "Spoiler":
                    {
                        SetMode(Mode.SpoilerHints);
                        SpoilerHints(hintObject);
                    }
                    break;
                case "Timed":
                    {
                        //incomplete
                        //SetMode(Mode.TimeHints);
                        //TimeHints(hintObject);
                    }
                    break;
                default:
                    break;
            }

            //better stt icon workaround
            if (betterSTTon)
            {
                SimulatedTwilightTownPlus.Visibility = Visibility.Visible;
                broadcast.SimulatedTwilightTownPlus.Visibility = Visibility.Visible;
            }

            return offset;
        }

        public void Join(string ip, int port)
        {
            if (mpClient.MessageHandlers.Count == 0)
            {
                mpClient.MessageHandlers.Add(NetworkMessages.ItemFound, OnItemFound);
                mpClient.MessageHandlers.Add(NetworkMessages.HintData, OnClientHintDataMSG);
                mpClient.MessageHandlers.Add(NetworkMessages.SyncData, OnClientSyncMSG);
            }
            if (Network.MP.IsRunning() == false)
            {
                mpClient.State = ClientState.Connecting;
                Network.MP.Join(ip, port);
                NetworkTimerStart(ClientTick);
                GhostItemToggle(true);
                ClientUpdateStatus();

            }
            else
            {
                Network.MP.Stop();
            }
        }

        private void ClientUpdateStatus()
        {
            if (mpClient == null)
                return;

            switch (mpClient.State)
            {
                case ClientState.Connecting:
                case ClientState.Join:
                    MultiplayerHost.IsEnabled = false;
                    MultiplayerJoin.Header = "Disconnect from Multiplayer";
                    SetStatusBar("NetworkStatus: Joining", false);
                    break;
                case ClientState.Running:
                    MultiplayerHost.IsEnabled = false;
                    MultiplayerJoin.Header = "Disconnect from Multiplayer";
                    SetStatusBar("NetworkStatus: Connected", false);
                    break;
                case ClientState.Disconnected:
                    MultiplayerHost.IsEnabled = true;
                    MultiplayerJoin.Header = "Disconnect from Multiplayer";
                    SetStatusBar("NetworkStatus: Joining", false);
                    break;
                default:
                    throw new InvalidOperationException("Client is an unsupported state");
            }
        }

    }


    public class MPClient
    {
        public ClientState State;
        public GameMode Mode;
        public Dictionary<NetworkMessages, NetworkMessageHandler> MessageHandlers;

        public MPClient()
        {
            State = ClientState.Init;
            Mode = new GameMode();
            MessageHandlers = new Dictionary<NetworkMessages, NetworkMessageHandler>();
        }

    }
}

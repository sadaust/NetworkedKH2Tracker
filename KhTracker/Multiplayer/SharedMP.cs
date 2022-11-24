using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections;


namespace KhTracker
{
    public enum NetworkMessages : byte
    {
        None,
        Join,
        ResendJoin,
        Quit,
        SyncRequest,
        SyncData,
        Shutdown,
        ItemFound,
        PlayerJoined,
        PlayerQuit,
        HintData,
        Last
    }

    public enum WorldIDs : byte
    {
        Error,
        SorasHeart,
        DriveForms,
        SimulatedTwilightTown,
        TwilightTown,
        HollowBastion,
        BeastsCastle,
        OlympusColiseum,
        Agrabah,
        LandofDragons,
        HundredAcreWood,
        PrideLands,
        DisneyCastle,
        HalloweenTown,
        PortRoyal,
        SpaceParanoids,
        TWTNW,
        GoA,
        Atlantica,
        PuzzSynth
    }

    public enum ItemIDs : byte
    {
        Error,
        Report1,
        Report2,
        Report3,
        Report4,
        Report5,
        Report6,
        Report7,
        Report8,
        Report9,
        Report10,
        Report11,
        Report12,
        Report13,
        Fire1,
        Fire2,
        Fire3,
        Blizzard1,
        Blizzard2,
        Blizzard3,
        Thunder1,
        Thunder2,
        Thunder3,
        Cure1,
        Cure2,
        Cure3,
        HadesCup,
        OlympusStone,
        Reflect1,
        Reflect2,
        Reflect3,
        Magnet1,
        Magnet2,
        Magnet3,
        Valor,
        Wisdom,
        Limit,
        Master,
        Final,
        Anti,
        OnceMore,
        SecondChance,
        UnknownDisk,
        TornPage1,
        TornPage2,
        TornPage3,
        TornPage4,
        TornPage5,
        Baseball,
        Lamp,
        Ukulele,
        Feather,
        Connection,
        Nonexistence,
        Peace,
        PromiseCharm,
        BeastWep,
        JackWep,
        SimbaWep,
        AuronWep,
        MulanWep,
        SparrowWep,
        AladdinWep,
        TronWep,
        MembershipCard,
        Picture,
        IceCream
    }

    public class GameMode
    {
        public bool bSharedHints;
        public bool bCoop;

        public GameMode()
        {
            bSharedHints = true;
            bCoop = true;
        }

        public byte[] ToByte()
        {
            byte[] ret;
            BitArray flags = new BitArray(new bool[] { bSharedHints, bCoop });
            ret = new byte[(flags.Length - 1) / 8 + 1];
            flags.CopyTo(ret, 0);
            return ret;
        }

        public int FromByte(byte[] data, int offset)
        {
            BitArray flags = new BitArray(new byte[] { data[offset] });
            offset += 1;
            bSharedHints = flags[0];
            bCoop = flags[1];

            return offset;
        }
    }

    public class JoinRequest
    {
        public byte[] magicNumber;
        public string ver;
        public string hintData;

        const string NetworkVersion = "1.0.0.2";

        public JoinRequest()
        {
            magicNumber = new byte[] { 0x53, 0x41, 0x44 };
            ver = NetworkVersion;
            hintData = "";
        }

        public JoinRequest(string hint)
        {
            magicNumber = new byte[] { 0x53, 0x41, 0x44 };
            ver = NetworkVersion;
            hintData = hint;
        }

        public byte[] ToByte()
        {
            byte[] ret;
            byte[] version = Encoding.UTF8.GetBytes(ver);
            byte[] hint = Encoding.UTF8.GetBytes(hintData);
            ret = new byte[magicNumber.Length + sizeof(int) + version.Length + sizeof(int) + hint.Length];
            int offset = 0;

            magicNumber.CopyTo(ret, offset);
            offset += magicNumber.Length;

            BitConverter.GetBytes(version.Length).CopyTo(ret, offset);
            offset += sizeof(int);

            version.CopyTo(ret, offset);
            offset += version.Length;

            BitConverter.GetBytes(hint.Length).CopyTo(ret, offset);
            offset += sizeof(int);

            hint.CopyTo(ret, offset);

            return ret;
        }

        public int FromByte(byte[] data, int offset)
        {
            int length;
            magicNumber = new byte[3];
            Buffer.BlockCopy(data, offset, magicNumber, 0, 3);
            offset += magicNumber.Length;

            length = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            ver = Encoding.UTF8.GetString(data, offset, length);
            offset += length;

            length = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            hintData = Encoding.UTF8.GetString(data, offset, length);
            offset += length;

            return offset;
        }

    }

    public class ItemFoundMSG
    {
        public bool bAdd;
        public bool bManual;
        public string sItemName;
        public string sWorldName;

        public ItemFoundMSG()
        {
            bAdd = bManual = false;

            sItemName = sWorldName = "";
        }

        public ItemFoundMSG(string item, string world, bool add, bool manual)
        {
            sItemName = item;
            sWorldName = world;
            bAdd = add;
            bManual = manual;
        }

        //ItemNameLength ItemName WorldNameLength WorldName bAdd&bManual
        public byte[] ToByte()
        {
            byte[] ret;
            BitArray flags = new BitArray(new bool[] { bAdd, bManual });
            ItemIDs name = ItemIDs.Error;
            WorldIDs world = WorldIDs.Error;
            Enum.TryParse<ItemIDs>(sItemName, out name);
            Enum.TryParse<WorldIDs>(sWorldName, out world);
            ret = new byte[sizeof(ItemIDs) + sizeof(WorldIDs) + ((flags.Length - 1) / 8 + 1)];
            int offset = 0;

            ret[offset] = (byte)name;
            offset += sizeof(ItemIDs);

            ret[offset] = (byte)world;
            offset += sizeof(WorldIDs);

            flags.CopyTo(ret, offset);

            return ret;
        }

        public int FromByte(byte[] data, int offset)
        {
            sItemName = ((ItemIDs)data[offset]).ToString();
            offset += sizeof(ItemIDs);

            sWorldName = ((WorldIDs)data[offset]).ToString();
            offset += sizeof(WorldIDs);

            BitArray flags = new BitArray(new byte[] { data[offset] });
            offset += 1;

            bAdd = flags[0];
            bManual = flags[1];

            return offset;
        }
    }

    public class HintDataMSG
    {
        public string hintData;

        public HintDataMSG()
        {
            hintData = "";
        }

        public HintDataMSG(string data)
        {
            hintData = data;
        }

        public byte[] ToByte()
        {
            byte[] hintByte = Encoding.UTF8.GetBytes(hintData);
            byte[] ret = new byte[sizeof(int) + hintData.Length];

            int offset = 0;

            BitConverter.GetBytes(hintByte.Length).CopyTo(ret, offset);
            offset += sizeof(int);

            hintByte.CopyTo(ret, offset);
            offset += hintByte.Length;

            return ret;
        }

        public int FromByte(byte[] data, int offset)
        {
            int length = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            hintData = Encoding.UTF8.GetString(data, offset, length);
            offset += length;

            return offset;
        }
    }

    public class SyncMsg
    {
        public List<(ItemIDs, WorldIDs)> FoundItems;

        public SyncMsg()
        {
            FoundItems = new List<(ItemIDs, WorldIDs)>();
        }

        public byte[] ToByte()
        {
            byte[] ret = new byte[sizeof(int) + (FoundItems.Count * 2)];
            int offset = 0;

            BitConverter.GetBytes(FoundItems.Count).CopyTo(ret, offset);
            offset += sizeof(int);

            foreach ((ItemIDs, WorldIDs) i in FoundItems)
            {
                ret[offset] = (byte)i.Item1;
                ret[offset + 1] = (byte)i.Item2;
                offset += 2;
            }

            return ret;
        }

        public int FromByte(byte[] data, int offset)
        {
            FoundItems.Clear();

            int length = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            for (int i = 0; i < length; ++i)
            {
                FoundItems.Add(((ItemIDs)data[offset], (WorldIDs)data[offset + 1]));
                offset += 2;
            }

            return offset;
        }
    }

    public enum ClientState
    {
        Init,
        Connecting,
        Join,
        Running,
        Disconnected,
        Unknown,
        Last
    }

    public delegate int NetworkMessageHandler(int msgStart, byte[] msgData, int source);

    public partial class MainWindow : Window
    {
        private void networkUpdateItem(string itemName, string worldName, bool add, bool manual)
        {
            bool hasItem = false;
            foreach (Item child in MainWindow.data.WorldsData[worldName].worldGrid.Children)
            {
                if (child.Name.Equals(itemName))
                {
                    hasItem = true;
                    break;
                }
            }
            if (!hasItem)
            {
                data.WorldsData[worldName].worldGrid.Add_Ghost(Data.GhostItems["Ghost_" + itemName], null);

                if (GetGameMode().bSharedHints && itemName.Contains("Report"))
                {
                    Item item = null;
                    var grid = data.WorldsData[worldName].worldGrid;
                    MainWindow window = ((MainWindow)Application.Current.MainWindow);
                    foreach (Item i in data.Items)
                    {
                        if (i.Name == itemName)
                        {
                            item = i;
                            break;
                        }
                    }
                    if (item == null)
                        return;

                    switch (MainWindow.data.mode)
                    {
                        case Mode.DAHints:
                            grid.Handle_PointReport(item, window, data);
                            break;
                        case Mode.PathHints:
                            grid.Handle_PathReport(item, window, data);
                            break;
                        case Mode.SpoilerHints:
                            grid.Handle_SpoilerReport(item, window, data);
                            break;
                        default:
                            grid.Handle_Report(item, window, data);
                            break;
                    }
                }
            }
        }

        private GameMode GetGameMode()
        {
            if (Network.MP.IsClient())
                return mpClient.Mode;
            else
                return mpServer.Mode;
        }

        private int OnItemFound(int offset, byte[] msg, int source)
        {
            ItemFoundMSG itemMsg = new ItemFoundMSG();
            offset = itemMsg.FromByte(msg, offset);

            //update tracker
            networkUpdateItem(itemMsg.sItemName, itemMsg.sWorldName, itemMsg.bAdd, itemMsg.bManual);

            // route message to other clients
            if (!Network.MP.IsClient() && mpServer.Mode.bCoop)
            {
                mpServer.AddItem(itemMsg.sItemName, itemMsg.sWorldName);
                
                //build update message
                byte[] byteItem = itemMsg.ToByte();
                byte[] outMsg = new byte[sizeof(NetworkMessages) + byteItem.Length];
                outMsg[0] = (byte)NetworkMessages.ItemFound;
                byteItem.CopyTo(outMsg, 1);

                Network.MP.Broadcast(source, outMsg, outMsg.Length);
            }

            return offset;
        }

        private int OnInvalidMSG(int start, byte[] msg, int source)
        {
            return start;
        }
    }
}

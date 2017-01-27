using System.Collections.Generic;

namespace Rasa.Managers
{
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class CommunicatorManager
    {
        private static CommunicatorManager _instance;
        private static readonly object InstanceLock = new object();
        public static Dictionary<string, MapChannelClient> PlayersByName = new Dictionary<string, MapChannelClient>();
        public static Dictionary<uint, MapChannelClient> PlayersByEntityId = new Dictionary<uint, MapChannelClient>();
        public static Dictionary<int, ChatChannel> ChannelsBySeed = new Dictionary<int, ChatChannel>();

        public static CommunicatorManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CommunicatorManager();
                    }
                }

                return _instance;
            }
        }

        private CommunicatorManager()
        {
        }

        public void AddClientToChannel(MapChannelClient mapClient, int cHash)
        {
            ChatChannel chatChannel;
            if (ChannelsBySeed.TryGetValue(cHash, out chatChannel))
            {
                var newLink = new ChatChannelPlayerLink();
                newLink.Next = null;
                newLink.EntityId = mapClient.ClientEntityId;
                newLink.Previous = null;
                if (chatChannel.FirstPlayer != null)
                {
                    // append
                    var currentLink = new ChatChannelPlayerLink();
                    while (currentLink.Next != null)
                        currentLink = currentLink.Next;
                    newLink.Previous = currentLink;
                    currentLink.Next = newLink;
                }
                else
                {
                    // set as first
                    chatChannel.FirstPlayer = newLink;
                }
            }
        }

        public int GenerateDefaultChannelHash(int channelId, int mapContextId, int instanceId)
        {
            var v = 0;
            v = (channelId ^ (channelId << 7)) ^ mapContextId ^ (mapContextId * 121) ^ ((instanceId + instanceId * 13) << 3);
            return v;
        }

        public void JoinDefaultLocalChannel(MapChannelClient mapClient, int channelId)
        {
            if (mapClient.JoinedChannels >= 14)
                return; // todo, send error to client
            // generate channel hash
            var cHash = GenerateDefaultChannelHash(channelId, mapClient.MapChannel.MapInfo.MapId, 0);
            // find channel
            ChatChannel chatChannel;
            if (ChannelsBySeed.TryGetValue(cHash, out chatChannel))
            {                
            }
            else
            {
                // channel does not exist, create it
                chatChannel = new ChatChannel();
                chatChannel.Name[0] = '\0';
                chatChannel.InstanceId = 0;
                chatChannel.ChannelId = channelId;
                chatChannel.MapContextId = mapClient.MapChannel.MapInfo.MapId;
                chatChannel.IsDefaultChannel = true;
                chatChannel.FirstPlayer = null;
                // register it
                ChannelsBySeed.Add(cHash, chatChannel);
            }
            // add channel entry to player
            mapClient.ChannelHashes[mapClient.JoinedChannels] = cHash;
            mapClient.JoinedChannels++;
            // add client to channel
            AddClientToChannel(mapClient, cHash);
            mapClient.Client.SendPacket(8, new ChatChannelJoinedPacket { ChannelId = channelId, MapContextId = mapClient.MapChannel.MapInfo.MapId });            
        }

        public void LoginOk(MapChannelClient mapClient, MapChannel mapChannel)
        {
            // send LoginOk (despite the original description in the python files, this will only show 'You have arrived at ....' msg in chat)
            mapClient.Player.Client.SendPacket(8, new LoginOkPacket(mapClient.Player.Actor.Name) );
            // send MOTD ( Recv_SendMOTD - receives MOTDDict {languageId: text} )
            // SendMOTD = 770		// Displayed only if different
            // PreviewMOTD = 769	// Displayed always
            mapClient.Player.Client.SendPacket(8, new PreviewMOTDPacket("Welcome to the Infinite Rasa server."));
        }

        public void PlayerEnterMap(MapChannelClient mapClient)
        {
            JoinDefaultLocalChannel(mapClient, 1); // join general
        }

        public void PlayerExitMap(Client client)
        {            
            CharacterManager.UpdateCharacter(client.MapClient.Player, 5);
            // save player time
            CharacterManager.UpdateCharacter(client.MapClient.Player, 7);
            // remove client from all channels
            for (var i = 0; i < client.MapClient.JoinedChannels; i++)
            {
                var chatChannel = ChannelsBySeed[client.MapClient.ChannelHashes[i]];
                if (chatChannel != null)
                {
                    // remove client link from channel
                    var currentLink = chatChannel.FirstPlayer;
                    while (currentLink != null)
                    {
                        if (currentLink.EntityId == client.MapClient.ClientEntityId)
                        {
                            // do removing
                            if (currentLink.Previous == null)
                            {
                                chatChannel.FirstPlayer = currentLink.Next;
                                if (currentLink.Next != null)
                                    currentLink.Next.Previous = null;
                            }
                            else
                            {
                                currentLink.Previous.Next = currentLink.Next;
                                if (currentLink.Next != null)
                                    currentLink.Next.Previous = currentLink.Previous;
                            }
                            break;
                        }
                        // next
                        currentLink = currentLink.Next;
                    }
                }
            }
            client.MapClient.JoinedChannels = 0;
        }

        public void Recv_RadialChat(Client client, string textMsg)
        {
            // check if it's gm command
            if (textMsg[0] == '.')
            {
                // it's GM command, check if client is GM
                if (client.Entry.Level > 0)
                {
                    // Client is GM
                    ChatCommandsManager.Instance.ProcessCommand(client.MapClient, textMsg);
                    return;
                }
            } 
            if (client.MapClient.Player == null)
                return;
            // go through all players and send chat message ( can ignore sync because playerList will not change )
            var mapChannel = client.MapClient.MapChannel;
            for (var i = 0; i < mapChannel.PlayerList.Count; i++)
            {
                var tempClient = mapChannel.PlayerList[i];
                if (tempClient.Player != null)
                {
                    var distance = Position.Distance(client.MapClient.Player.Actor.Position, tempClient.Player.Actor.Position);
                    if (distance <= 70.0) // 70 is about the range the client is visible
                    client.SendPacket(8, new RadialChatPacket
                        {
                            FamilyName = client.MapClient.Player.Actor.FamilyName,
                            TextMsg = textMsg,
                            EntityId = client.MapClient.Player.Actor.EntityId
                        }
                    );
                }
            }
        }
            
        public void RegisterPlayer(MapChannelClient mapClient)
        {
            PlayersByName.Add(mapClient.Player.Actor.Name, mapClient);
            PlayersByEntityId.Add(mapClient.ClientEntityId, mapClient);
        }

        public void SystemMessage(MapChannelClient mapClient, string textMsg)
        {
            mapClient.Player.Client.SendPacket(8, new SystemMessagePacket { TextMessage = textMsg });
        }

        public void UnregisterPlayer(MapChannelClient mapClient)
        {
            //var upperCase = new char[mapClient.Player.Actor.Name.Length+1];
            if (mapClient.Player != null)
            {
                /*var from = mapClient.Player.Actor.Name;
                for (var i = 0; i < from.Length; i++)
                {
                    var c = from[i];
                    if (c == '\0')
                    {
                        upperCase[i] = '\0';
                        break;
                    }
                    if (c >= 'a' && c <= 'z')
                        c = Convert.ToChar(c.ToString().ToUpper());
                    upperCase[i] = c;
                }*/
                //upperCase[from.Length] = '\0';
                PlayersByName.Remove(mapClient.Player.Actor.Name);
                PlayersByEntityId.Remove(mapClient.ClientEntityId);
            }
        }
    }
}

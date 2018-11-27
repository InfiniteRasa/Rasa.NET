using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class CommunicatorManager
    {
        private static CommunicatorManager _instance;
        private static readonly object InstanceLock = new object();
        public static Dictionary<string, Client> PlayersByName = new Dictionary<string, Client>();
        public static Dictionary<uint, Client> PlayersByEntityId = new Dictionary<uint, Client>();
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

        public void AddClientToChannel(Client client, int cHash)
        {
            if (ChannelsBySeed.TryGetValue(cHash, out ChatChannel chatChannel))
            {
                var newLink = new ChatChannelPlayerLink
                {
                    Next = null,
                    EntityId = client.MapClient.Player.Actor.EntityId,
                    Previous = null
                };

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

        public void JoinDefaultLocalChannel(Client client, uint channelId)
        {
            if (client.MapClient.JoinedChannels >= 14)
                return; // todo, send error to client
            // generate channel hash
            var cHash = GenerateDefaultChannelHash((int)channelId, (int)client.MapClient.MapChannel.MapInfo.MapContextId, 0);
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
                chatChannel.MapContextId = client.MapClient.MapChannel.MapInfo.MapContextId;
                chatChannel.IsDefaultChannel = true;
                chatChannel.FirstPlayer = null;
                // register it
                ChannelsBySeed.Add(cHash, chatChannel);
            }
            // add channel entry to player
            client.MapClient.ChannelHashes[client.MapClient.JoinedChannels] = cHash;
            client.MapClient.JoinedChannels++;
            // add client to channel
            AddClientToChannel(client, cHash);
            client.CallMethod(SysEntity.CommunicatorId, new ChatChannelJoinedPacket(channelId, client.MapClient.MapChannel.MapInfo.MapContextId));
        }

        public void LoginOk(Client client)
        {
            // send LoginOk (despite the original description in the python files, this will only show 'You have arrived at ....' msg in chat)
            client.CallMethod(SysEntity.CommunicatorId, new LoginOkPacket(client.MapClient.Player.Actor.Name));
            // send MOTD ( Recv_SendMOTD - receives MOTDDict {languageId: text} )
            // SendMOTD = 770		// Displayed only if different
            // PreviewMOTD = 769	// Displayed always
            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket("Welcome to the Infinite Rasa server."));
        }

        public void PlayerEnterMap(Client client)
        {
            JoinDefaultLocalChannel(client, 1); // join general
        }

        public void PlayerExitMap(Client client)
        {            
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, null);
            // save player time
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Login, null);
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
                        if (currentLink.EntityId == client.MapClient.Player.Actor.EntityId)
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
                if (client.AccountEntry.Level > 0)
                {
                    // Client is GM
                    ChatCommandsManager.Instance.ProcessCommand(client, textMsg);
                    return;
                }
                else
                    Logger.WriteLog(LogType.Security, $"AccountId = {client.AccountEntry.Id} tryed to use GM Command = {textMsg}");
            } 
            if (client.MapClient.Player == null)
                return;
            // go through all players and send chat message ( can ignore sync because playerList will not change )
            var mapChannel = client.MapClient.MapChannel;
            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];
                if (tempClient.MapClient.Player != null)
                {
                    var distance = Position.Distance(client.MapClient.Player.Actor.Position, tempClient.MapClient.Player.Actor.Position);
                    if (distance <= 70.0) // 70 is about the range the client is visible
                    client.CallMethod(SysEntity.CommunicatorId, new RadialChatPacket
                        {
                            FamilyName = client.MapClient.Player.Actor.FamilyName,
                            TextMsg = textMsg,
                            EntityId = client.MapClient.Player.Actor.EntityId
                        }
                    );
                }
            }
        }
            
        public void RegisterPlayer(Client client)
        {
            PlayersByName.Add(client.MapClient.Player.Actor.Name, client);
            PlayersByEntityId.Add(client.MapClient.Player.Actor.EntityId, client);
        }

        public void SystemMessage(Client client, string textMsg)
        {
            client.CallMethod(SysEntity.CommunicatorId, new SystemMessagePacket { TextMessage = textMsg });
        }

        public void UnregisterPlayer(Client client)
        {
            //var upperCase = new char[mapClient.Player.Actor.Name.Length+1];
            if (client.MapClient.Player != null)
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
                PlayersByName.Remove(client.MapClient.Player.Actor.Name);
                PlayersByEntityId.Remove(client.MapClient.Player.Actor.EntityId);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class CommunicatorManager
    {
        public static Dictionary<string, MapChannelClient> PlayersByName = new Dictionary<string, MapChannelClient>();
        public static Dictionary<uint, MapChannelClient> PlayersByEntityId = new Dictionary<uint, MapChannelClient>();
        public static Dictionary<int, ChatChannel> ChannelsBySeed = new Dictionary<int, ChatChannel>();

        public static void AddClientToChannel(MapChannelClient mapClient, int cHash)
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

        public static int GenerateDefaultChannelHash(int channelId, int mapContextId, int instanceId)
        {
            var v = 0;
            v = (channelId ^ (channelId << 7)) ^ mapContextId ^ (mapContextId * 121) ^ ((instanceId + instanceId * 13) << 3);
            return v;
        }

        public static void JoinDefaultLocalChannel(MapChannelClient mapClient, int channelId)
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

        public static void PlayerEnterMap(MapChannelClient mapClient)
        {
            JoinDefaultLocalChannel(mapClient, 1); // join general

        }

        public static void PlayerExitMap(Client client)
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

        public static void Recv_RadialChat(Client client, string textMsg)
        {
            // check if it's gm command
            if (textMsg[0] == '?')
            {
                // it's GM command, check if client is GM
                client.SendPacket(8, new RadialChatPacket   // this msg is just for fun, implement later
                    {
                        FamilyName = client.MapClient.Player.Actor.FamilyName,
                        TextMsg = "It is Gm Command, dont play with it!!",
                        EntityId = client.MapClient.Player.Actor.EntityId
                    }
                );
                return;
                if (client.Entry.Level > 0)
                {
                    // Client is GM
                    //ParseGmCommand(cm, textMsg))
                    return;
                }
            }
            if (client.MapClient.Player == null)
                return;
            // go through all players and send chat message ( can ignore sync because playerList will not change )
            var senderX = client.MapClient.Player.Actor.PosX;
            var senderY = client.MapClient.Player.Actor.PosY;
            var senderZ = client.MapClient.Player.Actor.PosZ;
            var mapChannel = client.MapClient.MapChannel;
            for (var i = 0; i < mapChannel.PlayerCount; i++)
            {
                var tempClient = mapChannel.PlayerList[i];
                if (tempClient.Player != null)
                {
                    // calulate distance
                    var distX = tempClient.Player.Actor.PosX;
                    var distY = tempClient.Player.Actor.PosY;
                    var distZ = tempClient.Player.Actor.PosZ;
                    var distance = Math.Sqrt(Math.Pow(distX - senderX, 2) + Math.Pow(distY -senderY, 2) + Math.Pow(distZ - senderZ, 2));
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
            
        public static void RegisterPlayer(MapChannelClient mapClient)
        {
            /*var upperCase = new char[mapClient.Player.Actor.Name.Length+1];
            var from = mapClient.Player.Actor.Name;
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
            }
            upperCase[from.Length] = '\0';*/
            PlayersByName.Add(mapClient.Player.Actor.Name, mapClient);
            PlayersByEntityId.Add(mapClient.ClientEntityId, mapClient);
        }

        public static void UnregisterPlayer(MapChannelClient mapClient)
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

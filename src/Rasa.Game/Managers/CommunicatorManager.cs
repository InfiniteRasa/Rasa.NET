using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Both;
    using Packets.Communicator.Client;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class CommunicatorManager
    {
        /*      Communicator Packets:
         *      -- WorldMsg
         * - Who
         * - ChangeClanName
         * - ChallengeClanToFeud
         * - FeudChallengeResponse
         * - RevokeClanFeud
         * - SurrenderClanFeud
         * - SurrenderWargame
         *      -- UserMethod
         * - PrivilegedCommand
         * - Whisper
         * - PartyChat
         * - GuildChat
         * - Shout
         * - RadialChat
         * - ChannelChat
         * - Reply
         * - ClanLeadersChat
         * - ChangeLastName
         * - ChangeFirstName
         * - Emote
         *      -- ActorMethod
         * - RequestLOSReport
         * - ToggleAfk
         * - GotoMob
         * 
         *      Comunicator Handlers:
         * - AddFriendAck
         * - AddIgnoreAck
         * - AdminMessage
         * - ChatChannelJoined
         * - ChatChannelLeft
         * - DisplayClientMessage
         * - FriendList
         * - IgnoreList
         * - LoginOk
         * - PlayerCountAck
         * - PlayerLogin
         * - PlayerLogout
         * - PreviewMOTD
         * - RemoveFriendAck
         * - RemoveIgnoreAck
         * - SendMOTD
         * - SystemMessage
         * - WhisperAck
         * - WhisperFailAck
         * - WhisperSelf
         * - WhoAck
         * - WhoFailAck
         * 
         *      Client and server packets:
         * - RadialChat
         * - ChannelChat
         * - ClanChat
         * - ClanLeadersChat
         * - Emote
         * - PartyChat
         * - Radial
         * - Shout
         * - Whisper
         */

        private static CommunicatorManager _instance;
        private static readonly object InstanceLock = new object();
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

        #region Handlers

        internal void ClanChat(Client client, ClanChatPacket packet)
        {
            var clanMembers = Server.Clients.FindAll(c => c.Player.ClanId == packet.ClanId);
            
            foreach(var member in clanMembers)
                member.CallMethod(SysEntity.CommunicatorId, new ClanChatPacket(client.Player.FamilyName, packet.Message));
        }

        internal void ChannelChat(Client client, ChannelChatPacket packet)
        {
            client.CallMethod(SysEntity.CommunicatorId, new ChannelChatPacket(client.Player.FamilyName, packet.ChannelId, packet.MapEntityId, packet.MapContextId, packet.Message));
        }

        internal void Emote(Client client, EmotePacket packet)
        {
            client.CallMethod(SysEntity.CommunicatorId, new EmotePacket(client.Player.Name, packet.Emote));
            CellManager.Instance.CellCallMethod(client.Player.MapChannel, client.Player, new PerformWindupPacket(PerformType.TwoArgs, ActionId.Gesture, (uint)new Random().Next(1, 100)));   // ToDo: just testing
        }

        internal void Who(Client client, WhoPacket packet)
        {
            // msgId from playermessagelanguage.py
            var msgId = 7u;

            client.CallMethod(SysEntity.CommunicatorId, new WhoFailAckPacket(packet.FamilyName, msgId));
        }

        #endregion

        public void AddClientToChannel(Client client, int cHash)
        {
            if (ChannelsBySeed.TryGetValue(cHash, out ChatChannel chatChannel))
            {
                var newLink = new ChatChannelPlayerLink
                {
                    Next = null,
                    EntityId = client.Player.EntityId,
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

        internal void AddFriendAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddFriendAckPacket(familyName, succsess));
        }

        internal void AddIgnoreAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddIgnoreAckPacket(familyName, succsess));
        }

        public int GenerateDefaultChannelHash(int channelId, int mapContextId, int instanceId)
        {
            var v = 0;
            v = (channelId ^ (channelId << 7)) ^ mapContextId ^ (mapContextId * 121) ^ ((instanceId + instanceId * 13) << 3);
            return v;
        }

        public void JoinDefaultLocalChannel(Client client, uint channelId)
        {
            if (client.Player.JoinedChannels >= 14)
                return; // todo, send error to client
            // generate channel hash
            var cHash = GenerateDefaultChannelHash((int)channelId, (int)client.Player.MapChannel.MapInfo.MapContextId, 0);
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
                chatChannel.MapContextId = client.Player.MapChannel.MapInfo.MapContextId;
                chatChannel.IsDefaultChannel = true;
                chatChannel.FirstPlayer = null;
                // register it
                ChannelsBySeed.Add(cHash, chatChannel);
            }
            // add channel entry to player
            client.Player.ChannelHashes[client.Player.JoinedChannels] = cHash;
            client.Player.JoinedChannels++;
            // add client to channel
            AddClientToChannel(client, cHash);
            client.CallMethod(SysEntity.CommunicatorId, new ChatChannelJoinedPacket(channelId, client.Player.MapChannel.MapInfo.MapContextId, client.Player.EntityId));
        }

        public void LoginOk(Client client)
        {
            // send LoginOk (despite the original description in the python files, this will only show 'You have arrived at ....' msg in chat)
            client.CallMethod(SysEntity.CommunicatorId, new LoginOkPacket(client.Player.Name));
            // send MOTD ( Recv_SendMOTD - receives MOTDDict {languageId: text} )
            // SendMOTD = 770		// Displayed only if different
            // PreviewMOTD = 769	// Displayed always
            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket("Welcome to the Infinite Rasa server."));
        }

        public void PlayerEnterMap(Client client)
        {
            JoinDefaultLocalChannel(client, 1); // join general

            SocialManager.Instance.FriendLoggedIn(client);
        }

        public void PlayerExitMap(Client client)
        {            
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, null);
            // save player time
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Login, null);
            // remove client from all channels
            for (var i = 0; i < client.Player.JoinedChannels; i++)
            {
                var chatChannel = ChannelsBySeed[client.Player.ChannelHashes[i]];
                if (chatChannel != null)
                {
                    // remove client link from channel
                    var currentLink = chatChannel.FirstPlayer;
                    while (currentLink != null)
                    {
                        if (currentLink.EntityId == client.Player.EntityId)
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

            client.Player.JoinedChannels = 0;

            SocialManager.Instance.FriendLoggedOut(client);
        }

        public void RadialChat(Client client, string textMsg)
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
            if (client.Player == null)
                return;
            // go through all players and send chat message ( can ignore sync because playerList will not change )
            var mapChannel = client.Player.MapChannel;
            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];
                if (tempClient.Player != null)
                {
                    var distance = Vector3.Distance(client.Player.Position, tempClient.Player.Position);
                    if (distance <= 70.0) // 70 is about the range the client is visible
                    client.CallMethod(SysEntity.CommunicatorId, new RadialChatPacket
                        {
                            FamilyName = client.Player.FamilyName,
                            TextMsg = textMsg,
                            EntityId = client.Player.EntityId
                        }
                    );
                }
            }
        }

        public void SystemMessage(Client client, string textMsg)
        {
            client.CallMethod(SysEntity.CommunicatorId, new SystemMessagePacket(textMsg));
        }
    }
}

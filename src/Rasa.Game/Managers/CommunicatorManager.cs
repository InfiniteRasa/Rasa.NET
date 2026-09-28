using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Both;
    using Packets.Communicator.Client;
    using Packets.ClientMethod.Server;
    using Packets.Protocol;
    using Packets.Clan.Server;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Rasa.Models;
    using Structures;

    public class CommunicatorManager
    {
        /*      Communicator Packets:
         *      -- WorldMsg
         * - Who                                => implemented
         * - ChangeClanName
         * - ChallengeClanToFeud
         * - FeudChallengeResponse
         * - RevokeClanFeud
         * - SurrenderClanFeud
         * - SurrenderWargame
         *      -- UserMethod
         * - PrivilegedCommand
         * - Whisper                            => implemented
         * - PartyChat
         * - GuildChat
         * - Shout
         * - RadialChat
         * - ChannelChat
         * - Reply                              => implemented
         * - ClanLeadersChat
         * - ChangeLastName
         * - ChangeFirstName
         * - Emote                              => implemented
         *      -- ActorMethod
         * - RequestLOSReport
         * - ToggleAfk                          => implemented (ManifestationManager)
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
         * - PlayerCountAck                     => implemented (Who result count)
         * - PlayerLogin
         * - PlayerLogout
         * - PreviewMOTD
         * - RemoveFriendAck
         * - RemoveIgnoreAck
         * - SendMOTD
         * - SystemMessage
         * - WhisperAck                         => implemented
         * - WhisperFailAck                     => implemented
         * - WhisperSelf                        => implemented
         * - WhoAck                             => implemented
         * - WhoFailAck                         => implemented
         * 
         *      Client and server packets:
         * - RadialChat
         * - ChannelChat
         * - ClanChat
         * - ClanLeadersChat
         * - Emote                              => implemented
         * - PartyChat
         * - Radial
         * - Shout                              => implemented
         * - Whisper                            => implemented
         */

        private static CommunicatorManager _instance;
        private static readonly object InstanceLock = new object();
        public static Dictionary<int, ChatChannel> ChannelsBySeed = new Dictionary<int, ChatChannel>();

        /// <summary>Manifestation.ChannelHashes is this long; joining past it would run off the end.</summary>
        public const int ChannelHashesPerPlayer = 14;

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
            // The clan id in the packet is the client's word; the sender's clan is the server's.
            // The broadcast used to go to whatever id the packet named, so a modified client could
            // post into any clan's chat, and with id 0 - no clan - reach every connection that is
            // not in one, including those still at character selection.
            var clanId = client.Player.ClanId;

            if (clanId == 0 || packet.ClanId != clanId)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} sent clan chat for clan {packet.ClanId} while in clan {clanId}.");
                return;
            }

            if (string.IsNullOrEmpty(packet.Message))
                return;

            var clanMembers = Server.Clients.FindAll(c => c.State == ClientState.Ingame && c.Player.ClanId == clanId);

            foreach(var member in clanMembers)
                member.CallMethod(SysEntity.CommunicatorId, new ClanChatPacket(client.Player.FamilyName, packet.Message));
        }

        /// <summary>
        /// /cl and /clanleader: the clan's officer channel, which reaches the ranks that run the
        /// clan rather than all of it.
        ///
        /// The client checks the sender's rank before it will send, and refuses with
        /// PmClanInsufficientLeaderChannelPermission - but that is the sender's own copy of their
        /// rank, and a demotion they have not been told about yet would still let it through.
        /// The rank that decides is the one in the clan roster here. The same check picks the
        /// recipients: the point of the channel is that the rest of the clan cannot read it.
        /// </summary>
        internal void ClanLeadersChat(Client client, ClanLeadersChatPacket packet)
        {
            // The clan id in the packet is the client's word; the sender's clan is the server's.
            var clanId = client.Player.ClanId;

            if (clanId == 0 || packet.ClanId != clanId)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} sent clan leaders chat for clan {packet.ClanId} while in clan {clanId}.");
                return;
            }

            if (string.IsNullOrEmpty(packet.Message))
                return;

            var sender = ClanManager.Instance.GetClanMember(clanId, client.Player.Id);

            if (sender == null || sender.Rank < ClanRank.MinRankToSpeakInLeadersChannel)
            {
                client.CallMethod(SysEntity.ClientClanManagerId,
                    new DisplayClanMessagePacket((int)PlayerMessage.PmClanInsufficientLeaderChannelPermission, new Dictionary<string, string>()));
                return;
            }

            foreach (var member in Server.Clients.FindAll(c => c.State == ClientState.Ingame && c.Player.ClanId == clanId))
            {
                var listener = ClanManager.Instance.GetClanMember(clanId, member.Player.Id);

                if (listener == null || listener.Rank < ClanRank.MinRankToSpeakInLeadersChannel)
                    continue;

                member.CallMethod(SysEntity.CommunicatorId, new ClanLeadersChatPacket(client.Player.FamilyName, packet.Message));
            }
        }

        /// <summary>
        /// A message on one of the numbered channels - /t is MAP_TRADE. This used to call back
        /// only the sender, so a player saw their own message and nobody else did.
        ///
        /// The channel is resolved from the server's own record of where the player is rather
        /// than from the ids in the packet: a client is free to name any channel and map, and
        /// following it would let someone talk on a channel they never joined, on a map they are
        /// not standing on.
        /// </summary>
        internal void ChannelChat(Client client, ChannelChatPacket packet)
        {
            if (client.Player == null || string.IsNullOrEmpty(packet.Message))
                return;

            var chatChannel = ChannelOf(client, packet.ChannelId);

            if (chatChannel == null)
            {
                Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} sent chat on channel {packet.ChannelId}, which they are not in.");
                return;
            }

            var outgoing = new ChannelChatPacket(client.Player.FamilyName, chatChannel.ChannelId,
                client.Player.EntityId, chatChannel.MapContextId, packet.Message);

            foreach (var entityId in chatChannel.Players)
            {
                var listener = Server.Clients.Find(c => c?.Player != null && c.Player.EntityId == entityId
                                                        && c.State == ClientState.Ingame);

                if (listener == null)
                    continue;

                // A player who has this one ignored does not hear them, the same rule the other
                // chat paths follow.
                if (listener != client && listener.Player.IgnoredPlayers.Contains(client.AccountEntry.Id))
                    continue;

                listener.CallMethod(SysEntity.CommunicatorId, outgoing);
            }
        }

        /// <summary>
        /// The channel this player is in with that id, or null when they are not in one. Looked up
        /// through the hashes recorded when they joined, so it cannot name a channel they never
        /// entered.
        /// </summary>
        private static ChatChannel ChannelOf(Client client, uint channelId)
        {
            for (var i = 0; i < client.Player.JoinedChannels; i++)
                if (ChannelsBySeed.TryGetValue(client.Player.ChannelHashes[i], out var chatChannel)
                    && chatChannel.ChannelId == channelId)
                    return chatChannel;

            return null;
        }

        internal void Emote(Client client, EmotePacket packet)
        {
            if (client.Player == null)
                return;

            // The client files this under RADIAL_EMOTE, so it is local chat like RadialChat
            // rather than something wider. Recv_Emote renders link(sender) + " " + msg, and
            // sender is the same string the rest of the chat system uses - FamilyName, which
            // is what Whisper and Reply resolve a target by.
            var mapChannel = client.Player.MapChannel;

            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];

                if (tempClient.Player == null)
                    continue;

                if (Vector3.Distance(client.Player.Position, tempClient.Player.Position) <= RadialRange)
                    tempClient.CallMethod(SysEntity.CommunicatorId, new EmotePacket(client.Player.FamilyName, packet.Emote));
            }
        }

        internal void Reply(Client client, ReplyPacket packet)
        {
            // /r carries the name from the last Recv_Whisper, which is the family name this
            // server sent as the sender, so it resolves exactly like /w.
            DeliverWhisper(client, packet.Reciver, packet.Message);
        }

        internal void PartyChat(Client client, PartyChatPacket packet)
        {
            var party = PartyManager.Instance.PartyOf(client);

            if (party == null)
            {
                client.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmActionFailedNoParty, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            // Recv_PartyChat(sender, msg, senderUserId, senderEntityId): senderUserId is compared
            // with the party leader's userId and GetCurrentUserId() to pick the leader colour, and
            // senderEntityId places the chat bubble. Members whose spot is held are skipped.
            foreach (var partyMember in party.Members)
            {
                if (!partyMember.IsOnline)
                    continue;

                var tempClient = Server.Clients.Find(c =>
                    c.State == ClientState.Ingame && c.AccountEntry != null && c.AccountEntry.Id == partyMember.UserId);

                tempClient?.CallMethod(SysEntity.CommunicatorId, new PartyChatPacket
                {
                    Sender = client.Player.FamilyName,
                    Message = packet.Message,
                    SenderUserId = client.AccountEntry.Id,
                    SenderEntityId = client.Player.EntityId
                });
            }
        }

        internal void Whisper(Client client, WhisperPacket packet)
        {
            DeliverWhisper(client, packet.Reciver, packet.Message);
        }

        /// <summary>
        /// Shared by Whisper and Reply. The sender used to be sent a Whisper from themselves,
        /// which the client prints as an incoming message, bubbles over the sender's own head,
        /// and pushes onto g_replyTo - so /r afterwards targeted yourself. An unknown or offline
        /// target threw on the null receiver and disconnected the sender. WhisperAck,
        /// WhisperFailAck and WhisperSelf were never sent at all.
        /// </summary>
        private void DeliverWhisper(Client sender, string targetName, string message)
        {
            var name = targetName?.Trim() ?? string.Empty;

            if (name.Length > 0 && string.Equals(name, sender.Player.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                sender.CallMethod(SysEntity.CommunicatorId, new WhisperSelfPacket(message));
                return;
            }

            // Family names are unique; typed names should not have to match their case.
            var target = name.Length == 0
                ? null
                : Server.Clients.Find(c =>
                    c.State == ClientState.Ingame &&
                    string.Equals(c.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                sender.CallMethod(SysEntity.CommunicatorId,
                    new WhisperFailAckPacket(name, PlayerMessage.PmWhisperTargetNotInGame));
                return;
            }

            // Ignore lists hold account ids, loaded by SocialManager.SetSocialContactList.
            if (target.Player.IgnoredPlayers.Contains(sender.AccountEntry.Id))
            {
                sender.CallMethod(SysEntity.CommunicatorId,
                    new WhisperFailAckPacket(target.Player.FamilyName, PlayerMessage.PmWhisperTargetIgnoringYou));
                return;
            }

            target.CallMethod(SysEntity.CommunicatorId, new WhisperPacket
            {
                Sender = sender.Player.FamilyName,
                Message = message,
                SenderEntityId = sender.Player.EntityId
            });

            // The target's canonical family name, not what was typed, so the sender sees the
            // name the way the target's family is actually spelled.
            sender.CallMethod(SysEntity.CommunicatorId,
                new WhisperAckPacket(target.Player.FamilyName, message, target.Player.IsAFK));
        }

        /// <summary>
        /// Most results the server will report for one /who. The client prints a line per
        /// result into the chat window, so an unbounded list on a busy server would flood it.
        /// </summary>
        private const int WhoResultLimit = 50;

        internal void Who(Client client, WhoPacket packet)
        {
            var search = (packet.SearchText ?? string.Empty).Trim();

            var matches = new List<Client>();

            foreach (var candidate in Server.Clients)
            {
                if (candidate.State != ClientState.Ingame || candidate.Player == null)
                    continue;

                // No argument lists everyone online; otherwise match either name, since
                // /who, /lookup and /whois all send free text rather than a specific field.
                if (search.Length > 0
                    && (candidate.Player.Name == null
                        || candidate.Player.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    && (candidate.Player.FamilyName == null
                        || candidate.Player.FamilyName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                matches.Add(candidate);

                if (matches.Count >= WhoResultLimit)
                    break;
            }

            if (matches.Count == 0)
            {
                // Recv_WhoFailAck fills only {'player': charName}. PmNoSuchUser's text uses
                // %(name)s, so it printed a translation-substitution error instead.
                // PmWhisperTargetNotInGame is "%(player)s could not be found."
                client.CallMethod(SysEntity.CommunicatorId,
                    new WhoFailAckPacket(search, (uint)PlayerMessage.PmWhisperTargetNotInGame));

                return;
            }

            foreach (var match in matches)
                client.CallMethod(SysEntity.CommunicatorId, new WhoAckPacket
                {
                    CharacterName = match.Player.Name,
                    FamilyName = match.Player.FamilyName,
                    ClanName = match.Player.ClanName,
                    // The client omits the title when this is None, and titledata has no
                    // entry for 0, which is what an untitled character carries.
                    TitleId = match.Player.CurrentTitle == 0 ? null : match.Player.CurrentTitle,
                    CharacterClass = match.Player.Class,
                    Level = match.Player.Level,
                    ContextId = match.Player.MapContextId,
                    // One instance per map today, so there is no ordinal to disambiguate;
                    // sending a value would make the client render "MapName(1)".
                    CurrentGameContextOrdinal = null,
                    IsAfk = match.Player.IsAFK,
                    IsTrialAccount = match.Player.IsTrialAccount
                });

            client.CallMethod(SysEntity.CommunicatorId,
                new PlayerCountAckPacket((uint)PlayerMessage.PmWhoCount, (uint)matches.Count));
        }

        #endregion

        /// <summary>Puts a player in a channel, once.</summary>
        public void AddClientToChannel(Client client, int cHash)
        {
            if (!ChannelsBySeed.TryGetValue(cHash, out var chatChannel))
                return;

            if (!chatChannel.Players.Contains(client.Player.EntityId))
                chatChannel.Players.Add(client.Player.EntityId);
        }

        internal void AddFriendAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddFriendAckPacket(familyName, succsess));
        }

        internal void AddIgnoreAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddIgnoreAckPacket(familyName, succsess));
        }

        /// <summary>PM_REMOVED_FROM_FRIEND_LIST or PM_FAILED_FRIEND_REMOVE, by success.</summary>
        internal void RemoveFriendAck(Client client, string familyName, bool success)
        {
            client.CallMethod(SysEntity.CommunicatorId, new RemoveFriendAckPacket(familyName, success));
        }

        /// <summary>PM_REMOVED_FROM_IGNORE_LIST or PM_FAILED_IGNORE_REMOVE, by success.</summary>
        internal void RemoveIgnoreAck(Client client, string familyName, bool success)
        {
            client.CallMethod(SysEntity.CommunicatorId, new RemoveIgnoreAckPacket(familyName, success));
        }

        public int GenerateDefaultChannelHash(int channelId, int mapContextId, int instanceId)
        {
            var v = 0;
            v = (channelId ^ (channelId << 7)) ^ mapContextId ^ (mapContextId * 121) ^ ((instanceId + instanceId * 13) << 3);
            return v;
        }

        /// <summary>
        /// The channels a player is put in when they arrive on a map. The client's own
        /// chatchannel table sorts these into global ones and per-map ones, and /t is MAP_TRADE,
        /// which is why it did nothing before: only GENERAL was ever joined, so the client's
        /// IsValidChannelId said the player was not in channel 6 and refused to send.
        ///
        /// NEW_PLAYER and the language and trial channels are left out - there is nothing here to
        /// decide who belongs in them - and clan chat has its own path.
        /// </summary>
        public static readonly uint[] GlobalChannels = { ChatChannelId.General, ChatChannelId.LookingForGroup };

        public static readonly uint[] MapChannels = { ChatChannelId.MapGeneral, ChatChannelId.MapTrade, ChatChannelId.MapDefense };

        /// <summary>
        /// A channel that spans the world. Hashed with map context zero so every map resolves to
        /// the same channel, which is what makes it global.
        /// </summary>
        public void JoinGlobalChannel(Client client, uint channelId)
        {
            Join(client, channelId, 0);
        }

        /// <summary>A channel that exists separately on each map: local chat, trade, defense.</summary>
        public void JoinDefaultLocalChannel(Client client, uint channelId)
        {
            Join(client, channelId, client.Player.MapChannel.MapInfo.MapContextId);
        }

        private void Join(Client client, uint channelId, uint mapContextId)
        {
            if (client.Player.JoinedChannels >= ChannelHashesPerPlayer)
            {
                Logger.WriteLog(LogType.Error, $"Character {client.Player.Id} is already in {client.Player.JoinedChannels} channels; {channelId} not joined.");
                return;
            }

            var cHash = GenerateDefaultChannelHash((int)channelId, (int)mapContextId, 0);

            if (!ChannelsBySeed.TryGetValue(cHash, out var chatChannel))
            {
                chatChannel = new ChatChannel
                {
                    InstanceId = 0,
                    ChannelId = channelId,
                    MapContextId = mapContextId,
                    IsDefaultChannel = true
                };
                chatChannel.Name[0] = '\0';

                ChannelsBySeed.Add(cHash, chatChannel);
            }

            client.Player.ChannelHashes[client.Player.JoinedChannels] = cHash;
            client.Player.JoinedChannels++;

            AddClientToChannel(client, cHash);

            client.CallMethod(SysEntity.CommunicatorId,
                new ChatChannelJoinedPacket(channelId, mapContextId, client.Player.EntityId));
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
            foreach (var channelId in GlobalChannels)
                JoinGlobalChannel(client, channelId);

            foreach (var channelId in MapChannels)
                JoinDefaultLocalChannel(client, channelId);

            SocialManager.Instance.FriendLoggedIn(client);
        }

        public void PlayerExitMap(Client client)
        {            
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, null);
            // save player time
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Login, null);

            LeaveMapChannels(client);

            if (client.AccountEntry != null)
                SocialManager.Instance.FriendLoggedOut(client);
        }

        /// <summary>
        /// Takes the player out of every chat channel it joined. The default channels are per
        /// map, so this runs when the player leaves a map for any reason; a dropship arrival
        /// used to join the new map's channel without leaving the old one, and after fourteen
        /// trips JoinDefaultLocalChannel refused.
        /// </summary>
        public void LeaveMapChannels(Client client)
        {
            for (var i = 0; i < client.Player.JoinedChannels; i++)
                if (ChannelsBySeed.TryGetValue(client.Player.ChannelHashes[i], out var chatChannel))
                    chatChannel.Players.Remove(client.Player.EntityId);

            client.Player.JoinedChannels = 0;
        }

        public void RadialChat(Client client, string textMsg)
        {
            // A leading dot is a command, whoever typed it. ProcessCommand decides whether this
            // account has the level for that particular one - this used to hold a single check
            // for all of them, which is why every command needed the same rank. Either way the
            // message is never broadcast: a mistyped command should not land in local chat.
            if (textMsg[0] == '.')
            {
                ChatCommandsManager.Instance.ProcessCommand(client, textMsg);
                return;
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
                    if (distance <= RadialRange)
                        tempClient.CallMethod(SysEntity.CommunicatorId, new RadialChatPacket
                        {
                            FamilyName = client.Player.FamilyName,
                            TextMsg = textMsg,
                            EntityId = client.Player.EntityId
                        }
                    );
                }
            }
        }

        /// <summary>
        /// How far a shout carries, in world units. RadialChat uses 70, "about the range the
        /// client is visible". Shout is deliberately wider: the client renders it in the chat
        /// window only, with no overhead bubble and no sender entity id, so the shouter does
        /// not need to be visible to the listener. The client ships no chat-range table, so
        /// this figure is ours to pick - raise it toward map size if a shout should reach the
        /// whole zone.
        /// </summary>
        private const float ShoutRange = 150.0f;

        /// <summary>
        /// Range of the local chat types (RadialChat, Emote). "70 is about the range the
        /// client is visible", per the original comment on RadialChat.
        /// </summary>
        private const float RadialRange = 70.0f;

        public void Shout(Client client, string textMsg)
        {
            if (client.Player == null)
                return;

            // Same iteration style as RadialChat: the map channel's client list does not
            // change while we are on the main loop, so no extra synchronisation is needed.
            var mapChannel = client.Player.MapChannel;

            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];

                if (tempClient.Player == null)
                    continue;

                if (Vector3.Distance(client.Player.Position, tempClient.Player.Position) <= ShoutRange)
                    tempClient.CallMethod(SysEntity.CommunicatorId, new ShoutPacket
                    {
                        FamilyName = client.Player.FamilyName,
                        TextMsg = textMsg
                    });
            }
        }

        public void SystemMessage(Client client, string textMsg)
        {
            client.CallMethod(SysEntity.CommunicatorId, new SystemMessagePacket(textMsg));
        }

        #region Player messages and notifications

        /// <summary>
        /// A player message, presented however that message is meant to be presented. Unlike
        /// DisplayClientMessage, which always prints a chat line, the client decides from the id
        /// whether to speak it, play a sound, throw it across the screen or print it - see
        /// DisplaySystemMessagePacket.
        /// </summary>
        public void DisplaySystemMessage(Client client, PlayerMessage message,
            Dictionary<string, string> args = null, MsgFilterId filterId = MsgFilterId.GeneralSystemMessages)
        {
            client.CallMethod(SysEntity.ClientMethodId, new DisplaySystemMessagePacket(message, args, filterId));
        }

        /// <summary>
        /// A player message put somewhere specific: Big across the middle of the screen,
        /// Destination on the sub-region strip.
        /// </summary>
        public void DisplayPlayerNotification(Client client, PlayerNotificationType type, PlayerMessage message,
            Dictionary<string, string> args = null)
        {
            client.CallMethod(SysEntity.ClientMethodId, new DisplayPlayerNotificationPacket(type, message, args));
        }

        /// <summary>Raises one of the client's own tutorial popups.</summary>
        public void DisplayPlayerTutorial(Client client, TutorialId tutorial)
        {
            client.CallMethod(SysEntity.ClientMethodId, new DisplayPlayerTutorialNotificationPacket(tutorial));
        }

        /// <summary>
        /// Plays a tutorial voice-over on its own - no window, no text. Null stops whatever is
        /// playing instead, which is the only way to cut one short.
        ///
        /// Raising a tutorial does not play its audio: the audioSetId column is None in every
        /// tutorialdata row the client shipped with, so a tutorial with a voice-over is this
        /// call alongside DisplayPlayerTutorial, not either on its own.
        /// </summary>
        public void PlayTutorialAudio(Client client, uint? audioSetId)
        {
            client.CallMethod(SysEntity.ClientMethodId, new PlayTutorialAudioPacket(audioSetId));
        }

        /// <summary>Stops the tutorial voice-over this player is hearing, if any.</summary>
        public void StopTutorialAudio(Client client)
        {
            PlayTutorialAudio(client, null);
        }

        /// <summary>
        /// The same notification to everyone in the player's visibility range, the player
        /// included - a region announcement, or big text for something that happened where
        /// several people can see it.
        /// </summary>
        public void NotifyCells(Client client, PlayerNotificationType type, PlayerMessage message,
            Dictionary<string, string> args = null)
        {
            // Client.CellCallMethod rather than CellManager's: that one addresses the packet to
            // the origin entity, and these all belong to the client-method entity.
            client.CellCallMethod(client, (ulong)SysEntity.ClientMethodId,
                new DisplayPlayerNotificationPacket(type, message, args));
        }

        #endregion

        #region Error dialogs

        /// <summary>
        /// A modal error the player can dismiss and carry on from. Use it only when the message
        /// has to be acknowledged - while the dialog is up the rest of the client's UI is
        /// suppressed - and the chat window for everything else.
        /// </summary>
        public void NonFatalError(Client client, PlayerMessage message, Dictionary<string, string> args = null)
        {
            client.CallMethod(SysEntity.ClientMethodId, new NonFatalErrorPacket(message, args));
        }

        /// <summary>
        /// The last thing a player is told. The client's OK button on this dialog calls
        /// PostQuitRequest, so it ends the session; send it only when the connection is going away
        /// regardless, to replace a silent drop with a reason.
        ///
        /// Sent straight down the socket rather than through the packet queue. The queue is drained
        /// by the MainLoop and Client.Close() closes the socket where it stands, so a queued fatal
        /// error would be thrown away by the very disconnect it is explaining. Callers should send
        /// this and then close.
        /// </summary>
        public void FatalError(Client client, PlayerMessage message, Dictionary<string, string> args = null)
        {
            if (client == null || client.State == ClientState.Disconnected)
                return;

            try
            {
                client.SendMessage(new CallMethodMessage((ulong)SysEntity.ClientMethodId,
                    new FatalErrorPacket(message, args)), false, 0, false);
            }
            catch (Exception e)
            {
                // The socket is already going; the disconnect it was explaining still happens.
                Logger.WriteLog(LogType.Network, $"Could not deliver a fatal error to a closing connection: {e.Message}");
            }
        }

        /// <summary>
        /// /gotomob &lt;name&gt; - put the caller next to the nearest creature of that name on this map.
        ///
        /// The client does the naming half: communicator.GotoMob walks its own creature name
        /// table, sends the name id that matches what was typed exactly, and a list of the ones
        /// that contain it as a whole word. So the server never sees the text as a name, only
        /// ids to look for among the creatures actually standing on the map.
        /// </summary>
        public void GotoMob(Client client, GotoMobPacket packet)
        {
            // The client only offers the command to a GM, but the client does not get to decide
            // that: this is a teleport, and the packet can be sent by anything.
            if (client?.AccountEntry == null || client.AccountEntry.Level < (byte)GmLevel.GameMaster)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client?.AccountEntry?.Id} (level {client?.AccountEntry?.Level}) sent GotoMob, which needs {(byte)GmLevel.GameMaster}");

                SystemMessage(client, "Unknown command.");
                return;
            }

            var mapChannel = client.Player?.MapChannel;

            if (mapChannel == null)
                return;

            var from = client.Player.Position;

            // An exact name beats every partial one outright, however far away it is: someone
            // who typed the whole name meant that creature. Only if none is on the map do the
            // partial matches get a turn, and then it is whichever is nearest.
            var found = packet.ExactMobNameId.HasValue
                ? NearestCreature(mapChannel, from, id => id == packet.ExactMobNameId.Value)
                : null;

            found ??= packet.PartialMobNameIds.Count > 0
                ? NearestCreature(mapChannel, from, id => packet.PartialMobNameIds.Contains(id))
                : null;

            if (found == null)
            {
                SystemMessage(client, $"Nothing called \"{packet.ArgString}\" is on this map right now.");
                return;
            }

            // Beside it rather than inside it. Two metres back along the line the caller came
            // from, or just to one side when they are already standing on top of it.
            var offset = from - found.Position;

            offset = offset.Length() > 0.1f
                ? Vector3.Normalize(offset) * 2f
                : new Vector3(2f, 0f, 0f);

            var destination = found.Position + offset;

            client.MoveObject(client.Player.EntityId, new Movement(destination, client.Movement?.ViewDirection ?? new Vector2(0f, 0f)));

            SystemMessage(client,
                $"{found.Name} ({found.EntityId}), {Vector3.Distance(from, found.Position):0} m away.");
        }

        private static Creature NearestCreature(MapChannel mapChannel, Vector3 from, Func<uint, bool> wanted)
        {
            Creature nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                foreach (var creature in cell.CreatureList)
                {
                    if (creature == null || !wanted(creature.NameId))
                        continue;

                    // A corpse is not somewhere to be sent; it is about to stop existing.
                    if (creature.State == CharacterState.Dead
                        || (creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0))
                        continue;

                    var distance = Vector3.Distance(from, creature.Position);

                    if (distance >= nearestDistance)
                        continue;

                    nearest = creature;
                    nearestDistance = distance;
                }

            return nearest;
        }

        #endregion
    }
}

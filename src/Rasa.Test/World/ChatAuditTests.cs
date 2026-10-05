extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Both;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.Char.ChatLog;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// The chat log (ChatAudit, the chat_log table): every line of chat a player sends is on it,
    /// with who said it, where, to whom, how many others were sent it and what came of it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ChatAuditTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>The log kept in memory: the rows a store was given, as they were given.</summary>
        private sealed class MemoryStore : ChatAudit.IStore
        {
            private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            public List<ChatLogEntry> Rows { get; } = new List<ChatLogEntry>();

            /// <summary>Set, every call throws it.</summary>
            public Exception Fails { get; set; }

            public uint Add(ChatLogEntry entry)
            {
                if (Fails != null)
                    throw Fails;

                var copy = (ChatLogEntry)Clone.Invoke(entry, null);

                copy.Id = (uint)Rows.Count + 1;
                Rows.Add(copy);

                return copy.Id;
            }
        }

        private readonly List<Client> _clients = new List<Client>();
        private readonly List<uint> _parties = new List<uint>();
        private readonly List<uint> _clans = new List<uint>();
        private MemoryStore _store;
        private ChatAudit.IStore _previousStore;
        private Func<DateTime> _previousClock;
        private DateTime _utc = Noon;
        private uint _nextAccount = 9700;

        [TestInitialize]
        public void KeepTheLogInMemory()
        {
            _previousStore = ChatAudit.Instance.Store;
            _previousClock = ChatAudit.Instance.UtcNow;
            _store = new MemoryStore();
            ChatAudit.Instance.Load(_store);
            ChatAudit.Instance.UtcNow = () => _utc;
        }

        [TestCleanup]
        public void RestoreTheLog()
        {
            ChatAudit.Instance.Load(_previousStore);
            ChatAudit.Instance.UtcNow = _previousClock;

            foreach (var client in _clients)
            {
                CommunicatorManager.Instance.LeaveMapChannels(client);

                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }

            foreach (var id in _parties)
                PartyManager.Instance.Parties.Remove(id);

            foreach (var id in _clans)
            {
                ClanManager.Instance.Clans.TryRemove(id, out _);
                ClanManager.Instance.ClanMembers.TryRemove(id, out _);
            }
        }

        #region Said, shouted, emoted

        [TestMethod]
        public void WhatIsSaidIsOnTheLogWithWhoSaidItWhereAndHowManyHeard()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var speaker = Player(world, 12, 34);
            var near = Player(world, 20, 34);
            var deaf = Player(world, 22, 34);
            var far = Player(world, 12 + 100, 34);

            speaker.AccountEntry.Level = (byte)GmLevel.GameMaster;
            deaf.Player.IgnoredPlayers.Add(speaker.AccountEntry.Id);

            chat.RadialChat(speaker, "hello there");

            Assert.AreEqual("hello there", Sent<RadialChatPacket>(near).Single().TextMsg, "it is said as it was");

            var row = _store.Rows.Single();

            Assert.AreEqual(Noon, row.CreatedAt);
            Assert.AreEqual((byte)ChatLogKind.Say, row.Kind);
            Assert.AreEqual((byte)ChatLogResult.Delivered, row.Result);
            Assert.AreEqual(speaker.AccountEntry.Id, row.AccountId);
            Assert.AreEqual((byte)GmLevel.GameMaster, row.AccountLevel);
            Assert.AreEqual(speaker.Player.Id, row.CharacterId);
            Assert.AreEqual(speaker.Player.Name, row.Name);
            Assert.AreEqual(speaker.Player.FamilyName, row.FamilyName);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, row.MapContextId);
            Assert.AreEqual(world.Map.InstanceId, row.InstanceId);
            Assert.AreEqual((12d, 0d, 34d), (row.CoordX, row.CoordY, row.CoordZ));
            Assert.AreEqual("hello there", row.Text);
            Assert.AreEqual(1u, row.HeardBy, "the one near: not the speaker, the one ignoring them, or the one out of range");
            Assert.AreEqual((0u, 0u, 0u, ""), (row.GroupId, row.TargetAccountId, row.TargetCharacterId, row.Target));

            // A shout carries further; an emote is local.
            _utc = Noon.AddSeconds(5);
            chat.Shout(speaker, "OVER HERE");
            chat.Emote(speaker, new EmotePacket { Emote = "waves at nobody" });

            Assert.HasCount(3, _store.Rows);
            Assert.AreEqual(((byte)ChatLogKind.Shout, "OVER HERE", 2u, Noon.AddSeconds(5)), (_store.Rows[1].Kind, _store.Rows[1].Text, _store.Rows[1].HeardBy, _store.Rows[1].CreatedAt));
            Assert.AreEqual(((byte)ChatLogKind.Emote, "waves at nobody", 1u), (_store.Rows[2].Kind, _store.Rows[2].Text, _store.Rows[2].HeardBy));

            // Alone, it is said to nobody, and is on the log all the same.
            chat.RadialChat(far, "anyone?");
            Assert.AreEqual(("anyone?", 0u, (byte)ChatLogResult.Delivered), (_store.Rows[3].Text, _store.Rows[3].HeardBy, _store.Rows[3].Result));
        }

        [TestMethod]
        public void ACommandIsNotChatAndAnEmptyLineIsNothingSaid()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var speaker = Player(world);

            // A line that begins with a dot is a command, whoever typed it: the game master audit log's.
            chat.RadialChat(speaker, ".nosuchcommand at all");
            chat.RadialChat(speaker, "");
            chat.RadialChat(speaker, null);
            chat.Shout(speaker, "");
            chat.Emote(speaker, new EmotePacket { Emote = null });

            Assert.IsEmpty(_store.Rows);
        }

        [TestMethod]
        public void ALineTooLongToBeSaidIsOnTheLogWithItsBeginning()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var speaker = Player(world);
            var near = Player(world);
            var longLine = new string('a', CommunicatorManager.MaxChatLength) + "tail";

            chat.RadialChat(speaker, longLine);
            chat.Shout(speaker, longLine);
            chat.JoinGlobalChannel(speaker, ChatChannelId.General);
            chat.ChannelChat(speaker, new ChannelChatPacket { ChannelId = ChatChannelId.General, Message = longLine });
            Drain(speaker, near);

            Assert.HasCount(3, _store.Rows);

            foreach (var row in _store.Rows)
            {
                Assert.AreEqual((byte)ChatLogResult.TooLong, row.Result);
                Assert.AreEqual(new string('a', ChatLogEntry.MaxTextLength), row.Text);
                Assert.AreEqual(0u, row.HeardBy);
            }

            Assert.AreEqual(((byte)ChatLogKind.Channel, ChatChannelId.General, "General"), (_store.Rows[2].Kind, _store.Rows[2].GroupId, _store.Rows[2].Target));

            // The longest line there is goes through whole.
            chat.RadialChat(speaker, new string('b', CommunicatorManager.MaxChatLength));
            Assert.AreEqual(((byte)ChatLogResult.Delivered, CommunicatorManager.MaxChatLength), (_store.Rows[3].Result, _store.Rows[3].Text.Length));
            Assert.HasCount(1, Sent<RadialChatPacket>(near));
        }

        [TestMethod]
        public void ASilencedPlayersLinesAreOnTheLogAndNobodyHearsThem()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var silenced = Player(world);
            var near = Player(world);
            var master = Player(world);

            master.AccountEntry.Level = (byte)GmLevel.GameMaster;
            silenced.AccountEntry.MutedUntil = long.MaxValue;
            silenced.Player.ClanId = 77;
            chat.JoinGlobalChannel(silenced, ChatChannelId.General);
            Drain(silenced, near, master);

            chat.RadialChat(silenced, "say");
            chat.Shout(silenced, "shout");
            chat.Emote(silenced, new EmotePacket { Emote = "emote" });
            chat.PartyChat(silenced, new PartyChatPacket { Message = "squad" });
            chat.ClanChat(silenced, new ClanChatPacket { ClanId = 77, Message = "clan" });
            chat.ClanLeadersChat(silenced, new ClanLeadersChatPacket { ClanId = 77, Message = "leaders" });
            chat.ChannelChat(silenced, new ChannelChatPacket { ChannelId = ChatChannelId.General, Message = "channel" });
            chat.Whisper(silenced, new WhisperPacket { Reciver = near.Player.FamilyName, Message = "whisper" });

            CollectionAssert.AreEqual(new[] { "say", "shout", "emote", "squad", "clan", "leaders", "channel", "whisper" }, _store.Rows.Select(r => r.Text).ToArray());
            CollectionAssert.AreEqual(
                new[] { ChatLogKind.Say, ChatLogKind.Shout, ChatLogKind.Emote, ChatLogKind.Squad, ChatLogKind.Clan, ChatLogKind.ClanLeaders, ChatLogKind.Channel, ChatLogKind.Whisper },
                _store.Rows.Select(r => (ChatLogKind)r.Kind).ToArray());
            Assert.IsTrue(_store.Rows.All(r => r.Result == (byte)ChatLogResult.Muted && r.HeardBy == 0));
            Assert.AreEqual((77u, 77u, ChatChannelId.General), (_store.Rows[4].GroupId, _store.Rows[5].GroupId, _store.Rows[6].GroupId));
            Assert.AreEqual(near.AccountEntry.Id, _store.Rows[7].TargetAccountId, "whom the whisper was for");
            Assert.IsEmpty(Packets(near).Where(packet => packet is RadialChatPacket || packet is ShoutPacket || packet is EmotePacket || packet is WhisperPacket).ToList());

            // A game master may still be whispered: that one is delivered.
            chat.Whisper(silenced, new WhisperPacket { Reciver = master.Player.FamilyName, Message = "why?" });

            var asked = _store.Rows.Last();

            Assert.AreEqual(((byte)ChatLogResult.Delivered, 1u, master.AccountEntry.Id), (asked.Result, asked.HeardBy, asked.TargetAccountId));
            Assert.AreEqual("why?", Sent<WhisperPacket>(master).Single().Message);
        }

        #endregion

        #region Whispers

        [TestMethod]
        public void AWhisperIsOnTheLogWithWhomItWasToAndWhatCameOfIt()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var sender = Player(world);
            var target = Player(world);
            var ignoring = Player(world);

            ignoring.Player.IgnoredPlayers.Add(sender.AccountEntry.Id);

            // Delivered: typed in any case, on the log by the name its owner has.
            chat.Whisper(sender, new WhisperPacket { Reciver = target.Player.FamilyName.ToUpperInvariant(), Message = "psst" });

            var row = _store.Rows.Single();

            Assert.AreEqual(((byte)ChatLogKind.Whisper, (byte)ChatLogResult.Delivered, "psst", 1u), (row.Kind, row.Result, row.Text, row.HeardBy));
            Assert.AreEqual((target.AccountEntry.Id, target.Player.Id, target.Player.FamilyName), (row.TargetAccountId, row.TargetCharacterId, row.Target));
            Assert.AreEqual(sender.AccountEntry.Id, row.AccountId);
            Assert.AreEqual("psst", Sent<WhisperPacket>(target).Single().Message);

            // A reply is a whisper.
            chat.Reply(target, new ReplyPacket { Reciver = sender.Player.FamilyName, Message = "what" });
            Assert.AreEqual(((byte)ChatLogKind.Whisper, target.AccountEntry.Id, sender.AccountEntry.Id, "what"),
                (_store.Rows[1].Kind, _store.Rows[1].AccountId, _store.Rows[1].TargetAccountId, _store.Rows[1].Text));

            // To nobody in the game: the name as it was typed.
            chat.Whisper(sender, new WhisperPacket { Reciver = " Nobodyhere ", Message = "hello?" });
            Assert.AreEqual(((byte)ChatLogResult.NoTarget, "Nobodyhere", 0u, 0u, 0u),
                (_store.Rows[2].Result, _store.Rows[2].Target, _store.Rows[2].TargetAccountId, _store.Rows[2].TargetCharacterId, _store.Rows[2].HeardBy));

            // To one who ignores them: not sent, and on the log as that.
            chat.Whisper(sender, new WhisperPacket { Reciver = ignoring.Player.FamilyName, Message = "listen" });
            Assert.AreEqual(((byte)ChatLogResult.Ignored, ignoring.AccountEntry.Id, 0u), (_store.Rows[3].Result, _store.Rows[3].TargetAccountId, _store.Rows[3].HeardBy));
            Assert.IsEmpty(Sent<WhisperPacket>(ignoring));

            // To themselves.
            chat.Whisper(sender, new WhisperPacket { Reciver = sender.Player.FamilyName, Message = "note to self" });
            Assert.AreEqual(((byte)ChatLogResult.Delivered, sender.AccountEntry.Id, 0u), (_store.Rows[4].Result, _store.Rows[4].TargetAccountId, _store.Rows[4].HeardBy));
            Assert.HasCount(5, _store.Rows);
        }

        #endregion

        #region Squad, clan and channels

        [TestMethod]
        public void SquadChatIsOnTheLogUnderItsSquad()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var lead = Player(world);
            var mate = Player(world);
            var loner = Player(world);
            var id = PartyManager.Instance.GetPartyId;

            PartyManager.Instance.Parties[id] = new Party(id, lead.AccountEntry.Id, new List<PartyMember> { new PartyMember(lead), new PartyMember(mate) });
            lead.Player.PartyId = id;
            mate.Player.PartyId = id;
            _parties.Add(id);

            chat.PartyChat(lead, new PartyChatPacket { Message = "regroup" });

            var row = _store.Rows.Single();

            Assert.AreEqual(((byte)ChatLogKind.Squad, (byte)ChatLogResult.Delivered, "regroup", id, 1u), (row.Kind, row.Result, row.Text, row.GroupId, row.HeardBy));
            Assert.AreEqual("regroup", Sent<PartyChatPacket>(mate).Single().Message);

            // In no squad: said to nobody.
            chat.PartyChat(loner, new PartyChatPacket { Message = "hello squad" });
            Assert.AreEqual(((byte)ChatLogResult.NotMember, 0u, 0u, "hello squad"), (_store.Rows[1].Result, _store.Rows[1].GroupId, _store.Rows[1].HeardBy, _store.Rows[1].Text));
        }

        [TestMethod]
        public void ClanChatIsOnTheLogUnderItsClanAndTheLeadersUnderTheirs()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var leader = Player(world);
            var officer = Player(world);
            var member = Player(world);
            var outsider = Player(world);
            const uint clan = 9742;

            ClanManager.Instance.Clans[clan] = new Lazy<ClanEntry>(() => new ClanEntry { Id = clan, Name = "Fixture Clan", RankTitle0 = "0", RankTitle1 = "1", RankTitle2 = "2", RankTitle3 = "3" });
            ClanManager.Instance.ClanMembers[clan] = new Lazy<List<ClanMemberEntry>>(() => new List<ClanMemberEntry>
            {
                new ClanMemberEntry { ClanId = clan, CharacterId = leader.Player.Id, Rank = 3 },
                new ClanMemberEntry { ClanId = clan, CharacterId = officer.Player.Id, Rank = ClanRank.MinRankToSpeakInLeadersChannel },
                new ClanMemberEntry { ClanId = clan, CharacterId = member.Player.Id, Rank = 0 }
            });
            _clans.Add(clan);

            foreach (var client in new[] { leader, officer, member })
                client.Player.ClanId = clan;

            chat.ClanChat(member, new ClanChatPacket { ClanId = clan, Message = "evening all" });

            var row = _store.Rows.Single();

            Assert.AreEqual(((byte)ChatLogKind.Clan, (byte)ChatLogResult.Delivered, clan, "Fixture Clan", 2u, "evening all"),
                (row.Kind, row.Result, row.GroupId, row.Target, row.HeardBy, row.Text));
            Assert.HasCount(1, Sent<ClanChatPacket>(leader));

            // The leaders' channel: the leaders hear it, the rest of the clan does not.
            chat.ClanLeadersChat(leader, new ClanLeadersChatPacket { ClanId = clan, Message = "promote them?" });
            Assert.AreEqual(((byte)ChatLogKind.ClanLeaders, (byte)ChatLogResult.Delivered, clan, "Fixture Clan", 1u),
                (_store.Rows[1].Kind, _store.Rows[1].Result, _store.Rows[1].GroupId, _store.Rows[1].Target, _store.Rows[1].HeardBy));
            Assert.HasCount(1, Sent<ClanLeadersChatPacket>(officer));
            Assert.HasCount(1, Sent<ClanLeadersChatPacket>(leader), "their own line back");
            Assert.IsEmpty(Sent<ClanLeadersChatPacket>(member));

            // Below the rank for it.
            chat.ClanLeadersChat(member, new ClanLeadersChatPacket { ClanId = clan, Message = "can I see this?" });
            Assert.AreEqual(((byte)ChatLogResult.NotMember, clan, 0u, "can I see this?"), (_store.Rows[2].Result, _store.Rows[2].GroupId, _store.Rows[2].HeardBy, _store.Rows[2].Text));
            Assert.IsEmpty(Sent<ClanLeadersChatPacket>(leader));

            // A clan that is not theirs: the one the packet named, and nobody sent it.
            chat.ClanChat(outsider, new ClanChatPacket { ClanId = clan, Message = "let me in" });
            Assert.AreEqual(((byte)ChatLogKind.Clan, (byte)ChatLogResult.NotMember, clan, "Fixture Clan", 0u),
                (_store.Rows[3].Kind, _store.Rows[3].Result, _store.Rows[3].GroupId, _store.Rows[3].Target, _store.Rows[3].HeardBy));
            Assert.IsEmpty(Sent<ClanChatPacket>(leader));
        }

        [TestMethod]
        public void ChannelChatIsOnTheLogUnderItsChannelByNameAndATeamsWithItsNumber()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var speaker = Player(world);
            var listener = Player(world);
            var other = Player(world);

            foreach (var client in new[] { speaker, listener })
            {
                chat.JoinGlobalChannel(client, ChatChannelId.General);
                chat.JoinDefaultLocalChannel(client, ChatChannelId.MapTrade);
                chat.JoinTeamChannel(client, world.Map, 2);
            }

            chat.JoinGlobalChannel(other, ChatChannelId.General);
            Drain(speaker, listener, other);

            chat.ChannelChat(speaker, new ChannelChatPacket { ChannelId = ChatChannelId.General, Message = "hi all" });
            chat.ChannelChat(speaker, new ChannelChatPacket { ChannelId = ChatChannelId.MapTrade, Message = "wts rifle" });
            chat.ChannelChat(speaker, new ChannelChatPacket { ChannelId = ChatChannelId.Team, Message = "left side" });

            Assert.HasCount(3, _store.Rows);
            Assert.IsTrue(_store.Rows.All(r => r.Kind == (byte)ChatLogKind.Channel && r.Result == (byte)ChatLogResult.Delivered));
            Assert.AreEqual((ChatChannelId.General, "General", 2u, "hi all"), (_store.Rows[0].GroupId, _store.Rows[0].Target, _store.Rows[0].HeardBy, _store.Rows[0].Text));
            Assert.AreEqual((ChatChannelId.MapTrade, "Trade", 1u), (_store.Rows[1].GroupId, _store.Rows[1].Target, _store.Rows[1].HeardBy));
            Assert.AreEqual((ChatChannelId.Team, "Team 2", 1u), (_store.Rows[2].GroupId, _store.Rows[2].Target, _store.Rows[2].HeardBy));
            Assert.AreEqual(world.Map.MapInfo.MapContextId, _store.Rows[1].MapContextId, "which map's Trade: the one they stood on");

            // A channel they are not on.
            chat.ChannelChat(other, new ChannelChatPacket { ChannelId = ChatChannelId.Team, Message = "which team am I" });
            Assert.AreEqual(((byte)ChatLogResult.NotMember, ChatChannelId.Team, "Team", 0u), (_store.Rows[3].Result, _store.Rows[3].GroupId, _store.Rows[3].Target, _store.Rows[3].HeardBy));

            Assert.AreEqual("Map General", CommunicatorManager.ChannelName(ChatChannelId.MapGeneral));
            Assert.AreEqual("LFG", CommunicatorManager.ChannelName(ChatChannelId.LookingForGroup));
            Assert.AreEqual("Defense", CommunicatorManager.ChannelName(ChatChannelId.MapDefense));
            Assert.AreEqual("Channel 99", CommunicatorManager.ChannelName(99));
        }

        #endregion

        #region The log itself

        [TestMethod]
        public void ALogThatCannotBeWrittenStopsNoChatAndNoLogKeepsNone()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var speaker = Player(world);
            var near = Player(world);

            _store.Fails = new InvalidOperationException("the database is away");

            chat.RadialChat(speaker, "still heard");
            chat.Whisper(speaker, new WhisperPacket { Reciver = near.Player.FamilyName, Message = "and this" });

            var packets = Packets(near);

            Assert.AreEqual("still heard", packets.OfType<RadialChatPacket>().Single().TextMsg);
            Assert.AreEqual("and this", packets.OfType<WhisperPacket>().Single().Message);
            Assert.IsEmpty(_store.Rows);
            Assert.AreEqual(0u, ChatAudit.Instance.Record(speaker, ChatLogKind.Say, "x", ChatLogResult.Delivered));

            // With no store nothing is kept, and nothing is asked of the client.
            ChatAudit.Instance.Load(null);
            chat.RadialChat(speaker, "nor this");
            Assert.AreEqual("nor this", Sent<RadialChatPacket>(near).Single().TextMsg);
            Assert.AreEqual(0u, ChatAudit.Instance.Record(null, ChatLogKind.Say, "x", ChatLogResult.Delivered));

            // A client with no account says nothing the log can name.
            ChatAudit.Instance.Load(_store);
            _store.Fails = null;
            Assert.AreEqual(0u, ChatAudit.Instance.Record(world.CreateClient(), ChatLogKind.Say, "x", ChatLogResult.Delivered));
            Assert.AreEqual(0u, ChatAudit.Instance.Record(speaker, ChatLogKind.Say, "", ChatLogResult.Delivered));
            Assert.IsEmpty(_store.Rows);
        }

        [TestMethod]
        public void TheServersLogIsTheCharacterDatabase()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using var world = new WorldTestContext();
            var listener = Player(world);

            ChatAudit.Instance.Load(new ChatAudit.ServerStore(context));

            var first = ChatAudit.Instance.Record(context.Client, ChatLogKind.Shout, "to the database", ChatLogResult.Delivered, 3);
            var second = ChatAudit.Instance.Record(context.Client, ChatLogKind.Whisper, "and a whisper", ChatLogResult.Delivered, 1, 0, null, listener);

            Assert.AreNotEqual(0u, first);
            Assert.AreNotEqual(first, second);

            using var unit = context.CreateChar();
            var rows = unit.ChatLogs.GetRecent(10);

            Assert.HasCount(2, rows);
            Assert.AreEqual(("and a whisper", (byte)ChatLogKind.Whisper, listener.AccountEntry.Id, listener.Player.FamilyName), (rows[0].Text, rows[0].Kind, rows[0].TargetAccountId, rows[0].Target));
            Assert.AreEqual(("to the database", (byte)ChatLogKind.Shout, 3u, Noon), (rows[1].Text, rows[1].Kind, rows[1].HeardBy, rows[1].CreatedAt));
            Assert.AreEqual(context.Client.AccountEntry.Id, rows[1].AccountId);
            Assert.AreEqual(context.Client.Player.Id, rows[1].CharacterId);

            // An account's lines are those it said and those it was whispered.
            Assert.HasCount(2, unit.ChatLogs.GetByAccount(context.Client.AccountEntry.Id, 10));
            Assert.AreEqual("and a whisper", unit.ChatLogs.GetByAccount(listener.AccountEntry.Id, 10).Single().Text);
        }

        [TestMethod]
        public void TheTableKeepsTheRowsItIsGiven()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            var database = Path.Combine(directory, "database");

            try
            {
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                    context.Database.Migrate();

                T Log<T>(Func<ChatLogRepository, T> action)
                {
                    using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database);
                    return action(new ChatLogRepository(context));
                }

                var longest = new string('x', ChatLogEntry.MaxTextLength);
                var first = Log(r => r.Add(new ChatLogEntry
                {
                    CreatedAt = Noon,
                    Kind = (byte)ChatLogKind.Whisper,
                    Result = (byte)ChatLogResult.Ignored,
                    AccountId = 7,
                    AccountLevel = 2,
                    CharacterId = 70,
                    Name = "First",
                    FamilyName = "Family",
                    MapContextId = 1220,
                    InstanceId = 3,
                    CoordX = 1.5,
                    CoordY = -2.25,
                    CoordZ = 300,
                    GroupId = 10000008,
                    TargetAccountId = 9,
                    TargetCharacterId = 90,
                    Target = "Other",
                    HeardBy = 4,
                    Text = longest
                }));
                var second = Log(r => r.Add(new ChatLogEntry { CreatedAt = Noon.AddMinutes(1), Kind = (byte)ChatLogKind.Say, Result = (byte)ChatLogResult.Delivered, AccountId = 8, Name = null, FamilyName = null, Target = null, Text = "café über 日本" }));
                var third = Log(r => r.Add(new ChatLogEntry { CreatedAt = Noon.AddMinutes(2), Kind = (byte)ChatLogKind.Say, Result = (byte)ChatLogResult.Delivered, AccountId = 9, Text = "third" }));

                CollectionAssert.AreEqual(new uint[] { 1, 2, 3 }, new[] { first, second, third });
                CollectionAssert.AreEqual(new uint[] { 3, 2, 1 }, Log(r => r.GetRecent(10)).Select(e => e.Id).ToArray(), "newest first");
                CollectionAssert.AreEqual(new uint[] { 3, 2 }, Log(r => r.GetRecent(2)).Select(e => e.Id).ToArray());
                Assert.IsEmpty(Log(r => r.GetRecent(-1)));
                CollectionAssert.AreEqual(new uint[] { 3, 1 }, Log(r => r.GetByAccount(9, 10)).Select(e => e.Id).ToArray(), "what 9 said, and what 9 was whispered");
                CollectionAssert.AreEqual(new uint[] { 1 }, Log(r => r.GetByAccount(7, 10)).Select(e => e.Id).ToArray());
                Assert.IsEmpty(Log(r => r.GetByAccount(6, 10)));

                var read = Log(r => r.GetRecent(10)).Single(e => e.Id == first);

                Assert.AreEqual(Noon, read.CreatedAt);
                Assert.AreEqual(((byte)ChatLogKind.Whisper, (byte)ChatLogResult.Ignored, 7u, (byte)2, 70u), (read.Kind, read.Result, read.AccountId, read.AccountLevel, read.CharacterId));
                Assert.AreEqual(("First", "Family", 1220u, 3u), (read.Name, read.FamilyName, read.MapContextId, read.InstanceId));
                Assert.AreEqual((1.5, -2.25, 300d), (read.CoordX, read.CoordY, read.CoordZ));
                Assert.AreEqual((10000008u, 9u, 90u, "Other", 4u), (read.GroupId, read.TargetAccountId, read.TargetCharacterId, read.Target, read.HeardBy));
                Assert.AreEqual(longest, read.Text);

                var plain = Log(r => r.GetRecent(10)).Single(e => e.Id == second);

                Assert.AreEqual(("", "", "", "café über 日本"), (plain.Name, plain.FamilyName, plain.Target, plain.Text));
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        #endregion

        /// <summary>A player in the world with an account, whom chat can find.</summary>
        private Client Player(WorldTestContext world, float x = 0, float z = 0)
        {
            var client = world.CreateClient(x, z);
            var id = _nextAccount++;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = id, FamilyName = $"Family{id}" });
            client.Player.FamilyName = $"Family{id}";

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);

            return client;
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();
    }
}

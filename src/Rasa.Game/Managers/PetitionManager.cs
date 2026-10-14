using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Memory;
    using Packets;
    using Packets.Petition.Client;
    using Packets.Petition.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    ///     Petition packets:
    ///     -- WorldMsg
    /// - CreateBugReport                     => implemented
    /// - CreateHelpRequest                   => implemented
    /// - CancelPetition                      => implemented (no client caller)
    /// - RetrievePetition                    => implemented (no client caller)
    /// - AddToPetition                       => implemented (no client caller)
    /// - SearchPetitions                     => implemented (no client caller)
    /// - SearchKB                            => no client caller
    /// - RetrieveKBArticle                   => no client caller
    ///
    ///     Petition handlers (client/petitionmanager.py):
    /// - CreatePetitionAck                   => implemented
    /// - CancelPetitionAck                   => implemented (empty body in the client)
    /// - RetrievePetitionAck                 => implemented (empty body in the client)
    /// - AddToPetitionAck                    => implemented (empty body in the client)
    /// - SearchPetitionsAck                  => implemented (empty body in the client)
    /// - SearchKBAck                         => empty body in the client
    /// - RetrieveKBArticleAck                => empty body in the client
    ///
    /// Only the two create calls are reachable: helpwindow.py wires Send on the Report Bug and
    /// Petition pages, and /bug and /petition open those same pages. The rest were the GM and
    /// knowledge-base half of the feature and nothing in the shipped client calls them, so
    /// their opcodes stay unhandled.
    ///
    /// Both create calls used to disconnect the player. An opcode with no registered packet
    /// leaves its payload unread, CallServerMethodMessage.ReadPacket then fails the 0x66
    /// terminator check, and Client.Close(true) runs - so pressing Send dropped you to the
    /// login screen with no message.
    /// </summary>
    public class PetitionManager
    {
        /// <summary>
        /// petition.summary is varchar(255) and petition.body is TEXT. The client caps neither
        /// edit box, so both are trimmed to fit rather than refused: a report that arrives long
        /// is still worth reading.
        /// </summary>
        private const int MaxSummaryLength = 255;
        private const int MaxBodyLength = 8000;

        /// <summary>A game master's answer goes in petition.resolution, varchar(255).</summary>
        public const int MaxResponseLength = 255;

        /// <summary>
        /// A petition plus everything appended to it. petition.body is MySQL TEXT, which holds
        /// 65535 *bytes*, and a character outside ASCII costs up to three of them - so the cap
        /// is on characters at a third of that, and the column cannot be overrun whatever is
        /// typed into it.
        /// </summary>
        private const int MaxTotalBodyLength = 16000;

        /// <summary>
        /// Rows in one search answer. Bodies are left out of those rows, but the socket buffer
        /// is 8 KB and a list has to fit in a frame; a player with more petitions than this has
        /// a different problem.
        /// </summary>
        private const int SearchResultLimit = 20;

        /// <summary>
        /// One filing per account per half minute. The window hides itself on Send and has to be
        /// reopened by hand, so this never gets in a real player's way, but it does stop a
        /// scripted client from filling the table as fast as the socket allows.
        /// </summary>
        private const int CooldownSeconds = 30;

        private readonly ConcurrentDictionary<uint, DateTime> _lastFiled = new ConcurrentDictionary<uint, DateTime>();

        #region Singleton

        private static PetitionManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public static PetitionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PetitionManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private PetitionManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        #endregion

        #region Handlers

        internal void CreateBugReport(Client client, CreateBugReportPacket packet)
        {
            File(client, PetitionType.BugReport, packet.Summary, packet.Body);
        }

        internal void CreateHelpRequest(Client client, CreateHelpRequestPacket packet)
        {
            File(client, PetitionType.HelpRequest, packet.Summary, packet.Body);
        }

        /// <summary>
        /// Withdraws a petition the player filed. Nothing in the shipped client sends this - see
        /// CancelPetitionPacket for the three separate reasons - so it is groundwork rather than
        /// a feature, and it is written to be safe against the client that would reach it first,
        /// which is a modified one.
        ///
        /// A petition can only be cancelled by the account that filed it, and only while it is
        /// open. Both refusals answer with the same failure, because an ack that distinguished
        /// "not yours" from "does not exist" would let a client walk the table.
        /// </summary>
        internal void CancelPetition(Client client, CancelPetitionPacket packet)
        {
            var accountId = client.AccountEntry.Id;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var petition = unitOfWork.Petitions.GetPetition(packet.PetitionId);

            if (petition == null || petition.AccountId != accountId
                                 || petition.Status != (byte)PetitionStatus.Open)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} could not cancel petition #{packet.PetitionId}: "
                    + (petition == null ? "no such petition."
                       : petition.AccountId != accountId ? "it belongs to another account."
                       : $"it is already {(PetitionStatus)petition.Status}."));

                client.CallMethod(SysEntity.ClientPetitionManagerId,
                    new CancelPetitionAckPacket(false, packet.PetitionId));
                return;
            }

            if (!unitOfWork.Petitions.SetPetitionStatus(packet.PetitionId, (byte)PetitionStatus.Cancelled, string.Empty))
            {
                client.CallMethod(SysEntity.ClientPetitionManagerId,
                    new CancelPetitionAckPacket(false, packet.PetitionId));
                return;
            }

            Logger.WriteLog(LogType.Command,
                $"Petition #{packet.PetitionId} withdrawn by {client.Player.FamilyName} [account {accountId}].");

            client.CallMethod(SysEntity.ClientPetitionManagerId,
                new CancelPetitionAckPacket(true, packet.PetitionId));
        }

        /// <summary>
        /// Reads back one of the caller's own petitions. Unreachable in the shipped client, like
        /// the cancel path, and refused the same way: a petition that is not yours is answered
        /// exactly as one that does not exist, so the ack cannot be used to find out which ids
        /// are real.
        ///
        /// Every status is readable, not just open ones - the point of retrieving a petition is
        /// usually to see what was done about it.
        /// </summary>
        internal void RetrievePetition(Client client, RetrievePetitionPacket packet)
        {
            var accountId = client.AccountEntry.Id;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var petition = unitOfWork.Petitions.GetPetition(packet.PetitionId);

            if (petition == null || petition.AccountId != accountId)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} could not retrieve petition #{packet.PetitionId}: "
                    + (petition == null ? "no such petition." : "it belongs to another account."));

                client.CallMethod(SysEntity.ClientPetitionManagerId,
                    new RetrievePetitionAckPacket(false, packet.PetitionId));
                return;
            }

            // The body can be 16,000 characters (three bytes each in UTF-8) and the reply has
            // to fit one send buffer; what does not fit is cut, with a marker, rather than the
            // send throwing and the client being disconnected for asking.
            var info = new PetitionInfo(petition);
            var overhead = PythonSize.Of(pw => new RetrievePetitionAckPacket(true, packet.PetitionId, new PetitionInfo(petition) { Body = "" }).Write(pw));

            info.Body = ClampBytes(info.Body, PythonSize.PayloadBudget - overhead);

            client.CallMethod(SysEntity.ClientPetitionManagerId,
                new RetrievePetitionAckPacket(true, packet.PetitionId, info));
        }

        /// <summary>
        /// Appends to a petition already filed - the player remembering something after the
        /// fact. Unreachable in the shipped client; groundwork.
        ///
        /// The text goes onto the end of the body under a dated separator rather than into a
        /// table of its own. A petition is read by a person, start to finish, and what they want
        /// is the story in order; a second table would buy ordering and per-entry metadata that
        /// nothing here has any use for.
        ///
        /// Only your own, and only while it is open: appending to something already answered
        /// would put text where nobody is going to look again.
        /// </summary>
        internal void AddToPetition(Client client, AddToPetitionPacket packet)
        {
            var accountId = client.AccountEntry.Id;
            var text = Clamp(packet.Text, MaxBodyLength);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var petition = unitOfWork.Petitions.GetPetition(packet.PetitionId);

            if (text.Length == 0 || petition == null || petition.AccountId != accountId
                || petition.Status != (byte)PetitionStatus.Open)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} could not add to petition #{packet.PetitionId}: "
                    + (text.Length == 0 ? "nothing to add."
                       : petition == null ? "no such petition."
                       : petition.AccountId != accountId ? "it belongs to another account."
                       : $"it is already {(PetitionStatus)petition.Status}."));

                Ack(client, new AddToPetitionAckPacket(false, packet.PetitionId));
                return;
            }

            var addition = $"\n\n--- added {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC ---\n{text}";

            if (petition.Body.Length + addition.Length > MaxTotalBodyLength)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} could not add to petition #{packet.PetitionId}: it is full.");

                Ack(client, new AddToPetitionAckPacket(false, packet.PetitionId));
                return;
            }

            if (!unitOfWork.Petitions.UpdatePetitionBody(packet.PetitionId, petition.Body + addition))
            {
                Ack(client, new AddToPetitionAckPacket(false, packet.PetitionId));
                return;
            }

            Logger.WriteLog(LogType.Command,
                $"Petition #{packet.PetitionId} added to by {client.Player.FamilyName} [account {accountId}].");

            Ack(client, new AddToPetitionAckPacket(true, packet.PetitionId));
        }

        /// <summary>
        /// The caller's own petitions, newest first, without their bodies.
        ///
        /// The request carries no arguments, so there is nothing to search by and only one thing
        /// it can honestly mean. It is tempting to read the name as the GM-side petition queue -
        /// that is most likely what it was in the original game, where the GM client was a
        /// different build - but answering it with everyone's petitions would hand any client
        /// that sends one opcode the help requests and bug reports of every player on the
        /// server, names and all. The operator's view of the queue is the Game console's
        /// `petition list`, which is reachable and cannot be asked for over the wire.
        /// </summary>
        internal void SearchPetitions(Client client, SearchPetitionsPacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var results = new List<PetitionInfo>();
            var size = PythonSize.Of(pw => new SearchPetitionsAckPacket(true, results).Write(pw));

            // Rows are added while the reply still fits a send buffer; a summary and a
            // resolution can each be 255 characters, so twenty rows do not always.
            foreach (var petition in unitOfWork.Petitions.ListPetitionsForAccount(client.AccountEntry.Id, SearchResultLimit))
            {
                var row = new PetitionInfo(petition);
                var rowSize = PythonSize.Of(pw => row.Write(pw, false));

                if (size + rowSize + PythonSize.ListHeaderSlack > PythonSize.PayloadBudget)
                    break;

                size += rowSize;
                results.Add(row);
            }

            Ack(client, new SearchPetitionsAckPacket(true, results));
        }

        #endregion

        #region Console

        /// <summary>Newest first. A null status means every status.</summary>
        public List<PetitionEntry> List(PetitionStatus? status, int limit)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.Petitions.ListPetitions((byte?)status, limit);
        }

        public PetitionEntry Get(uint id)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.Petitions.GetPetition(id);
        }

        /// <summary>
        /// The console's resolve: marks a petition dealt with, with a line about what was done -
        /// or, with none, keeps the answer it has. The player is told now if they are in the
        /// world and when they next arrive in it if not (<see cref="Tell"/>).
        /// </summary>
        public bool Resolve(uint id, string resolution)
        {
            var note = Clamp(resolution, MaxResponseLength);

            return Settle(id, note.Length > 0 ? note : null, close: true).Answer == PetitionAnswer.Done;
        }

        #endregion

        #region Answers

        /// <summary>
        /// A game master's answer, through the REST API (respondpetition): written as the
        /// petition's resolution, which it replaces, and told to the player. The petition stays
        /// open. Only an open petition is answered.
        /// </summary>
        public PetitionResult Respond(uint id, string response) => Settle(id, Clamp(response, MaxResponseLength), close: false);

        /// <summary>
        /// Marks a petition resolved, through the REST API (closepetition), keeping the answer it
        /// has, and tells the player. Only an open petition is closed.
        /// </summary>
        public PetitionResult Close(uint id) => Settle(id, null, close: true);

        /// <summary>
        /// Writes the answer when there is one, the close when asked for, and tells the player.
        /// A petition that is not open - resolved, or withdrawn - is left as it is.
        /// </summary>
        private PetitionResult Settle(uint id, string response, bool close)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var petition = unitOfWork.Petitions.GetPetition(id);

            if (petition == null)
                return new PetitionResult(PetitionAnswer.NoSuchPetition);

            if (petition.Status != (byte)PetitionStatus.Open)
                return new PetitionResult(PetitionAnswer.NotOpen, (PetitionStatus)petition.Status);

            if (response != null)
            {
                if (!unitOfWork.Petitions.SetPetitionResolution(id, response))
                    return new PetitionResult(PetitionAnswer.NotSaved);

                petition.Resolution = response;
            }

            if (close)
            {
                if (!unitOfWork.Petitions.SetPetitionStatus(id, (byte)PetitionStatus.Resolved, petition.Resolution))
                    return new PetitionResult(PetitionAnswer.NotSaved);

                petition.Status = (byte)PetitionStatus.Resolved;
            }

            return new PetitionResult(PetitionAnswer.Done, (PetitionStatus)petition.Status, Tell(unitOfWork, petition));
        }

        /// <summary>
        /// Tells the player who filed it what has become of their petition: now, when they are
        /// in the world, and otherwise when they next arrive in it (<see cref="DeliverNotices"/>),
        /// by petition.notice_pending. True when they were told now.
        /// </summary>
        private static bool Tell(ICharUnitOfWork unitOfWork, PetitionEntry petition)
        {
            // The console's resolve runs on its own thread, and the main loop adds to and takes
            // from the client list every tick: it is walked under its lock, as every other walk
            // of it is, and the message is sent outside it.
            Client author;

            lock (Server.Clients)
                author = Server.Clients.FirstOrDefault(
                    c => c != null && c.State == ClientState.Ingame && c.AccountEntry?.Id == petition.AccountId);

            if (author != null)
            {
                // Answered or resolved whatever happens to the message: a connection closing
                // under it leaves the player to be told when they next arrive.
                try
                {
                    CommunicatorManager.Instance.SystemMessage(author, NoticeOf(petition));

                    if (petition.NoticePending)
                        unitOfWork.Petitions.SetNoticePending(petition.Id, false);

                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Petition #{petition.Id}: its author could not be told now, and will be when they next arrive: {e.Message}");
                }
            }

            unitOfWork.Petitions.SetNoticePending(petition.Id, true);
            return false;
        }

        /// <summary>
        /// What the player is told, by where the petition is now: one line for an answer and a
        /// close made while they were away. Null for one they withdrew.
        /// </summary>
        public static string NoticeOf(PetitionEntry petition)
        {
            var answer = string.IsNullOrWhiteSpace(petition.Resolution) ? null : petition.Resolution.Trim();

            return (PetitionStatus)petition.Status switch
            {
                PetitionStatus.Resolved => $"Your petition #{petition.Id} has been resolved" + (answer == null ? "." : $": {answer}"),
                PetitionStatus.Open when answer != null => $"A game master has answered your petition #{petition.Id}: {answer}",
                _ => null
            };
        }

        /// <summary>
        /// On every arrival in the world (CommunicatorManager.LoginOk): what was answered or closed
        /// while the player was away, one line a petition, and then nothing more to tell.
        /// </summary>
        public void DeliverNotices(Client client)
        {
            var accountId = client?.AccountEntry?.Id;

            if (accountId == null || _gameUnitOfWorkFactory == null)
                return;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                foreach (var petition in unitOfWork.Petitions.ListNoticesPending(accountId.Value))
                {
                    var notice = NoticeOf(petition);

                    if (notice != null)
                        CommunicatorManager.Instance.SystemMessage(client, notice);

                    unitOfWork.Petitions.SetNoticePending(petition.Id, false);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Petitions: what was answered for account {accountId} could not be told: {e.GetBaseException().Message}");
            }
        }

        /// <summary>
        /// For the REST API's getopenpetitions and getallpetitions: newest first, every column,
        /// with the account's login and family names and the character's name. A null status is
        /// every status.
        /// </summary>
        public List<Api.PetitionRecord> ListRecords(PetitionStatus? status)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var accounts = new Dictionary<uint, GameAccountEntry>();
            var characters = new Dictionary<uint, CharacterEntry>();
            var records = new List<Api.PetitionRecord>();

            foreach (var petition in unitOfWork.Petitions.ListPetitions((byte?)status, int.MaxValue))
            {
                if (!accounts.TryGetValue(petition.AccountId, out var account))
                    accounts[petition.AccountId] = account = unitOfWork.GameAccounts.Find(petition.AccountId);

                if (!characters.TryGetValue(petition.CharacterId, out var character))
                    characters[petition.CharacterId] = character = unitOfWork.Characters.Find(petition.CharacterId);

                records.Add(new Api.PetitionRecord(petition, account?.Name, account?.FamilyName, character?.Name));
            }

            return records;
        }

        #endregion

        /// <summary>
        /// Drops the cooldown when the player leaves the world. Called from
        /// MapChannelManager.RemovePlayer alongside the other per-player manager cleanup.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client.AccountEntry == null)
                return;

            _lastFiled.TryRemove(client.AccountEntry.Id, out _);
        }

        private void File(Client client, PetitionType type, string summary, string body)
        {
            var accountId = client.AccountEntry.Id;

            summary = Clamp(summary, MaxSummaryLength);
            body = Clamp(body, MaxBodyLength);

            // The description always ends up non-empty - petitionmanager.py appends the client
            // version to it before sending - so the subject is the only field that tells a real
            // filing from someone closing the window with the Send button.
            if (summary.Length == 0)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} filed a {type} with no subject. Refused.");
                Ack(client, false, 0);
                return;
            }

            if (OnCooldown(accountId))
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} filed a {type} inside the {CooldownSeconds}s cooldown. Refused.");
                Ack(client, false, 0);
                return;
            }

            var position = client.Player.Position;

            var entry = new PetitionEntry(
                accountId,
                client.Player.Id,
                (byte)type,
                summary,
                body,
                client.Player.MapContextId,
                position.X,
                position.Y,
                position.Z);

            uint petitionId;

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                petitionId = unitOfWork.Petitions.AddPetition(entry);

            if (petitionId == 0)
            {
                // The row did not go in, so nothing was filed and the cooldown is not spent.
                Ack(client, false, 0);
                return;
            }

            _lastFiled[accountId] = DateTime.UtcNow;

            // Logged as well as stored: whoever is watching the console is the person a help
            // request is waiting on, and they should not have to query the table to notice one.
            Logger.WriteLog(LogType.Command,
                $"Petition #{petitionId} ({type}) from {client.Player.FamilyName} "
                + $"[account {accountId}, character {client.Player.Id}] on map {client.Player.MapContextId}: {summary}");

            Ack(client, true, petitionId);
        }

        /// <summary>
        /// SearchKB(searchText) - the knowledge base, which is operator-written prose rather than
        /// anything to do with this player's petitions, so it is answered for anyone who asks.
        /// Empty search text lists what there is.
        /// </summary>
        public void SearchKB(Client client, SearchKBPacket packet)
        {
            var results = KnowledgeBaseManager.Instance.Search(packet.SearchText);

            // success is about the search having run, not about it having found anything: a
            // search with no matches succeeded and returned nothing.
            Ack(client, new SearchKBAckPacket(true, results));
        }

        /// <summary>
        /// RetrieveKBArticle(kbArticleId) - one article in full. An id nobody has an article for
        /// is answered with success false and the id it asked about, not ignored.
        /// </summary>
        public void RetrieveKBArticle(Client client, RetrieveKBArticlePacket packet)
        {
            var article = KnowledgeBaseManager.Instance.Get(packet.ArticleId);

            Ack(client, new RetrieveKBArticleAckPacket(article != null, packet.ArticleId, article));
        }

        private static void Ack(Client client, bool success, uint petitionId)
        {
            Ack(client, new CreatePetitionAckPacket(success, petitionId));
        }

        private static void Ack(Client client, PythonPacket packet)
        {
            client.CallMethod(SysEntity.ClientPetitionManagerId, packet);
        }

        private bool OnCooldown(uint accountId)
        {
            return _lastFiled.TryGetValue(accountId, out var last)
                   && (DateTime.UtcNow - last).TotalSeconds < CooldownSeconds;
        }

        /// <summary>
        /// A null arrives when the client marshals an empty edit box as None rather than as a
        /// zero-length string, and both columns are NOT NULL.
        /// </summary>
        /// <summary>
        /// The longest prefix of <paramref name="value"/> that fits <paramref name="maxBytes"/>
        /// of UTF-8, with a marker when anything was cut; never splits a character.
        /// </summary>
        private static string ClampBytes(string value, int maxBytes)
        {
            const string marker = "\n[... cut to fit ...]";

            if (string.IsNullOrEmpty(value) || System.Text.Encoding.UTF8.GetByteCount(value) <= maxBytes)
                return value ?? string.Empty;

            var limit = Math.Max(0, maxBytes - System.Text.Encoding.UTF8.GetByteCount(marker));
            var bytes = 0;
            var length = 0;

            foreach (var rune in value.EnumerateRunes())
            {
                var runeBytes = rune.Utf8SequenceLength;

                if (bytes + runeBytes > limit)
                    break;

                bytes += runeBytes;
                length += rune.Utf16SequenceLength;
            }

            return value.Substring(0, length) + marker;
        }

        private static string Clamp(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    /// <summary>What came of a game master's answer or close.</summary>
    public enum PetitionAnswer
    {
        Done,
        NoSuchPetition,

        /// <summary>Resolved already, or withdrawn by the player: <see cref="PetitionResult.Status"/> says which.</summary>
        NotOpen,

        /// <summary>The database would not take it.</summary>
        NotSaved
    }

    public sealed class PetitionResult
    {
        public PetitionResult(PetitionAnswer answer, PetitionStatus status = PetitionStatus.Open, bool told = false)
        {
            Answer = answer;
            Status = status;
            Told = told;
        }

        public PetitionAnswer Answer { get; }

        /// <summary>Where the petition is now.</summary>
        public PetitionStatus Status { get; }

        /// <summary>The player was told now; false for one who will be told when they next arrive in the world.</summary>
        public bool Told { get; }
    }
}

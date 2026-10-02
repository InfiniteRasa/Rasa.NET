using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Wargame.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// Clan Feuds: one PvP clan's leader challenges another's, the other accepts, and for a while
    /// the two clans are at war. This is the social half - challenges, answers, the running feud,
    /// its clock and its end. The fighting is Pvp's: the two clans' members are enemies there, and
    /// a player an enemy brings to zero is a <see cref="Kill"/> for the feud (Pvp.Defeat).
    ///
    /// From the client (client/wargame.py, communicator.py, ui/clandeclarationofwar.py):
    ///
    ///  - /feud &lt;clan&gt; sends ChallengeClanToFeud(clan, false): a check only, answered with
    ///    ClanWargameTestSuccess, which opens the Declaration of War dialog; its Send, and Declare
    ///    War in the clan war search, send ChallengeClanToFeud(clan, true).
    ///  - The challenged leader gets ClanWargameInviteReceived: an indicator that opens the dialog
    ///    to Accept or Cancel, which send FeudChallengeResponse (as do /accept_feud and
    ///    /decline_feud). /revoke_feud withdraws a challenge, /surrender_feud gives a feud up.
    ///  - A running feud is AddToClanWargame to every member, the tracker and the Clan Warfare
    ///    list, and WargameData on every member's actor, {feudId: side}, which is what makes the
    ///    other clan's members enemies and one's own allies to the client. It ends with
    ///    WargameVictory, WargameDefeat or WargameTied (the big text) and RemoveFromClanWargame.
    ///
    /// The rules are the client's own messages: a challenge, an answer, a revoke or a surrender is
    /// the clan leader's (CLAN_RANK_TO_CHALLENGE); both clans must be PvP clans; the clan must
    /// exist and not be one's own; the challenged leader must be online; and there is at most one
    /// challenge, in either direction, or one feud between two clans. A squad cannot hold members
    /// of two clans at feud (PartyManager.SeparateFeuding, CanSquadTogether).
    ///
    /// Ours, since nothing in the client says: a feud lasts <see cref="Duration"/> (a week
    /// unless a GM changes it with .feud length); a challenge waits until it is answered or
    /// revoked, or a clan disbands; the side a member is on is true for the challenging clan.
    /// Feuds and challenges are kept (<see cref="IStore"/>, the clan_feud and clan_feud_challenge
    /// tables) and read back when the server starts (<see cref="Load"/>), so both last through a
    /// restart: a feud keeps its id, score and end time, and its clock runs on while the server is
    /// down. A challenge read back is put to the challenged clan's leader again the first time they
    /// enter the world, since their client lost it. Duels and squad wargames are not kept. A clan that disbands
    /// cancels its feuds (WargameCancelled). Members who join or leave a clan mid-feud join or
    /// leave its feuds.
    /// </summary>
    public class ClanFeuds
    {
        private static ClanFeuds _instance;
        private static readonly object InstanceLock = new object();

        public static ClanFeuds Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new ClanFeuds(new ServerClans())
                        {
                            Forfeit = (loser, winner, character, loserName, winnerName) =>
                                InventoryManager.Instance.ForfeitWagers(loser, winner, character, loserName, winnerName)
                        };

                return _instance;
            }
        }

        public static readonly TimeSpan DefaultDuration = TimeSpan.FromDays(7);

        /// <summary>How long a feud started from now lasts.</summary>
        public TimeSpan Duration { get; set; } = DefaultDuration;

        /// <summary>What the feuds need to know about clans and who is online.</summary>
        public interface IClans
        {
            ClanEntry Find(uint clanId);
            ClanEntry FindByName(string name);

            /// <summary>The character's rank in the clan, or null if they are not in it.</summary>
            byte? RankOf(uint clanId, uint characterId);

            /// <summary>The clan's members in the world.</summary>
            List<Client> Online(uint clanId);
        }

        public sealed class Challenge
        {
            public uint WargameId { get; set; }
            public uint ChallengerClanId { get; set; }
            public uint TargetClanId { get; set; }

            /// <summary>The character who made the challenge.</summary>
            public uint ChallengerCharacterId { get; set; }

            /// <summary>Read back at startup: the challenged leader's client has not been told of it since.</summary>
            public bool Restored { get; set; }
        }

        /// <summary>Where feuds and challenges are kept through a restart.</summary>
        public interface IStore
        {
            (List<ClanFeudEntry> Feuds, List<ClanFeudChallengeEntry> Challenges) Load();
            void SaveFeud(ClanFeudEntry feud);
            void DeleteFeud(uint id);
            void SaveChallenge(ClanFeudChallengeEntry challenge);
            void DeleteChallenge(uint wargameId);
        }

        public sealed class Feud
        {
            public uint Id { get; set; }
            public uint ChallengerClanId { get; set; }
            public uint TargetClanId { get; set; }
            public long EndTick { get; set; }
            public int ChallengerKills { get; set; }
            public int TargetKills { get; set; }

            /// <summary>The character who made the challenge; 0 for a feud nobody challenged for (a game master's).</summary>
            public uint ChallengerCharacterId { get; set; }

            /// <summary>The character who accepted the challenge; 0 likewise.</summary>
            public uint TargetCharacterId { get; set; }

            /// <summary>The character who stood for a clan when the feud was made: its challenger, or the one who accepted.</summary>
            public uint CharacterFor(uint clanId) => clanId == ChallengerClanId ? ChallengerCharacterId : clanId == TargetClanId ? TargetCharacterId : 0;

            public bool Involves(uint clanId) => clanId != 0 && (ChallengerClanId == clanId || TargetClanId == clanId);
            public bool Between(uint a, uint b) => ChallengerClanId == a && TargetClanId == b || ChallengerClanId == b && TargetClanId == a;
            public uint OtherThan(uint clanId) => ChallengerClanId == clanId ? TargetClanId : ChallengerClanId;
        }

        private readonly IClans _clans;
        private readonly object _sync = new object();
        private readonly List<Challenge> _challenges = new List<Challenge>();
        private readonly Dictionary<uint, Feud> _feuds = new Dictionary<uint, Feud>();
        private uint _nextId = 1;

        /// <summary>The restored challenges each leader has been reminded of: (wargame id, character id).</summary>
        private readonly HashSet<(uint, uint)> _reminded = new HashSet<(uint, uint)>();

        /// <summary>Where feuds and challenges are kept; null keeps nothing. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>
        /// What the losers of a feud that was won forfeit: (losing clan, winning clan, the winning
        /// clan's character of the challenge, the two clans' names). The live server's is
        /// InventoryManager.ForfeitWagers - their wagered items, to the winners' lockbox; null does
        /// nothing.
        /// </summary>
        public Action<uint, uint, uint, string, string> Forfeit { get; set; }

        /// <summary>The wall clock a kept feud's end is written in: Unix milliseconds, UTC. Replaceable for tests.</summary>
        public Func<long> UtcNow { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public ClanFeuds(IClans clans)
        {
            _clans = clans;
        }

        /// <summary>The clock feuds run on; replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        public List<Feud> Feuds
        {
            get { lock (_sync) return _feuds.Values.ToList(); }
        }

        public List<Challenge> Challenges
        {
            get { lock (_sync) return _challenges.ToList(); }
        }

        #region Client requests

        /// <summary>ChallengeClanToFeud: invite false is the check behind /feud, true the challenge itself.</summary>
        public void ChallengeClanToFeud(Client client, string clanName, bool invite)
        {
            if (!TryLeader(client, PlayerMessage.PmWargameFeudYouAreNotLeaderOfClan, out var own))
                return;

            var name = (clanName ?? "").Trim();
            var target = _clans.FindByName(name);

            if (target == null)
            {
                Say(client, PlayerMessage.PmWargameFeudClanDoesNotExist, ("clan", name));
                return;
            }

            if (target.Id == own.Id)
            {
                Say(client, PlayerMessage.PmWargameFeudCantChallengeOwnClan);
                return;
            }

            if (!own.IsPvP)
            {
                Say(client, PlayerMessage.PmWargameFeudYourClanNotPvp);
                return;
            }

            if (!target.IsPvP)
            {
                Say(client, PlayerMessage.PmWargameFeudTheirClanNotPvp, ("clan", target.Name));
                return;
            }

            Client targetLeader;
            Challenge challenge;

            lock (_sync)
            {
                if (_feuds.Values.Any(f => f.Between(own.Id, target.Id)))
                {
                    Say(client, PlayerMessage.PmWargameFeudCantChallengeAlreadyFighting, ("clan", target.Name));
                    return;
                }

                if (_challenges.Any(c => c.ChallengerClanId == own.Id && c.TargetClanId == target.Id))
                {
                    Say(client, PlayerMessage.PmWargameFeudCantChallengeYouAlreadyInvited, ("clan", target.Name));
                    return;
                }

                if (_challenges.Any(c => c.ChallengerClanId == target.Id && c.TargetClanId == own.Id))
                {
                    Say(client, PlayerMessage.PmWargameFeudCantChallengeAlreadyInvitedYou, ("clan", target.Name));
                    return;
                }

                targetLeader = LeaderOnline(target.Id);

                if (targetLeader == null)
                {
                    Say(client, PlayerMessage.PmWargameFeudLeaderNotOnline, ("clan", target.Name));
                    return;
                }

                if (!invite)
                {
                    client.CallMethod(SysEntity.ClientWargameManagerId, new ClanWargameTestSuccessPacket(target.Name));
                    return;
                }

                challenge = new Challenge { WargameId = _nextId++, ChallengerClanId = own.Id, TargetClanId = target.Id, ChallengerCharacterId = client.Player.Id };
                _challenges.Add(challenge);
            }

            Keep(challenge);

            Logger.WriteLog(LogType.Debug, $"Clan feud: {own.Name} ({own.Id}) challenged {target.Name} ({target.Id}), wargame {challenge.WargameId}.");

            Say(client, PlayerMessage.PmWargameFeudYouChallenged, ("clan", target.Name));

            targetLeader.CallMethod(SysEntity.ClientWargameManagerId, new ClanWargameInviteReceivedPacket(challenge.WargameId, own.Name));

            foreach (var member in _clans.Online(target.Id))
                Say(member, PlayerMessage.PmWargameFeudYouveBeenChallenged, ("inviterClan", own.Name));
        }

        /// <summary>FeudChallengeResponse: the challenged leader's Accept or Cancel.</summary>
        public void FeudChallengeResponse(Client client, string challengerName, bool accept)
        {
            if (!TryLeader(client, PlayerMessage.PmWargameFeudAcceptFailedYouAreNotLeader, out var own))
                return;

            var name = (challengerName ?? "").Trim();
            var challenger = _clans.FindByName(name);

            if (challenger == null)
            {
                Say(client, PlayerMessage.PmWargameFeudChallengerClanNotOnline, ("inviterClan", name));
                return;
            }

            Challenge challenge;

            lock (_sync)
            {
                challenge = _challenges.Find(c => c.ChallengerClanId == challenger.Id && c.TargetClanId == own.Id);

                if (challenge == null)
                {
                    Say(client, PlayerMessage.PmWargameFeudAcceptFailedNoPendingChallenge, ("inviterClan", challenger.Name));
                    return;
                }

                _challenges.Remove(challenge);
            }

            Forget(challenge);

            if (!accept)
            {
                Say(client, PlayerMessage.PmWargameFeudYouDeclined, ("inviterClan", challenger.Name));

                foreach (var member in _clans.Online(challenger.Id))
                    Say(member, IsLeader(member, challenger.Id) ? PlayerMessage.PmWargameFuedYouWereDeclined : PlayerMessage.PmWargameFeudDeclined,
                        ("clan", own.Name));

                return;
            }

            Start(challenger, own, challenge.WargameId, challenge.ChallengerCharacterId, client.Player.Id);
        }

        /// <summary>RevokeClanFeud: the challenger's leader withdraws a challenge not yet answered.</summary>
        public void RevokeClanFeud(Client client, string clanName)
        {
            if (!TryLeader(client, PlayerMessage.PmWargameFeudOnlyClanLeadersCanRevoke, out var own))
                return;

            var name = (clanName ?? "").Trim();
            var target = _clans.FindByName(name);
            Challenge challenge = null;

            lock (_sync)
            {
                if (target != null)
                {
                    challenge = _challenges.Find(c => c.ChallengerClanId == own.Id && c.TargetClanId == target.Id);

                    if (challenge != null)
                        _challenges.Remove(challenge);
                }
            }

            if (challenge == null)
            {
                Say(client, PlayerMessage.PmWargameFeudRevokeFailedNoChallenge, ("clan", target?.Name ?? name));
                return;
            }

            Forget(challenge);

            Say(client, PlayerMessage.PmWargameFeudYouRevoked, ("clan", target.Name));

            var targetLeader = LeaderOnline(target.Id);

            if (targetLeader != null)
                Say(targetLeader, PlayerMessage.PmWargameFeudWasRevoked, ("inviterClan", own.Name));
        }

        /// <summary>SurrenderClanFeud: a leader gives the feud to the other clan.</summary>
        public void SurrenderClanFeud(Client client, string clanName)
        {
            if (!TryLeader(client, PlayerMessage.PmWargameFeudOnlyClanLeadersCanSurrender, out var own))
                return;

            var name = (clanName ?? "").Trim();
            var other = _clans.FindByName(name);
            Feud feud = null;

            lock (_sync)
                if (other != null)
                    feud = _feuds.Values.FirstOrDefault(f => f.Between(own.Id, other.Id));

            if (feud == null)
            {
                Say(client, PlayerMessage.PmWargameFeudYouAreNotInFeudWithClan, ("clan", other?.Name ?? name));
                return;
            }

            Say(client, PlayerMessage.PmWargameFeudYouSurrenderedYourClan, ("clan", other.Name));
            End(feud, Outcome.Won, other.Id);
        }

        #endregion

        #region The running feud

        public enum Outcome
        {
            /// <summary>The winner named won.</summary>
            Won,
            Tied,
            Cancelled
        }

        /// <summary>
        /// Starts a feud between two clans: everyone in either who is online is told, put in it and
        /// shown to everyone around as being in it, and squads holding both clans are split.
        /// Returns null if the two are already at feud. The two characters are who made the
        /// challenge and who accepted it, when anyone did.
        /// </summary>
        public Feud Start(ClanEntry challenger, ClanEntry target, uint wargameId = 0, uint challengerCharacterId = 0, uint targetCharacterId = 0)
        {
            if (challenger == null || target == null || challenger.Id == target.Id)
                return null;

            Feud feud;
            List<Challenge> moot;

            lock (_sync)
            {
                if (_feuds.Values.Any(f => f.Between(challenger.Id, target.Id)))
                    return null;

                // A challenge the other way that was still open is moot now.
                moot = _challenges.Where(c => c.ChallengerClanId == challenger.Id && c.TargetClanId == target.Id
                    || c.ChallengerClanId == target.Id && c.TargetClanId == challenger.Id).ToList();
                _challenges.RemoveAll(moot.Contains);

                feud = new Feud
                {
                    Id = wargameId != 0 ? wargameId : _nextId++,
                    ChallengerClanId = challenger.Id,
                    TargetClanId = target.Id,
                    EndTick = Now() + (long)Duration.TotalMilliseconds,
                    ChallengerCharacterId = challengerCharacterId,
                    TargetCharacterId = targetCharacterId
                };

                _feuds[feud.Id] = feud;
            }

            foreach (var challenge in moot)
                Forget(challenge);

            Keep(feud);

            Logger.WriteLog(LogType.Debug, $"Clan feud {feud.Id}: {challenger.Name} ({challenger.Id}) against {target.Name} ({target.Id}), for {Duration.TotalMinutes:0} minutes.");

            foreach (var (clan, other) in new[] { (challenger, target), (target, challenger) })
                foreach (var member in _clans.Online(clan.Id))
                {
                    Say(member, PlayerMessage.PmWargameFeudStarted, ("clan", other.Name));
                    SendFeud(member, feud);
                    ShowWargameData(member);
                }

            SeparateSquads(feud);

            return feud;
        }

        /// <summary>
        /// Ends a feud: each member online hears how it went and has it taken off their tracker,
        /// and everyone around sees them out of it. A feud that was won costs the losing clan's
        /// members what they wagered (<see cref="Forfeit"/>).
        /// </summary>
        public void End(Feud feud, Outcome outcome, uint winnerClanId = 0)
        {
            if (feud == null)
                return;

            lock (_sync)
                if (!_feuds.Remove(feud.Id))
                    return;

            Forget(feud);

            Logger.WriteLog(LogType.Debug, $"Clan feud {feud.Id} ended: {outcome}{(outcome == Outcome.Won ? $", clan {winnerClanId} won" : "")}, {feud.ChallengerKills} : {feud.TargetKills}.");

            foreach (var clanId in new[] { feud.ChallengerClanId, feud.TargetClanId })
                foreach (var member in _clans.Online(clanId))
                {
                    switch (outcome)
                    {
                        case Outcome.Won when clanId == winnerClanId:
                            member.CallMethod(SysEntity.ClientWargameManagerId, WargameResultPacket.Victory(feud.Id));
                            Say(member, PlayerMessage.PmWargameFeudYourClanWon);
                            PvpPrestige.FeudWon(member);
                            break;

                        case Outcome.Won:
                            member.CallMethod(SysEntity.ClientWargameManagerId, WargameResultPacket.Defeat(feud.Id));
                            Say(member, PlayerMessage.PmWargameFeudYourClanLost);
                            break;

                        case Outcome.Tied:
                            member.CallMethod(SysEntity.ClientWargameManagerId, WargameResultPacket.Tied(feud.Id));
                            Say(member, PlayerMessage.PmWargameFeudEndedInTie);
                            break;

                        default:
                            member.CallMethod(SysEntity.ClientWargameManagerId, WargameResultPacket.Cancelled(feud.Id));
                            break;
                    }

                    member.CallMethod(SysEntity.ClientWargameManagerId, new RemoveFromClanWargamePacket(feud.Id));
                    ShowWargameData(member);
                }

            if (outcome == Outcome.Won && feud.Involves(winnerClanId) && Forfeit != null)
            {
                var loserClanId = feud.OtherThan(winnerClanId);

                Try($"forfeiting the wagers of feud {feud.Id}", () => Forfeit(loserClanId, winnerClanId, feud.CharacterFor(winnerClanId),
                    _clans.Find(loserClanId)?.Name ?? "", _clans.Find(winnerClanId)?.Name ?? ""));
            }
        }

        /// <summary>The feud's clock has run out: the clan with more kills wins, equal is a tie.</summary>
        public void Expire(Feud feud)
        {
            if (feud == null)
                return;

            if (feud.ChallengerKills == feud.TargetKills)
                End(feud, Outcome.Tied);
            else
                End(feud, Outcome.Won, feud.ChallengerKills > feud.TargetKills ? feud.ChallengerClanId : feud.TargetClanId);
        }

        /// <summary>Ends the feuds whose time is up. From the map channel worker.</summary>
        public void Worker()
        {
            List<Feud> due;
            var now = Now();

            lock (_sync)
                due = _feuds.Values.Where(f => now >= f.EndTick).ToList();

            foreach (var feud in due)
                Expire(feud);
        }

        /// <summary>
        /// A kill between the two clans of a feud (Pvp.Defeat): counted for the killer's clan, the
        /// score sent, and the killer's prestige given (PvpPrestige.FeudKill). A kill across too
        /// many levels has no credit (PvpPrestige.HasKillCredit): it is not counted and gives no
        /// prestige, and both are told (PM_WARGAME_FEUD_NO_KILL_CREDIT_LEVELS). Returns whether
        /// the kill was counted.
        /// </summary>
        public bool Kill(Client killer, Client victim)
        {
            var killerClan = killer?.Player?.ClanId ?? 0;
            var victimClan = victim?.Player?.ClanId ?? 0;
            Feud feud;

            lock (_sync)
            {
                feud = _feuds.Values.FirstOrDefault(f => f.Between(killerClan, victimClan) && killerClan != victimClan);

                if (feud == null)
                    return false;
            }

            if (!PvpPrestige.HasKillCredit(killer.Player.Level, victim.Player.Level))
            {
                var clanName = _clans.Find(killerClan)?.Name ?? "";

                foreach (var told in new[] { killer, victim })
                    Say(told, PlayerMessage.PmWargameFeudNoKillCreditLevels,
                        ("killerClanName", clanName), ("victimName", victim.Player.FamilyName), ("killerName", killer.Player.FamilyName));

                return false;
            }

            lock (_sync)
            {
                // Ended between the two looks: nothing to count.
                if (!_feuds.ContainsKey(feud.Id))
                    return false;

                if (killerClan == feud.ChallengerClanId)
                    feud.ChallengerKills++;
                else
                    feud.TargetKills++;
            }

            Keep(feud);

            var killerClanName = _clans.Find(killerClan)?.Name ?? "";
            var victimClanName = _clans.Find(victimClan)?.Name ?? "";

            Say(victim, PlayerMessage.PmWargameFeudYouWereKilled, ("killerName", killer.Player.FamilyName), ("killerClanName", killerClanName));
            Say(killer, PlayerMessage.PmWargameFeudYouMadeAKill, ("victimName", victim.Player.FamilyName), ("victimClanName", victimClanName));

            // A feud kill generates prestige for the killer and steals some of the victim's.
            PvpPrestige.FeudKill(killer, victim);

            foreach (var clanId in new[] { feud.ChallengerClanId, feud.TargetClanId })
            {
                var ours = clanId == feud.ChallengerClanId ? feud.ChallengerKills : feud.TargetKills;
                var theirs = clanId == feud.ChallengerClanId ? feud.TargetKills : feud.ChallengerKills;
                var packet = new WargameScoreboardPacket(feud.Id, ours, theirs, victim.AccountEntry?.Id ?? 0, killer.AccountEntry?.Id ?? 0);

                foreach (var member in _clans.Online(clanId))
                    member.CallMethod(SysEntity.ClientWargameManagerId, packet);
            }

            return true;
        }

        #endregion

        #region Members coming and going

        /// <summary>
        /// On entering the world, a login or a map link: the player's clan's feuds, again. Sent
        /// again after a map link it only refreshes the tracker. WargameData needs nothing here -
        /// it goes with the player's entity (ManifestationManager.CreatePlayerEntityData).
        /// </summary>
        public void PlayerEnteredWorld(Client client)
        {
            var clanId = client?.Player?.ClanId ?? 0;

            foreach (var feud in FeudsOf(clanId))
                SendFeud(client, feud);

            RemindOfRestoredChallenges(client, clanId);
        }

        /// <summary>
        /// A challenge read back at startup, to the challenged clan's leader once: their client
        /// lost its indicator with the restart. A challenge made since was put to them as it was made.
        /// </summary>
        private void RemindOfRestoredChallenges(Client client, uint clanId)
        {
            if (clanId == 0 || !IsLeader(client, clanId))
                return;

            List<Challenge> due;

            lock (_sync)
                due = _challenges.Where(c => c.Restored && c.TargetClanId == clanId && _reminded.Add((c.WargameId, client.Player.Id))).ToList();

            foreach (var challenge in due)
                client.CallMethod(SysEntity.ClientWargameManagerId,
                    new ClanWargameInviteReceivedPacket(challenge.WargameId, _clans.Find(challenge.ChallengerClanId)?.Name ?? ""));
        }

        /// <summary>A player has just joined a clan: into its feuds, and out of any squad that now mixes feuding clans.</summary>
        public void MemberJoined(Client client)
        {
            var feuds = FeudsOf(client?.Player?.ClanId ?? 0);

            if (feuds.Count == 0)
                return;

            foreach (var feud in feuds)
            {
                SendFeud(client, feud);
                SeparateSquads(feud);
            }

            ShowWargameData(client);
        }

        /// <summary>A player has just left a clan (ClanId already 0): out of its feuds.</summary>
        public void MemberLeft(Client client, uint oldClanId)
        {
            var feuds = FeudsOf(oldClanId);

            if (feuds.Count == 0 || client?.Player == null)
                return;

            foreach (var feud in feuds)
                client.CallMethod(SysEntity.ClientWargameManagerId, new RemoveFromClanWargamePacket(feud.Id));

            ShowWargameData(client);
        }

        /// <summary>
        /// A clan is disbanding (before its members are cleared): its feuds are cancelled and its
        /// challenges dropped. A challenge it had made is taken back from the other leader.
        /// </summary>
        public void ClanDisbanded(uint clanId)
        {
            List<Challenge> dropped;

            lock (_sync)
            {
                dropped = _challenges.Where(c => c.ChallengerClanId == clanId || c.TargetClanId == clanId).ToList();
                _challenges.RemoveAll(c => c.ChallengerClanId == clanId || c.TargetClanId == clanId);
            }

            foreach (var challenge in dropped)
                Forget(challenge);

            var clanName = _clans.Find(clanId)?.Name ?? "";

            foreach (var challenge in dropped.Where(c => c.ChallengerClanId == clanId))
            {
                var leader = LeaderOnline(challenge.TargetClanId);

                if (leader != null)
                    Say(leader, PlayerMessage.PmWargameFeudWasRevoked, ("inviterClan", clanName));
            }

            foreach (var feud in FeudsOf(clanId))
                End(feud, Outcome.Cancelled);
        }

        #endregion

        #region Keeping

        /// <summary>
        /// At startup, after the clans are loaded: the feuds and challenges kept through the
        /// restart, back as they were - each feud's clock set from its end time, so one that ended
        /// while the server was down ends on the first tick - and from now on every change kept in
        /// <paramref name="store"/>. A row naming a clan that is gone, or a challenge between clans
        /// already at feud, is dropped. New ids follow on from the highest kept.
        /// </summary>
        public void Load(IStore store)
        {
            Store = store;

            if (store == null)
                return;

            List<ClanFeudEntry> feudRows;
            List<ClanFeudChallengeEntry> challengeRows;

            try
            {
                (feudRows, challengeRows) = store.Load();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Clan feuds could not be read back: {e.Message}");
                return;
            }

            var dropFeuds = new List<uint>();
            var dropChallenges = new List<uint>();
            var now = Now();
            var utcNow = UtcNow();

            lock (_sync)
            {
                foreach (var row in feudRows)
                {
                    if (_clans.Find(row.ChallengerClanId) == null || _clans.Find(row.TargetClanId) == null || row.ChallengerClanId == row.TargetClanId
                        || _feuds.ContainsKey(row.Id) || _feuds.Values.Any(f => f.Between(row.ChallengerClanId, row.TargetClanId)))
                    {
                        dropFeuds.Add(row.Id);
                        continue;
                    }

                    _feuds[row.Id] = new Feud
                    {
                        Id = row.Id,
                        ChallengerClanId = row.ChallengerClanId,
                        TargetClanId = row.TargetClanId,
                        EndTick = now + (row.EndsAt - utcNow),
                        ChallengerKills = row.ChallengerKills,
                        TargetKills = row.TargetKills,
                        ChallengerCharacterId = row.ChallengerCharacterId,
                        TargetCharacterId = row.TargetCharacterId
                    };
                }

                foreach (var row in challengeRows)
                {
                    if (_clans.Find(row.ChallengerClanId) == null || _clans.Find(row.TargetClanId) == null || row.ChallengerClanId == row.TargetClanId
                        || _feuds.ContainsKey(row.WargameId) || _feuds.Values.Any(f => f.Between(row.ChallengerClanId, row.TargetClanId))
                        || _challenges.Any(c => c.WargameId == row.WargameId
                            || c.ChallengerClanId == row.ChallengerClanId && c.TargetClanId == row.TargetClanId
                            || c.ChallengerClanId == row.TargetClanId && c.TargetClanId == row.ChallengerClanId))
                    {
                        dropChallenges.Add(row.WargameId);
                        continue;
                    }

                    _challenges.Add(new Challenge
                    {
                        WargameId = row.WargameId,
                        ChallengerClanId = row.ChallengerClanId,
                        TargetClanId = row.TargetClanId,
                        ChallengerCharacterId = row.ChallengerCharacterId,
                        Restored = true
                    });
                }

                var highest = feudRows.Select(r => r.Id).Concat(challengeRows.Select(r => r.WargameId)).DefaultIfEmpty(0u).Max();

                if (highest >= _nextId)
                    _nextId = highest + 1;
            }

            foreach (var id in dropFeuds)
                Try($"dropping feud {id}", () => store.DeleteFeud(id));

            foreach (var id in dropChallenges)
                Try($"dropping challenge {id}", () => store.DeleteChallenge(id));

            Logger.WriteLog(LogType.Initialize, $"Clan feuds: {feudRows.Count - dropFeuds.Count} feuds and {challengeRows.Count - dropChallenges.Count} challenges read back"
                + (dropFeuds.Count + dropChallenges.Count > 0 ? $", {dropFeuds.Count + dropChallenges.Count} dropped." : "."));
        }

        private void Keep(Feud feud)
        {
            var store = Store;

            if (store == null)
                return;

            ClanFeudEntry row;

            lock (_sync)
                row = new ClanFeudEntry
                {
                    Id = feud.Id,
                    ChallengerClanId = feud.ChallengerClanId,
                    TargetClanId = feud.TargetClanId,
                    EndsAt = UtcNow() + (feud.EndTick - Now()),
                    ChallengerKills = feud.ChallengerKills,
                    TargetKills = feud.TargetKills,
                    ChallengerCharacterId = feud.ChallengerCharacterId,
                    TargetCharacterId = feud.TargetCharacterId
                };

            Try($"saving feud {feud.Id}", () => store.SaveFeud(row));
        }

        private void Forget(Feud feud)
        {
            var store = Store;

            if (store != null)
                Try($"removing feud {feud.Id}", () => store.DeleteFeud(feud.Id));
        }

        private void Keep(Challenge challenge)
        {
            var store = Store;

            if (store != null)
                Try($"saving challenge {challenge.WargameId}", () => store.SaveChallenge(new ClanFeudChallengeEntry
                {
                    WargameId = challenge.WargameId,
                    ChallengerClanId = challenge.ChallengerClanId,
                    TargetClanId = challenge.TargetClanId,
                    ChallengerCharacterId = challenge.ChallengerCharacterId
                }));
        }

        private void Forget(Challenge challenge)
        {
            var store = Store;

            if (store != null)
                Try($"removing challenge {challenge.WargameId}", () => store.DeleteChallenge(challenge.WargameId));
        }

        /// <summary>A store that cannot be reached costs the feud its restart, not the server its feud.</summary>
        private static void Try(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Clan feuds: {what} failed: {e.Message}");
            }
        }

        #endregion

        #region Queries

        public List<Feud> FeudsOf(uint clanId)
        {
            if (clanId == 0)
                return new List<Feud>();

            lock (_sync)
                return _feuds.Values.Where(f => f.Involves(clanId)).OrderBy(f => f.Id).ToList();
        }

        public bool AreFeuding(uint clanA, uint clanB)
        {
            if (clanA == 0 || clanB == 0 || clanA == clanB)
                return false;

            lock (_sync)
                return _feuds.Values.Any(f => f.Between(clanA, clanB));
        }

        /// <summary>Whether a squad of players from these clans would hold two clans at feud.</summary>
        public bool AnyFeuding(IEnumerable<uint> clanIds)
        {
            var clans = clanIds.Where(c => c != 0).Distinct().ToList();

            for (var i = 0; i < clans.Count; i++)
                for (var j = i + 1; j < clans.Count; j++)
                    if (AreFeuding(clans[i], clans[j]))
                        return true;

            return false;
        }

        /// <summary>The player's feuds in WargameData: {feudId: side}, true for the challenging clan. Wargames.DataOf adds their duel.</summary>
        public Dictionary<uint, bool> WargameDataOf(Manifestation player)
        {
            var clanId = player?.ClanId ?? 0;

            return FeudsOf(clanId).ToDictionary(f => f.Id, f => f.ChallengerClanId == clanId);
        }

        public int SecondsLeft(Feud feud) => (int)Math.Max(0, (feud.EndTick - Now() + 999) / 1000);

        #endregion

        #region Helpers

        private void SendFeud(Client client, Feud feud)
        {
            var clanId = client?.Player?.ClanId ?? 0;

            if (!feud.Involves(clanId))
                return;

            var mine = clanId == feud.ChallengerClanId;

            client.CallMethod(SysEntity.ClientWargameManagerId, new AddToClanWargamePacket(feud.Id,
                _clans.Find(clanId)?.Name ?? "",
                _clans.Find(feud.OtherThan(clanId))?.Name ?? "",
                SecondsLeft(feud),
                mine ? feud.ChallengerKills : feud.TargetKills,
                mine ? feud.TargetKills : feud.ChallengerKills));
        }

        /// <summary>
        /// The player's WargameData - their feuds and any duel - to everyone who can see them,
        /// themselves included, and who is now an enemy (Wargames.Show).
        /// </summary>
        private static void ShowWargameData(Client client) => Wargames.Show(client);

        private void SeparateSquads(Feud feud)
        {
            try
            {
                PartyManager.Instance.SeparateFeuding(feud.ChallengerClanId, feud.TargetClanId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Clan feud {feud.Id}: separating squads failed: {e.Message}");
            }
        }

        private bool TryLeader(Client client, PlayerMessage refusal, out ClanEntry clan)
        {
            clan = null;

            var player = client?.Player;

            if (player == null)
                return false;

            if (player.ClanId != 0 && _clans.RankOf(player.ClanId, player.Id) is byte rank && rank >= ClanRank.MinRankToChallenge)
                clan = _clans.Find(player.ClanId);

            if (clan == null)
            {
                Say(client, refusal);
                return false;
            }

            return true;
        }

        private bool IsLeader(Client client, uint clanId) =>
            client?.Player != null && _clans.RankOf(clanId, client.Player.Id) is byte rank && rank >= ClanRank.MinRankToChallenge;

        private Client LeaderOnline(uint clanId) => _clans.Online(clanId).FirstOrDefault(c => IsLeader(c, clanId));

        private static void Say(Client client, PlayerMessage message, params (string Key, string Value)[] args)
        {
            client?.CallMethod(SysEntity.ClientWargameManagerId,
                new DisplayWargameMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value ?? "")));
        }

        #endregion

        /// <summary>The live server's store: the character database's clan_feud and clan_feud_challenge tables.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public (List<ClanFeudEntry> Feuds, List<ClanFeudChallengeEntry> Challenges) Load()
            {
                using var unitOfWork = _factory.CreateChar();
                return (unitOfWork.ClanFeuds.GetFeuds(), unitOfWork.ClanFeuds.GetChallenges());
            }

            public void SaveFeud(ClanFeudEntry feud)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ClanFeuds.SaveFeud(feud);
            }

            public void DeleteFeud(uint id)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ClanFeuds.DeleteFeud(id);
            }

            public void SaveChallenge(ClanFeudChallengeEntry challenge)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ClanFeuds.SaveChallenge(challenge);
            }

            public void DeleteChallenge(uint wargameId)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ClanFeuds.DeleteChallenge(wargameId);
            }
        }

        /// <summary>The live server's clans: ClanManager's cache and the connections in the world.</summary>
        private sealed class ServerClans : IClans
        {
            public ClanEntry Find(uint clanId) => ClanManager.Instance.Clans.GetValueOrDefault(clanId)?.Value;

            public ClanEntry FindByName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                return ClanManager.Instance.Clans.Values
                    .Select(c => c.Value)
                    .FirstOrDefault(c => c != null && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            public byte? RankOf(uint clanId, uint characterId) => ClanManager.Instance.GetClanMember(clanId, characterId)?.Rank;

            public List<Client> Online(uint clanId)
            {
                lock (Server.Clients)
                    return Server.Clients.Where(c => c?.Player != null && c.Player.Id != 0 && c.Player.ClanId == clanId
                        && (c.State == ClientState.Ingame || c.State == ClientState.Teleporting || c.State == ClientState.Loading)).ToList();
            }
        }
    }
}

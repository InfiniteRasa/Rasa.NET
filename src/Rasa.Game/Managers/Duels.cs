using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Wargame.Server;
    using Structures;

    /// <summary>
    /// Duel Wargames: one player challenges another, the other accepts, and the two are enemies
    /// (Pvp) until one has made the kills the duel was for, one surrenders or leaves, or its time
    /// is up.
    ///
    /// From the client (client/wargame.py, communicator.py, ui/radialwindow.py):
    ///
    ///  - /duel &lt;name&gt; [minutes] [kills] and the radial menu's Invite to Duel send
    ///    ChallengeUserToWargameByName(targetName, timeMins, maxKills); the menu sends the name
    ///    alone, and a missing or unreadable number is 0.
    ///  - The challenger gets ChallengingToWargameDuel - an indicator whose dialog has Revoke,
    ///    which sends WargameChallengeRevoked(). The one challenged gets ChallengedToWargameDuel -
    ///    an indicator whose dialog has Accept and Decline, which send WargameChallengeResponse(1,
    ///    None) and (0, PM_WARGAME_REFUSED). Neither names the wargame: a player holds one
    ///    challenge at a time, either way.
    ///  - A refused challenge is WargameChallengeRefused to both; a revoked or lapsed one is
    ///    RevokeWargameChallenge. An accepted one starts: WargameStarted (the tracker and the start
    ///    sound), SetWargameMaxKills and DisplayWargameTimer for the tracker, and each duelist's
    ///    WargameData {wargameId: side} to everyone around them, which with TargetCategory HOSTILE
    ///    makes them enemies to each other (Pvp).
    ///  - A defeat (Pvp.Defeat) is a kill: WargameScoreboard to both. It ends with WargameVictory,
    ///    WargameDefeat or WargameTied, on which the client shows "Duel Wargame: You Win!" / "You
    ///    Lost!" / "Tie Game!" and says PM_WARGAME_VICTORY / DEFEAT / TIED itself.
    ///  - /surrender sends SurrenderWargame(): the duel goes to the other side.
    ///
    /// The refusals are the client's messages: a name nobody in the world has, oneself, the other
    /// map, a duel or a challenge already open on either side, an ignore, the same squad, a squad on
    /// one side only, and a squad invitation still open. Two players each in a squad of their own
    /// challenging each other is a Squad Wargame (SquadWargames), which takes the challenge from here.
    ///
    /// Ours, since nothing in the client says: a challenge lapses after <see cref="ChallengeTimeout"/>;
    /// a duel is for one kill unless more were asked for (at most <see cref="MaxKillsLimit"/>) and
    /// lasts <see cref="DefaultMinutes"/> unless a time was asked for (at most
    /// <see cref="MaxMinutes"/>); when its time is up the one with more kills wins and equal is a
    /// tie; leaving the map or the world forfeits it to the other, who is told PM_WARGAME_PLAYER_LEFT,
    /// while the one who left is told RemoveFromWargame (PM_WARGAME_YOU_LEFT). Wargame ids start at
    /// <see cref="FirstWargameId"/>, clear of the feuds' own. Nothing is saved: a restart ends every
    /// duel and challenge.
    /// </summary>
    public class Duels
    {
        private static Duels _instance;
        private static readonly object InstanceLock = new object();

        public static Duels Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new Duels();

                return _instance;
            }
        }

        public static readonly TimeSpan ChallengeTimeout = TimeSpan.FromSeconds(60);
        public const int DefaultMinutes = 10;
        public const int MaxMinutes = 60;
        public const int MaxKillsLimit = 99;
        public const uint FirstWargameId = 1_000_000_000;

        public sealed class Challenge
        {
            public uint WargameId { get; set; }
            public Client Challenger { get; set; }
            public Client Target { get; set; }
            public long ExpiresTick { get; set; }
            public int Minutes { get; set; }
            public int MaxKills { get; set; }

            public bool Involves(Client client) => client != null && (Challenger == client || Target == client);
            public Client Other(Client client) => Challenger == client ? Target : Challenger;
        }

        public sealed class Duel
        {
            public uint WargameId { get; set; }
            public Client Challenger { get; set; }
            public Client Target { get; set; }
            public long EndTick { get; set; }
            public int MaxKills { get; set; }
            public int ChallengerKills { get; set; }
            public int TargetKills { get; set; }

            public bool Involves(Client client) => client != null && (Challenger == client || Target == client);
            public bool Involves(Manifestation player) => player != null && (Challenger?.Player == player || Target?.Player == player);
            public Client Other(Client client) => Challenger == client ? Target : Challenger;
            public int KillsOf(Client client) => client == Challenger ? ChallengerKills : TargetKills;
        }

        private readonly object _sync = new object();
        private readonly List<Challenge> _challenges = new List<Challenge>();
        private readonly List<Duel> _duels = new List<Duel>();
        private uint _nextId = FirstWargameId;

        /// <summary>The clock duels run on; replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Who is in the world by name; replaceable for tests.</summary>
        public Func<string, Client> FindByName { get; set; } = FindIngame;

        public List<Duel> Running
        {
            get { lock (_sync) return _duels.ToList(); }
        }

        public List<Challenge> Pending
        {
            get { lock (_sync) return _challenges.ToList(); }
        }

        #region Client requests

        /// <summary>ChallengeUserToWargameByName: /duel and the radial menu's Invite to Duel.</summary>
        public void ChallengeUserToWargameByName(Client client, string targetName, int timeMins, int maxKills)
        {
            if (client?.Player == null || client.State != ClientState.Ingame)
                return;

            var name = (targetName ?? "").Trim();
            var target = name.Length == 0 ? null : FindByName(name);

            if (target?.Player == null)
            {
                Say(client, PlayerMessage.PmWargameNoTargetByName, ("target", name));
                return;
            }

            name = target.Player.FamilyName ?? name;

            if (target == client)
            {
                Say(client, PlayerMessage.PmWargameCannotChallengeYourself);
                return;
            }

            // Both in squads, and not the same one: a Squad Wargame.
            var mySquad = client.Player.PartyId;
            var theirSquad = target.Player.PartyId;

            if (mySquad != 0 && theirSquad != 0 && mySquad != theirSquad)
            {
                SquadWargames.Instance.ChallengeSquad(client, target, timeMins, maxKills);
                return;
            }

            if (Refusal(client, target) is (PlayerMessage message, bool named))
            {
                if (named)
                    Say(client, message, ("target", name));
                else
                    Say(client, message);

                return;
            }

            Challenge challenge;

            lock (_sync)
            {
                // Checked again under the lock: two challenges crossing.
                if (_duels.Any(d => d.Involves(client) || d.Involves(target)) || _challenges.Any(c => c.Involves(client) || c.Involves(target)))
                {
                    Say(client, PlayerMessage.PmWargameAlreadyChallenged, ("target", name));
                    return;
                }

                challenge = new Challenge
                {
                    WargameId = NextWargameId(),
                    Challenger = client,
                    Target = target,
                    ExpiresTick = Now() + (long)ChallengeTimeout.TotalMilliseconds,
                    Minutes = timeMins <= 0 ? DefaultMinutes : Math.Min(timeMins, MaxMinutes),
                    MaxKills = maxKills <= 0 ? 1 : Math.Min(maxKills, MaxKillsLimit)
                };

                _challenges.Add(challenge);
            }

            var challengerName = client.Player.FamilyName ?? "";

            Logger.WriteLog(LogType.Debug, $"Duel {challenge.WargameId}: {challengerName} challenged {name} ({challenge.MaxKills} kills, {challenge.Minutes} minutes).");

            client.CallMethod(SysEntity.ClientWargameManagerId, new ChallengingToWargameDuelPacket(challenge.WargameId, name));
            Say(client, PlayerMessage.PmWargameDuelPlayerChallenged, ("target", name));

            target.CallMethod(SysEntity.ClientWargameManagerId, new ChallengedToWargameDuelPacket(challenge.WargameId, challengerName));
            Say(target, PlayerMessage.PmWargameDuelPlayerChallengeReceived, ("challenger", challengerName));
        }

        /// <summary>WargameChallengeResponse: the challenged player's Accept or Decline.</summary>
        public void WargameChallengeResponse(Client client, bool accepted)
        {
            if (client?.Player == null)
                return;

            Challenge challenge;

            lock (_sync)
            {
                challenge = _challenges.Find(c => c.Target == client);

                if (challenge == null)
                {
                    Logger.WriteLog(LogType.Debug, $"WargameChallengeResponse from {client.Player.FamilyName} with no challenge to answer.");
                    return;
                }

                _challenges.Remove(challenge);
            }

            var challenger = challenge.Challenger;
            var challengerName = challenger?.Player?.FamilyName ?? "";
            var targetName = client.Player.FamilyName ?? "";

            if (!accepted)
            {
                client.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(challenge.WargameId, true));
                Say(client, PlayerMessage.PmWargameYouRefused, ("challenger", challengerName));

                challenger?.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(challenge.WargameId, false));
                Say(challenger, PlayerMessage.PmWargameRefused, ("target", targetName));
                return;
            }

            // The challenger has gone, or the two are no longer on one map.
            if (!InWorld(challenger) || !InWorld(client) || challenger.Player.MapChannel != client.Player.MapChannel)
            {
                Say(client, PlayerMessage.PmWargameResponseNotOnSameMap, ("challenger", challengerName));
                client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

                Say(challenger, PlayerMessage.PmWargameRespondedButNotOnSameMap, ("target", targetName));
                challenger?.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
                return;
            }

            Start(challenge);
        }

        /// <summary>WargameChallengeRevoked: the challenger's Revoke.</summary>
        public void WargameChallengeRevoked(Client client)
        {
            Challenge challenge;

            lock (_sync)
            {
                challenge = _challenges.Find(c => c.Challenger == client);

                if (challenge == null)
                    return;

                _challenges.Remove(challenge);
            }

            client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
            Say(client, PlayerMessage.PmWargameChallengeRevoked);

            challenge.Target?.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
            Say(challenge.Target, PlayerMessage.PmWargameRevoked, ("player", client.Player?.FamilyName ?? ""));
        }

        /// <summary>SurrenderWargame: /surrender - the duel goes to the other side.</summary>
        public void SurrenderWargame(Client client)
        {
            var duel = DuelOf(client);

            if (duel == null)
            {
                Say(client, PlayerMessage.PmWargameNotInSquadOrDuel);
                return;
            }

            Logger.WriteLog(LogType.Debug, $"Duel {duel.WargameId}: {client.Player?.FamilyName} surrendered.");
            End(duel, duel.Other(client));
        }

        #endregion

        #region The running duel

        private void Start(Challenge challenge)
        {
            var duel = new Duel
            {
                WargameId = challenge.WargameId,
                Challenger = challenge.Challenger,
                Target = challenge.Target,
                EndTick = Now() + challenge.Minutes * 60_000L,
                MaxKills = challenge.MaxKills
            };

            lock (_sync)
                _duels.Add(duel);

            Logger.WriteLog(LogType.Debug, $"Duel {duel.WargameId}: {duel.Challenger.Player.FamilyName} against {duel.Target.Player.FamilyName}.");

            Say(duel.Challenger, PlayerMessage.PmWargameYourChallengeAccepted, ("target", duel.Target.Player.FamilyName ?? ""));
            Say(duel.Target, PlayerMessage.PmWargameYouAccepted);

            foreach (var side in new[] { duel.Challenger, duel.Target })
            {
                var other = duel.Other(side);

                side.CallMethod(SysEntity.ClientWargameManagerId, new WargameStartedPacket(duel.WargameId, new List<uint> { UserId(other) }));
                side.CallMethod(SysEntity.ClientWargameManagerId, new SetWargameMaxKillsPacket(duel.WargameId, duel.MaxKills));
                side.CallMethod(SysEntity.ClientWargameManagerId, new DisplayWargameTimerPacket(duel.WargameId, MillisecondsLeft(duel)));
            }

            Wargames.Show(duel.Challenger);
            Wargames.Show(duel.Target);
        }

        /// <summary>
        /// A defeat between the two duelists (Pvp.Defeat): counted, the score sent to both, and the
        /// duel over if the killer has made its kills. Returns whether it was a duel kill.
        /// </summary>
        public bool Kill(Client killer, Client victim)
        {
            Duel duel;
            int killerKills, victimKills;

            lock (_sync)
            {
                duel = _duels.Find(d => d.Involves(killer) && d.Involves(victim) && killer != victim);

                if (duel == null)
                    return false;

                if (killer == duel.Challenger)
                    duel.ChallengerKills++;
                else
                    duel.TargetKills++;

                killerKills = duel.KillsOf(killer);
                victimKills = duel.KillsOf(victim);
            }

            var killerId = UserId(killer);
            var victimId = UserId(victim);

            killer.CallMethod(SysEntity.ClientWargameManagerId, new WargameScoreboardPacket(duel.WargameId, killerKills, victimKills, victimId, killerId));
            victim.CallMethod(SysEntity.ClientWargameManagerId, new WargameScoreboardPacket(duel.WargameId, victimKills, killerKills, victimId, killerId));

            if (killerKills >= duel.MaxKills)
                End(duel, killer);

            return true;
        }

        /// <summary>
        /// Ends a duel: the winner is told Victory and the other Defeat, or both a tie for no
        /// winner, and both are out of it for everyone around.
        /// </summary>
        public void End(Duel duel, Client winner)
        {
            if (duel == null)
                return;

            lock (_sync)
                if (!_duels.Remove(duel))
                    return;

            Logger.WriteLog(LogType.Debug, $"Duel {duel.WargameId} ended: {(winner == null ? "a tie" : $"{winner.Player?.FamilyName} won")}, {duel.ChallengerKills} : {duel.TargetKills}.");

            foreach (var side in new[] { duel.Challenger, duel.Target })
            {
                if (side == null)
                    continue;

                side.CallMethod(SysEntity.ClientWargameManagerId,
                    winner == null ? WargameResultPacket.Tied(duel.WargameId)
                    : winner == side ? WargameResultPacket.Victory(duel.WargameId)
                    : WargameResultPacket.Defeat(duel.WargameId));

                if (InWorld(side))
                    Wargames.Show(side);
            }
        }

        /// <summary>Lapsed challenges and duels whose time is up. From the map channel worker.</summary>
        public void Worker()
        {
            List<Challenge> lapsed;
            List<Duel> due;
            var now = Now();

            lock (_sync)
            {
                lapsed = _challenges.Where(c => now >= c.ExpiresTick).ToList();
                _challenges.RemoveAll(c => now >= c.ExpiresTick);
                due = _duels.Where(d => now >= d.EndTick).ToList();
            }

            foreach (var challenge in lapsed)
                foreach (var side in new[] { challenge.Challenger, challenge.Target })
                {
                    side?.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
                    Say(side, PlayerMessage.PmWargameChallengeTimedOut);
                }

            foreach (var duel in due)
                End(duel, duel.ChallengerKills == duel.TargetKills ? null
                    : duel.ChallengerKills > duel.TargetKills ? duel.Challenger : duel.Target);
        }

        /// <summary>
        /// A player leaving the map or the world (ManifestationManager.RemovePlayerCharacter): an
        /// open challenge is off, and a duel goes to the other side.
        /// </summary>
        public void PlayerLeft(Client client)
        {
            if (client == null)
                return;

            List<Challenge> challenges;
            Duel duel;

            lock (_sync)
            {
                challenges = _challenges.Where(c => c.Involves(client)).ToList();
                _challenges.RemoveAll(c => c.Involves(client));
                duel = _duels.Find(d => d.Involves(client));

                if (duel != null)
                    _duels.Remove(duel);
            }

            foreach (var challenge in challenges)
                foreach (var side in new[] { challenge.Challenger, challenge.Target })
                {
                    side?.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

                    if (side != client)
                        Say(side, PlayerMessage.PmWargameChallengeRevoked);
                }

            if (duel == null)
                return;

            var other = duel.Other(client);

            Logger.WriteLog(LogType.Debug, $"Duel {duel.WargameId}: {client.Player?.FamilyName} left; {other?.Player?.FamilyName} wins.");

            client.CallMethod(SysEntity.ClientWargameManagerId, new RemoveFromWargamePacket(duel.WargameId));

            if (other == null)
                return;

            Say(other, PlayerMessage.PmWargamePlayerLeft, ("player", client.Player?.FamilyName ?? ""));
            other.CallMethod(SysEntity.ClientWargameManagerId, WargameResultPacket.Victory(duel.WargameId));

            if (InWorld(other))
                Wargames.Show(other);
        }

        #endregion

        #region Queries

        public Duel DuelOf(Client client)
        {
            lock (_sync)
                return _duels.Find(d => d.Involves(client));
        }

        public bool IsDueling(Client client) => DuelOf(client) != null;

        /// <summary>Whether the player has a challenge open, made or received.</summary>
        public bool HasChallenge(Client client)
        {
            lock (_sync)
                return _challenges.Any(c => c.Involves(client));
        }

        /// <summary>The player's duel in WargameData: {wargameId: side}, true for the challenger.</summary>
        public Dictionary<uint, bool> WargameDataOf(Manifestation player)
        {
            var data = new Dictionary<uint, bool>();

            if (player == null)
                return data;

            lock (_sync)
                foreach (var duel in _duels.Where(d => d.Involves(player)))
                    data[duel.WargameId] = duel.Challenger?.Player == player;

            return data;
        }

        public int MillisecondsLeft(Duel duel) => (int)Math.Max(0, Math.Min(int.MaxValue, duel.EndTick - Now()));

        /// <summary>The next wargame id, for a duel or a squad wargame: the two share the numbers.</summary>
        internal uint NextWargameId()
        {
            lock (_sync)
                return _nextId++;
        }

        #endregion

        #region Helpers

        /// <summary>Why the challenger may not challenge this player, and whether the message names them; null if they may.</summary>
        private (PlayerMessage Message, bool Named)? Refusal(Client client, Client target)
        {
            lock (_sync)
            {
                if (_duels.Any(d => d.Involves(client)))
                    return (PlayerMessage.PmWargameYouAlreadyWargaming, false);

                if (_challenges.Any(c => c.Challenger == client))
                    return (PlayerMessage.PmWargameFailWaitForResponse, false);

                if (_challenges.Any(c => c.Target == client))
                    return (PlayerMessage.PmWargameAcceptOrDeclineFirst, false);

                if (_duels.Any(d => d.Involves(target)))
                    return (PlayerMessage.PmWargameTargetAlreadyWargaming, true);

                if (_challenges.Any(c => c.Target == target))
                    return (PlayerMessage.PmWargameAlreadyChallenged, true);

                if (_challenges.Any(c => c.Challenger == target))
                    return (PlayerMessage.PmWargameAlreadyChallenging, true);
            }

            if (SameMapRefusal(client, target) is PlayerMessage elsewhere)
                return (elsewhere, true);

            var mySquad = client.Player.PartyId;
            var theirSquad = target.Player.PartyId;

            if (mySquad != 0 && mySquad == theirSquad)
                return (PlayerMessage.PmWargameFailInSameParty, true);

            if (mySquad != 0 && theirSquad == 0)
                return (PlayerMessage.PmWargameTargetNotInSquad, true);

            if (theirSquad != 0)
                return (PlayerMessage.PmWargameFailTargetInSquad, true);

            return InviteRefusal(client, target) is PlayerMessage inviting ? (inviting, true) : null;
        }

        /// <summary>A challenge to a player on another map, or to one who has the challenger ignored; null otherwise. Duels and squads alike.</summary>
        internal static PlayerMessage? SameMapRefusal(Client client, Client target)
        {
            if (client.Player.MapChannel == null || client.Player.MapChannel != target.Player.MapChannel)
                return PlayerMessage.PmWargameChallengeNotOnSameMap;

            if (client.AccountEntry != null && target.Player.IgnoredPlayers.Contains(client.AccountEntry.Id))
                return PlayerMessage.PmWargameInviteIgnored;

            return null;
        }

        /// <summary>A challenge while either side has a squad invitation open, given or received; null otherwise. Duels and squads alike.</summary>
        internal static PlayerMessage? InviteRefusal(Client client, Client target)
        {
            var party = PartyManager.Instance;

            if (party.IsInviting(client))
                return PlayerMessage.PmWargameYouInvitingToParty;

            if (party.IsInvited(client))
                return PlayerMessage.PmWargameYouInvitedToParty;

            if (party.IsInviting(target))
                return PlayerMessage.PmWargameTheyInvitingToParty;

            if (party.IsInvited(target))
                return PlayerMessage.PmWargameTheyInvitedToParty;

            return null;
        }

        private static bool InWorld(Client client) =>
            client?.Player != null && client.State == ClientState.Ingame && !client.Player.Disconected;

        /// <summary>The id the scoreboard and WargameStarted name a player by: their account.</summary>
        private static uint UserId(Client client) => client?.AccountEntry?.Id ?? client?.Player?.Id ?? 0;

        private static Client FindIngame(string familyName)
        {
            lock (Server.Clients)
                return Server.Clients.Find(c => c?.Player != null && c.State == ClientState.Ingame
                                                && string.Equals(c.Player.FamilyName, familyName, StringComparison.Ordinal))
                       ?? Server.Clients.Find(c => c?.Player != null && c.State == ClientState.Ingame
                                                   && string.Equals(c.Player.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));
        }

        private static void Say(Client client, PlayerMessage message, params (string Key, string Value)[] args)
        {
            client?.CallMethod(SysEntity.ClientWargameManagerId,
                new DisplayWargameMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value ?? "")));
        }

        #endregion
    }
}

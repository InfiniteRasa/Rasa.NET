using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Wargame.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// Squad Wargames: one squad leader challenges another, the other accepts, and the two squads
    /// are enemies (Pvp) until one side has made the kills the wargame was for, a leader
    /// surrenders, a side has nobody left in it, or its time is up. Like a duel, a loss is a defeat
    /// and not a death (PlayerDeath): the client's squad wargame carries no death penalty.
    ///
    /// From the client (client/wargame.py, ui/wargamechallenge.py, ui/statusupdaterwindow.py):
    ///
    ///  - The challenge is the duel's: /duel &lt;name&gt; [minutes] [kills] and the radial menu send
    ///    ChallengeUserToWargameByName, and it is a squad wargame when both players are in squads
    ///    (Duels hands it over). Only squad leaders challenge and are challenged.
    ///  - Every member of the challenging squad gets ChallengingToWargameSquad, with the squad
    ///    challenged listed; every member of the other gets ChallengedToWargameSquad, with the
    ///    challengers listed. Both open the same window, whose buttons only a squad leader has:
    ///    Revoke for the challengers (WargameChallengeRevoked()), Accept and Decline for the
    ///    challenged (WargameChallengeResponse(1, None) / (0, PM_WARGAME_REFUSED)). Neither names
    ///    the wargame: a squad holds one challenge at a time, either way.
    ///  - A refused challenge is WargameChallengeRefused to both squads; a revoked or lapsed one is
    ///    RevokeWargameChallenge. An accepted one starts as a duel does - WargameStarted with the
    ///    other squad's account ids, SetWargameMaxKills, DisplayWargameTimer, and WargameData
    ///    {wargameId: side} on every member - but for everyone on both sides.
    ///  - Each defeat is WargameScoreboard to everyone in it, the squad's kills against the other
    ///    squad's, with the victim and the killer: the client keeps each player's kills and deaths
    ///    for its results window. It ends with WargameVictory, WargameDefeat or WargameTied, on
    ///    which the client says PM_WARGAME_SQUAD_YOU_WON / LOST / TIED and offers the results.
    ///  - /surrender sends SurrenderWargame(): a squad leader gives the wargame to the other side;
    ///    anyone else is told PM_WARGAME_NOT_ABLE_TO_SURRENDER.
    ///
    /// Ours, since nothing in the client says:
    ///  - a squad is its members in the world on the map of its leader when the challenge is made;
    ///    those still in the squad and on the map when it is accepted are the ones who fight;
    ///  - a member who leaves the squad, the map or the world is out of it (RemoveFromWargame,
    ///    PM_WARGAME_YOU_LEFT; PM_WARGAME_PLAYER_LEFT to everyone else), and a side with nobody
    ///    left loses; nobody joins a squad in a wargame or with a challenge open (PartyManager);
    ///  - the leader is whoever leads the squad at the time, so a squad handed on mid-wargame is
    ///    surrendered by its new leader;
    ///  - a squad wargame is for <see cref="DefaultMaxKills"/> kills by a side unless another number
    ///    was asked for, and the duel's limits, lapse and times apply (Duels); when the time is up
    ///    the side with more kills wins and equal is a tie. A wargame under way is not saved;
    ///    one that has ended is put on record (PvpRecords): who won, how, the two squads'
    ///    kills, and everyone who started it with their kills and deaths, those who left it too.
    /// </summary>
    public class SquadWargames
    {
        private static SquadWargames _instance;
        private static readonly object InstanceLock = new object();

        public static SquadWargames Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new SquadWargames();

                return _instance;
            }
        }

        /// <summary>A squad wargame's kills for one side, unless another number was asked for.</summary>
        public const int DefaultMaxKills = 10;

        /// <summary>One squad in a challenge or a wargame: the members taking part, and their kills.</summary>
        public sealed class Side
        {
            public uint PartyId { get; set; }
            public string LeaderName { get; set; }
            public List<Client> Members { get; } = new List<Client>();
            public int Kills { get; set; }

            public bool Has(Client client) => client != null && Members.Contains(client);
            public bool Has(Manifestation player) => player != null && Members.Any(m => m?.Player == player);
        }

        public sealed class Challenge
        {
            public uint WargameId { get; set; }
            public Side Challenger { get; set; }
            public Side Target { get; set; }
            public long ExpiresTick { get; set; }
            public int Minutes { get; set; }
            public int MaxKills { get; set; }

            public bool Involves(Client client) => Challenger.Has(client) || Target.Has(client);
            public bool Involves(uint partyId) => partyId != 0 && (Challenger.PartyId == partyId || Target.PartyId == partyId);
            public IEnumerable<Client> Everyone => Challenger.Members.Concat(Target.Members).ToList();
        }

        public sealed class War
        {
            public uint WargameId { get; set; }
            public Side Challenger { get; set; }
            public Side Target { get; set; }
            public long EndTick { get; set; }
            public int MaxKills { get; set; }

            /// <summary>For its record: when it began (UTC), where, and everyone who began it with what they have done since.</summary>
            public DateTime StartedAt { get; set; }
            public uint MapContextId { get; set; }
            public uint InstanceId { get; set; }
            public Dictionary<uint, PvpMatchPlayerEntry> Scores { get; } = new Dictionary<uint, PvpMatchPlayerEntry>();

            public bool Involves(Client client) => Challenger.Has(client) || Target.Has(client);
            public bool Involves(uint partyId) => partyId != 0 && (Challenger.PartyId == partyId || Target.PartyId == partyId);
            public Side SideOf(Client client) => Challenger.Has(client) ? Challenger : Target.Has(client) ? Target : null;
            public Side Other(Side side) => side == Challenger ? Target : Challenger;
            public IEnumerable<Client> Everyone => Challenger.Members.Concat(Target.Members).ToList();
        }

        private readonly object _sync = new object();
        private readonly List<Challenge> _challenges = new List<Challenge>();
        private readonly List<War> _wars = new List<War>();

        /// <summary>Where the record of each wargame that ends is kept; one with no store keeps nothing.</summary>
        public PvpRecords Records { get; set; } = PvpRecords.Instance;

        /// <summary>The clock squad wargames run on: the duels' own.</summary>
        private static long Now() => Duels.Instance.Now();

        public List<War> Running
        {
            get { lock (_sync) return _wars.ToList(); }
        }

        public List<Challenge> Pending
        {
            get { lock (_sync) return _challenges.ToList(); }
        }

        #region Client requests

        /// <summary>
        /// A challenge between two players who are each in a squad (Duels.ChallengeUserToWargameByName,
        /// which has found the target and refused the same squad).
        /// </summary>
        public void ChallengeSquad(Client client, Client target, int timeMins, int maxKills)
        {
            var name = target.Player.FamilyName ?? "";
            var parties = PartyManager.Instance;
            var myParty = parties.PartyOf(client);
            var theirParty = parties.PartyOf(target);

            if (myParty == null || myParty.PartyLeaderId != AccountId(client))
            {
                Say(client, PlayerMessage.PmWargameYouNotSquadLeader);
                return;
            }

            if (theirParty == null || theirParty.PartyLeaderId != AccountId(target))
            {
                Say(client, PlayerMessage.PmWargameTargetNotSquadLeader, ("target", name));
                return;
            }

            if (Refusal(myParty.Id, theirParty.Id) is (PlayerMessage message, bool named))
            {
                if (named)
                    Say(client, message, ("target", name));
                else
                    Say(client, message);

                return;
            }

            if (Duels.SameMapRefusal(client, target) is PlayerMessage elsewhere)
            {
                Say(client, elsewhere, ("target", name));
                return;
            }

            if (Duels.InviteRefusal(client, target) is PlayerMessage inviting)
            {
                Say(client, inviting, ("target", name));
                return;
            }

            var map = client.Player.MapChannel;
            var challengers = SquadOn(myParty, map);
            var challenged = SquadOn(theirParty, map);
            var challengerName = client.Player.FamilyName ?? "";

            Challenge challenge;

            lock (_sync)
            {
                // Checked again under the lock: two challenges crossing.
                if (_wars.Any(w => w.Involves(myParty.Id) || w.Involves(theirParty.Id)) || _challenges.Any(c => c.Involves(myParty.Id) || c.Involves(theirParty.Id)))
                {
                    Say(client, PlayerMessage.PmWargameAlreadyChallenged, ("target", name));
                    return;
                }

                challenge = new Challenge
                {
                    WargameId = Duels.Instance.NextWargameId(),
                    Challenger = new Side { PartyId = myParty.Id, LeaderName = challengerName },
                    Target = new Side { PartyId = theirParty.Id, LeaderName = name },
                    ExpiresTick = Now() + (long)Duels.ChallengeTimeout.TotalMilliseconds,
                    Minutes = timeMins <= 0 ? Duels.DefaultMinutes : Math.Min(timeMins, Duels.MaxMinutes),
                    MaxKills = maxKills <= 0 ? DefaultMaxKills : Math.Min(maxKills, Duels.MaxKillsLimit)
                };

                challenge.Challenger.Members.AddRange(challengers);
                challenge.Target.Members.AddRange(challenged);
                _challenges.Add(challenge);
            }

            Logger.WriteLog(LogType.Debug, $"Squad wargame {challenge.WargameId}: {challengerName}'s squad ({challengers.Count}) challenged {name}'s ({challenged.Count}), {challenge.MaxKills} kills, {challenge.Minutes} minutes.");

            var challengerInfo = InfoOf(challengers);
            var challengedInfo = InfoOf(challenged);

            foreach (var member in challengers)
                member.CallMethod(SysEntity.ClientWargameManagerId,
                    new ChallengingToWargameSquadPacket(challenge.WargameId, name, challengedInfo, challenge.Minutes, challenge.MaxKills));

            Say(client, PlayerMessage.PmWargameSquadChallenged, ("target", name));

            foreach (var member in challenged)
            {
                member.CallMethod(SysEntity.ClientWargameManagerId,
                    new ChallengedToWargameSquadPacket(challenge.WargameId, challengerName, challengerInfo, challenge.Minutes, challenge.MaxKills));
                Say(member, PlayerMessage.PmWargameSquadChallengeReceived, ("challenger", challengerName));
            }
        }

        /// <summary>
        /// WargameChallengeResponse from a squad challenged: its leader's Accept or Decline. False
        /// when the player's squad has no challenge to answer - then it is a duel's (Duels).
        /// </summary>
        public bool WargameChallengeResponse(Client client, bool accepted)
        {
            var party = PartyManager.Instance.PartyOf(client);

            if (party == null)
                return false;

            Challenge challenge;

            lock (_sync)
            {
                challenge = _challenges.Find(c => c.Target.PartyId == party.Id);

                if (challenge == null)
                    return false;

                if (party.PartyLeaderId != AccountId(client) || !challenge.Target.Has(client))
                {
                    Logger.WriteLog(LogType.Debug, $"Squad wargame {challenge.WargameId}: {client.Player?.FamilyName} answered for a squad they do not lead.");
                    return true;
                }

                _challenges.Remove(challenge);
            }

            var targetName = client.Player.FamilyName ?? "";
            var challengerName = challenge.Challenger.LeaderName;

            if (!accepted)
            {
                foreach (var member in challenge.Target.Members)
                    member.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(challenge.WargameId, true));

                Say(client, PlayerMessage.PmWargameYouRefused, ("challenger", challengerName));

                foreach (var member in challenge.Challenger.Members)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(challenge.WargameId, false));
                    Say(member, PlayerMessage.PmWargameRefused, ("target", targetName));
                }

                return true;
            }

            // Who still fights: in the squad, in the world, and on the map of the leader who accepted.
            var map = client.Player.MapChannel;
            var dropped = new List<Client>();

            foreach (var side in new[] { challenge.Challenger, challenge.Target })
                foreach (var member in side.Members.ToList())
                    if (!InWorld(member) || member.Player.MapChannel != map || PartyManager.Instance.PartyOf(member)?.Id != side.PartyId)
                    {
                        side.Members.Remove(member);
                        dropped.Add(member);
                    }

            foreach (var member in dropped)
                member.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

            if (challenge.Challenger.Members.Count == 0 || challenge.Target.Members.Count == 0)
            {
                Say(client, PlayerMessage.PmWargameResponseNotOnSameMap, ("challenger", challengerName));

                foreach (var member in challenge.Everyone)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

                    if (challenge.Challenger.Has(member))
                        Say(member, PlayerMessage.PmWargameRespondedButNotOnSameMap, ("target", targetName));
                }

                return true;
            }

            Start(challenge, client);
            return true;
        }

        /// <summary>
        /// WargameChallengeRevoked from a challenging squad: its leader's Revoke. False when the
        /// player's squad has made no challenge - then it is a duel's (Duels).
        /// </summary>
        public bool WargameChallengeRevoked(Client client)
        {
            var party = PartyManager.Instance.PartyOf(client);

            if (party == null)
                return false;

            Challenge challenge;

            lock (_sync)
            {
                challenge = _challenges.Find(c => c.Challenger.PartyId == party.Id);

                if (challenge == null)
                    return false;

                if (party.PartyLeaderId != AccountId(client))
                    return true;

                _challenges.Remove(challenge);
            }

            foreach (var member in challenge.Everyone)
                member.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

            Say(client, PlayerMessage.PmWargameChallengeRevoked);

            foreach (var member in challenge.Target.Members)
                Say(member, PlayerMessage.PmWargameRevoked, ("player", client.Player?.FamilyName ?? ""));

            return true;
        }

        /// <summary>
        /// SurrenderWargame from a player in a squad wargame: their squad leader gives it to the
        /// other side; anyone else may not. False when they are in none - then it is a duel's.
        /// </summary>
        public bool SurrenderWargame(Client client)
        {
            var war = WarOf(client);

            if (war == null)
                return false;

            var side = war.SideOf(client);
            var party = PartyManager.Instance.PartyOf(client);

            if (side == null || party == null || party.Id != side.PartyId || party.PartyLeaderId != AccountId(client))
            {
                Say(client, PlayerMessage.PmWargameNotAbleToSurrender);
                return true;
            }

            Logger.WriteLog(LogType.Debug, $"Squad wargame {war.WargameId}: {client.Player?.FamilyName} surrendered for their squad.");
            End(war, war.Other(side), "surrender");
            return true;
        }

        #endregion

        #region The running wargame

        private void Start(Challenge challenge, Client acceptedBy)
        {
            var war = new War
            {
                WargameId = challenge.WargameId,
                Challenger = challenge.Challenger,
                Target = challenge.Target,
                EndTick = Now() + challenge.Minutes * 60_000L,
                MaxKills = challenge.MaxKills
            };

            // For the record: when and where, and who began it on which side.
            var map = acceptedBy?.Player?.MapChannel;

            war.StartedAt = (Records ?? PvpRecords.Instance).UtcNow();
            war.MapContextId = map?.MapInfo?.MapContextId ?? 0;
            war.InstanceId = map?.InstanceId ?? 0;

            foreach (var (side, number) in new[] { (war.Challenger, (byte)1), (war.Target, (byte)2) })
                foreach (var member in side.Members)
                    if (member?.Player != null)
                        war.Scores[member.Player.Id] = PvpRecords.PlayerOf(member.Player, number);

            lock (_sync)
                _wars.Add(war);

            Logger.WriteLog(LogType.Debug, $"Squad wargame {war.WargameId}: {war.Challenger.Members.Count} against {war.Target.Members.Count}.");

            foreach (var member in war.Challenger.Members)
                Say(member, PlayerMessage.PmWargameYourChallengeAccepted, ("target", acceptedBy.Player.FamilyName ?? ""));

            Say(acceptedBy, PlayerMessage.PmWargameYouAccepted);

            foreach (var side in new[] { war.Challenger, war.Target })
            {
                var enemies = war.Other(side).Members.Select(UserId).ToList();

                foreach (var member in side.Members)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId, new WargameStartedPacket(war.WargameId, enemies));
                    member.CallMethod(SysEntity.ClientWargameManagerId, new SetWargameMaxKillsPacket(war.WargameId, war.MaxKills));
                    member.CallMethod(SysEntity.ClientWargameManagerId, new DisplayWargameTimerPacket(war.WargameId, MillisecondsLeft(war)));
                }
            }

            foreach (var member in war.Everyone)
                Wargames.Show(member);
        }

        /// <summary>
        /// A defeat between two players on opposite sides of a squad wargame (Pvp.CountKill):
        /// counted for the killer's squad, the score sent to everyone in it, and the wargame over if
        /// that squad has made its kills. Returns whether it was a squad wargame kill.
        /// </summary>
        public bool Kill(Client killer, Client victim)
        {
            War war;
            Side killers;
            int kills, against;

            lock (_sync)
            {
                war = _wars.Find(w => w.Involves(killer) && w.Involves(victim) && w.SideOf(killer) != w.SideOf(victim));

                if (war == null)
                    return false;

                killers = war.SideOf(killer);
                kills = ++killers.Kills;
                against = war.Other(killers).Kills;

                if (killer.Player != null && war.Scores.TryGetValue(killer.Player.Id, out var killerScore))
                    killerScore.Kills++;

                if (victim.Player != null && war.Scores.TryGetValue(victim.Player.Id, out var victimScore))
                    victimScore.Deaths++;
            }

            var killerId = UserId(killer);
            var victimId = UserId(victim);

            foreach (var member in killers.Members.ToList())
                member.CallMethod(SysEntity.ClientWargameManagerId, new WargameScoreboardPacket(war.WargameId, kills, against, victimId, killerId));

            foreach (var member in war.Other(killers).Members.ToList())
                member.CallMethod(SysEntity.ClientWargameManagerId, new WargameScoreboardPacket(war.WargameId, against, kills, victimId, killerId));

            if (kills >= war.MaxKills)
                End(war, killers, "kills");

            return true;
        }

        /// <summary>
        /// Ends a squad wargame: the winning side is told Victory and the other Defeat, or both a
        /// tie for no winner, and everyone is out of it for everyone around.
        /// <paramref name="reason"/> is for its record: kills, time, surrender, forfeit.
        /// </summary>
        public void End(War war, Side winner, string reason = null)
        {
            if (war == null)
                return;

            lock (_sync)
                if (!_wars.Remove(war))
                    return;

            Logger.WriteLog(LogType.Debug, $"Squad wargame {war.WargameId} ended: {(winner == null ? "a tie" : $"{winner.LeaderName}'s squad won")}, {war.Challenger.Kills} : {war.Target.Kills}.");

            Records?.Record(new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.SquadWargame,
                WargameId = war.WargameId,
                MapContextId = war.MapContextId,
                InstanceId = war.InstanceId,
                StartedAt = war.StartedAt,
                Outcome = (byte)(winner == null ? PvpMatchOutcome.Tied : PvpMatchOutcome.Won),
                WinnerSide = winner == null ? (byte)0 : winner == war.Challenger ? (byte)1 : (byte)2,
                Reason = reason ?? "",
                Side1Name = war.Challenger.LeaderName ?? "",
                Side1Score = war.Challenger.Kills,
                Side1Kills = war.Challenger.Kills,
                Side2Name = war.Target.LeaderName ?? "",
                Side2Score = war.Target.Kills,
                Side2Kills = war.Target.Kills
            }, war.Scores.Values);

            foreach (var side in new[] { war.Challenger, war.Target })
                foreach (var member in side.Members)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId,
                        winner == null ? WargameResultPacket.Tied(war.WargameId)
                        : winner == side ? WargameResultPacket.Victory(war.WargameId)
                        : WargameResultPacket.Defeat(war.WargameId));

                    if (InWorld(member))
                        Wargames.Show(member);
                }
        }

        /// <summary>Lapsed challenges and squad wargames whose time is up. From the map channel worker.</summary>
        public void Worker()
        {
            List<Challenge> lapsed;
            List<War> due;
            var now = Now();

            lock (_sync)
            {
                lapsed = _challenges.Where(c => now >= c.ExpiresTick).ToList();
                _challenges.RemoveAll(c => now >= c.ExpiresTick);
                due = _wars.Where(w => now >= w.EndTick).ToList();
            }

            foreach (var challenge in lapsed)
                foreach (var member in challenge.Everyone)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
                    Say(member, PlayerMessage.PmWargameChallengeTimedOut);
                }

            foreach (var war in due)
                End(war, war.Challenger.Kills == war.Target.Kills ? null
                    : war.Challenger.Kills > war.Target.Kills ? war.Challenger : war.Target, "time");
        }

        /// <summary>A player leaving the map or the world (ManifestationManager.RemovePlayerCharacter).</summary>
        public void PlayerLeft(Client client) => Leave(client, inWorld: false);

        /// <summary>A player taken out of their squad - left, kicked, separated from a feuding clan, or the squad disbanded (PartyManager).</summary>
        public void LeftSquad(Client client) => Leave(client, inWorld: InWorld(client));

        /// <summary>
        /// A member who is no longer with their squad: out of its open challenge (whose window
        /// closes for them) and of its wargame (RemoveFromWargame, and PM_WARGAME_PLAYER_LEFT to
        /// the rest). A challenge with a side left empty is off; a wargame goes to the other side.
        /// </summary>
        private void Leave(Client client, bool inWorld)
        {
            if (client == null)
                return;

            var revoked = new List<Challenge>();
            var lostMember = new List<Challenge>();
            var wars = new List<War>();

            lock (_sync)
            {
                foreach (var challenge in _challenges.Where(c => c.Involves(client)).ToList())
                {
                    challenge.Challenger.Members.Remove(client);
                    challenge.Target.Members.Remove(client);

                    if (challenge.Challenger.Members.Count == 0 || challenge.Target.Members.Count == 0)
                    {
                        _challenges.Remove(challenge);
                        revoked.Add(challenge);
                    }
                    else
                        lostMember.Add(challenge);
                }

                foreach (var war in _wars.Where(w => w.Involves(client)).ToList())
                {
                    war.SideOf(client).Members.Remove(client);
                    wars.Add(war);

                    // Still on its record, as one who did not see it out.
                    if (client.Player != null && war.Scores.TryGetValue(client.Player.Id, out var score))
                        score.PresentAtEnd = false;
                }
            }

            foreach (var challenge in lostMember)
                client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

            foreach (var challenge in revoked)
            {
                client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));

                foreach (var member in challenge.Everyone)
                {
                    member.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(challenge.WargameId));
                    Say(member, PlayerMessage.PmWargameChallengeRevoked);
                }
            }

            foreach (var war in wars)
            {
                Logger.WriteLog(LogType.Debug, $"Squad wargame {war.WargameId}: {client.Player?.FamilyName} left.");

                client.CallMethod(SysEntity.ClientWargameManagerId, new RemoveFromWargamePacket(war.WargameId));

                if (inWorld)
                    Wargames.Show(client);

                foreach (var member in war.Everyone)
                    Say(member, PlayerMessage.PmWargamePlayerLeft, ("player", client.Player?.FamilyName ?? ""));

                if (war.Challenger.Members.Count == 0)
                    End(war, war.Target, "forfeit");
                else if (war.Target.Members.Count == 0)
                    End(war, war.Challenger, "forfeit");
            }
        }

        #endregion

        #region Queries

        public War WarOf(Client client)
        {
            lock (_sync)
                return _wars.Find(w => w.Involves(client));
        }

        public bool IsWargaming(Client client) => WarOf(client) != null;

        /// <summary>Whether the player is in a squad challenge still open, made or received.</summary>
        public bool HasChallenge(Client client)
        {
            lock (_sync)
                return _challenges.Any(c => c.Involves(client));
        }

        /// <summary>Whether the two players are on opposite sides of a squad wargame.</summary>
        public bool AreOpposed(Client one, Client other)
        {
            if (one == null || other == null || one == other)
                return false;

            lock (_sync)
                return _wars.Any(w => w.Involves(one) && w.Involves(other) && w.SideOf(one) != w.SideOf(other));
        }

        /// <summary>The player's squad wargame in WargameData: {wargameId: side}, true for the challenging squad.</summary>
        public Dictionary<uint, bool> WargameDataOf(Manifestation player)
        {
            var data = new Dictionary<uint, bool>();

            if (player == null)
                return data;

            lock (_sync)
                foreach (var war in _wars)
                    if (war.Challenger.Has(player))
                        data[war.WargameId] = true;
                    else if (war.Target.Has(player))
                        data[war.WargameId] = false;

            return data;
        }

        public int MillisecondsLeft(War war) => (int)Math.Max(0, Math.Min(int.MaxValue, war.EndTick - Now()));

        #endregion

        #region Helpers

        /// <summary>Why one squad may not challenge the other, and whether the message names the target; null if it may.</summary>
        private (PlayerMessage Message, bool Named)? Refusal(uint mine, uint theirs)
        {
            lock (_sync)
            {
                if (_wars.Any(w => w.Involves(mine)))
                    return (PlayerMessage.PmWargameYouAlreadyWargaming, false);

                if (_challenges.Any(c => c.Challenger.PartyId == mine))
                    return (PlayerMessage.PmWargameFailWaitForResponse, false);

                if (_challenges.Any(c => c.Target.PartyId == mine))
                    return (PlayerMessage.PmWargameAcceptOrDeclineFirst, false);

                if (_wars.Any(w => w.Involves(theirs)))
                    return (PlayerMessage.PmWargameTargetAlreadyWargaming, true);

                if (_challenges.Any(c => c.Target.PartyId == theirs))
                    return (PlayerMessage.PmWargameAlreadyChallenged, true);

                if (_challenges.Any(c => c.Challenger.PartyId == theirs))
                    return (PlayerMessage.PmWargameAlreadyChallenging, true);
            }

            return null;
        }

        /// <summary>A squad's members in the world on this map, its leader first.</summary>
        private static List<Client> SquadOn(Party party, MapChannel map)
        {
            return PartyManager.OnlineMembers(party)
                .Where(c => InWorld(c) && c.Player.MapChannel == map)
                .OrderBy(c => AccountId(c) == party.PartyLeaderId ? 0 : 1)
                .ToList();
        }

        /// <summary>The squad info a challenge lists: (userId, name, classId, level, isAfk) a member.</summary>
        private static List<PartyMember> InfoOf(IEnumerable<Client> members) => members.Select(m => new PartyMember(m)).ToList();

        private static bool InWorld(Client client) =>
            client?.Player != null && client.State == ClientState.Ingame && !client.Player.Disconected;

        private static uint AccountId(Client client) => client?.AccountEntry?.Id ?? 0;

        /// <summary>The id the scoreboard and WargameStarted name a player by: their account, as the squad info does.</summary>
        private static uint UserId(Client client) => client?.AccountEntry?.Id ?? client?.Player?.Id ?? 0;

        private static void Say(Client client, PlayerMessage message, params (string Key, string Value)[] args)
        {
            client?.CallMethod(SysEntity.ClientWargameManagerId,
                new DisplayWargameMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value ?? "")));
        }

        #endregion
    }
}

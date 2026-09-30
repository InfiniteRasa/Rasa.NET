using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Minion.Client;
    using Packets.Minion.Server;
    using Structures;

    /// <summary>
    /// Player-owned minions, and the nine commands the client can give one.
    ///
    /// The game calls these *subordinates*; only the wire says minion. The client's own Command
    /// System help draws the line that matters: "Only certain subordinates may be directly
    /// controlled by a player; these are summoned using the Create Clone, Spotter, and Bot
    /// Construction ability." Everything here is about those - not turrets, not mission NPCs.
    ///
    /// Spotter, Bot Construction and Create Clone summon them (AbilityManager.SummonMinion).
    /// <c>.minion</c> is a GM tool that adopts any creature as the caller's minion, for
    /// exercising the command layer without the abilities.
    ///
    /// Two client-side facts shape all of this:
    ///
    /// The client does not know which entity is its minion. <c>minioncommand.py</c> sends every
    /// command with no minion id at all and lets the server work it out, and the id in the acks is
    /// used only to substitute a name into a message. So the mapping lives here and nowhere else.
    ///
    /// Every command is hidden unless the MinionCommands server flag is set. Without it the
    /// keybinds and <c>/cmd</c> do nothing at all client-side, so a server that is not ready for
    /// these simply never sees them.
    ///
    /// Threading: commands arrive on the main loop (inbound packets are drained there) and
    /// <see cref="Worker"/> runs on the same loop from MapChannelManager, so the table is only
    /// ever touched from one thread and is not locked.
    /// </summary>
    public class MinionManager
    {
        private static MinionManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// Master entity id to that player's minions. A list rather than a single entry because
        /// "only 1 bot can be active at a time" is a rule of the ability, not of the wire - the
        /// Sapper's Crab Mines allow three - so the limit belongs at the summon, not here.
        /// </summary>
        private readonly Dictionary<ulong, List<Creature>> _byMaster = new Dictionary<ulong, List<Creature>>();

        /// <summary>
        /// How close a minion tries to stay to whatever it is following. The client refuses a
        /// Follow Target order past MAX_MINION_FOLLOW_TARGET_DISTANCE (20 m); this is the distance
        /// it closes to once it has one, and is deliberately well inside that.
        /// </summary>
        public const float FollowDistance = 4.0f;

        /// <summary>
        /// Distance limits, all from the client's own shared/gameconstants.py. They are checked
        /// again here because the client's refusal is a convenience, not a constraint - anything
        /// can send any packet.
        /// </summary>
        public const float MaxAssistTargetDistance = 20.0f;
        public const float MaxFollowTargetDistance = 20.0f;
        public const float MaxGoDistance = 40.0f;
        public const float MaxTargetDistance = 100.0f;

        public static MinionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MinionManager();
                    }
                }

                return _instance;
            }
        }

        private MinionManager()
        {
        }

        // ------------------------------------------------------------------ ownership

        /// <summary>
        /// Makes an already-spawned creature the caller's minion.
        ///
        /// <paramref name="despawnMs"/> of 0 means it stays until dismissed or its master leaves,
        /// which is what a GM test minion wants; a summoned one will carry the ability's duration.
        /// </summary>
        public void Adopt(Client master, Creature minion, long despawnMs = 0)
        {
            if (master?.Player == null || minion == null)
                return;

            minion.MasterEntityId = master.Player.EntityId;
            minion.Stance = MinionStance.Defensive;   // "Subordinates enter the world with the default stance of Defensive."
            minion.DespawnTime = despawnMs;

            if (!_byMaster.TryGetValue(master.Player.EntityId, out var list))
                _byMaster[master.Player.EntityId] = list = new List<Creature>();

            list.Add(minion);

            // Follow the master out of the gate: "All subordinates will appear with the default
            // setting to follow their master."
            BehaviorManager.Instance.SetActionFollow(minion, master.Player.EntityId);

            // And fight what they fight: assisting the master, so a bot goes for whatever its
            // owner targets until told to assist someone else (Assist Target).
            minion.Controller.ActionFollow.AssistTargetId = master.Player.EntityId;

            master.CallMethod(master.Player.EntityId, new MinionAddedPacket(minion.EntityId));
        }

        /// <summary>Every minion this player owns, newest last. Never null.</summary>
        public IReadOnlyList<Creature> MinionsOf(Client master)
        {
            if (master?.Player == null)
                return System.Array.Empty<Creature>();

            return _byMaster.TryGetValue(master.Player.EntityId, out var list)
                ? list
                : (IReadOnlyList<Creature>)System.Array.Empty<Creature>();
        }

        /// <summary>
        /// The minion a command applies to. The client names no minion, so with more than one the
        /// most recently adopted wins - the same one the ability would have just created.
        /// </summary>
        public Creature ActiveMinion(Client master)
        {
            var list = MinionsOf(master);

            for (var i = list.Count - 1; i >= 0; i--)
                if (list[i].State != CharacterState.Dead)
                    return list[i];

            return null;
        }

        /// <summary>
        /// Unlinks a minion and leaves it where it is - a summon killed in the fight, whose corpse
        /// goes the usual way. It is nobody's to command any more.
        /// </summary>
        public void Release(Creature minion)
        {
            if (minion == null)
                return;

            if (_byMaster.TryGetValue(minion.MasterEntityId, out var list))
            {
                list.Remove(minion);

                if (list.Count == 0)
                    _byMaster.Remove(minion.MasterEntityId);
            }

            minion.MasterEntityId = 0;
        }

        /// <summary>Unlinks a minion and takes it out of the world.</summary>
        public void Dismiss(MapChannel mapChannel, Creature minion)
        {
            if (minion == null)
                return;

            if (_byMaster.TryGetValue(minion.MasterEntityId, out var list))
            {
                list.Remove(minion);

                if (list.Count == 0)
                    _byMaster.Remove(minion.MasterEntityId);
            }

            minion.MasterEntityId = 0;

            if (mapChannel != null)
                CellManager.Instance.RemoveCreatureFromWorld(mapChannel, minion);
        }

        /// <summary>
        /// Everything this player owns, gone. Called when they log out or change map: the client's
        /// own rule is that "player-controlled subordinates will teleport with their masters, but
        /// not change maps", so a map change is a dismissal, not a bug.
        /// </summary>
        public void DismissAll(Client master)
        {
            if (master?.Player == null)
                return;

            if (!_byMaster.TryGetValue(master.Player.EntityId, out var list))
                return;

            foreach (var minion in list.ToList())
                Dismiss(master.Player.MapChannel, minion);

            _byMaster.Remove(master.Player.EntityId);
        }

        /// <summary>Counts down despawn timers and clears up minions whose master is gone.</summary>
        public void Worker(MapChannel mapChannel, long delta)
        {
            if (_byMaster.Count == 0)
                return;

            var expired = new List<Creature>();

            foreach (var entry in _byMaster)
            {
                // TryGetValue rather than GetPlayer, which throws on a miss - and a master who
                // has logged out is exactly the case this loop exists to handle.
                EntityManager.Instance.Players.TryGetValue(entry.Key, out var master);

                foreach (var minion in entry.Value)
                {
                    if (minion.MapContextId != mapChannel.MapInfo.MapContextId)
                        continue;

                    // Master logged out, died out of the world, or walked into another map.
                    if (master == null || master.MapChannel != mapChannel)
                    {
                        expired.Add(minion);
                        continue;
                    }

                    if (minion.DespawnTime <= 0)
                        continue;

                    minion.DespawnTime -= delta;

                    if (minion.DespawnTime <= 0)
                        expired.Add(minion);
                }
            }

            foreach (var minion in expired)
                Dismiss(mapChannel, minion);
        }

        // ------------------------------------------------------------------ commands

        public void Stay(Client client, MinionStayPacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, GameOpcode.MinionStayAck, error);
                return;
            }

            // "The Stay command will simply set the subordinate's anchor point to its current
            // location."
            BehaviorManager.Instance.SetActionAnchor(minion, minion.Position);
            Answer(client, GameOpcode.MinionStayAck, PlayerMessage.PmMinionStaySuccess, minion);
        }

        public void Go(Client client, MinionGoPacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, GameOpcode.MinionGoAck, error);
                return;
            }

            if (Vector3.Distance(client.Player.Position, packet.Destination) > MaxGoDistance)
            {
                Refuse(client, GameOpcode.MinionGoAck, PlayerMessage.PmMinionTargetTooFarAway, minion);
                return;
            }

            BehaviorManager.Instance.SetActionAnchor(minion, packet.Destination);
            Answer(client, GameOpcode.MinionGoAck, PlayerMessage.PmMinionGoSuccess, minion);
        }

        public void FollowMe(Client client, MinionFollowMePacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, GameOpcode.MinionFollowMeAck, error);
                return;
            }

            // Follow Me clears the anchor: "Issuing a Follow Me command to a subordinate that has
            // an assigned anchor point will cause the subordinate to regroup around the player."
            BehaviorManager.Instance.SetActionFollow(minion, client.Player.EntityId);
            Answer(client, GameOpcode.MinionFollowMeAck, PlayerMessage.PmMinionFollowMeSuccess, minion);
        }

        public void FollowTarget(Client client, MinionFollowTargetPacket packet)
        {
            Targeted(client, GameOpcode.MinionFollowTargetAck, packet.TargetId, MaxFollowTargetDistance,
                     requireFriendly: true, success: PlayerMessage.PmMinionFollowTargetSuccess,
                     apply: (minion, target) => BehaviorManager.Instance.SetActionFollow(minion, target.EntityId));
        }

        public void TargetMe(Client client, MinionTargetMePacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, GameOpcode.MinionTargetMeAck, error);
                return;
            }

            minion.Target = client.Player.EntityId;
            Answer(client, GameOpcode.MinionTargetMeAck, PlayerMessage.PmMinionTargetMeSuccess, minion);
        }

        public void Target(Client client, MinionTargetPacket packet)
        {
            // The one command with no friendliness requirement: targetMustBeFriendly is False in
            // the client, because pointing a minion at an enemy is the whole point of it.
            Targeted(client, GameOpcode.MinionTargetAck, packet.TargetId, MaxTargetDistance,
                     requireFriendly: false, success: PlayerMessage.PmMinionTargetSuccess,
                     apply: (minion, target) => minion.Target = target.EntityId);
        }

        public void AssistMe(Client client, MinionAssistMePacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, GameOpcode.MinionAssistMeAck, error);
                return;
            }

            minion.Controller.ActionFollow.AssistTargetId = client.Player.EntityId;
            Answer(client, GameOpcode.MinionAssistMeAck, PlayerMessage.PmMinionAssistMeSuccess, minion);
        }

        public void AssistTarget(Client client, MinionAssistTargetPacket packet)
        {
            Targeted(client, GameOpcode.MinionAssistTargetAck, packet.TargetId, MaxAssistTargetDistance,
                     requireFriendly: true, success: PlayerMessage.PmMinionAssistTargetSuccess,
                     apply: (minion, target) => minion.Controller.ActionFollow.AssistTargetId = target.EntityId);
        }

        /// <summary>
        /// A <c>/cmd</c> string. Everything the player typed arrives here unparsed; the slash
        /// command does no validation of its own.
        /// </summary>
        public void Command(Client client, MinionCommandPacket packet)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                client.CallMethod(client.Player.EntityId, MinionAckPacket.Command(false, error));
                return;
            }

            if (!ParseCommand(packet.CommandString, out var stance, out var order, out var problem))
            {
                client.CallMethod(client.Player.EntityId, MinionAckPacket.Command(false, problem));
                return;
            }

            if (stance.HasValue)
            {
                SetStance(minion, stance.Value);

                client.CallMethod(client.Player.EntityId,
                    MinionAckPacket.Temperament(true, StanceMessage(stance.Value), minion.EntityId));
            }

            switch (order)
            {
                case MinionOrder.Stay:
                    BehaviorManager.Instance.SetActionAnchor(minion, minion.Position);
                    Answer(client, GameOpcode.MinionStayAck, PlayerMessage.PmMinionStaySuccess, minion);
                    break;

                case MinionOrder.Follow:
                    BehaviorManager.Instance.SetActionFollow(minion, client.Player.EntityId);
                    Answer(client, GameOpcode.MinionFollowMeAck, PlayerMessage.PmMinionFollowMeSuccess, minion);
                    break;

                case MinionOrder.Target:
                    minion.Target = client.Player.EntityId;
                    Answer(client, GameOpcode.MinionTargetMeAck, PlayerMessage.PmMinionTargetMeSuccess, minion);
                    break;

                case MinionOrder.Assist:
                    minion.Controller.ActionFollow.AssistTargetId = client.Player.EntityId;
                    Answer(client, GameOpcode.MinionAssistMeAck, PlayerMessage.PmMinionAssistMeSuccess, minion);
                    break;

                case MinionOrder.None:
                    // A bare stance change. The temperament ack above is the whole answer; sending
                    // a MinionCommandAck as well would print a second line for one command.
                    break;
            }
        }

        // ------------------------------------------------------------------ /cmd grammar

        /// <summary>An order a /cmd string can carry alongside or instead of a stance.</summary>
        public enum MinionOrder
        {
            None = 0,
            Stay,
            Follow,
            Target,
            Assist
        }

        /// <summary>
        /// Splits a <c>/cmd</c> string into an optional stance and an optional order.
        ///
        /// The grammar is reconstructed from the help text's own examples - <c>/cmd passive</c>,
        /// <c>/cmd stay</c>, <c>/cmd passive follow</c>, <c>/cmd aggressive stay</c> - and from
        /// the PlayerMessages that exist to refuse it: PmMinionInvalidCommand for a word that is
        /// neither, and three separate TooManyArgs messages, one per category, for a category
        /// given twice.
        ///
        /// The targeted forms the help lists (<c>/cmd follow &lt;player-name&gt;</c>) are not
        /// handled: a name has to be resolved to an entity the minion can see, and the keybind
        /// path already covers targeted orders with an id. A trailing name is refused rather than
        /// silently treated as the untargeted form.
        /// </summary>
        public static bool ParseCommand(string input, out MinionStance? stance, out MinionOrder order, out PlayerMessage error)
        {
            stance = null;
            order = MinionOrder.None;
            error = PlayerMessage.PmMinionInvalidCommand;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var words = input.Trim().ToLowerInvariant().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                switch (word)
                {
                    case "passive":
                    case "defensive":
                    case "aggressive":
                        if (stance.HasValue)
                        {
                            error = PlayerMessage.PmMinionTooManyArgsTemperament;
                            return false;
                        }

                        stance = word == "passive" ? MinionStance.Passive
                               : word == "defensive" ? MinionStance.Defensive
                               : MinionStance.Aggressive;
                        break;

                    case "stay":
                    case "follow":
                        if (order != MinionOrder.None)
                        {
                            error = PlayerMessage.PmMinionTooManyArgsMovement;
                            return false;
                        }

                        order = word == "stay" ? MinionOrder.Stay : MinionOrder.Follow;
                        break;

                    case "target":
                    case "assist":
                        if (order != MinionOrder.None)
                        {
                            error = PlayerMessage.PmMinionTooManyArgsTargeting;
                            return false;
                        }

                        order = word == "target" ? MinionOrder.Target : MinionOrder.Assist;
                        break;

                    default:
                        error = PlayerMessage.PmMinionInvalidCommand;
                        return false;
                }
            }

            if (!stance.HasValue && order == MinionOrder.None)
                return false;

            return true;
        }

        public void SetStance(Creature minion, MinionStance stance)
        {
            minion.Stance = stance;

            // "Setting a subordinate's combat stance to Passive will clear its hate list." There
            // is no hate list yet - what fighting there is tracks one target - so dropping the
            // fight is the whole of it.
            if (stance == MinionStance.Passive && minion.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
            {
                minion.Controller.ActionFighting.TargetEntityId = 0;
                BehaviorManager.Instance.SetActionFollow(minion, minion.MasterEntityId);
            }
        }

        private static PlayerMessage StanceMessage(MinionStance stance)
        {
            switch (stance)
            {
                case MinionStance.Passive: return PlayerMessage.PmMinionPassiveSuccess;
                case MinionStance.Aggressive: return PlayerMessage.PmMinionAggressiveSuccess;
                default: return PlayerMessage.PmMinionDefensiveSuccess;
            }
        }

        // ------------------------------------------------------------------ plumbing

        /// <summary>
        /// The minion a command should act on, or null with the message that says why not. Mirrors
        /// the client's own <c>_GetMinionCommandError</c>, which checks the same two things before
        /// it will even send.
        /// </summary>
        private Creature Resolve(Client client, out PlayerMessage error)
        {
            error = PlayerMessage.PmMinionNotFound;

            if (client?.Player == null)
                return null;

            if (client.Player.State == CharacterState.Dead
                || (client.Player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0))
            {
                error = PlayerMessage.PmMinionMasterDead;
                return null;
            }

            return ActiveMinion(client);
        }

        /// <summary>
        /// The three commands that name a target: resolve it, check it the way the client would
        /// have, then apply.
        /// </summary>
        private void Targeted(Client client, GameOpcode ack, ulong targetId, float maxDistance, bool requireFriendly,
                              PlayerMessage success, System.Action<Creature, Actor> apply)
        {
            var minion = Resolve(client, out var error);

            if (minion == null)
            {
                Refuse(client, ack, error, null, targetId);
                return;
            }

            if (targetId == 0)
            {
                Refuse(client, ack, PlayerMessage.PmMinionNoTarget, minion, targetId);
                return;
            }

            // Not EntityManager.GetActor: that indexes the dictionary and throws on a miss, and
            // the id here came off the wire. A client that names something that has since died
            // would take the map thread down with it.
            EntityManager.Instance.Actors.TryGetValue(targetId, out var target);

            if (target == null)
            {
                Refuse(client, ack, PlayerMessage.PmMinionTargetNotFound, minion, targetId);
                return;
            }

            if (requireFriendly && !IsFriendly(client, target))
            {
                Refuse(client, ack, PlayerMessage.PmMinionTargetNotFriendly, minion, targetId);
                return;
            }

            if (Vector3.Distance(client.Player.Position, target.Position) > maxDistance)
            {
                Refuse(client, ack, PlayerMessage.PmMinionTargetTooFarAway, minion, targetId);
                return;
            }

            apply(minion, target);

            client.CallMethod(client.Player.EntityId,
                MinionAckPacket.ForTarget(ack, true, success, minion.EntityId, targetId));
        }

        /// <summary>
        /// Friendly means another player, or a creature on the player's own side. The client tests
        /// the target's own target-category, which is the same question asked from its end.
        /// </summary>
        private static bool IsFriendly(Client client, Actor target)
        {
            if (EntityManager.Instance.Players.ContainsKey(target.EntityId))
                return true;

            if (EntityManager.Instance.Creatures.TryGetValue(target.EntityId, out var creature))
                return creature.TargetCategory == TargetCategory.Friendly;

            return false;
        }

        private void Answer(Client client, GameOpcode ack, PlayerMessage message, Creature minion)
        {
            client.CallMethod(client.Player.EntityId,
                MinionAckPacket.ForMinion(ack, true, message, minion.EntityId));
        }

        /// <summary>
        /// A refusal, in the shape the refused opcode expects. The arity is a property of the
        /// opcode, not of whether the command worked - a failed FollowTarget still has to be four
        /// fields long or the client raises unpacking it.
        /// </summary>
        private void Refuse(Client client, GameOpcode ack, PlayerMessage message, Creature minion = null, ulong targetId = 0)
        {
            if (client?.Player == null)
                return;

            var minionId = minion?.EntityId ?? 0;

            var packet = ack == GameOpcode.MinionFollowTargetAck
                      || ack == GameOpcode.MinionTargetAck
                      || ack == GameOpcode.MinionAssistTargetAck
                ? MinionAckPacket.ForTarget(ack, false, message, minionId, targetId)
                : MinionAckPacket.ForMinion(ack, false, message, minionId);

            client.CallMethod(client.Player.EntityId, packet);
        }
    }
}

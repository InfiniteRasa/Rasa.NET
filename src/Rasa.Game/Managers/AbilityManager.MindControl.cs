using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Mind Control (AA_MEDIC_MIND_CONTROL 304, abilities.mindcontrol): "Takes control of an
    /// enemy's mind." Biological targets only, and "affected targets are temporarily immune to
    /// reapplication". What it does depends on the pump:
    /// - P1 Frighten: the target drops its fight and runs from the caster;
    /// - P2 Confusion: it attacks enemies or allies at random;
    /// - P3 Subversion: it attacks its allies;
    /// - P4 Enslavement: it assists the caster and attacks its allies;
    /// - P5 Infectious: as P4, and whoever it hits has PERCENTAGE_CHANCE (50) to be confused
    ///   (P2) for GAME_EFFECT_ARG1 (5) seconds.
    ///
    /// From the client and its data:
    /// - MindControlAction is TARGET_NON_FRIENDLY, and its DoAbility announces each
    ///   (entityId, effectTypeId) of its hit data as an attach, so the recovery carries
    ///   EffectAttach hits with MIND_CONTROL_EFFECT (168);
    /// - MindControlEffect is hostile, and its OnTick(target, infectedId) announces the effect's
    ///   attach on the infected creature: that is the tick P5's slave sends on each infection;
    /// - DURATION 15 s, INTERVAL 4 s; range 20-60 m, reuse 45 s.
    ///
    /// How it is carried out:
    /// - the effect carries its pump (GameEffect.MindControlPump), and BehaviorManager.MayFight
    ///   reads it: Frighten fights nobody, Confusion any combatant, Subversion only its own
    ///   category, and whoever a confused or subverted creature turns on may answer it;
    /// - P1-P3 do not scan for enemies (BehaviorManager.ScansForEnemies); MindControlWorker
    ///   keeps a frightened creature running and, every INTERVAL, turns a confused or subverted
    ///   one on someone new within its aggro range;
    /// - P4-P5 are the Traitor turn (TurnCreature) with the creature's own aggro range, following
    ///   the caster and assisting them;
    /// - a creature is immune from the moment it is controlled until DURATION after it ends.
    /// A target that is not biological, or immune, is refused (GameEffectAttachFailed, IMMUNE).
    /// The PvP effects are not carried out.
    /// </summary>
    public partial class AbilityManager
    {
        public const string MindControlModule = "abilities.mindcontrol";
        public const int MindControlTypeId = 168;                // MIND_CONTROL_EFFECT

        public const int MindControlFrighten = 1;
        public const int MindControlConfusion = 2;
        public const int MindControlSubversion = 3;
        public const int MindControlEnslavement = 4;
        public const int MindControlInfectious = 5;

        /// <summary>How close a frightened creature lets the caster come before it runs again.</summary>
        public const float MindControlFearRange = 30f;

        /// <summary>The reach a confused creature looks for someone within, when it has no aggro range of its own.</summary>
        public const float MindControlDefaultReach = 20f;

        private class MindSlave
        {
            public Creature Creature;
            public Manifestation Controller;
            public MapChannel MapChannel;
            public GameEffect Effect;
            public int Pump;
            public int IntervalMs;
            public int InfectChance;
            public int InfectSeconds;
            public long NextPickAt;
            public Vector3 FearFrom;
        }

        private static readonly List<MindSlave> MindSlaves = new List<MindSlave>();
        private static readonly Dictionary<ulong, long> MindControlImmuneUntil = new Dictionary<ulong, long>();
        private static readonly object MindControlLock = new object();
        private static readonly Random MindControlRandom = new Random();

        /// <summary>The pump of the Mind Control on this actor, 0 when there is none.</summary>
        public static int MindControlPumpOf(Actor actor)
        {
            if (actor == null)
                return 0;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.MindControlPump > 0)
                    return effect.MindControlPump;

            return 0;
        }

        /// <summary>Confused or subverted: turned on its own side, so anyone it attacks may answer it.</summary>
        public static bool IsMindConfused(Actor actor)
        {
            var pump = MindControlPumpOf(actor);

            return pump == MindControlConfusion || pump == MindControlSubversion;
        }

        /// <summary>"Biological targets only."</summary>
        public static bool CanMindControl(Creature creature)
        {
            return creature != null && CreatureManager.CreatureFlagsOf(creature).Contains((int)CreatureFlag.Biological);
        }

        /// <summary>Controlled now, or not long since.</summary>
        public static bool IsMindControlImmune(Creature creature, long now)
        {
            lock (MindControlLock)
                return MindControlImmuneUntil.TryGetValue(creature.EntityId, out var until) && now < until;
        }

        /// <summary>Takes the creature's mind for DURATION, as the pump says; false when it cannot.</summary>
        private bool AttachMindControl(MapChannel mapChannel, Manifestation player, Creature target, ActionLevelInfo info)
        {
            if (target == null || target.State == CharacterState.Dead || target.State == CharacterState.Dying || !IsHostile(player, target))
                return false;

            if (!CanMindControl(target) || IsMindControlImmune(target, Environment.TickCount64) || MindControlPumpOf(target) != 0)
            {
                CellManager.Instance.CellCallMethod(mapChannel, target,
                    new GameEffectAttachFailedPacket(MindControlTypeId, GameEffectAttachFailedPacket.FailReason.Immune, player.EntityId));
                return false;
            }

            var pump = Math.Clamp((int)info.Level, MindControlFrighten, MindControlInfectious);
            var seconds = info.Get(AbilityProperty.Duration, 15);

            var slave = new MindSlave
            {
                Creature = target,
                Controller = player,
                MapChannel = mapChannel,
                Pump = pump,
                IntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 4)) * 1000,
                InfectChance = pump == MindControlInfectious ? info.Get(AbilityProperty.PercentageChance, 50) : 0,
                InfectSeconds = info.Get(AbilityProperty.GameEffectArg1, 5),
                FearFrom = player.Position
            };

            if (pump >= MindControlEnslavement)
            {
                // Enslavement: Traitor's turn, keeping its own aggro range, and at the caster's side.
                var effect = TurnCreature(mapChannel, player, target, info, MindControlTypeId, keepAggroRange: true);

                if (effect == null)
                    return false;

                effect.MindControlPump = pump;
                slave.Effect = effect;

                var restore = effect.OnDetached;

                effect.OnDetached = (map, actor, e) =>
                {
                    if (actor is Creature freed && freed.Controller != null)
                    {
                        freed.Controller.ActionFollow.FollowTargetId = 0;
                        freed.Controller.ActionFollow.AssistTargetId = 0;
                        freed.Controller.ActionFollow.HasAnchor = false;
                    }

                    Released(slave, seconds);
                    restore?.Invoke(map, actor, e);
                };

                if (target.Controller != null)
                {
                    BehaviorManager.Instance.SetActionFollow(target, player.EntityId);
                    target.Controller.ActionFollow.AssistTargetId = player.EntityId;
                }
            }
            else
            {
                slave.Effect = Confuse(mapChannel, player, target, pump, seconds, info.Level, info.ActionId, slave);

                if (slave.Effect == null)
                    return false;
            }

            lock (MindControlLock)
            {
                MindSlaves.Add(slave);
                MindControlImmuneUntil[target.EntityId] = long.MaxValue;
            }

            return true;
        }

        /// <summary>
        /// Frighten, Confusion or Subversion on the creature for the given seconds; null when it
        /// did not take. Its hate is wiped and its fight dropped: the worker decides what it does.
        /// </summary>
        private static GameEffect Confuse(MapChannel mapChannel, Manifestation controller, Creature target, int pump, int seconds, uint level, ActionId actionId, MindSlave slave)
        {
            var effect = new GameEffect
            {
                TypeId = MindControlTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = level,
                ActionId = actionId,
                SourceId = controller.EntityId,
                Source = controller,
                SourceLevel = controller.Level,
                ExpiresTick = Environment.TickCount64 + seconds * 1000L,
                AnnounceOnAttach = false,
                IsBuff = false,
                AllowDetach = false,
                MindControlPump = pump
            };

            effect.OnDetached = (map, actor, e) =>
            {
                Released(slave, seconds);

                if (actor is Creature freed)
                {
                    freed.Hate.Clear();

                    if (freed.State != CharacterState.Dead)
                        BehaviorManager.Instance.StopFighting(freed);
                }
            };

            GameEffectManager.Instance.Attach(mapChannel, target, effect);

            if (!target.ActiveEffects.ContainsKey(effect.EffectId))
                return null;

            target.Hate.Clear();
            BehaviorManager.Instance.StopFighting(target);

            if (pump == MindControlFrighten)
                BehaviorManager.Instance.Flee(mapChannel, target, controller.Position);

            return effect;
        }

        /// <summary>The control has ended: off the list, and immune for DURATION more.</summary>
        private static void Released(MindSlave slave, int seconds)
        {
            lock (MindControlLock)
            {
                MindSlaves.Remove(slave);
                MindControlImmuneUntil[slave.Creature.EntityId] = Environment.TickCount64 + seconds * 1000L;
            }
        }

        /// <summary>
        /// A creature has hit another (MissileManager). One under P5's Infectious control may
        /// confuse its victim: PERCENTAGE_CHANCE for GAME_EFFECT_ARG1 seconds, announced by a tick
        /// of the slave's effect carrying the victim's id.
        /// </summary>
        internal void OnMindSlaveHit(MapChannel mapChannel, Creature attacker, Creature victim)
        {
            MindSlave slave;

            lock (MindControlLock)
                slave = MindSlaves.FirstOrDefault(s => s.Creature == attacker && s.Pump == MindControlInfectious);

            if (slave == null || slave.InfectChance <= 0 || victim == attacker)
                return;

            if (victim.State == CharacterState.Dead || victim.State == CharacterState.Dying || !CanMindControl(victim)
                || MindControlPumpOf(victim) != 0 || IsMindControlImmune(victim, Environment.TickCount64))
                return;

            if (MindControlRandom.Next(100) >= slave.InfectChance)
                return;

            var infected = new MindSlave
            {
                Creature = victim,
                Controller = slave.Controller,
                MapChannel = mapChannel,
                Pump = MindControlConfusion,
                IntervalMs = slave.IntervalMs
            };

            infected.Effect = Confuse(mapChannel, slave.Controller, victim, MindControlConfusion, slave.InfectSeconds, slave.Effect.EffectLevel, slave.Effect.ActionId, infected);

            if (infected.Effect == null)
                return;

            lock (MindControlLock)
            {
                MindSlaves.Add(infected);
                MindControlImmuneUntil[victim.EntityId] = long.MaxValue;
            }

            var tick = new GameEffectTickPacket(slave.Effect.EffectId, GameEffectTickPacket.TickKind.EntityId);
            tick.Entries.Add(new TickEntry { EntityId = victim.EntityId });
            CellManager.Instance.CellCallMethod(mapChannel, attacker, tick);
        }

        /// <summary>
        /// Runs the controlled creatures on this map: the frightened kept running, the confused
        /// and subverted turned on someone new every INTERVAL, the enslaved let go when their
        /// master leaves; and the spent immunities forgotten.
        /// </summary>
        internal void MindControlWorker(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;
            List<MindSlave> here;

            lock (MindControlLock)
            {
                foreach (var spent in MindControlImmuneUntil.Where(p => p.Value <= now).Select(p => p.Key).ToList())
                    MindControlImmuneUntil.Remove(spent);

                here = MindSlaves.Where(s => s.MapChannel == mapChannel).ToList();
            }

            foreach (var slave in here)
            {
                var creature = slave.Creature;

                // Gone from the map, or dead: its effects went with it.
                if (!EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) || registered != creature
                    || creature.State == CharacterState.Dead || creature.Controller == null)
                {
                    lock (MindControlLock)
                        MindSlaves.Remove(slave);

                    continue;
                }

                var controllerHere = EntityManager.Instance.Players.TryGetValue(slave.Controller.EntityId, out var controller)
                    && controller == slave.Controller && controller.MapContextId == creature.MapContextId;

                switch (slave.Pump)
                {
                    case MindControlFrighten:
                    {
                        if (controllerHere)
                            slave.FearFrom = controller.Position;

                        var fleeing = creature.Controller.CurrentAction == BehaviorManager.BehaviorActionWander && creature.Controller.ActionWander.Fleeing;

                        if (!fleeing && Vector3.Distance(creature.Position, slave.FearFrom) < MindControlFearRange)
                            BehaviorManager.Instance.Flee(mapChannel, creature, slave.FearFrom);

                        break;
                    }

                    case MindControlConfusion:
                    case MindControlSubversion:
                    {
                        if (now < slave.NextPickAt)
                            break;

                        slave.NextPickAt = now + slave.IntervalMs;

                        var victim = PickMindControlVictim(mapChannel, creature);

                        if (victim == 0)
                            break;

                        creature.Hate.Clear();
                        creature.Target = victim;
                        BehaviorManager.Instance.SetActionFighting(creature, victim);
                        break;
                    }

                    default:
                        // Enslaved: no master on the map, no slave.
                        if (!controllerHere)
                            GameEffectManager.Instance.DettachEffect(mapChannel, creature, slave.Effect);

                        break;
                }
            }
        }

        /// <summary>
        /// Someone at random within the creature's aggro range that it may fight now
        /// (Threat.CanFight, which Mind Control bends): any combatant, players included, when
        /// confused; its own kind when subverted. 0 for nobody.
        /// </summary>
        private static ulong PickMindControlVictim(MapChannel mapChannel, Creature creature)
        {
            var reach = creature.AggroRange > 0 ? creature.AggroRange : MindControlDefaultReach;
            var candidates = new List<ulong>();

            foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
            {
                foreach (var other in cell.CreatureList)
                    if (other != creature && Vector3.Distance(creature.Position, other.Position) <= reach && Threat.CanFight(creature, other.EntityId))
                        candidates.Add(other.EntityId);

                foreach (var client in cell.ClientList)
                {
                    var player = client.Player;

                    if (player == null || player.GmFlagAlwaysFriendly || Vector3.Distance(creature.Position, player.Position) > reach)
                        continue;

                    if (Threat.CanFight(creature, player.EntityId))
                        candidates.Add(player.EntityId);
                }
            }

            if (candidates.Count == 0)
                return 0;

            lock (MindControlLock)
                return candidates[MindControlRandom.Next(candidates.Count)];
        }
    }
}

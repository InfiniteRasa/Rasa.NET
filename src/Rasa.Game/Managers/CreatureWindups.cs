using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A creature ability lands when its windup is done, not when it starts.
    ///
    /// The client plays a creature ability as a player's: the windup (PerformWindup, its
    /// animation and FX for the argument's windup time), and then, when the server resolves it,
    /// the recovery with the hits. Every one of the creature ability classes has
    /// delayResolution - it waits on the server - and most blockMovement and stopMovement. The
    /// server used to resolve a creature's ability on the next tick after the windup went out,
    /// so a Treeback's stomp knocked players twenty metres before the stomp was raised, and a
    /// Linker's chest blast went off at the start of a 7.3 s windup the client never got to show.
    ///
    /// Now an ability's missile waits for the windup - the creature_action row's, which is the
    /// client's (WindupMs, the argument's own when the row has none) - and then for the flight:
    /// VFX_VELOCITY metres a second where the argument has one (the Treeback's stomp wave 50,
    /// the Strider's eye 30), otherwise the half a millisecond a metre every missile has. The
    /// creature stands and does nothing else meanwhile (IsWindingUp), and one killed before its
    /// windup is done does not land it (MissileManager.DoWork).
    ///
    /// Weapon attacks - action 1 and 174 pairs, a module under weapons. - are shots and blows,
    /// not abilities, and land as they did.
    ///
    /// The actions that are not missiles wait the same way (After): Rage, Scourge and the warcry
    /// (CreatureBuffs), the Howler's shriek (CreatureDebuffs), the Technician's turret and the
    /// Hunter's pet (CreatureSummons) and the Amoeboid's vomit send their windup when they start
    /// and do what they do, with its recovery, when it is done - the effect goes on, the ally
    /// comes, the child is born then, and not before the animation that shows it.
    ///
    /// The Linker's chest blast has more to its windup: LinkerChestBlastWindupEffect, "absorbs
    /// damage done to a Linker performing the Chest Blast ability" (LINKER_CHEST_BLAST_WINDUP 270,
    /// FX at level 1), put on the Linker for the windup with the argument's
    /// EFFECT_DAMAGE_ABSORPTION_PERCENT (100) taken off every hit.
    /// </summary>
    public static class CreatureWindups
    {
        public const ActionId LinkerChestBlast = (ActionId)263;
        public const int ChestBlastWindupTypeId = 270;      // LINKER_CHEST_BLAST_WINDUP

        /// <summary>Milliseconds a missile takes a metre when its argument gives no VFX_VELOCITY, as MissileLaunch has it.</summary>
        public const double DefaultMsPerMetre = 0.5;

        /// <summary>Whether this action is an ability rather than a weapon's shot or blow.</summary>
        public static bool IsAbility(string module) => !string.IsNullOrEmpty(module) && !module.StartsWith("weapons.");

        /// <summary>The windup: the row's, else the argument's.</summary>
        public static int WindupMsOf(CreatureAction action, ActionLevelInfo info)
        {
            if (action != null && action.WindupTime > 0)
                return (int)action.WindupTime;

            return Math.Max(0, info?.WindupMs ?? 0);
        }

        /// <summary>The flight over this distance: at VFX_VELOCITY metres a second, else DefaultMsPerMetre.</summary>
        public static int FlightMs(ActionLevelInfo info, float distance)
        {
            var velocity = info?.Get(AbilityProperty.VfxVelocity) ?? 0;

            return velocity > 0
                ? (int)Math.Round(Math.Max(0, distance) / velocity * 1000)
                : (int)(Math.Max(0, distance) * DefaultMsPerMetre);
        }

        /// <summary>
        /// Whether the target of an ability wound up at an area is still in it as it lands: the
        /// cone aimed, or the ring centred, where the target stood at the start of the windup
        /// (Missile.AreaCentre), the ring around the creature where it stands. An ability with no
        /// area always finds its target.
        /// </summary>
        public static bool StillCaught(Creature creature, Missile missile)
        {
            var target = missile?.TargetActor;

            if (creature == null || target == null)
                return true;

            var area = CreatureAreaAttacks.AreaOf(missile.ActionId, missile.ActionArgId);

            if (!area.IsArea)
                return true;

            return area.Contains(creature.Position, AbilityManager.FacingOf(creature), missile.AreaCentre ?? target.Position, target.Position);
        }

        private sealed class Deferred
        {
            public MapChannel MapChannel;
            public Creature Creature;
            public long At;
            public Action Resolve;
            public CreatureAction Action;
        }

        /// <summary>
        /// A wound-up action that comes to nothing, put right on the clients: ActionInterrupt,
        /// which Actor.Recv_ActionInterrupt answers by cancelling the action when it is the one
        /// the actor is performing - its windup animation and FX stopped, and the actor out of
        /// WINDUP. Every creature ability waits on the server for its recovery (delayResolution),
        /// so one that never gets one otherwise stays wound up until its next action. Not for a
        /// creature dead or dying: DEAD is the end of whatever it was doing already.
        /// </summary>
        public static void Interrupt(MapChannel mapChannel, Creature creature, ActionId actionId, uint actionArgId)
        {
            if (mapChannel == null || creature == null || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            CellManager.Instance.CellCallMethod(mapChannel, creature, new ActionInterruptPacket(creature.EntityId, actionId, actionArgId));
        }

        private static readonly List<Deferred> Pending = new List<Deferred>();
        private static readonly object PendingLock = new object();

        /// <summary>
        /// A creature action that is not a missile, wound up: the creature stops and stands for
        /// windupMs, and resolve runs when it is done - unless the creature is dead, dying or
        /// stunned by then, when nothing comes of it and the action is interrupted on the clients.
        /// No windup, and it runs now.
        /// </summary>
        /// <param name="action">The action wound up, to interrupt if it comes to nothing.</param>
        public static void After(MapChannel mapChannel, Creature creature, int windupMs, Action resolve, CreatureAction action = null)
        {
            if (resolve == null)
                return;

            if (windupMs <= 0 || mapChannel == null || creature == null)
            {
                resolve();
                return;
            }

            var at = Environment.TickCount64 + windupMs;

            creature.Controller.WindupUntil = at;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            BehaviorManager.Instance?.StopMoving(creature);

            lock (PendingLock)
                Pending.Add(new Deferred { MapChannel = mapChannel, Creature = creature, At = at, Resolve = resolve, Action = action });
        }

        /// <summary>Whether this creature has a wound-up action waiting on its windup.</summary>
        public static bool HasPending(Creature creature)
        {
            lock (PendingLock)
                return Pending.Any(p => p.Creature == creature);
        }

        /// <summary>The wound-up actions on this map whose windup is done. Run every map tick.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Deferred> due;
            var now = Environment.TickCount64;

            lock (PendingLock)
            {
                due = Pending.Where(p => p.MapChannel == mapChannel && now >= p.At).ToList();

                foreach (var deferred in due)
                    Pending.Remove(deferred);
            }

            foreach (var deferred in due)
            {
                var creature = deferred.Creature;

                if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying
                    || creature.MapContextId != mapChannel.MapInfo.MapContextId
                    || !creature.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                    continue;

                // Stunned or knocked down out of it: nothing comes of it, and the clients are told.
                if (Stuns.IsStunned(creature))
                {
                    if (deferred.Action != null)
                        Interrupt(mapChannel, creature, deferred.Action.ActionId, deferred.Action.ActionArgId);

                    continue;
                }

                try
                {
                    deferred.Resolve();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"CreatureWindups: {creature.EntityId}'s wound-up action threw and was dropped: {e}");

                    if (deferred.Action != null)
                        Interrupt(mapChannel, creature, deferred.Action.ActionId, deferred.Action.ActionArgId);
                }
            }
        }

        /// <summary>Whether the creature is winding an ability up: it stands and does nothing else.</summary>
        public static bool IsWindingUp(Creature creature) =>
            creature?.Controller != null && Environment.TickCount64 < creature.Controller.WindupUntil;

        /// <summary>
        /// A creature starts winding an ability up: how long until its missile lands (windup and
        /// flight), or null for an action that lands as it always did - a weapon's, or an ability
        /// with no windup and no velocity.
        /// </summary>
        public static int? Begin(MapChannel mapChannel, Creature creature, CreatureAction action, float distance)
        {
            if (creature == null || action == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetAction(action.ActionId, action.ActionArgId, out var module, out var info)
                || !IsAbility(module))
                return null;

            var windupMs = WindupMsOf(action, info);
            var landsIn = windupMs + FlightMs(info, distance);

            if (windupMs > 0)
                creature.Controller.WindupUntil = Environment.TickCount64 + windupMs;

            if (action.ActionId == LinkerChestBlast && windupMs > 0 && mapChannel != null)
                Shield(mapChannel, creature, info, windupMs);

            return landsIn;
        }

        private static void Shield(MapChannel mapChannel, Creature linker, ActionLevelInfo info, int windupMs)
        {
            var absorb = info.Get(AbilityProperty.EffectDamageAbsorptionPercent, 100);

            GameEffectManager.Instance.Attach(mapChannel, linker, new GameEffect
            {
                TypeId = ChestBlastWindupTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                ActionId = info.ActionId,
                SourceId = linker.EntityId,
                Source = linker,
                SourceLevel = (int)linker.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ResistModifier = CreatureSummons.ResistFor(absorb),
                ExpiresTick = Environment.TickCount64 + windupMs
            });
        }
    }
}

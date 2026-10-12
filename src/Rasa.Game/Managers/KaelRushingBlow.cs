using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game.Missions.World;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// CR_KAEL_RUSHING_BLOW (496): the Kael charges whoever it is fighting and brings its fists
    /// down where they stood.
    ///
    /// What the client has:
    ///  - KaelRushingBlowAbility is the player's Rushing Blow (abilities.rushingblow) made a
    ///    creature's: its Windup sets the windup to the distance to the target over the action's
    ///    VFX_VELOCITY (30 m/s), with stopMovement, blockMovement and moveInterrupts all off, and
    ///    plays ABILITY_CREATURE_KAEL_RUSHING_BLOW_WINDUP (animation family 1438) scaled to it,
    ///    then _RESOLVE (1439) over a 1166 ms recovery. Nothing in it moves the Kael. What does
    ///    is a movement: a MoveObject of MovementType.Rush puts its controller into the client's
    ///    RushingMoveState, which runs it to the position at the velocity (BehaviorManager.Rush),
    ///    as a Commando is run (AbilityManager.RushingBlow).
    ///  - Its DoAbility reads hitdata as (entityId, rawInfo) pairs, unlike every other creature
    ///    class (RecoveryShape.EntityRawInfo).
    ///  - Argument 1 (CR_KAEL_RUSHING_BLOW_SHARED_MINION): damage 60-80, EFFECT_RADIUS 15,
    ///    KNOCKBACK_DISTANCE 3 at CHANCE_KNOCK_BACK 30, DURATION 1, range 50, reuse 0. The other
    ///    arguments are the same blow harder - _SHARED_BOSS 2, _OPERATION_MINION 3,
    ///    _OPERATION_BOSS 4, _EPIC_MINIBOSS 5.
    ///
    /// So the charge goes like this. When the Kael picks the blow (8-50 m from a target it can
    /// see, per its creature_action row), the windup goes out and the Kael is sent running toward
    /// where the target stood, ChargeStopShort short of it, at the speed that brings it there as
    /// the client's windup ends. The clients make the run; the server keeps its own place along
    /// it a tick at a time. It does nothing else meanwhile. Where
    /// it lands the blow falls on everyone within EFFECT_RADIUS of that spot - the target if it
    /// is still there, or anyone who stood near it - and each is knocked back 3 m on a 30% roll
    /// or staggered for DURATION seconds otherwise, as the Commando's blow stuns for its DURATION.
    ///
    /// The destination is fixed when the charge starts, as the client's windup time is: a target
    /// that runs sideways gets out of the way; one that stays, or runs straight away but not
    /// fifteen metres, does not. The charge follows the line along the navmesh and stops where
    /// the ground ends or a force field stands, as a knockback does. A Kael killed or put into
    /// its Critical Death window mid-charge never lands the blow, and is where the run ends: its
    /// clients cannot be stopped part-way (BehaviorManager.EndCarry). A manually authorized
    /// charge also requires its original combat grant when it lands; rearming the creature
    /// cannot revive an old charge.
    ///
    /// CR_HUMAN_RUSHING_BLOW (504), the AFS soldiers', is charged the same way. Its class is the
    /// player's own RushingBlowAction (abilities.rushingblow), a DamageBase on its target: the
    /// windup is the distance over DEFAULT_PROJECTILE_VELOCITY (its argument has no
    /// VFX_VELOCITY), and the blow falls on the target alone, as the Commando's does - argument 1
    /// gives no EFFECT_RADIUS - wherever it has got to in the third of a second a 20 m charge
    /// takes. Its KNOCKBACK_DISTANCE 3 has no CHANCE_KNOCK_BACK, so it always knocks back
    /// (PlayerCrowdControl). One thing the server cannot mend: RushingBlowAction.Windup reads its
    /// target before setting it, which a player's own client has set already and a watcher's has
    /// not (KaelRushingBlowAbility.Windup calls SetTarget first), so a watching client is likely
    /// to drop the windup's animation; the run and the hit are sent all the same.
    /// </summary>
    public static class KaelRushingBlow
    {
        public const ActionId Action = ActionId.CrKaelRushingBlow;

        /// <summary>CR_HUMAN_RUSHING_BLOW: a charge with a blow on its target alone.</summary>
        public const ActionId HumanAction = ActionId.CrHumanRushingBlow;

        /// <summary>DEFAULT_PROJECTILE_VELOCITY, what the client falls back on when an argument gives no VFX_VELOCITY.</summary>
        public const float DefaultVelocity = 70f;

        /// <summary>How far short of the target's spot the charge ends, as the Commando's does. Not in the client.</summary>
        public const float ChargeStopShort = AbilityManager.ChargeStopShort;

        /// <summary>The Kael's area if an argument gives no EFFECT_RADIUS (every one of its arguments gives 15).</summary>
        public const float DefaultRadius = 5f;

        private sealed class Charge
        {
            public MapChannel MapChannel;
            public Creature Kael;
            public CreatureAction Action;
            public Actor Target;
            public Vector3 Impact;
            public float Radius;
            public int Damage;
            public long LandsAt;
            public ScriptedCombatAuthorization CombatAuthorization;
        }

        private static readonly List<Charge> Charges = new List<Charge>();
        private static readonly object ChargesLock = new object();

        /// <summary>Whether the action is a charge: the Kael's rushing blow or the human one.</summary>
        public static bool Is(CreatureAction action) => action != null && (action.ActionId == Action || action.ActionId == HumanAction);

        /// <summary>
        /// The area the blow falls on around where the target stood: EFFECT_RADIUS, the Kael's
        /// DefaultRadius when its argument has none, and 0 for the human blow without one, which
        /// strikes its target alone.
        /// </summary>
        public static float RadiusOf(ActionId actionId, ActionLevelInfo level)
        {
            if (level != null && level.Get(AbilityProperty.EffectRadius) > 0)
                return level.Get(AbilityProperty.EffectRadius);

            return actionId == Action ? DefaultRadius : 0f;
        }

        /// <summary>Whether the Kael is mid-charge: carried, and doing nothing else until its blow lands.</summary>
        public static bool IsCharging(Creature creature)
        {
            // A loop, not Any(): this is asked of every creature on every think, and Any with a
            // lambda that captures the creature allocates a closure and a delegate each time.
            lock (ChargesLock)
            {
                foreach (var charge in Charges)
                    if (charge.Kael == creature)
                        return true;

                return false;
            }
        }

        /// <summary>The windup the client plays for a charge over this distance at this speed, in ms.</summary>
        public static long WindupMs(float distance, float velocity)
        {
            if (distance <= 0 || velocity <= 0)
                return 0;

            return (long)Math.Round(distance / velocity * 1000.0);
        }

        /// <summary>
        /// Where a charge from `from` at a target standing at `target` ends: ChargeStopShort short
        /// of it on the flat, along the ground where the map has a navmesh.
        /// </summary>
        public static Vector3 ChargeEnd(MapChannel mapChannel, Vector3 from, Vector3 target)
        {
            var flat = new Vector2(target.X - from.X, target.Z - from.Z).Length();
            var run = Math.Max(0f, flat - ChargeStopShort);

            if (run <= 0.1f)
                return from;

            return CrowdControl.KnockbackDestination(mapChannel, from, CrowdControl.AwayFrom(from, target), run);
        }

        /// <summary>Starts the charge: the windup to everyone who can see the Kael, and the carry.</summary>
        public static void Start(MapChannel mapChannel, Creature kael, CreatureAction action, Actor target, int damage)
        {
            if (mapChannel == null || kael == null || target == null)
                return;

            var authorization = kael.ScriptedCombatAuthorization;
            if (!CreatureWindups.HasCombatAuthorization(mapChannel, kael, authorization))
                return;

            AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var level);

            var velocity = level != null && level.Get(AbilityProperty.VfxVelocity) > 0 ? level.Get(AbilityProperty.VfxVelocity) : DefaultVelocity;
            var radius = RadiusOf(action.ActionId, level);
            var windupMs = WindupMs(Vector3.Distance(kael.Position, target.Position), velocity);
            var impact = target.Position;
            var end = CrowdControl.BeforeFields(mapChannel, kael, kael.Position, ChargeEnd(mapChannel, kael.Position, impact));
            var run = Vector3.Distance(kael.Position, end);

            CellManager.Instance.CellCallMethod(mapChannel, kael,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, target.EntityId));

            // The run, told to the clients once: there as the windup ends.
            if (run > 0.1f && windupMs > 0)
                BehaviorManager.Instance.Rush(kael, end, run / (windupMs / 1000f));

            lock (ChargesLock)
            {
                Charges.RemoveAll(c => c.Kael == kael);
                Charges.Add(new Charge
                {
                    MapChannel = mapChannel,
                    Kael = kael,
                    Action = action,
                    Target = target,
                    Impact = impact,
                    Radius = radius,
                    Damage = damage,
                    LandsAt = Environment.TickCount64 + windupMs,
                    CombatAuthorization = authorization
                });
            }
        }

        /// <summary>Lands due blows on this map, unless their Kael has fallen or their manual combat grant has expired.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Charge> due;
            var now = Environment.TickCount64;

            lock (ChargesLock)
            {
                due = Charges.Where(c => c.MapChannel == mapChannel && (now >= c.LandsAt || !Standing(c.Kael))).ToList();

                foreach (var charge in due)
                    Charges.Remove(charge);
            }

            foreach (var charge in due)
            {
                var kael = charge.Kael;

                if (!Standing(kael) || kael.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                if (!CreatureWindups.HasCombatAuthorization(mapChannel, kael, charge.CombatAuthorization))
                {
                    if (CreatureManager.IsLivingOnMap(mapChannel, kael))
                    {
                        BehaviorManager.Instance.EndCarry(mapChannel, kael);
                        BehaviorManager.Instance.StopMoving(kael);
                        CreatureWindups.Interrupt(mapChannel, kael, charge.Action.ActionId, charge.Action.ActionArgId);
                    }
                    continue;
                }

                // The run is over: it is where it ends, as its clients have it.
                BehaviorManager.Instance.EndCarry(mapChannel, kael);
                kael.LastYaw = AbilityManager.YawTowards(kael.Position, charge.Impact);
                BehaviorManager.Instance.StopMoving(kael);

                // A blow with no area is the target's, wherever it has got to (CreatureStrike
                // passes over one that has died or left); one with an area falls on whoever is in
                // it, the target only if it is still there.
                if (charge.Radius <= 0)
                {
                    MissileManager.Instance.CreatureStrike(mapChannel, kael, charge.Action, charge.Target, charge.Damage,
                        default, charge.Impact, charge.CombatAuthorization);
                    continue;
                }

                var aimedAt = charge.Target != null && Vector3.DistanceSquared(charge.Target.Position, charge.Impact) <= charge.Radius * charge.Radius
                    ? charge.Target
                    : null;

                MissileManager.Instance.CreatureStrike(mapChannel, kael, charge.Action, aimedAt, charge.Damage,
                    CreatureArea.AroundPoint(charge.Radius), charge.Impact, charge.CombatAuthorization);
            }
        }

        private static bool Standing(Creature kael)
        {
            return kael != null && kael.State != CharacterState.Dead && kael.State != CharacterState.Dying
                && kael.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
        }
    }
}

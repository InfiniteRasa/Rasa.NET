using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Knockback, slows and freezes on creatures. A player is knocked back and stunned by
    /// PlayerCrowdControl; nothing a creature does slows or roots a player yet.
    ///
    /// Knockback: the creature slides KNOCKBACK_DISTANCE metres straight away from whoever hit it,
    /// at KnockbackSpeed, stopping short where the navmesh ends, then takes the client's getup
    /// time (KNOCKBACK_GETUP_TIME_MSEC 2000) to recover. It is a stun for all of that - the
    /// effect is flagged IsStun, so it also counts towards Critical Death. The client's
    /// KnockbackEffect (KNOCKBACK 8, and CRIT_SONIC 7 for a Sonic crit) takes the duration in its
    /// OnAttach. Sources: Tectonic Strike 3-15 m, Concussive Wave 20/30 m, Rushing Blow 3-15 m,
    /// Force Blast P6/P7 10 m (KNOCKBACK_DISTANCE), and Sonic crits at the client's
    /// KNOCKBACK_DEFAULT_DISTANCE of 10 m.
    ///
    /// Slow: an effect with MovementModifierPercent below 100; the creature moves at that percent
    /// of its speed (GameEffectManager.UpdateMovementMod sets MovementSpeed, which BehaviorManager
    /// applies, and tells the clients so its animation slows to match). Ruin P5: its
    /// EFFECT_MOVEMENT_MODIFIER 20, which the tooltip shows as "Movement: -20%". Virulent crits:
    /// CRIT_VIRULENT, "Crippled: %(snareMod)s%% Movement", VirulentCrippleSlowPercent for
    /// VirulentCrippleMs - neither number is in the client.
    ///
    /// Freeze: an effect flagged IsRoot; the creature cannot move but still attacks whatever is
    /// in reach. Net guns: NET_GUN_IMMOBILIZE_EFFECT 125 (its client class blocks movement on
    /// attach) for NetGunRootMs a hit - the weapon's own duration is not in the server's data.
    /// </summary>
    public static class CrowdControl
    {
        public const int KnockbackTypeId = 8;               // KNOCKBACK
        public const int CritSonicTypeId = 7;               // CRIT_SONIC
        public const int CritVirulentTypeId = 10000018;     // CRIT_VIRULENT
        public const int NetGunRootTypeId = 125;            // NET_GUN_IMMOBILIZE_EFFECT

        /// <summary>gameconstants.KNOCKBACK_DEFAULT_DISTANCE: a knockback with no distance of its own.</summary>
        public const float DefaultKnockbackDistance = 10f;

        /// <summary>gameconstants.KNOCKBACK_GETUP_TIME_MSEC: the time on the ground after landing.</summary>
        public const int GetupMs = 2000;

        /// <summary>Metres a second a knocked-back creature slides. Not in the client.</summary>
        public const float KnockbackSpeed = 15f;

        /// <summary>Virulent crit: "Crippled: -50% Movement" for 4 s. Not in the client.</summary>
        public const int VirulentCrippleSlowPercent = 50;
        public const int VirulentCrippleMs = 4000;

        /// <summary>How long a net gun hit holds a creature. Not in the server's weapon data.</summary>
        public const int NetGunRootMs = 3000;

        /// <summary>How far a knockback path may stray from the straight line onto the navmesh before it stops there.</summary>
        private const float PathTolerance = 1.0f;
        private const float PathStep = 0.5f;

        public static bool IsRooted(Actor actor)
        {
            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.IsRoot && !effect.IsExpired)
                    return true;

            return false;
        }

        /// <summary>The horizontal direction from source to target, or +Z when they stand on each other.</summary>
        public static Vector3 AwayFrom(Vector3 source, Vector3 target)
        {
            var dir = new Vector3(target.X - source.X, 0f, target.Z - source.Z);
            var length = dir.Length();

            return length < 0.1f ? new Vector3(0f, 0f, 1f) : dir / length;
        }

        /// <summary>
        /// Where a knockback of <paramref name="distance"/> along <paramref name="dir"/> ends: the
        /// straight line, walked in half-metre steps and stopped at the last one still on the
        /// navmesh, so a creature is not driven into a wall or off a cliff. With no navmesh, the
        /// full distance.
        /// </summary>
        public static Vector3 KnockbackDestination(MapChannel mapChannel, Vector3 from, Vector3 dir, float distance)
        {
            var navMesh = mapChannel?.NavMesh;

            if (navMesh == null)
                return from + dir * distance;

            var end = from;

            for (var d = PathStep; d <= distance + 0.001f; d += PathStep)
            {
                var probe = from + dir * d;
                var ground = navMesh.Nearest(probe);

                if (ground == null)
                    break;

                var gap = new Vector2(ground.Value.X - probe.X, ground.Value.Z - probe.Z).Length();

                if (gap > PathTolerance)
                    break;

                end = ground.Value;
            }

            return end;
        }

        /// <summary>
        /// Knocks a creature back from <paramref name="source"/>. typeId is the client effect that
        /// shows it: KNOCKBACK, or CRIT_SONIC for a Sonic crit. extraStunMs keeps it down that much
        /// longer after the getup time (Hand to Hand's "Stun Duration").
        /// </summary>
        public static bool Knockback(MapChannel mapChannel, Creature target, Actor source, float distance, int typeId, DamageType damageType, int extraStunMs = 0)
        {
            if (target == null || source == null || distance <= 0f || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return false;

            if (!target.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return false;

            var dir = AwayFrom(source.Position, target.Position);
            var destination = KnockbackDestination(mapChannel, target.Position, dir, distance);
            var travelled = Vector3.Distance(target.Position, destination);
            var flightMs = (int)(travelled / KnockbackSpeed * 1000f);
            var downMs = flightMs + GetupMs + Math.Max(0, extraStunMs);

            // An emplacement is knocked down where it stands, not off its mount.
            if (travelled > 0.1f && !Emplacements.Is(target))
            {
                target.KnockbackTo = destination;
                target.KnockbackDirection = dir;
            }

            var knock = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source.EntityId,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? 1,
                IsBuff = false,
                IsStun = true,
                ExpiresTick = Environment.TickCount64 + downMs
            };

            // KnockbackEffect.OnAttach(target, duration).
            GameEffectManager.Instance.Attach(mapChannel, target, knock, downMs / 1000.0);

            // A knockback is a stun: it may open the Critical Death window.
            CritDeathManager.Instance.TryEnterPreDeath(mapChannel, target, source, damageType);

            return true;
        }

        /// <summary>
        /// Vortex: "Draws nearby unfriendlies forcefully towards the user with a vortex of kinetic
        /// force" (uielement 472). The creature is dragged to PullStopShort metres from the puller,
        /// along the ground where the map has a navmesh, over the flail the client plays on it -
        /// VortexAction.OnAbility puts the target into the flailing posture and stands it up again
        /// after the action's recovery time - so it arrives as it gets up. It goes where it is
        /// dragged and nothing else meanwhile (BehaviorManager.StepKnockback), facing the puller.
        /// No client effect: the flail is the client's own. Returns whether it was moved.
        /// </summary>
        public static bool Pull(MapChannel mapChannel, Creature target, Actor puller, int flailMs)
        {
            if (target == null || puller == null || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return false;

            if (!target.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return false;

            // Nothing drags an emplacement off its mount.
            if (Emplacements.Is(target))
                return false;

            var (destination, speed) = PullPath(mapChannel, target.Position, puller.Position, flailMs);

            if (Vector3.Distance(target.Position, destination) <= 0.1f)
                return false;

            target.KnockbackTo = destination;
            target.KnockbackDirection = AwayFrom(target.Position, puller.Position);
            target.KnockbackSpeed = speed;
            target.KnockbackIsPull = true;

            BehaviorManager.Instance.StopMoving(target);

            return true;
        }

        /// <summary>How far short of the puller a pulled creature stops, in metres. Not in the client.</summary>
        public const float PullStopShort = 2f;

        /// <summary>The slowest a pull goes, so a short flail over a long way does not become a slow crawl.</summary>
        public const float PullMinSpeed = KnockbackSpeed;

        /// <summary>
        /// Where a pull from `from` towards `puller` ends and how fast it goes to arrive in flailMs:
        /// PullStopShort short of the puller along the line (or where the navmesh ends on the way),
        /// at no less than PullMinSpeed.
        /// </summary>
        public static (Vector3 Destination, float Speed) PullPath(MapChannel mapChannel, Vector3 from, Vector3 puller, int flailMs)
        {
            var dir = AwayFrom(from, puller);
            var flat = new Vector2(puller.X - from.X, puller.Z - from.Z).Length();
            var distance = Math.Max(0f, flat - PullStopShort);

            if (distance <= 0.1f)
                return (from, PullMinSpeed);

            var destination = KnockbackDestination(mapChannel, from, dir, distance);
            var travelled = Vector3.Distance(from, destination);
            var speed = flailMs > 0 ? Math.Max(PullMinSpeed, travelled / (flailMs / 1000f)) : PullMinSpeed;

            return (destination, speed);
        }

        /// <summary>Slows a creature to (100 - slowPercent)% of its speed for durationMs, shown as the given effect type.</summary>
        public static bool Slow(MapChannel mapChannel, Creature target, Actor source, int typeId, int slowPercent, int durationMs, string tooltipKey = null)
        {
            if (target == null || slowPercent <= 0 || durationMs <= 0 || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return false;

            var slow = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source?.EntityId ?? 0,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? 1,
                IsBuff = false,
                MovementModifierPercent = Math.Max(1, 100 - slowPercent),
                ExpiresTick = Environment.TickCount64 + durationMs
            };

            if (tooltipKey != null)
                slow.Tooltip[tooltipKey] = -slowPercent;

            GameEffectManager.Instance.Attach(mapChannel, target, slow);

            return true;
        }

        /// <summary>Holds a creature where it stands for durationMs; it can still attack.</summary>
        public static bool Root(MapChannel mapChannel, Creature target, Actor source, int typeId, int durationMs)
        {
            if (target == null || durationMs <= 0 || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return false;

            var root = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source?.EntityId ?? 0,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? 1,
                IsBuff = false,
                IsRoot = true,
                ExpiresTick = Environment.TickCount64 + durationMs
            };

            GameEffectManager.Instance.Attach(mapChannel, target, root);
            BehaviorManager.Instance.StopMoving(target);

            return true;
        }
    }
}

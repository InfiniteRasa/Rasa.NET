using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The area a creature's attack covers, from its action data - the same three shapes a
    /// player's direct-damage ability reads (AbilityManager.ResolveDirectDamage):
    ///
    ///  - CONE_RADIUS: a cone the action's range long, CONE_RADIUS degrees either side of the
    ///    line to the target (Kael tectonic strike, Howler sonic attack, Treeback stomp);
    ///  - RADIUS_AROUND_SOURCE: everything that close to the creature (Kael smash and ground
    ///    pound, Lightbender quill, the mech's ground pound);
    ///  - RADIUS_AROUND_TARGET: everything that close to whoever it aimed at.
    ///
    /// The client plays these as one hit per victim: the recovery lists every entity hit with its
    /// own rawInfo, and KaelSmashAbility and the rest walk the list announcing each. Before this
    /// every creature attack hit its target alone, whatever area its data gave it.
    ///
    /// EFFECT_RADIUS is not read here. On a creature action it belongs to a bomb or a projectile
    /// that lands somewhere (Kael rushing blow, Stalker egg drop, the death explosions), which is
    /// more than a hit and has its own handling to come.
    /// </summary>
    public readonly struct CreatureArea
    {
        /// <summary>The slack a cone's reach is given past the action's range, as a player's cone has.</summary>
        public const float ConeRangeSlack = 2.5f;

        public readonly float ConeHalfAngle;
        public readonly float ConeRange;
        public readonly float AroundSource;
        public readonly float AroundTarget;

        public CreatureArea(float coneHalfAngle, float coneRange, float aroundSource, float aroundTarget)
        {
            ConeHalfAngle = coneHalfAngle;
            ConeRange = coneRange;
            AroundSource = aroundSource;
            AroundTarget = aroundTarget;
        }

        public bool IsArea => ConeHalfAngle > 0 || AroundSource > 0 || AroundTarget > 0;

        /// <summary>Everything within radius of a point the attack names itself - where a charge's blow lands (KaelRushingBlow).</summary>
        public static CreatureArea AroundPoint(float radius) => new CreatureArea(0, 0, 0, radius);

        /// <summary>The area an action's level gives, if it gives one.</summary>
        public static CreatureArea Of(ActionLevelInfo info)
        {
            if (info == null)
                return default;

            if (info.Has(AbilityProperty.ConeRadius) && info.Get(AbilityProperty.ConeRadius) > 0)
                return new CreatureArea(info.Get(AbilityProperty.ConeRadius), Math.Max(1, info.MaxRange) + ConeRangeSlack, 0, 0);

            if (info.Has(AbilityProperty.RadiusAroundSource) && info.Get(AbilityProperty.RadiusAroundSource) > 0)
                return new CreatureArea(0, 0, info.Get(AbilityProperty.RadiusAroundSource), 0);

            if (info.Has(AbilityProperty.RadiusAroundTarget) && info.Get(AbilityProperty.RadiusAroundTarget) > 0)
                return new CreatureArea(0, 0, 0, info.Get(AbilityProperty.RadiusAroundTarget));

            return default;
        }

        /// <summary>
        /// Whether a point is inside the area of an attack made from source at target. A cone
        /// with nothing to aim at points the way the source faces.
        /// </summary>
        public bool Contains(Vector3 source, Vector3 facing, Vector3? target, Vector3 point)
        {
            if (ConeHalfAngle > 0)
            {
                var aim = target.HasValue ? target.Value - source : facing;

                return AbilityManager.InCone(source, aim, point, ConeRange, ConeHalfAngle);
            }

            if (AroundSource > 0)
                return Vector3.DistanceSquared(source, point) <= AroundSource * AroundSource;

            if (AroundTarget > 0 && target.HasValue)
                return Vector3.DistanceSquared(target.Value, point) <= AroundTarget * AroundTarget;

            return false;
        }
    }

    public static class CreatureAreaAttacks
    {
        /// <summary>The area of a creature action, from its action data; none for a weapon pair or an action without one.</summary>
        public static CreatureArea AreaOf(ActionId actionId, uint actionArgId)
        {
            if (AbilityManager.Instance == null || !AbilityManager.Instance.TryGetLevel(actionId, actionArgId, out var info))
                return default;

            return CreatureArea.Of(info);
        }

        /// <summary>
        /// The players on the map, other than the one it aimed at, that an attack from attacker
        /// covers and that it may fight: alive, on the map, and inside the area. centre, when
        /// given, stands in for the target's position.
        /// </summary>
        public static List<Manifestation> PlayersCaught(MapChannel mapChannel, Creature attacker, CreatureArea area, Actor aimedAt, Vector3? centre = null)
        {
            var caught = new List<Manifestation>();

            if (mapChannel == null || attacker == null || !area.IsArea)
                return caught;

            var facing = AbilityManager.FacingOf(attacker);
            var target = centre ?? aimedAt?.Position;

            foreach (var client in mapChannel.ClientList)
            {
                var player = client?.Player;

                if (player == null || player == aimedAt || player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                    continue;

                if (player.MapContextId != mapChannel.MapInfo.MapContextId)
                    continue;

                if (!area.Contains(attacker.Position, facing, target, player.Position))
                    continue;

                if (!TargetCategories.MayFightPlayer(attacker.TargetCategory, player.CombatCategory))
                    continue;

                caught.Add(player);
            }

            return caught;
        }
    }
}

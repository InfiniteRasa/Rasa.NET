using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Cover and crouching. "Crouch behind sandbags, walls, trees, and other types of cover to
    /// reduce the damage you suffer from ranged attacks. When you take a hit, a color-coded display
    /// indicates how much of the attack's damage is mitigated by cover ... When the display is red,
    /// you endure the attack's full damage" (the strategy guide). "Enemies deliver bonus damage to
    /// crouched targets" in melee.
    ///
    /// From the client:
    /// - every damage announcement carries rawInfo.coverModifier, the share of the hit that got
    ///   through. The directional hit display (directionalhitwindow.py) is red at 0.8 and above,
    ///   orange from 0.6, yellow from 0.2 and green below; the overhead cover icon on whatever you
    ///   hit (overheadwindow.py) reads no cover at 0.8, half from 0.5, full below;
    /// - gameconstants: CROUCHED_MELEE_DAMAGE_TAKEN 1.25 - a melee hit on a crouched target does a
    ///   quarter more (the crit side of crouching, CROUCHED_MELEE_TO_BE_CRIT_MOD and
    ///   CROUCHED_RANGED_TO_CRIT, is CriticalHits');
    /// - COVER_DAMAGE_MODIFIER_LOOKUP, ([20, 21, 22, 23, 24, 13], [13, 13, 25, 26, 27, 28, 28]):
    ///   six entries for a standing target and seven for a crouched one, what look like skeleton
    ///   points the original cast its rays to. The skeletons are not in anything we have, so the
    ///   points are laid out on the body instead, six standing and seven crouched
    ///   (<see cref="SamplePoints"/>).
    ///
    /// The server's part: a ranged weapon hit is cast for from the attacker's eyes to each point on
    /// the target, against the map's collision meshes and terrain (Navigation.CoverMesh, built by
    /// Rasa.NavMesh as &lt;map&gt;.cover). The share of points in the clear is the modifier; the hit
    /// does that share of its damage, and never less than <see cref="MinModifier"/> - a target
    /// wholly behind cover still takes a sliver. Crouching lowers the target to
    /// <see cref="CrouchedHeightFactor"/> of its height, which is what puts it behind a sandbag.
    /// Target Painting's EFFECT_COVER_MODIFIER ("Reduced Cover: 50%" .. 0%) scales how much of the
    /// cover still counts on a painted target. Melee ignores cover. With no cover file for the map
    /// every modifier is 1.
    /// </summary>
    public static class Cover
    {
        /// <summary>gameconstants.CROUCHED_MELEE_DAMAGE_TAKEN.</summary>
        public const double CrouchedMeleeDamageTaken = 1.25;

        /// <summary>The least share of a hit that lands, however well covered the target is. Not in the client.</summary>
        public const double MinModifier = 0.1;

        /// <summary>A standing player's height, to lay the sample points on. Not in the client's data we have.</summary>
        public const float PlayerHeight = 1.8f;

        /// <summary>A creature's height at scale 1, for the same. Not in the client's data we have.</summary>
        public const float CreatureHeight = 1.8f;

        /// <summary>A crouched target's height, as a share of its standing height.</summary>
        public const float CrouchedHeightFactor = 0.55f;

        /// <summary>Half a body's width, for the points either side of its middle.</summary>
        public const float HalfWidth = 0.3f;

        /// <summary>Where an attacker sees from, as a share of its height.</summary>
        public const float EyeFactor = 0.9f;

        // (share of height, sideways offset in half-widths) - six standing, seven crouched.
        private static readonly (float Up, float Side)[] StandingPoints =
        {
            (0.95f, 0f), (0.75f, 0f), (0.55f, -1f), (0.55f, 1f), (0.4f, 0f), (0.2f, 0f)
        };

        private static readonly (float Up, float Side)[] CrouchedPoints =
        {
            (0.95f, 0f), (0.8f, -1f), (0.8f, 1f), (0.6f, 0f), (0.4f, -1f), (0.4f, 1f), (0.2f, 0f)
        };

        public static float HeightOf(Actor actor)
        {
            var height = actor is Creature creature ? CreatureHeight * (float)Math.Max(0.1, creature.Scale) : PlayerHeight;

            return IsCrouching(actor) ? height * CrouchedHeightFactor : height;
        }

        public static bool IsCrouching(Actor actor) => actor != null && actor.IsCrouching;

        public static Vector3 EyeOf(Actor actor) => actor.Position + new Vector3(0f, HeightOf(actor) * EyeFactor, 0f);

        /// <summary>The points on the target the rays are cast to, facing the attacker at <paramref name="from"/>.</summary>
        public static Vector3[] SamplePoints(Actor target, Vector3 from)
        {
            var layout = IsCrouching(target) ? CrouchedPoints : StandingPoints;
            var height = HeightOf(target);
            var toTarget = new Vector3(target.Position.X - from.X, 0f, target.Position.Z - from.Z);
            var side = toTarget.LengthSquared() < 1e-4f ? Vector3.UnitX : Vector3.Normalize(new Vector3(-toTarget.Z, 0f, toTarget.X));
            var points = new Vector3[layout.Length];

            for (var i = 0; i < layout.Length; i++)
                points[i] = target.Position + new Vector3(0f, height * layout[i].Up, 0f) + side * (HalfWidth * layout[i].Side);

            return points;
        }

        /// <summary>The share of the points in the clear, before the floor and Target Painting.</summary>
        public static double Visible(Navigation.CoverMesh cover, Vector3 eye, Vector3[] points)
        {
            if (cover == null || points.Length == 0)
                return 1.0;

            var clear = 0;

            foreach (var point in points)
                if (!cover.Blocked(eye, point))
                    clear++;

            return clear / (double)points.Length;
        }

        /// <summary>
        /// The modifier from the share in the clear: the cover's share, scaled by what of it still
        /// counts (Target Painting), taken off 1, floored at <see cref="MinModifier"/>. A target in
        /// the open is 1 whatever else.
        /// </summary>
        public static double ModifierFrom(double visible, int coverCountsPercent = 100)
        {
            if (visible >= 1.0)
                return 1.0;

            var covered = (1.0 - Math.Max(0.0, visible)) * Math.Max(0, Math.Min(100, coverCountsPercent)) / 100.0;

            return Math.Max(MinModifier, 1.0 - covered);
        }

        /// <summary>The share of a ranged hit from attacker that gets past the cover around target.</summary>
        public static double Modifier(MapChannel mapChannel, Actor attacker, Actor target)
        {
            var cover = mapChannel?.Cover;

            if (cover == null || attacker == null || target == null)
                return 1.0;

            var eye = EyeOf(attacker);

            return ModifierFrom(Visible(cover, eye, SamplePoints(target, eye)), GameEffectManager.CoverCountsPercentOf(target));
        }
    }
}

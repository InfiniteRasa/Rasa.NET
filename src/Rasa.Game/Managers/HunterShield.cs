using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Bane Hunter's shield: what it sends back at those who hit it from the front.
    ///
    /// The shield is the Hunter's weapon, Weapon_Creature_Hunter (10171) - its FX are
    /// weapon_bane_hunter_shield_draw, _persistent and _stow - so it is up while the Hunter has its
    /// weapon drawn, which is for as long as it fights (CreatureWeaponDraw), and down when it puts
    /// it away. The client's HunterShieldEffect (HUNTER_SHIELD 381, FX at level 1,
    /// EFFECT_CREATURE_HUNTER_SHIELD) is "a Hunter's shield to reflect damage": its
    /// Recv_ReflectDamage(damageTargetId, rawInfo) floats the reflected hit over the one it goes
    /// back at, at once - the Shield Drone's own call (ShieldDroneReflectPacket), without the
    /// drone's flight. Nothing in the client gives a share or a duration, so both are ours:
    /// <see cref="ReflectPercent"/> of every hit the Hunter takes from in front of it, for the
    /// whole fight. A hit from behind - an attacker in the half the Hunter has its back to - goes
    /// round the shield and is not reflected. The Hunter takes the hit either way.
    ///
    /// What is reflected is what the Hunter actually took (armour and health), the attacker takes
    /// it through ActorManager.Damage as a hit from the Hunter, and it is reflected from the same
    /// hits as the Shield Drone's (MissileManager): shots, blows and the abilities that land as
    /// missiles.
    ///
    /// A Hunter is recognised by its entity class: Bane_Hunter, Bane_Hunter_Boss and the
    /// lightning miniboss.
    /// </summary>
    public static class HunterShield
    {
        /// <summary>HUNTER_SHIELD.</summary>
        public const int TypeId = 381;

        /// <summary>What is sent back of a hit from the front. Ours.</summary>
        public const int ReflectPercent = 33;

        /// <summary>Weapon_Creature_Hunter: the shield.</summary>
        public const uint ShieldWeaponClass = 10171;

        /// <summary>Bane_Hunter, Bane_Hunter_Boss and Bane_Hunter_Lightning_Miniboss_EPIC.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)10166, (EntityClasses)10327, (EntityClasses)30132
        };

        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);

        public static bool IsUp(Creature creature) =>
            creature != null && creature.ActiveEffects.Values.Any(e => e.TypeId == TypeId);

        /// <summary>The shield goes up: a Hunter has drawn its weapon for a fight. Announced, with no timer.</summary>
        public static void Raise(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || !Is(creature) || IsUp(creature)
                || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
            {
                TypeId = TypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ShowsDuration = false
            });
        }

        /// <summary>The shield comes down: the Hunter has put its weapon away.</summary>
        public static void Lower(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null)
                return;

            foreach (var shield in creature.ActiveEffects.Values.Where(e => e.TypeId == TypeId).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, creature, shield);
        }

        /// <summary>
        /// Whether a point is in the half in front of the Hunter: within 90 degrees either side of
        /// the way it faces, on the flat. Standing on it counts as in front.
        /// </summary>
        public static bool InFront(Actor hunter, Vector3 point)
        {
            var to = new Vector2(point.X - hunter.Position.X, point.Z - hunter.Position.Z);

            if (to.LengthSquared() < 0.25f)
                return true;

            var facing = AbilityManager.FacingOf(hunter);

            return Vector2.Dot(new Vector2(facing.X, facing.Z), to) >= 0f;
        }

        /// <summary>
        /// Sends ReflectPercent of what the Hunter took back at whoever hit it, if its shield is up
        /// and they hit it from the front. The attacker takes it as a hit from the Hunter - so it
        /// can kill, and the kill is the Hunter's - and the clients that see the Hunter are told
        /// through the shield's own ReflectDamage.
        /// </summary>
        public static void Reflect(MapChannel mapChannel, Creature hunter, Actor attacker, int taken, DamageType damageType)
        {
            if (mapChannel == null || hunter == null || attacker == null || attacker == hunter || taken <= 0)
                return;

            if (attacker.State == CharacterState.Dead || attacker.State == CharacterState.Dying)
                return;

            var shield = hunter.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == TypeId);

            if (shield == null || !InFront(hunter, attacker.Position))
                return;

            var reflected = taken * ReflectPercent / 100;

            if (reflected <= 0)
                return;

            var dealt = ActorManager.Instance.Damage(mapChannel, attacker, reflected, hunter, out var outcome, damageType);

            CellManager.Instance.CellCallMethod(mapChannel, hunter, new ShieldDroneReflectPacket(
                shield.EffectId, attacker.EntityId, damageType, outcome.Delivered,
                dealt > 0 && attacker.Attributes[Attributes.Health].Current <= 0, outcome.Absorbed, outcome.Immune));
        }
    }
}

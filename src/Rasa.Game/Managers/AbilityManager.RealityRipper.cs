using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// Reality Ripper (AA_DEMOLITIONIST_REALITY_RIPPER 301, abilities.realityripper): a rift thrown
    /// at a spot (TARGET_LOCATION, 20 m) that "sucks them all into a nice little package"
    /// (uielement 5890). From the client and its data:
    /// - the rift is a creature of class Ability_Reality_Ripper (10997) carrying
    ///   REALITY_RIPPER_SOURCE (203, FX ABILITY_REALITY_RIPPER_EFFECT_P01-P05 by pump), which
    ///   holds it still;
    /// - each enemy it takes carries REALITY_RIPPER_TARGET (204), a DamageOverTime whose
    ///   OnAttach(bRootTarget, bTeleportTarget, teleportDuration) flails it while it is dragged
    ///   in, stands it up after teleportDuration, holds it in place for as long as the effect
    ///   lasts, and asks the rift - the effect's source - to draw REALITY_RIPPER_TARGET_VISUAL
    ///   (205) to it;
    /// - per pump: RADIUS_AROUND_SOURCE 10-18 m, DURATION 5-25 s, 30-46 damage (exponentially
    ///   scaled) every INTERVAL of 1 s, HATE_TRANSFER_PERCENT 25.
    ///
    /// The server's part:
    /// - the rift is a FRIENDLY, scripted creature (as a crab mine is) at the spot, so hostile creatures
    ///   can attack it, and killing it ends it early;
    /// - every INTERVAL, each hostile creature within the radius not already taken is dragged to
    ///   CrowdControl.PullStopShort of the rift over TeleportMs (CrowdControl.Pull), and given 204
    ///   for what is left of the rift's time: rooted, and hurt every INTERVAL through the effect
    ///   tick, credited to the caster;
    /// - HATE_TRANSFER_PERCENT: of the hate the caster earns on a creature within the rift's
    ///   radius - its own damage ticks included - 25% goes to the rift instead
    ///   (AbilityManager.HateSinkFor, Threat);
    /// - when DURATION is up, or the rift is killed, its effects come off, the creatures are let
    ///   go, and the rift is taken away.
    ///
    /// Not in the client, so chosen: TeleportMs 1000, and the rift's health, RipperBaseHealth at
    /// level 1 scaled like its damage.
    /// </summary>
    public partial class AbilityManager
    {
        public const string RealityRipperModule = "abilities.realityripper";
        public const int RealityRipperSourceTypeId = 203;               // REALITY_RIPPER_SOURCE
        public const int RealityRipperTargetTypeId = 204;               // REALITY_RIPPER_TARGET
        public const EntityClasses RealityRipperClass = (EntityClasses)10997; // Ability_Reality_Ripper

        public const int RipperTeleportMs = 1000;
        public const int RipperBaseHealth = 100;

        private sealed class Ripper
        {
            public MapChannel MapChannel;
            public Creature Creature;
            public Manifestation Owner;
            public GameEffect Source;
            public float Radius;
            public int IntervalMs;
            public int DamageMin;
            public int DamageMax;
            public int ScaleType;
            public int HateTransferPercent;
            public uint Level;
            public long ExpiresAt;
            public long NextPullAt;
            public readonly Dictionary<Creature, GameEffect> Taken = new Dictionary<Creature, GameEffect>();
        }

        private static readonly List<Ripper> Rippers = new List<Ripper>();
        private static readonly object RippersLock = new object();

        public static bool IsRealityRipper(Creature creature)
        {
            lock (RippersLock)
                return Rippers.Any(r => r.Creature == creature);
        }

        /// <summary>The share of a piece of hate that goes to a summon with this HATE_TRANSFER_PERCENT.</summary>
        public static double HateTransferred(double hate, int percent)
        {
            return hate <= 0 || percent <= 0 ? 0 : hate * Math.Min(100, percent) / 100.0;
        }

        /// <summary>
        /// HATE_TRANSFER_PERCENT, as the share of the threat an owner generates that goes to their
        /// summon instead: the owner's summon - trap, turret, rift, reanimated creature or spotter -
        /// nearest the creature among those whose reach the creature is in (a trap's, turret's or
        /// reanimated creature's attack range, a rift's radius), and its percent. Null when there
        /// is none.
        /// </summary>
        public static (Creature Summon, int Percent) HateSinkFor(Manifestation owner, Creature victim)
        {
            if (owner == null || victim == null)
                return (null, 0);

            Creature best = null;
            var percent = 0;
            var bestDistance = float.MaxValue;

            void Consider(Creature summon, float reach, int transfer)
            {
                if (summon == null || transfer <= 0 || summon == victim || summon.State == CharacterState.Dead
                    || summon.MapContextId != victim.MapContextId)
                    return;

                var distance = System.Numerics.Vector3.Distance(summon.Position, victim.Position);

                if (distance > reach || distance >= bestDistance)
                    return;

                best = summon;
                percent = transfer;
                bestDistance = distance;
            }

            lock (TrapsLock)
                foreach (var trap in Traps)
                    if (trap.Owner == owner && trap.RemoveAt == 0)
                        Consider(trap.Creature, trap.Range, trap.HateTransferPercent);

            lock (RippersLock)
                foreach (var ripper in Rippers)
                    if (ripper.Owner == owner)
                        Consider(ripper.Creature, ripper.Radius, ripper.HateTransferPercent);

            foreach (var (risen, reach, transfer) in RisenOf(owner))
                Consider(risen, reach, transfer);

            // Spotter's MINION_HATE_FROM_MASTER_PERCENT.
            foreach (var (spotter, reach, transfer) in SpottersOf(owner))
                Consider(spotter, reach, transfer);

            return (best, percent);
        }

        /// <summary>Opens a rift at the spot the ability was thrown at.</summary>
        private void OpenRealityRipper(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var level = Math.Max(1u, (uint)player.Level);
            var health = Scale((int)level, RipperBaseHealth, 2);
            var durationMs = Math.Max(1, info.Get(AbilityProperty.Duration, 5)) * 1000L;
            var spot = NavMeshManager.SnapToGround(mapChannel, action.TargetLocation ?? player.Position);

            var rift = new Creature
            {
                EntityClass = RealityRipperClass,
                TargetCategory = TargetCategory.Friendly,
                Level = level,
                MaxHitPoints = (uint)health,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                IsScripted = true,
                MasterEntityId = player.EntityId,
                Name = "Reality Ripper"
            };

            rift.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 1, 1, 1, 0, 0));
            rift.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 1, 1, 1, 0, 0));
            rift.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 1, 1, 1, 0, 0));
            rift.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 0, 0));
            rift.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            rift.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            rift.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            rift.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0));
            rift.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            rift.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));

            CreatureManager.Instance.SetLocation(rift, spot, player.Rotation, player.MapContextId);
            CellManager.Instance.AddToWorld(mapChannel, rift);

            var now = Environment.TickCount64;
            var source = NewEffect(mapChannel, player, info, RealityRipperSourceTypeId, null);

            source.ExpiresTick = now + durationMs;
            source.AnnounceOnAttach = true;
            source.AllowDetach = false;

            GameEffectManager.Instance.Attach(mapChannel, rift, source);

            var min = info.Get(AbilityProperty.DamageAmountMin);
            var ripper = new Ripper
            {
                MapChannel = mapChannel,
                Creature = rift,
                Owner = player,
                Source = source,
                Radius = info.Get(AbilityProperty.RadiusAroundSource, info.Get(AbilityProperty.EffectRadius, 10)),
                IntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000,
                DamageMin = min,
                DamageMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                ScaleType = info.Get(AbilityProperty.DamageScaleType),
                HateTransferPercent = info.Get(AbilityProperty.HateTransferPercent),
                Level = Math.Max(1u, info.Level),
                ExpiresAt = now + durationMs,
                NextPullAt = now
            };

            lock (RippersLock)
                Rippers.Add(ripper);
        }

        /// <summary>Runs the rifts on this map: take in whoever is in reach, move the hate over, close the spent ones.</summary>
        internal void RealityRipperWorker(MapChannel mapChannel)
        {
            List<Ripper> rippers;

            lock (RippersLock)
                rippers = Rippers.Where(r => r.MapChannel == mapChannel).ToList();

            if (rippers.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var ripper in rippers)
            {
                var rift = ripper.Creature;

                if (now >= ripper.ExpiresAt || rift.State == CharacterState.Dead
                    || ripper.Owner.MapChannel != mapChannel || ripper.Owner.MapContextId != rift.MapContextId)
                {
                    CloseRipper(ripper);
                    continue;
                }

                if (now < ripper.NextPullAt)
                    continue;

                ripper.NextPullAt = now + ripper.IntervalMs;

                // Let go of the ones that died or were taken off it some other way.
                foreach (var gone in ripper.Taken.Where(t => t.Key.State == CharacterState.Dead || !t.Key.ActiveEffects.ContainsKey(t.Value.EffectId)).Select(t => t.Key).ToList())
                    ripper.Taken.Remove(gone);

                foreach (var creature in HostilesWithin(mapChannel, ripper.Owner, rift.Position, ripper.Radius))
                    if (!ripper.Taken.ContainsKey(creature) && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying)
                        Take(ripper, creature, now);
            }
        }

        /// <summary>Drags a creature in and holds it there, hurting it, for as long as the rift is open.</summary>
        private void Take(Ripper ripper, Creature creature, long now)
        {
            var mapChannel = ripper.MapChannel;
            var remaining = ripper.ExpiresAt - now;

            if (remaining <= 0)
                return;

            CrowdControl.Pull(mapChannel, creature, ripper.Creature, RipperTeleportMs);

            var held = new GameEffect
            {
                TypeId = RealityRipperTargetTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = ripper.Level,
                ActionId = ActionId.AaDemolitionistRealityRipper,
                // The client's effect asks its source for the beam to draw, so that is the rift;
                // the damage and the kill are the caster's.
                SourceId = ripper.Creature.EntityId,
                Source = ripper.Owner,
                SourceLevel = ripper.Owner.Level,
                IsBuff = false,
                IsRoot = true,
                AnnounceOnAttach = true,
                AllowDetach = false,
                ExpiresTick = now + remaining,
                TickIntervalMs = ripper.IntervalMs,
                NextTickTick = now + ripper.IntervalMs,
                TickDamageMin = ripper.DamageMin,
                TickDamageMax = ripper.DamageMax,
                TickScaleType = ripper.ScaleType,
                TickDamageType = DamageType.Physical
            };

            // RealityRipperTargetEffect.OnAttach(target, bRootTarget, bTeleportTarget, teleportDuration).
            GameEffectManager.Instance.Attach(mapChannel, creature, held, 1, 1, RipperTeleportMs);

            ripper.Taken[creature] = held;

            Threat.FromDamage(creature, ripper.Owner, 0);
        }

        /// <summary>The rift was destroyed: it closes now.</summary>
        internal void RealityRipperKilled(MapChannel mapChannel, Creature creature)
        {
            Ripper ripper;

            lock (RippersLock)
                ripper = Rippers.FirstOrDefault(r => r.Creature == creature);

            if (ripper != null)
                CloseRipper(ripper);
        }

        /// <summary>Lets everyone go, takes the rift's own effect off and the rift out of the world.</summary>
        private static void CloseRipper(Ripper ripper)
        {
            lock (RippersLock)
                if (!Rippers.Remove(ripper))
                    return;

            var mapChannel = ripper.MapChannel;
            var rift = ripper.Creature;

            foreach (var (creature, held) in ripper.Taken)
            {
                if (creature.ActiveEffects.ContainsKey(held.EffectId))
                    GameEffectManager.Instance.DettachEffect(mapChannel, creature, held);

                // Nothing left to hate there.
                creature.Hate.Remove(rift.EntityId);
            }

            if (rift.ActiveEffects.ContainsKey(ripper.Source.EffectId))
                GameEffectManager.Instance.DettachEffect(mapChannel, rift, ripper.Source);

            rift.State = CharacterState.Dead;
            rift.Attributes[Attributes.Health].Current = 0;

            CellManager.Instance.RemoveCreatureFromWorld(mapChannel, rift);
        }
    }
}

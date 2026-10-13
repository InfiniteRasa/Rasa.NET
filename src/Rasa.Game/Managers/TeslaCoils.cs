using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Bane tesla coils zap players who come near them.
    ///
    /// The client: "A TeslaCoil is an object that zaps actors within it's range of influence with a
    /// damage-over-time GameEffect" (augmentations/teslacoil.py), and that effect is
    /// TESLA_COIL_ZAP (88), a DamageOverTime (gameeffects/teslacoilzap.py TeslaCoilZapEffect,
    /// which cannot be taken off by its holder), drawn at level 1 with
    /// OBJECT_ABILITY_TESLA_COIL_EFFECT (gameeffectdata.specialFX (88, 1): 501, family
    /// weapon_bane_teslacoil_resolve.pkg) and ticked with [(targetId, rawInfo)], which it floats
    /// over the target as damage. Nothing says how near, how often or how hard.
    ///
    /// Ours, as LavaDamage is:
    ///  - a coil that is standing (WorldDestructibles for the .map's, PlacedDestructibles for one
    ///    the server put down: one shot down zaps nobody until it is back)
    ///    zaps a living player within <see cref="Radius"/> of its foot, level, and from
    ///    <see cref="Below"/> under its foot to <see cref="Above"/> over it - the coil is 14 m high;
    ///  - the zap goes on at once, the coil its source (the effect's sourceId), and the first jolt
    ///    with it; then one every <see cref="IntervalMs"/> for as long as they stay. Leaving and
    ///    coming back does not bring the next one forward;
    ///  - a jolt is rolled between <see cref="DamageMin"/> and <see cref="DamageMax"/>,
    ///    Electrical: resisted as electrical is, taken by a shield and by armour first, and it kills;
    ///  - from nobody (no actor has the kill), and not a debuff anyone put there (Environmental);
    ///  - the effect comes off <see cref="LeaveGraceMs"/> after the last time they were in reach
    ///    of a standing coil;
    ///  - players only: the coils are the Bane's.
    /// </summary>
    public static class TeslaCoils
    {
        public const int EffectTypeId = 88;         // TESLA_COIL_ZAP
        public const uint EffectLevel = 1;          // gameeffectdata.specialFX (88, 1)

        public const float Radius = 12f;
        public const float Below = 2f;
        public const float Above = 16f;

        public const long IntervalMs = 2000;
        public const int DamageMin = 150;
        public const int DamageMax = 300;
        public const long LeaveGraceMs = 1000;

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A jolt's roll, from the least to the most inclusive; a test's to replace.</summary>
        internal static Func<int, int, int> Roll { get; set; } = (min, max) => Random.Shared.Next(min, max + 1);

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Roll = (min, max) => Random.Shared.Next(min, max + 1);
        }

        /// <summary>A player being zapped on a map channel.</summary>
        private sealed class Contact
        {
            public long LastTouch { get; set; }
            public long NextJolt { get; set; }
        }

        private static readonly ConditionalWeakTable<MapChannel, Dictionary<ulong, Contact>> Contacts = new();

        /// <summary>The .map's coils of a map.</summary>
        public static IEnumerable<WorldDestructibles.Destructible> OnMap(uint mapContextId) =>
            WorldDestructibles.OnMap(mapContextId).Where(coil => coil.Usable.ClassId == MapUsables.BaneTeslaCoil);

        /// <summary>A coil that is standing on a map channel: its entity, and its foot.</summary>
        public sealed record Standing(ulong EntityId, Vector3 Foot);

        /// <summary>The coils standing on a map channel now: the .map's and those the server put down.</summary>
        public static List<Standing> StandingOn(MapChannel map)
        {
            if (map?.MapInfo == null)
                return new List<Standing>();

            return OnMap(map.MapInfo.MapContextId)
                .Where(coil => WorldDestructibles.IsUp(map, coil.EntityId))
                .Select(coil => new Standing(coil.EntityId, coil.Usable.Position))
                .Concat(PlacedDestructibles.StandingCoils(map).Select(coil => new Standing(coil.EntityId, coil.Position)))
                .ToList();
        }

        /// <summary>Whether a point is in a coil's reach.</summary>
        public static bool InReach(WorldDestructibles.Destructible coil, Vector3 position) => InReach(coil.Usable.Position, position);

        /// <summary>Whether a point is in reach of a coil standing on this foot.</summary>
        public static bool InReach(Vector3 foot, Vector3 position)
        {
            var level = new Vector2(position.X - foot.X, position.Z - foot.Z);

            return level.Length() <= Radius && position.Y >= foot.Y - Below && position.Y <= foot.Y + Above;
        }

        /// <summary>The zap on a player, if it is.</summary>
        public static GameEffect ZapOn(Manifestation player) =>
            player?.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == EffectTypeId);

        /// <summary>Everyone on the map: who is in a standing coil's reach, and who has left it.</summary>
        public static void Worker(MapChannel map)
        {
            if (map?.MapInfo == null)
                return;

            var coils = StandingOn(map);
            var contacts = Contacts.GetOrCreateValue(map);

            // Nothing standing to zap anyone, and nobody zapped to let go of.
            if (coils.Count == 0 && contacts.Count == 0)
                return;

            var now = Now();

            foreach (var client in map.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null || !ReferenceEquals(player.MapChannel, map) || client.State != ClientState.Ingame)
                    continue;

                var alive = player.State != CharacterState.Dead && player.State != CharacterState.Dying
                    && (!player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current > 0);
                var coil = alive ? coils.FirstOrDefault(c => InReach(c.Foot, player.Position)) : null;
                var zap = ZapOn(player);

                if (!contacts.TryGetValue(player.EntityId, out var contact))
                    contacts[player.EntityId] = contact = new Contact();

                if (coil != null)
                {
                    contact.LastTouch = now;
                    zap ??= Attach(map, player, coil.EntityId);

                    if (now >= contact.NextJolt)
                    {
                        contact.NextJolt = now + IntervalMs;
                        Jolt(map, player, zap);
                    }

                    continue;
                }

                if (zap != null && (!alive || now - contact.LastTouch >= LeaveGraceMs))
                    GameEffectManager.Instance.DettachEffect(map, player, zap);
            }
        }

        private static GameEffect Attach(MapChannel map, Manifestation player, ulong coilEntityId)
        {
            var effect = new GameEffect
            {
                TypeId = EffectTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(map),
                EffectLevel = EffectLevel,
                SourceId = coilEntityId,
                IsBuff = false,
                Environmental = true
            };

            GameEffectManager.Instance.Attach(map, player, effect);

            return player.ActiveEffects.ContainsKey(effect.EffectId) ? effect : null;
        }

        private static void Jolt(MapChannel map, Manifestation player, GameEffect zap)
        {
            var rolled = Math.Clamp(Roll(DamageMin, DamageMax), DamageMin, DamageMax);
            var amount = GameEffectManager.ApplyResist(player, rolled, out var resisted, DamageType.Electrical);
            var taken = ActorManager.Instance.Damage(map, player, amount, null, out var outcome, DamageType.Electrical, isPeriodic: true);

            if (zap == null)
                return;

            var tick = new GameEffectTickPacket(zap.EffectId, GameEffectTickPacket.TickKind.Damage);

            tick.Entries.Add(new TickEntry
            {
                EntityId = player.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                Resisted = resisted,
                DamageType = DamageType.Electrical,
                DeathBlow = taken > 0 && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0
            });

            CellManager.Instance.CellCallMethod(map, player, tick);
        }

        /// <summary>Forgets a map channel's (tests).</summary>
        public static void Forget(MapChannel map)
        {
            if (map != null)
                Contacts.Remove(map);
        }
    }
}

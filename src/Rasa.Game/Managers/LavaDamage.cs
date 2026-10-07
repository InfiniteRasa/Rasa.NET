using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Lava burns. The client has the effect and the numbers and nothing else:
    ///  - LAVA_DAMAGE 348, a DamageOverTime (gameeffects/damageovertime.py LavaDamage) the tray
    ///    names "Burning", drawn at level 1 (gameeffectdata.specialFX), ticked with
    ///    [(targetId, rawInfo)] which it floats on the target as damage;
    ///  - gameconstants.py LAVA_DAMAGE_INTERVAL = 3, LAVA_DAMAGE_MIN = 500, LAVA_DAMAGE_MAX = 1000.
    /// A client never says it is in lava, so the server looks. All of the game's lava is in two
    /// tables generated from the client's meshes and .map files:
    ///  - LavaSurfaces, the lakes: flat squares, a height and a footprint each;
    ///  - LavaFlows, the rest: the rivers with their slopes and falls, the cliff walls a fall runs
    ///    down, a cave room and two detention walls, as the triangles the client draws as lava.
    ///
    /// Lava is stood on, not waded in. A lava mesh has a floor in the client's collision data,
    /// 0.14 m under the lava as it is drawn - a lake's, a river's and a slope's alike - where a
    /// pond's mesh has none and the floor is the bed below the water. So a player is in lava when
    /// their feet are at its surface: over a lake's footprint or a river's triangle, no more than
    /// <see cref="Above"/> over the lava there or <see cref="Below"/> under it. A lake's plane
    /// and a river's edge run on under the ground around them, and ground higher than that is not
    /// lava. A fall's sheet cannot be stood on: it burns whoever is within <see cref="Reach"/>
    /// of it. Where a cliff wall's rock stands in front of its fall, nobody is.
    ///
    /// Ours, the client saying nothing of it:
    ///  - the first burn is at once, and then one every three seconds for as long as they stay.
    ///    Stepping out and back in does not bring the next one forward or put it off;
    ///  - a burn is rolled between the minimum and the maximum, whatever the player's level;
    ///  - it is Fire: resisted as fire is, taken by a shield and by armour before health as any
    ///    hit is (ActorManager.Damage), stopped by an immunity to fire, and it kills;
    ///  - it is from nobody. The client says "hit on you" with no source, and nobody has the kill;
    ///  - it is not a debuff anyone put there (GameEffect.Environmental): Cure neither keeps it
    ///    off nor takes it off;
    ///  - Burning stays on for <see cref="LeaveGraceMs"/> after the last time they were found in
    ///    the lava, so a jump across it does not blink the effect off and on;
    ///  - players only. A creature's way across a lake is the navmesh's, which is the lava's own
    ///    floor, and the Lavar live there.
    ///
    /// Read from the client's data. Not seen on a client: if a player turns out to sink into
    /// lava rather than stand on it, <see cref="Below"/> is the number to raise.
    /// </summary>
    public static class LavaDamage
    {
        public const int EffectTypeId = 348;        // LAVA_DAMAGE

        /// <summary>gameeffectdata.specialFX has the effect's FX at this level and no other.</summary>
        public const uint EffectLevel = 1;

        /// <summary>LAVA_DAMAGE_INTERVAL, in milliseconds.</summary>
        public const int IntervalMs = 3000;

        /// <summary>LAVA_DAMAGE_MIN and LAVA_DAMAGE_MAX.</summary>
        public const int DamageMin = 500;
        public const int DamageMax = 1000;

        /// <summary>Ours: how far over the lava's surface feet may be and still be in it. Ground a hand higher is the shore.</summary>
        public const float Above = 0.1f;

        /// <summary>Ours: how far under the lava's surface feet may be and still be in it. The floor is 0.14 m under.</summary>
        public const float Below = 0.5f;

        /// <summary>Ours: how near a fall's sheet burns, measured straight out from it. About a body's width.</summary>
        public const float Reach = 0.6f;

        /// <summary>Ours: how long Burning stays on after the last time they were found in the lava.</summary>
        public const long LeaveGraceMs = 1000;

        /// <summary>A burn's roll, from the least to the most inclusive; a test's to replace.</summary>
        internal static Func<int, int, int> Roll { get; set; } = (min, max) => Random.Shared.Next(min, max + 1);

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Puts the roll and the clock back; a test's.</summary>
        internal static void Reset()
        {
            Roll = (min, max) => Random.Shared.Next(min, max + 1);
            Now = () => Environment.TickCount64;
        }

        /// <summary>Whether a point on the map is in its lava: at the surface of a lake or a river, or against a fall.</summary>
        public static bool InLava(string mapName, Vector3 position)
        {
            if (mapName == null)
                return false;

            if (LavaSurfaces.ByMap.TryGetValue(mapName, out var surfaces))
                foreach (var surface in surfaces)
                    if (position.Y <= surface.SurfaceY + Above && position.Y >= surface.SurfaceY - Below
                        && surface.Covers(position.X, position.Z))
                        return true;

            return LavaFlowFields.Touches(mapName, position, Above, Below, Reach);
        }

        /// <summary>Burning on a player, if it is.</summary>
        public static GameEffect BurningOn(Manifestation player) =>
            player?.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == EffectTypeId);

        /// <summary>One accepted Move: where the player is now.</summary>
        public static void OnMove(Client client)
        {
            var mapChannel = client?.Player?.MapChannel;

            if (mapChannel != null && HasLava(mapChannel))
                Check(mapChannel, client, Now());
        }

        /// <summary>Everyone on the map, moving or not: who is in the lava and who has left it.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel == null || !HasLava(mapChannel))
                return;

            var now = Now();

            foreach (var client in mapChannel.ClientList.ToArray())
                if (ReferenceEquals(client?.Player?.MapChannel, mapChannel))
                    Check(mapChannel, client, now);
        }

        private static bool HasLava(MapChannel mapChannel) =>
            mapChannel.MapInfo?.MapName != null
            && (LavaSurfaces.ByMap.ContainsKey(mapChannel.MapInfo.MapName) || LavaFlowFields.Has(mapChannel.MapInfo.MapName));

        private static void Check(MapChannel mapChannel, Client client, long now)
        {
            var player = client.Player;
            var contact = player.Lava;
            var alive = player.State != CharacterState.Dead && player.State != CharacterState.Dying;
            var burning = BurningOn(player);

            if (alive && InLava(mapChannel.MapInfo.MapName, player.Position))
            {
                contact.LastTouch = now;
                burning ??= Ignite(mapChannel, player);

                if (now >= contact.NextBurn)
                {
                    contact.NextBurn = now + IntervalMs;
                    Burn(mapChannel, player, burning);
                }

                return;
            }

            if (burning != null && (!alive || now - contact.LastTouch >= LeaveGraceMs))
                GameEffectManager.Instance.DettachEffect(mapChannel, player, burning);
        }

        /// <summary>Burning goes on: the icon and the FX, with no end but leaving the lava. Null if it did not take.</summary>
        private static GameEffect Ignite(MapChannel mapChannel, Manifestation player)
        {
            var effect = new GameEffect
            {
                TypeId = EffectTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = EffectLevel,
                IsBuff = false,
                Environmental = true
            };

            GameEffectManager.Instance.Attach(mapChannel, player, effect);

            return player.ActiveEffects.ContainsKey(effect.EffectId) ? effect : null;
        }

        /// <summary>One burn: the damage, and the tick that floats it over the player for everyone who sees them.</summary>
        private static void Burn(MapChannel mapChannel, Manifestation player, GameEffect burning)
        {
            var rolled = Math.Clamp(Roll(DamageMin, DamageMax), DamageMin, DamageMax);
            var amount = GameEffectManager.ApplyResist(player, rolled, out var resisted, DamageType.Fire);
            var taken = ActorManager.Instance.Damage(mapChannel, player, amount, null, out var outcome, DamageType.Fire, isPeriodic: true);

            if (burning == null)
                return;

            // After the damage, so that a death blow is known; the effect itself went with a kill.
            var tick = new GameEffectTickPacket(burning.EffectId, GameEffectTickPacket.TickKind.Damage);

            tick.Entries.Add(new TickEntry
            {
                EntityId = player.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                Resisted = resisted,
                DamageType = DamageType.Fire,
                DeathBlow = taken > 0 && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0
            });

            CellManager.Instance.CellCallMethod(mapChannel, player, tick);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Models;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The abilities that play with what can be seen, on top of Managers.Detection.
    ///
    /// Cloak Wave (abilities.cloakwave), the Spy's signature: STEALTH_EFFECT 123, the client's
    /// StealthEffect, on the performer and their squad within RADIUS_AROUND_SOURCE (25 m) for
    /// EFFECT_DURATION_MS (60 s). While it is on, creatures cannot notice them and lose them if
    /// they were already fighting, and players outside the squad do not see them at all. Firing a
    /// weapon, launching an ability's missile or taking damage gives them away; it may also be
    /// right-clicked off (allowDetach).
    ///
    /// Tactical Evasion (abilities.tacticalevasion), the Ranger's way out, whose pumps the
    /// client's own description sorts into three shapes - "Pump 1: Clear Enemy Hate, Pump 2:
    /// -Damage Taken +Radius, Pump 3: Clear Enemy Hate, Pump 4: -Damage Taken +Radius, Pump 5:
    /// Delayed Teleport":
    ///
    ///  - P1 / P3, the mag flash (TACTICAL_EVASION_MAG_FLASH_EFFECT 10000075, a BlindEffect):
    ///    everything hostile within EFFECT_RADIUS (10 / 20 m) is blinded for EFFECT_DURATION_MS
    ///    (5 s): "Clear Enemy Hate" wipes each one's aggro table (Threat.Clear), and "blinded NPCs
    ///    lose their target and are unable to re-target for a short time" is Detection's blind,
    ///    which keeps the scan from picking anyone out until it wears off.
    ///  - P2 / P4, the smoke screen: TACTICAL_EVASION_SMOKE_SCREEN_AURA_EFFECT 10000077 on a
    ///    marker where it was thrown, and TACTICAL_EVASION_SMOKE_SCREEN_EFFECT 10000076 on the
    ///    performer and the squad standing inside it, taking EFFECT_MODIFIER (30 / 60) percent
    ///    off ranged damage landing on them. The smoke stays where it was made and is re-applied
    ///    every EFFECT_INTERVAL_MS (1 s) for as long as EFFECT_DURATION_MS (10 s), so walking out
    ///    of it loses it within the second, as "as long as the user and his squad members remain
    ///    inside the smoke screen" asks.
    ///  - P5, the delayed teleport: the spot is flagged (TACTICAL_EVASION_LOCATION_EFFECT 10000079
    ///    there) and TACTICAL_EVASION_TELEPORT_EFFECT 10000078, "Tactical Retreat", runs on the
    ///    performer for EFFECT_DURATION_MS (60 s); when it runs out they are put back on the
    ///    flagged spot - "they flag a safe location for future teleportation, they can then
    ///    assault a target, and if timed well they will escape the engagement". Right-clicking
    ///    the effect off calls the retreat off, since a detach is not an expiry.
    /// </summary>
    public partial class AbilityManager
    {
        private const int StealthTypeId = 123;                  // STEALTH_EFFECT
        private const int MagFlashTypeId = 10000075;            // TACTICAL_EVASION_MAG_FLASH_EFFECT
        private const int SmokeScreenTypeId = 10000076;         // TACTICAL_EVASION_SMOKE_SCREEN_EFFECT
        private const int SmokeAuraTypeId = 10000077;           // TACTICAL_EVASION_SMOKE_SCREEN_AURA_EFFECT
        private const int TacticalRetreatTypeId = 10000078;     // TACTICAL_EVASION_TELEPORT_EFFECT
        private const int RetreatMarkTypeId = 10000079;         // TACTICAL_EVASION_LOCATION_EFFECT

        /// <summary>Sys_GameEffect_Proxy, the empty entity an effect is put on to hold it at a spot.</summary>
        private const EntityClasses SmokeClass = (EntityClasses)10000043;

        private sealed class SmokeScreen
        {
            public MapChannel MapChannel;
            public Manifestation Player;
            public DynamicObject Marker;
            public int EffectId;
            public float Radius;
            public int Mitigation;
            public int IntervalMs;
            public long NextTick;
            public long EndsAt;
        }

        private static readonly List<SmokeScreen> SmokeScreens = new List<SmokeScreen>();
        private static readonly object SmokeScreensLock = new object();

        private void CloakWave(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, AbilityRecoveryPacket recovery)
        {
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 25);
            var durationMs = Math.Max(1000, info.Get(AbilityProperty.EffectDurationMs, 60000));

            foreach (var ally in SquadWithin(mapChannel, player, radius))
            {
                var cloak = NewEffect(mapChannel, player, info, StealthTypeId, null);

                cloak.IsBuff = true;
                cloak.AllowDetach = true;
                cloak.Hides = true;
                cloak.ExpiresTick = Environment.TickCount64 + durationMs;

                GameEffectManager.Instance.Attach(mapChannel, ally, cloak);
                Hit(recovery, ally);
            }
        }

        private void TacticalEvasion(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, AbilityRecoveryPacket recovery)
        {
            var durationMs = Math.Max(1000, info.Get(AbilityProperty.EffectDurationMs, 5000));

            // P2 / P4: the smoke screen, which has an interval to re-apply itself on.
            if (info.Has(AbilityProperty.EffectIntervalMs))
            {
                ThrowSmokeScreen(mapChannel, player, info, durationMs);
                Hit(recovery, player);
                return;
            }

            // P5: the flagged spot and the retreat to it.
            if (!info.Has(AbilityProperty.EffectRadius))
            {
                var retreat = NewEffect(mapChannel, player, info, TacticalRetreatTypeId, null);

                retreat.IsBuff = true;
                retreat.AllowDetach = true;
                retreat.ReturnTo = player.Position;
                retreat.ExpiresTick = Environment.TickCount64 + durationMs;
                retreat.OnExpired = Retreat;

                GameEffectManager.Instance.Attach(mapChannel, player, retreat);
                MarkRetreat(mapChannel, player, info);
                Hit(recovery, player);
                return;
            }

            // P1 / P3: the mag flash, which blinds what is around.
            var radius = info.Get(AbilityProperty.EffectRadius, info.Get(AbilityProperty.RadiusAroundSource, 10));

            foreach (var creature in HostilesWithin(mapChannel, player, player.Position, radius))
            {
                // "Clear Enemy Hate": it forgets everyone it was fighting...
                Threat.Clear(creature);

                // ...and the flash keeps it from picking anyone out again for a few seconds.
                var flash = NewEffect(mapChannel, player, info, MagFlashTypeId, null);

                flash.IsBuff = false;
                flash.Blinds = true;
                flash.ExpiresTick = Environment.TickCount64 + durationMs;

                GameEffectManager.Instance.Attach(mapChannel, creature, flash);
                Hit(recovery, creature);
            }

            if (recovery.Hits.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);
        }

        /// <summary>The spot a Tactical Retreat returns to, shown by a marker standing there.</summary>
        private void MarkRetreat(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var mark = new DynamicObject
            {
                EntityClassId = SmokeClass,
                Position = player.Position,
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, mark);

            var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);

            CellManager.Instance.CellCallMethod(mark, MarkerEffect(RetreatMarkTypeId, effectId, info, player));

            lock (SmokeScreensLock)
                SmokeScreens.Add(new SmokeScreen
                {
                    MapChannel = mapChannel,
                    Player = player,
                    Marker = mark,
                    EffectId = effectId,
                    Radius = 0,                 // no smoke: the marker only stands there
                    IntervalMs = 1000,
                    NextTick = long.MaxValue,
                    EndsAt = Environment.TickCount64 + Math.Max(1000, info.Get(AbilityProperty.EffectDurationMs, 60000))
                });
        }

        private void ThrowSmokeScreen(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, int durationMs)
        {
            var marker = new DynamicObject
            {
                EntityClassId = SmokeClass,
                Position = player.Position,
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, marker);

            var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);

            CellManager.Instance.CellCallMethod(marker, MarkerEffect(SmokeAuraTypeId, effectId, info, player));

            var interval = Math.Max(250, info.Get(AbilityProperty.EffectIntervalMs, 1000));

            lock (SmokeScreensLock)
                SmokeScreens.Add(new SmokeScreen
                {
                    MapChannel = mapChannel,
                    Player = player,
                    Marker = marker,
                    EffectId = effectId,
                    Radius = info.Get(AbilityProperty.EffectRadius, 5),
                    Mitigation = info.Get(AbilityProperty.EffectModifier, 30),
                    IntervalMs = interval,
                    NextTick = Environment.TickCount64,
                    EndsAt = Environment.TickCount64 + durationMs
                });
        }

        private static GameEffectAttachedPacket MarkerEffect(int typeId, int effectId, ActionLevelInfo info, Manifestation player)
        {
            return new GameEffectAttachedPacket
            {
                EffectTypeId = typeId,
                EffectId = effectId,
                EffectLevel = Math.Max(1u, info.Level),
                SourceId = player.EntityId,
                Announced = true,
                Duration = null,
                DamageType = 0,
                AttrId = 1,
                IsActive = true,
                IsBuff = true,
                IsDebuff = false,
                IsNegativeEffect = false,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            };
        }

        /// <summary>
        /// The smoke screens and retreat marks on this map: the smoke covers whoever is standing
        /// in it this second, and a marker is taken away when its time is up.
        /// </summary>
        internal void SmokeWorker(MapChannel mapChannel)
        {
            List<SmokeScreen> mine;
            var now = Environment.TickCount64;

            lock (SmokeScreensLock)
                mine = SmokeScreens.Where(s => s.MapChannel == mapChannel).ToList();

            foreach (var smoke in mine)
            {
                var gone = now >= smoke.EndsAt || smoke.Player == null || smoke.Player.MapContextId != mapChannel.MapInfo.MapContextId;

                if (!gone && smoke.Radius > 0 && now >= smoke.NextTick)
                {
                    smoke.NextTick = now + smoke.IntervalMs;

                    foreach (var ally in SquadWithin(mapChannel, smoke.Player, smoke.Radius + 50f))
                        if (Vector3.Distance(ally.Position, smoke.Marker.Position) <= smoke.Radius)
                            Cover(mapChannel, smoke, ally);
                }

                if (!gone)
                    continue;

                lock (SmokeScreensLock)
                    SmokeScreens.Remove(smoke);

                CellManager.Instance.CellCallMethod(smoke.Marker, new GameEffectDetachedPacket { EffectId = smoke.EffectId });
                CellManager.Instance.RemoveFromWorld(mapChannel, smoke.Marker);
            }
        }

        /// <summary>One second of smoke on somebody standing in it; it falls off a step after they leave.</summary>
        private static void Cover(MapChannel mapChannel, SmokeScreen smoke, Manifestation ally)
        {
            var covered = ally.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == SmokeScreenTypeId);

            if (covered != null)
            {
                covered.ExpiresTick = Environment.TickCount64 + smoke.IntervalMs * 3 / 2;
                return;
            }

            var effect = new GameEffect
            {
                TypeId = SmokeScreenTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = smoke.Player.EntityId,
                Source = smoke.Player,
                SourceLevel = smoke.Player.Level,
                IsBuff = true,
                IncomingRangedPercent = smoke.Mitigation,
                ExpiresTick = Environment.TickCount64 + smoke.IntervalMs * 3 / 2
            };

            effect.Tooltip["modAmt"] = smoke.Mitigation;        // "Incoming ranged damage reduced by %(modAmt)s%%"

            GameEffectManager.Instance.Attach(mapChannel, ally, effect);
        }

        /// <summary>The Tactical Retreat's time is up: the Ranger is put back on the flagged spot.</summary>
        private static void Retreat(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (!(actor is Manifestation player) || !effect.ReturnTo.HasValue)
                return;

            var client = mapChannel.ClientList.Find(c => c?.Player == player);

            if (client == null)
                return;

            var home = effect.ReturnTo.Value;

            player.PlaceAt(home);
            client.MoveObject(player.EntityId, new Movement(home, client.Movement.ViewDirection));
        }
    }
}

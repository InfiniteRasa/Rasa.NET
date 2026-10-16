using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Mag Flash Grenade (CONSUMABLE_FLASH_BANG 498, abilities.flashbang).
    ///
    /// The client's FlashBangAbility is TARGET_LOCATION with FLASH_BANG_DELAY_EFFECT 130 as its
    /// targetGameEffect and the ParticleLocationProxy (1400) as its splatClassId, as the fireworks
    /// are: a proxy goes down at the aimed spot carrying the delay effect, and is the recovery's
    /// hit, so OnServerResolution announces it. DELAY_TIME_MS (3 s) later it goes off:
    /// FlashBangDelayEffect.OnTick(target, targetIds) announces FLASH_BANG_BLIND_EFFECT 124 on
    /// each id it is given, and FlashBangBlindEffect is a BlindEffect, which on a player's own
    /// client drops their target and blocks target lock while it lasts.
    ///
    /// So at the blast everything hostile within RADIUS_AROUND_TARGET (15 m) of the spot -
    /// VictimsWithin, so never the thrower, friendly creatures, summons on their side, or a player
    /// who is not their enemy - is given the blind effect for EFFECT_DURATION_MS (5 s), quietly,
    /// and the delay effect's tick names them, which announces it. It Blinds on the server as
    /// Tactical Evasion's mag flash does (Detection.IsBlind): a creature drops what it was
    /// fighting and notices nobody until it wears off; an enemy player's target is let go of on
    /// the server too. A RESIST_BLIND creature or a player's anti-blind module may turn it away
    /// (GameEffectManager.Attach), and an enemy player's lasts half as long, as any control does
    /// in a fight between players. A moment later the delay effect ends and the proxy is taken away.
    ///
    /// A flash whose thrower has left the map before it goes off is taken away without going
    /// off. A client that comes into view of one still waiting is shown its delay effect,
    /// announced, so the flash plays for them (ShowFlashGrenadeTo).
    /// </summary>
    public partial class AbilityManager
    {
        public const string FlashGrenadeModule = "abilities.flashbang";

        /// <summary>FLASH_BANG_DELAY_EFFECT: FlashBangAbility's targetGameEffect, on the proxy until the flash.</summary>
        public const int FlashGrenadeDelayTypeId = 130;

        /// <summary>FLASH_BANG_BLIND_EFFECT: what the flash leaves on whoever it catches.</summary>
        public const int FlashGrenadeBlindTypeId = 124;

        /// <summary>How long the proxy stays after the flash, for its FX to play.</summary>
        public const int FlashGrenadeLingerMs = 2000;

        private sealed class FlashGrenade
        {
            public MapChannel MapChannel;
            public Manifestation Player;
            public ActionLevelInfo Info;
            public DynamicObject Proxy;
            public int EffectId;
            public float Radius;
            public int BlindMs;
            public long FlashAt;
            public long RemoveAt;
        }

        private static readonly List<FlashGrenade> FlashGrenades = new List<FlashGrenade>();
        private static readonly object FlashGrenadesLock = new object();

        /// <summary>Puts the proxy down where it was aimed, the delay effect on it, and the proxy as the recovery's hit.</summary>
        private static void ThrowFlashGrenade(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action, AbilityRecoveryPacket recovery)
        {
            var aimed = action.TargetLocation ?? player.Position + FacingOf(player) * 10f;
            var proxy = new DynamicObject
            {
                EntityClassId = LocationProxyClass,
                Position = NavMeshManager.SnapToGround(mapChannel, aimed),
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, proxy);

            var flash = new FlashGrenade
            {
                MapChannel = mapChannel,
                Player = player,
                Info = info,
                Proxy = proxy,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                Radius = info.Get(AbilityProperty.RadiusAroundTarget, 15),
                BlindMs = Math.Max(0, info.Get(AbilityProperty.EffectDurationMs, 5000)),
                FlashAt = Environment.TickCount64 + Math.Max(0, info.Get(AbilityProperty.DelayTimeMs, 3000))
            };

            // Not announced here: the recovery's hit announces it; a client without the thrower
            // announces it itself.
            CellManager.Instance.CellCallMethod(proxy, FlashGrenadeAttached(flash, false));
            recovery.Hits.Add(new AbilityHit { EntityId = proxy.EntityId });

            // Listed only now: the proxy went to everyone in view as it was added (ShowFlashGrenadeTo).
            lock (FlashGrenadesLock)
                FlashGrenades.Add(flash);
        }

        private static GameEffectAttachedPacket FlashGrenadeAttached(FlashGrenade flash, bool announced)
        {
            return new GameEffectAttachedPacket
            {
                EffectTypeId = FlashGrenadeDelayTypeId,
                EffectId = flash.EffectId,
                EffectLevel = 1,
                SourceId = flash.Player.EntityId,
                Announced = announced,
                Duration = null,
                DamageType = 0,
                AttrId = 1,
                IsActive = true,
                IsBuff = false,
                IsDebuff = true,
                IsNegativeEffect = true,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            };
        }

        /// <summary>
        /// A client has just been sent this object (DynamicObjectManager.CreateDynamicObjectOnClient):
        /// if it is a flash grenade still waiting to go off, its delay effect goes with it,
        /// announced. A spent one is not shown.
        /// </summary>
        internal static void ShowFlashGrenadeTo(Client client, DynamicObject proxy)
        {
            if (client == null || proxy?.EntityClassId != LocationProxyClass)
                return;

            FlashGrenade waiting;

            lock (FlashGrenadesLock)
                waiting = FlashGrenades.FirstOrDefault(flash => flash.Proxy == proxy && flash.RemoveAt == 0);

            if (waiting != null)
                client.CallMethod(proxy.EntityId, FlashGrenadeAttached(waiting, true));
        }

        /// <summary>Runs the flash grenades on this map: the flashes that are due, and the spent ones taken away.</summary>
        internal void FlashGrenadeWorker(MapChannel mapChannel, long now = 0)
        {
            if (now == 0)
                now = Environment.TickCount64;

            List<FlashGrenade> mine;

            lock (FlashGrenadesLock)
                mine = FlashGrenades.Where(flash => flash.MapChannel == mapChannel).ToList();

            foreach (var flash in mine)
            {
                try
                {
                    if (flash.RemoveAt == 0)
                    {
                        // The thrower has gone: the grenade goes with them, unexploded.
                        if (flash.Player == null || flash.Player.MapChannel != mapChannel || flash.Player.MapContextId != mapChannel.MapInfo.MapContextId)
                            flash.RemoveAt = now;
                        else if (now >= flash.FlashAt)
                        {
                            Flash(flash);
                            flash.RemoveAt = now + FlashGrenadeLingerMs;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Mag flash grenade on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                    flash.RemoveAt = now;
                }

                if (flash.RemoveAt != 0 && now >= flash.RemoveAt)
                {
                    // Ended only now, so the flash's FX has played on it.
                    CellManager.Instance.CellCallMethod(flash.Proxy, new GameEffectDetachedPacket { EffectId = flash.EffectId });
                    CellManager.Instance.RemoveFromWorld(mapChannel, flash.Proxy);

                    lock (FlashGrenadesLock)
                        FlashGrenades.Remove(flash);
                }
            }
        }

        /// <summary>The flash: the blind on everyone hostile in the radius, and the tick that announces it on them.</summary>
        private static void Flash(FlashGrenade flash)
        {
            var mapChannel = flash.MapChannel;
            var player = flash.Player;
            var reached = new GameEffectTickPacket(flash.EffectId, GameEffectTickPacket.TickKind.EntityIds);

            foreach (var victim in VictimsWithin(mapChannel, player, flash.Proxy.Position, flash.Radius))
            {
                var blind = NewEffect(mapChannel, player, flash.Info, FlashGrenadeBlindTypeId, null);

                blind.EffectLevel = 1;
                blind.IsBuff = false;
                blind.Blinds = true;
                // From the clock, as GameEffectManager.Attach scales a player's by it.
                blind.ExpiresTick = Environment.TickCount64 + flash.BlindMs;

                GameEffectManager.Instance.Attach(mapChannel, victim, blind);

                // Immune, resisted, or under PvP Safety: nothing went on, and nothing is announced.
                if (!victim.ActiveEffects.ContainsKey(blind.EffectId))
                    continue;

                // A player's client drops its target itself (BlindEffect, without telling the
                // server); the server lets go of it too.
                if (victim is Manifestation blinded && mapChannel.ClientList.Find(c => c?.Player == blinded) is { } client)
                    ManifestationManager.Instance.SetTargetId(client, 0);

                reached.Entries.Add(new TickEntry { EntityId = victim.EntityId });
            }

            CellManager.Instance.CellCallMethod(flash.Proxy, reached);
        }
    }
}

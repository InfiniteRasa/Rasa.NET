using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The EMP Bombs, Class I to X (CONSUMABLE_BOMB 10000004, abilities.placebomb, levels 1-10).
    ///
    /// The client's PlaceBomb is TARGET_NONE and hostile, with PLACE_BOMB 10000060 as its
    /// targetGameEffect; PlaceBombEffect is a BombEffect, which plays its explosion where its
    /// holder stands and then takes the holder away (removeTarget). So the bomb is an object put
    /// down DROP_DISTANCE_CENTIMETERS (20) in front of the player - UsableAbilityPlaceBomb
    /// 10000059, the class made for it - carrying PLACE_BOMB at the bomb's level, whose specialFX
    /// is the blast. The object is the recovery's hit, so the client's OnServerResolution
    /// announces the effect on it.
    ///
    /// DELAY (5) seconds later it goes off: everything hostile within EFFECT_RADIUS (15 m) of it -
    /// VictimsWithin, so never the thrower, friendly creatures, summons on their side, or a player
    /// who is not their enemy - takes DAMAGE_AMOUNT (Class I 2000-3000 up to Class X) of
    /// DAMAGE_TYPE (EMP), the full roll at any distance, with a crit roll, through resistances and
    /// shields as any ability damage. It is CallGameEffectMethod DoExplosion(damageData) on the
    /// bomb, which plays the blast and floats every (entityId, rawInfo); the client takes the bomb
    /// away itself, and the server a moment later.
    ///
    /// A bomb whose thrower has left the map before it goes off is taken away without going off:
    /// there is nobody for its damage to be from. A client that comes into view of a bomb still
    /// waiting is shown its effect, announced, so the blast has something to play on
    /// (ShowPlacedBombTo).
    /// </summary>
    public partial class AbilityManager
    {
        public const string PlaceBombModule = "abilities.placebomb";

        /// <summary>PLACE_BOMB: PlaceBomb's targetGameEffect.</summary>
        public const int PlaceBombTypeId = 10000060;

        /// <summary>UsableAbilityPlaceBomb: the bomb on the ground.</summary>
        public const EntityClasses PlacedBombClass = (EntityClasses)10000059;

        /// <summary>How long a spent bomb stays on the server after its blast; the client has already taken it away.</summary>
        public const int PlacedBombLingerMs = 2000;

        private sealed class PlacedBomb
        {
            public MapChannel MapChannel;
            public Manifestation Player;
            public DynamicObject Bomb;
            public int EffectId;
            public uint Level;
            public float Radius;
            public int DamageMin, DamageMax, ScaleType;
            public DamageType DamageType;
            public long ExplodeAt;
            public long RemoveAt;
        }

        private static readonly List<PlacedBomb> PlacedBombs = new List<PlacedBomb>();
        private static readonly object PlacedBombsLock = new object();

        /// <summary>Puts the bomb down in front of the player, its effect on it, and the bomb as the recovery's hit.</summary>
        private static void PlaceBomb(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, AbilityRecoveryPacket recovery)
        {
            var distance = info.Get(AbilityProperty.DropDistanceCentimeters, 20) / 100f;
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var bomb = new DynamicObject
            {
                EntityClassId = PlacedBombClass,
                Position = NavMeshManager.SnapToGround(mapChannel, player.Position + FacingOf(player) * distance),
                Rotation = player.Rotation,
                MapContextId = mapChannel.MapInfo.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, bomb);

            var placed = new PlacedBomb
            {
                MapChannel = mapChannel,
                Player = player,
                Bomb = bomb,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                Level = Math.Max(1, info.Level),
                Radius = info.Get(AbilityProperty.EffectRadius, 15),
                DamageMin = min,
                DamageMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                ScaleType = info.Get(AbilityProperty.DamageScaleType),
                DamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.EMP),
                ExplodeAt = Environment.TickCount64 + Math.Max(0, info.Get(AbilityProperty.Delay, 5)) * 1000L
            };

            // Not announced here: the recovery's hit announces it, as OnServerResolution does a
            // targetGameEffect; a client without the thrower announces it itself.
            CellManager.Instance.CellCallMethod(bomb, PlacedBombAttached(placed, false));
            recovery.Hits.Add(new AbilityHit { EntityId = bomb.EntityId });

            // Listed only now: the bomb went to everyone in view as it was added (ShowPlacedBombTo).
            lock (PlacedBombsLock)
                PlacedBombs.Add(placed);
        }

        private static GameEffectAttachedPacket PlacedBombAttached(PlacedBomb placed, bool announced)
        {
            return new GameEffectAttachedPacket
            {
                EffectTypeId = PlaceBombTypeId,
                EffectId = placed.EffectId,
                // BombEffect.Recv_DoExplosion plays specialFX[(typeId, level)]: every class has one.
                EffectLevel = placed.Level,
                SourceId = placed.Player.EntityId,
                Announced = announced,
                Duration = null,
                DamageType = (int)placed.DamageType,
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
        /// if it is a bomb still waiting to go off, its effect goes with it, announced, so the
        /// blast plays for them too. A spent one is not shown.
        /// </summary>
        internal static void ShowPlacedBombTo(Client client, DynamicObject obj)
        {
            if (client == null || obj?.EntityClassId != PlacedBombClass)
                return;

            PlacedBomb waiting;

            lock (PlacedBombsLock)
                waiting = PlacedBombs.FirstOrDefault(placed => placed.Bomb == obj && placed.RemoveAt == 0);

            if (waiting != null)
                client.CallMethod(obj.EntityId, PlacedBombAttached(waiting, true));
        }

        /// <summary>Runs the bombs on this map: the blasts that are due, and the spent ones taken away.</summary>
        internal void PlacedBombWorker(MapChannel mapChannel, long now = 0)
        {
            if (now == 0)
                now = Environment.TickCount64;

            List<PlacedBomb> mine;

            lock (PlacedBombsLock)
                mine = PlacedBombs.Where(placed => placed.MapChannel == mapChannel).ToList();

            foreach (var placed in mine)
            {
                try
                {
                    if (placed.RemoveAt == 0)
                    {
                        // The thrower has gone: the bomb goes with them, unexploded.
                        if (placed.Player == null || placed.Player.MapChannel != mapChannel || placed.Player.MapContextId != mapChannel.MapInfo.MapContextId)
                        {
                            CellManager.Instance.CellCallMethod(placed.Bomb, new GameEffectDetachedPacket { EffectId = placed.EffectId });
                            placed.RemoveAt = now;
                        }
                        else if (now >= placed.ExplodeAt)
                        {
                            Explode(placed);
                            placed.RemoveAt = now + PlacedBombLingerMs;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"EMP bomb on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                    placed.RemoveAt = now;
                }

                if (placed.RemoveAt != 0 && now >= placed.RemoveAt)
                {
                    CellManager.Instance.RemoveFromWorld(mapChannel, placed.Bomb);

                    lock (PlacedBombsLock)
                        PlacedBombs.Remove(placed);
                }
            }
        }

        /// <summary>The blast: everyone hostile in the radius hit, then DoExplosion on the bomb to show it.</summary>
        private void Explode(PlacedBomb placed)
        {
            var mapChannel = placed.MapChannel;
            var player = placed.Player;
            var client = mapChannel.ClientList.Find(c => c?.Player == player);
            var blast = new GameEffectAnnounceDamagePacket(placed.EffectId, "DoExplosion");
            var critChance = CriticalHits.AttackerChance(player, false);

            foreach (var victim in VictimsWithin(mapChannel, player, placed.Bomb.Position, placed.Radius))
            {
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, BombRandom.Next(placed.DamageMin, placed.DamageMax + 1), placed.ScaleType));
                var crit = CriticalHits.Resolve(player, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, placed.DamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, player, out var outcome, placed.DamageType);

                var entry = new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = placed.DamageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                };

                blast.Hits.Add(entry);

                // A hit on a creature a mission counts.
                if (client != null && taken > 0 && victim is Creature counted && counted.DbId != 0)
                    (_missionManager ?? MissionApplication.Instance).RecordProgress(client,
                        MissionProgressEvent.AbilityHit((uint)ActionId.ConsumableBomb, counted.DbId));

                // A crit's side effect is announced by the blast (HitEffects).
                if (crit && !outcome.Immune && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    using (HitEffects.On(victim, player, entry.TargetEffectIds))
                        CritEffects.OnCritical(mapChannel, victim, player, placed.DamageType, amount);
            }

            if (client != null && blast.Hits.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);

            CellManager.Instance.CellCallMethod(placed.Bomb, blast);
        }
    }
}

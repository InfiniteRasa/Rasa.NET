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
    /// Two of the Demolitionist's explosions around the performer.
    ///
    /// Self Destruct (abilities.selfdestruct): SELF_DESTRUCT_BOMB 10000032 on the performer for
    /// DURATION (30 s); the client's SelfDestructEffect is a BombEffect, a toggle, and does not
    /// remove its holder. The tooltip: "You will detonate, doing massive damage to nearby enemies,
    /// and return to your marked location when you next take damage". So it arms where it is
    /// used, and the next damage its holder takes - the damage still lands - sets it off: every
    /// hostile within EFFECT_RADIUS (10 → 30 m) of where the holder now stands takes DAMAGE_AMOUNT
    /// (226-300 → 376-450) scaled to their level, physical, with a crit roll, shown by
    /// DoExplosion on the holder; then the holder is put back where they armed it. It does not
    /// kill the holder. Unused, it runs out with nothing happening; pressed again, it is disarmed.
    /// SELF_DESTRUCT_LOCATION 10000031, a plain effect, would follow its holder like any other, so
    /// it is not sent.
    ///
    /// Scatterbombs (abilities.scatterbombs): "5 bombs located in a circle RADIUS m around the
    /// user, each affecting EFFECT_RADIUS m" - five points RADIUS_AROUND_SOURCE (5 m) from the
    /// performer, the first straight ahead, and every hostile within EFFECT_RADIUS (7 m) of a
    /// point takes DAMAGE_AMOUNT (100-175, scaled) of the pump's DAMAGE_TYPE (physical, ice,
    /// laser, electrical, virulent); one standing where the circles cross is hit by each. Each
    /// hit rolls to crit with PER_PUMP_MOD (15) percent added for every pump of the skill owned,
    /// "+15% chance to crit per pump". The client's ScatterBombsBombEffect,
    /// SCATTER_BOMB_EXPLOSION 355, is a BombEffect, and a BombEffect plays its explosion on
    /// whoever holds it, so the bombs are five dynamic objects (Sys_GameEffect_Proxy 10000043,
    /// the class made to hold an effect at a spot) put down on the circle, one DoExplosion each,
    /// and taken away a couple of seconds later. Its removeTarget of 0 says the same: the client
    /// leaves the bombs standing for the server to clear, as it does the Fire Support beacon.
    /// </summary>
    public partial class AbilityManager
    {
        private const int SelfDestructBombTypeId = 10000032;    // SELF_DESTRUCT_BOMB
        private const int ScatterBombTypeId = 355;              // SCATTER_BOMB_EXPLOSION

        /// <summary>skilldata T3_DEMOLITIONIST_SCATTERBOMBS: pumps owned add crit chance.</summary>
        private const int ScatterbombsSkillId = 79;

        /// <summary>Sys_GameEffect_Proxy, the empty entity an effect is put on to play it at a spot.</summary>
        private const EntityClasses ScatterbombClass = (EntityClasses)10000043;

        /// <summary>How long a spent bomb stays, so its blast has something to play on.</summary>
        private const int ScatterbombLingerMs = 2000;

        public const int ScatterbombCount = 5;

        private sealed class SpentBomb
        {
            public MapChannel MapChannel;
            public DynamicObject Proxy;
            public int EffectId;
            public long RemoveAt;
        }

        private static readonly List<SpentBomb> SpentBombs = new List<SpentBomb>();
        private static readonly object SpentBombsLock = new object();

        private void ArmSelfDestruct(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var bomb = NewEffect(mapChannel, player, info, SelfDestructBombTypeId, info.Get(AbilityProperty.Duration, 30));

            bomb.IsBuff = true;
            bomb.AllowDetach = true;
            bomb.TickDamageMin = info.Get(AbilityProperty.DamageAmountMin);
            bomb.TickDamageMax = Math.Max(bomb.TickDamageMin, info.Get(AbilityProperty.DamageAmountMax, bomb.TickDamageMin));
            bomb.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            bomb.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            bomb.TickRadius = info.Get(AbilityProperty.EffectRadius, 10);
            bomb.ReturnTo = player.Position;

            GameEffectManager.Instance.Attach(mapChannel, player, bomb);
        }

        /// <summary>
        /// Called when a player has taken damage, with what the hit took off armour and health: an
        /// armed Self Destruct goes off, and Conversion heals the squad. Its marked spot
        /// is cleared before its blast is dealt, so nothing the blast sets off can set it off again.
        /// </summary>
        internal static void OnPlayerDamaged(MapChannel mapChannel, Manifestation player, int damage = 0)
        {
            if (player == null || player.State == CharacterState.Dead)
                return;

            // Conversion turns what the hit took into healing for the squad.
            ConvertDamage(mapChannel, player, damage);

            // Being hit gives a cloaked player away, and lets go of their machines an enemy hacked.
            if (damage > 0)
            {
                Stealth.Break(mapChannel, player);
                ReleaseHackedPets(mapChannel, player);
            }

            // An enemy's Called Shot aim lands on the first hit that does damage, as on a creature.
            if (damage > 0)
                foreach (var armed in player.ActiveEffects.Values.Where(e => e.OnDamaged != null).ToList())
                {
                    var land = armed.OnDamaged;

                    armed.OnDamaged = null;
                    GameEffectManager.Instance.DettachEffect(mapChannel, player, armed);
                    land(mapChannel, player, armed);
                }

            // A Thrax's Explosive Nanites go off on the damage its holder takes.
            if (damage > 0)
                OnPlayerNanites(mapChannel, player);

            var bomb = player.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == SelfDestructBombTypeId && e.ReturnTo.HasValue && !e.IsExpired);

            if (bomb == null)
                return;

            var home = bomb.ReturnTo.Value;
            bomb.ReturnTo = null;

            var blast = new GameEffectAnnounceDamagePacket(bomb.EffectId, "DoExplosion");
            var critChance = CriticalHits.AttackerChance(player, false);
            var victims = VictimsWithin(mapChannel, player, player.Position, bomb.TickRadius);

            foreach (var victim in victims)
            {
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(bomb.SourceLevel, BombRandom.Next(bomb.TickDamageMin, bomb.TickDamageMax + 1), bomb.TickScaleType));
                var crit = CriticalHits.Resolve(player, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, bomb.TickDamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, player, out var outcome, bomb.TickDamageType);

                var entry = new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = bomb.TickDamageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                };

                blast.Hits.Add(entry);

                // A crit's side effect is announced by the blast (HitEffects), so it goes on
                // before the blast goes out.
                if (crit && !outcome.Immune && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    using (HitEffects.On(victim, player, entry.TargetEffectIds))
                        CritEffects.OnCritical(mapChannel, victim, player, bomb.TickDamageType, amount);
            }

            // The blast plays where the holder stands, before they are sent home.
            CellManager.Instance.CellCallMethod(mapChannel, player, blast);
            GameEffectManager.Instance.DettachEffect(mapChannel, player, bomb);

            var client = mapChannel.ClientList.FirstOrDefault(c => c.Player == player);

            if (client == null)
                return;

            if (victims.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);

            player.PlaceAt(home);
            client.MoveObject(player.EntityId, new Movement(home, client.Movement.ViewDirection));
        }

        /// <summary>The five bomb points: radius metres out from the centre, evenly round, the first along the facing.</summary>
        public static List<Vector3> ScatterbombPoints(Vector3 centre, Vector3 facing, float radius)
        {
            var points = new List<Vector3>();
            var start = Math.Atan2(facing.X, facing.Z);

            if (facing.X == 0 && facing.Z == 0)
                start = 0;

            for (var i = 0; i < ScatterbombCount; i++)
            {
                var angle = start + i * 2 * Math.PI / ScatterbombCount;

                points.Add(new Vector3(centre.X + radius * (float)Math.Sin(angle), centre.Y, centre.Z + radius * (float)Math.Cos(angle)));
            }

            return points;
        }

        private void DropScatterbombs(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info)
        {
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var scaleType = info.Get(AbilityProperty.DamageScaleType);
            var ring = info.Get(AbilityProperty.RadiusAroundSource, 5);
            var reach = info.Get(AbilityProperty.EffectRadius, 7);
            var pumps = Math.Max((int)info.Level, ManifestationManager.SkillPump(player, ScatterbombsSkillId));
            var critChance = CriticalHits.AttackerChance(player, false, info.Get(AbilityProperty.PerPumpMod, 15) * pumps);
            var now = Environment.TickCount64;
            var hitAny = false;

            foreach (var point in ScatterbombPoints(player.Position, FacingOf(player), ring))
            {
                var bomb = new DynamicObject
                {
                    EntityClassId = ScatterbombClass,
                    Position = NavMeshManager.SnapToGround(mapChannel, point),
                    MapContextId = mapChannel.MapInfo.MapContextId,
                    IsEnabled = false
                };

                CellManager.Instance.AddToWorld(mapChannel, bomb);

                var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);

                CellManager.Instance.CellCallMethod(bomb, new GameEffectAttachedPacket
                {
                    EffectTypeId = ScatterBombTypeId,
                    EffectId = effectId,
                    EffectLevel = Math.Max(1u, info.Level),
                    SourceId = player.EntityId,
                    Announced = true,
                    Duration = null,
                    DamageType = (int)damageType,
                    AttrId = 1,
                    IsActive = true,
                    IsBuff = false,
                    IsDebuff = true,
                    IsNegativeEffect = true,
                    Extras = new Dictionary<string, object>(),
                    Args = new List<object>()
                });

                var blast = new GameEffectAnnounceDamagePacket(effectId, "DoExplosion");

                foreach (var victim in VictimsWithin(mapChannel, player, bomb.Position, reach))
                {
                    if (victim.State == CharacterState.Dead || victim.State == CharacterState.Dying || victim.Attributes[Attributes.Health].Current <= 0)
                        continue;

                    var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, BombRandom.Next(min, max + 1), scaleType));
                    var crit = CriticalHits.Resolve(player, victim, false, critChance, ref rolled);
                    var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, damageType);
                    var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, player, out var outcome, damageType);

                    var entry = new TickEntry
                    {
                        EntityId = victim.EntityId,
                        Amount = outcome.Delivered,
                        Absorbed = outcome.Absorbed,
                        WasImmune = outcome.Immune,
                        Resisted = resisted,
                        DamageType = damageType,
                        IsCritical = crit,
                        DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                    };

                    blast.Hits.Add(entry);
                    hitAny = true;

                    // A crit's side effect is announced by the blast (HitEffects).
                    if (crit && !outcome.Immune && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                        using (HitEffects.On(victim, player, entry.TargetEffectIds))
                            CritEffects.OnCritical(mapChannel, victim, player, damageType, amount);
                }

                CellManager.Instance.CellCallMethod(bomb, blast);

                lock (SpentBombsLock)
                    SpentBombs.Add(new SpentBomb { MapChannel = mapChannel, Proxy = bomb, EffectId = effectId, RemoveAt = now + ScatterbombLingerMs });
            }

            if (hitAny && client != null)
                ManifestationManager.Instance.EnterCombat(client);
        }

        /// <summary>Takes spent scatterbombs on this map away once their blasts have played.</summary>
        internal void ScatterbombWorker(MapChannel mapChannel)
        {
            List<SpentBomb> done;
            var now = Environment.TickCount64;

            lock (SpentBombsLock)
            {
                done = SpentBombs.Where(b => b.MapChannel == mapChannel && now >= b.RemoveAt).ToList();

                foreach (var bomb in done)
                    SpentBombs.Remove(bomb);
            }

            foreach (var bomb in done)
            {
                CellManager.Instance.CellCallMethod(bomb.Proxy, new GameEffectDetachedPacket { EffectId = bomb.EffectId });
                CellManager.Instance.RemoveFromWorld(mapChannel, bomb.Proxy);
            }
        }
    }
}

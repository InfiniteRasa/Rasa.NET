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
    /// Spotter (AA_RANGER_SPOTTER 389, abilities.spotter): "Summons a helper to assist the user in
    /// combat for a set time ... Only 1 spotter can be active at a time. Can be targeted and
    /// destroyed. Different types of Spotters are created with each pump level" - Pistoleer,
    /// Rifleman, Technician, Grenadier, Chaingunner (uielement ID_ABILITY_SPOTTER_*).
    ///
    /// From the client and its data:
    /// - SpotterAction is TARGET_NONE, delayResolution; the spotter carries SPOTTER_MINION (442),
    ///   whose OnAnnounceAttach makes the client count the spotter's damage as its source's (the
    ///   master's) - AddExtraDamageSource, as Create Clone and Bot Construction do - and ends with
    ///   SPOTTER_DESPAWN (448). SPOTTER_MASTER (446) and SPOTTER_PERCEPTION (447) are in
    ///   gameeffectdata but their classes are not in the client's spotter module, so they are not
    ///   sent;
    /// - per pump: CREATURE_VARIANT_ID 591 / 592 / 590 / 589 / 588, CREATURE_LIFETIME_MS 900000
    ///   (15 min), CREATURE_LEVEL_DIFFERENCE 0, MINION_HATE_TO_MASTER_PERCENT 50,
    ///   MINION_HATE_FROM_MASTER_PERCENT 50; creature names 7071-7075 are the five types by name;
    /// - the help's "Command System": Create Clone, Spotter and Bot Construction make the
    ///   subordinates a player commands.
    ///
    /// The variant table is not in anything we have, so each type is the client's own AFS soldier
    /// class for it with the soldier weapon it is named for (<see cref="SpotterVariants"/>):
    /// Redshirt_Human_T3_* classes with Weapon_Human_Redshirt_* weapons.
    ///
    /// The server's part:
    /// - the spotter is a FRIENDLY creature of the player's level (plus CREATURE_LEVEL_DIFFERENCE)
    ///   beside the player, with the player's maximum health and armour; its one attack is its
    ///   weapon's, the weapon's level-50 damage scaled to that level as Polymorph scales a creature
    ///   weapon, at the weapon action's range and refire;
    /// - it is the player's commandable minion (MinionManager.Adopt): it follows them, takes the
    ///   minion commands, and assists them - it fights what they fight; its kills are the
    ///   player's;
    /// - MINION_HATE_TO_MASTER_PERCENT: of the hate the spotter earns, that share goes to the
    ///   player instead (Creature.HateToMasterPercent, Threat); MINION_HATE_FROM_MASTER_PERCENT: of
    ///   the hate the player earns on a creature within the spotter's attack range, that share
    ///   goes to the spotter (HateSinkFor);
    /// - after CREATURE_LIFETIME_MS, or when the player casts another, SPOTTER_DESPAWN plays and it
    ///   is taken away SpotterDespawnMs later. Killed, it is an ordinary corpse.
    ///
    /// Not done: the stealth detection range on the radar ("+15m enemy detection per pump", PvP).
    /// </summary>
    public partial class AbilityManager
    {
        public const string SpotterModule = "abilities.spotter";
        public const int SpotterMinionTypeId = 442;              // SPOTTER_MINION
        public const int SpotterDespawnTypeId = 448;             // SPOTTER_DESPAWN

        /// <summary>How long the despawn plays before the spotter is taken away.</summary>
        public const int SpotterDespawnMs = 2000;

        /// <summary>The least time between a spotter's shots, whatever its weapon's action says.</summary>
        public const int SpotterMinRefireMs = 500;

        public sealed class SpotterVariant
        {
            public string Name;
            public uint NameId;
            public uint CreatureClassId;
            public uint WeaponClassId;
        }

        /// <summary>CREATURE_VARIANT_ID → the soldier that answers it.</summary>
        public static readonly Dictionary<int, SpotterVariant> SpotterVariants = new Dictionary<int, SpotterVariant>
        {
            // Pump 1: Pistoleer - Redshirt_Human_T3_Pistol_Male, Weapon_Human_Redshirt_Pistol_Physical (1/133).
            [591] = new SpotterVariant { Name = "Pistoleer", NameId = 7071, CreatureClassId = 21900, WeaponClassId = 6271 },
            // Pump 2: Rifleman - Redshirt_Human_T3_Rifle_Male, Weapon_Human_Redshirt_Rifle_Physical (1/134).
            [592] = new SpotterVariant { Name = "Rifleman", NameId = 7072, CreatureClassId = 21904, WeaponClassId = 4095 },
            // Pump 3: Technician - Redshirt_Human_T3_InjectionGun_Male, Weapon_Human_Redshirt_InjectionGun_Virulent (399/1).
            [590] = new SpotterVariant { Name = "Technician", NameId = 7073, CreatureClassId = 21920, WeaponClassId = 10636 },
            // Pump 4: Grenadier - Redshirt_Human_T3_RocketLauncher_Male, Weapon_Human_Redshirt_GrenadeLauncher_Physical (141/6).
            [589] = new SpotterVariant { Name = "Grenadier", NameId = 7074, CreatureClassId = 21916, WeaponClassId = 20526 },
            // Pump 5: Chaingunner - Redshirt_Human_T3_MachineGun_Male, Weapon_Human_Redshirt_MachineGun_Physical (149/1).
            [588] = new SpotterVariant { Name = "Chaingunner", NameId = 7075, CreatureClassId = 21914, WeaponClassId = 20535 },
        };

        /// <summary>
        /// A summoned minion - a spotter, or a Bot Construction bot, which works the same way
        /// (AbilityManager.BotConstruction). Module is which ability made it: one of each at a time.
        /// </summary>
        private sealed class Spotter
        {
            public string Module;
            public MapChannel MapChannel;
            public Creature Creature;
            public Manifestation Owner;
            public GameEffect Effect;
            public int DespawnTypeId;
            public int HateFromMasterPercent;
            public long RemoveAt;

            /// <summary>A Repair or Multi Bot: percent of max armour it restores every BotRepairIntervalMs; 0 for none.</summary>
            public int RepairPercent;
            public long NextRepairAt;
        }

        /// <summary>What a summon ability makes: the creature, its attacks, and its minion / despawn effects.</summary>
        private sealed class MinionSpec
        {
            public string Module;
            public string Name;
            public uint NameId;
            public uint CreatureClassId;
            public uint AppearanceWeaponClassId;
            public List<WeaponClassInfo> Weapons = new List<WeaponClassInfo>();
            public int MinionTypeId;
            public int DespawnTypeId;
            public int RepairPercent;

            /// <summary>Last touches on the creature once it is built at its level (Create Clone's looks and weapon); null for none.</summary>
            public Action<Creature> Dress;
        }

        private static readonly List<Spotter> Spotters = new List<Spotter>();
        private static readonly object SpottersLock = new object();

        public static bool IsSpotter(Creature creature)
        {
            lock (SpottersLock)
                return Spotters.Any(s => s.Creature == creature);
        }

        /// <summary>A creature's attack from its weapon: the weapon's damage at this level, the action's range and refire.</summary>
        public static CreatureAction WeaponAttackFor(WeaponClassInfo weapon, int level)
        {
            TryGetActionLevel(weapon.WeaponAttackActionId, weapon.WeaponAttackArgId, out var action);

            var min = ScaleToLevel(Math.Max(1, weapon.MinDamage), level);
            var max = Math.Max(min, ScaleToLevel(Math.Max(1, weapon.MaxDamage), level));

            return new CreatureAction
            {
                Description = "spotter weapon",
                ActionId = weapon.WeaponAttackActionId,
                ActionArgId = weapon.WeaponAttackArgId,
                RangeMin = 0,
                RangeMax = action != null && action.MaxRange > 0 ? action.MaxRange : 20,
                Cooldown = Math.Max(SpotterMinRefireMs, action != null ? action.RecoveryMs + action.ReuseMs : 1000),
                MinDamage = (uint)min,
                MaxDamage = (uint)max,
                DamageType = weapon.DamageType
            };
        }

        private static bool TryGetActionLevel(ActionId actionId, uint level, out ActionLevelInfo info)
        {
            info = null;
            return Instance != null && Instance.TryGetLevel(actionId, level, out info);
        }

        /// <summary>A weapon class's WeaponClassInfo, or null when the server has none.</summary>
        private static WeaponClassInfo WeaponInfoOf(uint weaponClassId)
        {
            return EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)weaponClassId, out var weaponClass)
                ? weaponClass.WeaponClassInfo
                : null;
        }

        /// <summary>Summons the pump's spotter beside the player, sending away the one they had.</summary>
        private void SummonSpotter(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var variantId = info.Get(AbilityProperty.CreatureVariantId);
            var weapon = SpotterVariants.TryGetValue(variantId, out var variant) ? WeaponInfoOf(variant.WeaponClassId) : null;

            if (weapon == null)
            {
                Logger.WriteLog(LogType.Error, $"Spotter level {info.Level}: creature variant {variantId} is not known; nothing summoned.");
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            var spec = new MinionSpec
            {
                Module = SpotterModule,
                Name = variant.Name,
                NameId = variant.NameId,
                CreatureClassId = variant.CreatureClassId,
                AppearanceWeaponClassId = variant.WeaponClassId,
                MinionTypeId = SpotterMinionTypeId,
                DespawnTypeId = SpotterDespawnTypeId
            };

            spec.Weapons.Add(weapon);

            SummonMinion(mapChannel, client, player, info, action, spec);
        }

        /// <summary>
        /// Makes the spec's creature the player's minion beside them, sending away the one this
        /// ability made before: "Only 1 spotter" / "Only 1 bot can be active at a time."
        /// </summary>
        private void SummonMinion(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action, MinionSpec spec)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            List<Spotter> old;

            lock (SpottersLock)
                old = Spotters.Where(s => s.Owner == player && s.Module == spec.Module && s.RemoveAt == 0).ToList();

            foreach (var previous in old)
                SendSpotterAway(previous);

            var level = (int)ReanimatedLevel(player.Level, info.Get(AbilityProperty.CreatureLevelDifference));
            var health = player.Attributes.TryGetValue(Attributes.Health, out var playerHealth) ? Math.Max(1, playerHealth.CurrentMax) : 100;
            var armor = player.Attributes.TryGetValue(Attributes.Armor, out var playerArmor) ? Math.Max(0, playerArmor.CurrentMax) : 0;
            var spot = NavMeshManager.SnapToGround(mapChannel, player.Position - FacingOf(player) * 2f);

            var spotter = new Creature
            {
                EntityClass = (EntityClasses)spec.CreatureClassId,
                NameId = spec.NameId,
                TargetCategory = TargetCategory.Friendly,
                Level = (uint)level,
                MaxHitPoints = (uint)health,
                RunSpeed = 9f,
                WalkSpeed = 5f,
                AggroRange = 20f,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                Name = spec.Name,
                HateToMasterPercent = info.Get(AbilityProperty.MinionHateToMasterPercent)
            };

            if (spec.AppearanceWeaponClassId != 0)
                spotter.AppearanceData[EquipmentData.Weapon] = new AppearanceData { SlotId = EquipmentData.Weapon, Class = spec.AppearanceWeaponClassId, Color = Color.RandomColor(), Hue2 = Color.RandomColor() };

            foreach (var weapon in spec.Weapons)
                spotter.Actions.Add(WeaponAttackFor(weapon, level));

            spec.Dress?.Invoke(spotter);

            spotter.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 1, 1, 1, 0, 0));
            spotter.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 1, 1, 1, 0, 0));
            spotter.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 1, 1, 1, 0, 0));
            spotter.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 5, 1000));
            spotter.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            spotter.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            spotter.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            spotter.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, armor, armor, armor, 5, 1000));
            spotter.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            spotter.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));

            CreatureManager.Instance.SetLocation(spotter, spot, player.Rotation, player.MapContextId);
            CellManager.Instance.AddToWorld(mapChannel, spotter);

            // The player's to command; it follows them and fights what they fight.
            MinionManager.Instance.Adopt(client, spotter);

            // SPOTTER_MINION / BOT_CONSTRUCTION_MINION: the client counts its damage as the player's.
            var lifetimeMs = Math.Max(1000, info.Get(AbilityProperty.CreatureLifetimeMs, 900000));
            var effect = NewEffect(mapChannel, player, info, spec.MinionTypeId, null);

            effect.ExpiresTick = Environment.TickCount64 + lifetimeMs;
            effect.AnnounceOnAttach = true;
            effect.AnnounceToNewcomers = false;     // its start FX is the summon's arrival
            effect.AllowDetach = false;
            effect.OnExpired = (map, actor, e) => EndSpotter(spotter);

            GameEffectManager.Instance.Attach(mapChannel, spotter, effect);

            lock (SpottersLock)
                Spotters.Add(new Spotter
                {
                    Module = spec.Module,
                    MapChannel = mapChannel,
                    Creature = spotter,
                    Owner = player,
                    Effect = effect,
                    DespawnTypeId = spec.DespawnTypeId,
                    HateFromMasterPercent = info.Get(AbilityProperty.MinionHateFromMasterPercent),
                    RepairPercent = spec.RepairPercent,
                    NextRepairAt = Environment.TickCount64 + BotRepairIntervalMs
                });

            Hit(recovery, spotter);
            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        private static void EndSpotter(Creature creature)
        {
            Spotter spotter;

            lock (SpottersLock)
                spotter = Spotters.FirstOrDefault(s => s.Creature == creature);

            if (spotter != null)
                SendSpotterAway(spotter);
        }

        /// <summary>SPOTTER_DESPAWN plays, it stops what it was doing, and it is taken away shortly.</summary>
        private static void SendSpotterAway(Spotter spotter)
        {
            if (spotter.RemoveAt != 0)
                return;

            var creature = spotter.Creature;
            var mapChannel = spotter.MapChannel;

            spotter.RemoveAt = Environment.TickCount64 + SpotterDespawnMs;

            if (creature.State == CharacterState.Dead)
                return;

            creature.Stance = MinionStance.Passive;
            creature.Hate.Clear();
            BehaviorManager.Instance.StopFighting(creature);

            var despawn = new GameEffect
            {
                TypeId = spotter.DespawnTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = spotter.Effect.EffectLevel,
                SourceId = spotter.Owner.EntityId,
                IsBuff = true
            };

            CellManager.Instance.CellCallMethod(mapChannel, creature, GameEffectManager.AttachedPacket(despawn, true));
        }

        /// <summary>Runs the spotters on this map: take away the spent, let go of the fallen and the dismissed.</summary>
        internal void SpotterWorker(MapChannel mapChannel)
        {
            List<Spotter> here;

            lock (SpottersLock)
                here = Spotters.Where(s => s.MapChannel == mapChannel).ToList();

            if (here.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var spotter in here)
            {
                var creature = spotter.Creature;

                // Dismissed already (its master left, or a GM): nothing left to run.
                if (!EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) || registered != creature)
                {
                    lock (SpottersLock)
                        Spotters.Remove(spotter);

                    continue;
                }

                // Killed: an ordinary corpse now, no longer anyone's to command.
                if (creature.State == CharacterState.Dead)
                {
                    lock (SpottersLock)
                        Spotters.Remove(spotter);

                    MinionManager.Instance.Release(creature);
                    continue;
                }

                if (spotter.RemoveAt != 0 && now >= spotter.RemoveAt)
                {
                    lock (SpottersLock)
                        Spotters.Remove(spotter);

                    MinionManager.Instance.Dismiss(mapChannel, creature);
                    continue;
                }

                // A Repair or Multi Bot tends its owner's and their squad's armour.
                if (spotter.RemoveAt == 0 && spotter.RepairPercent > 0 && now >= spotter.NextRepairAt)
                {
                    spotter.NextRepairAt = now + BotRepairIntervalMs;
                    RepairAround(mapChannel, spotter);
                }
            }
        }

        /// <summary>The owner's live spotters, with the reach they draw hate over and their MINION_HATE_FROM_MASTER_PERCENT.</summary>
        private static List<(Creature Creature, float Reach, int Percent)> SpottersOf(Manifestation owner)
        {
            lock (SpottersLock)
                return Spotters
                    .Where(s => s.Owner == owner && s.RemoveAt == 0 && s.Creature.State != CharacterState.Dead)
                    .Select(s => (s.Creature, Math.Max(ReanimatedMinReach, s.Creature.Actions.Count == 0 ? 0f : (float)s.Creature.Actions.Max(a => a.RangeMax)), s.HateFromMasterPercent))
                    .ToList();
        }
    }
}

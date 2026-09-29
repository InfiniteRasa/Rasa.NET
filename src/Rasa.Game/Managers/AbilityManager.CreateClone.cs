using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Create Clone (AA_EXOBIOLOGIST_CREATE_CLONE 253, abilities.createclone): "Creates a clone of
    /// the user to assist in combat for a set time. Only 1 clone can be active at a time. Can be
    /// targeted and destroyed. Pump 2-5: +Duration, +Clone Level" - Doppelganger, Caricature, Body
    /// Double, Stand-In, Dead Ringer.
    ///
    /// From the client and its data - the same shape as Spotter and Bot Construction:
    /// - CreateCloneAction is TARGET_NONE, delayResolution; the clone carries CREATE_CLONE_MINION
    ///   (453), whose OnAnnounceAttach counts its damage as its master's, and ends with
    ///   CREATE_CLONE_DESPAWN (454). CREATE_CLONE_MASTER (452) has no class in the client's module,
    ///   so it is not sent;
    /// - per pump: CREATURE_LIFETIME_MS 300000 / 420000 / 540000 / 720000 / 900000 (5-15 min),
    ///   CREATURE_LEVEL_DIFFERENCE -5 .. -1, MINION_HATE_TO/FROM_MASTER_PERCENT 50; no variant -
    ///   the clone is the caster;
    /// - creature name 9082 is "Clone of %s", and a creature's name puts its actor name
    ///   (Recv_ActorName) into the %s: sent the player's name, it reads "Clone of Atomsk".
    ///
    /// The clone is a FRIENDLY creature on the player's body model (NPC_Human_Swapset_Male_Boss /
    /// Female_Boss, human NPC classes with no vendor augmentation), with the player's scale and appearance - every slot, the
    /// weapon too - at the player's level plus CREATURE_LEVEL_DIFFERENCE, with the player's maximum health and armour. Its attack is the
    /// player's equipped weapon: the weapon's damage (a player weapon's own, not a level-50
    /// creature figure) scaled down by the levels it is below the player as creature weapons scale
    /// (2^(difference / 8)), at the weapon action's range and the weapon's refire. With no weapon
    /// equipped it has no attack and stays out of fights. Everything else - minion commands,
    /// assisting the owner, the hate shares, one at a time, lifetime and despawn - is SummonMinion.
    /// </summary>
    public partial class AbilityManager
    {
        public const string CreateCloneModule = "abilities.createclone";
        public const int CloneMinionTypeId = 453;                // CREATE_CLONE_MINION
        public const int CloneDespawnTypeId = 454;               // CREATE_CLONE_DESPAWN
        public const uint CloneNameId = 9082;                    // "Clone of %s"

        /// <summary>
        /// NPC_Human_Swapset_Male_Boss / _Female_Boss: creatures on the player's own body models
        /// (4257 / 4254) that wear appearance data, as the human NPCs do (HumanBaseMale / Female
        /// are Manifestation classes, not creatures). Augmentations CREATURE, NPC and HARVESTABLE
        /// only: the plain NPC_Human_Swapset_Male / _Female (3846 / 3848) also carry VENDOR, which
        /// a clone is not. Same creature flags as those (BIOLOGICAL, SPECIES_HUMAN), so no boss
        /// indicator - that comes from the flags, not the class name. None of the three puts a use
        /// action on a living friendly creature: NPC only offers Converse with a conversation
        /// status, which a clone is never sent.
        /// </summary>
        public const uint CloneMaleClassId = 10610;
        public const uint CloneFemaleClassId = 10609;

        /// <summary>A player weapon's damage at a level below the player's, as creature weapons scale: 2^(difference / 8).</summary>
        public static int CloneDamage(int damage, int playerLevel, int cloneLevel)
        {
            if (damage <= 0)
                return 0;

            return Math.Max(1, (int)Math.Round(damage * Math.Pow(2.0, (cloneLevel - playerLevel) / 8.0)));
        }

        /// <summary>The clone's attack with the player's weapon.</summary>
        public static CreatureAction CloneAttackFor(WeaponClassInfo weapon, int refireMs, int playerLevel, int cloneLevel)
        {
            TryGetActionLevel(weapon.WeaponAttackActionId, weapon.WeaponAttackArgId, out var action);

            var min = CloneDamage(Math.Max(1, weapon.MinDamage), playerLevel, cloneLevel);
            var max = Math.Max(min, CloneDamage(Math.Max(1, weapon.MaxDamage), playerLevel, cloneLevel));

            return new CreatureAction
            {
                Description = "clone weapon",
                ActionId = weapon.WeaponAttackActionId,
                ActionArgId = weapon.WeaponAttackArgId,
                RangeMin = 0,
                RangeMax = action != null && action.MaxRange > 0 ? action.MaxRange : 20,
                Cooldown = Math.Max(SpotterMinRefireMs, refireMs > 0 ? refireMs : action != null ? action.RecoveryMs + action.ReuseMs : 1000),
                MinDamage = (uint)min,
                MaxDamage = (uint)max,
                DamageType = weapon.DamageType
            };
        }

        /// <summary>A copy of the player's appearance, slot by slot, so the clone's never changes with theirs.</summary>
        public static Dictionary<EquipmentData, AppearanceData> CopyAppearance(Dictionary<EquipmentData, AppearanceData> appearance)
        {
            var copy = new Dictionary<EquipmentData, AppearanceData>();

            if (appearance == null)
                return copy;

            foreach (var (slot, data) in appearance)
                if (data != null)
                    copy[slot] = new AppearanceData { SlotId = data.SlotId, Class = data.Class, Color = data.Color, Hue2 = data.Hue2 };

            return copy;
        }

        /// <summary>Makes the player's clone beside them, sending away the clone they had.</summary>
        private void CreateClone(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var weaponItem = InventoryManager.Instance.CurrentWeapon(client);
            var weapon = EntityClassManager.Instance.GetWeaponClassInfo(weaponItem);
            var refire = (int)(weaponItem?.ItemTemplate?.WeaponInfo?.Refire ?? 0);

            var spec = new MinionSpec
            {
                Module = CreateCloneModule,
                Name = $"Clone of {player.Name}",
                NameId = CloneNameId,
                CreatureClassId = player.Gender == 0 ? CloneMaleClassId : CloneFemaleClassId,
                MinionTypeId = CloneMinionTypeId,
                DespawnTypeId = CloneDespawnTypeId,
                Dress = clone =>
                {
                    clone.AppearanceData = CopyAppearance(player.AppearanceData);
                    clone.Scale = player.Scale > 0 ? player.Scale : 1.0d;
                    clone.ActorName = player.Name;

                    if (weapon != null)
                        clone.Actions.Add(CloneAttackFor(weapon, refire, player.Level, (int)clone.Level));
                }
            };

            SummonMinion(mapChannel, client, player, info, action, spec);
        }
    }
}

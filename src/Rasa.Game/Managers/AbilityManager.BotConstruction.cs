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
    /// Bot Construction (AA_ENGINEER_BOT_CONSTRUCTION 262, abilities.botconstruction): "Creates a
    /// bot to assist the user in combat for a set time. Only 1 bot can be active at a time. Can be
    /// targeted and destroyed. Different types of bots are created with each pump level. Pump 1:
    /// Flame Bot, Pump 2: Rocket Bot, Pump 3: Repair Bot, Pump 4: Shield Bot, Pump 5: Multi Bot."
    ///
    /// From the client and its data - the same shape as Spotter:
    /// - BotConstructionAction is TARGET_NONE, delayResolution; the bot carries
    ///   BOT_CONSTRUCTION_MINION (450), whose OnAnnounceAttach counts its damage as its master's,
    ///   and ends with BOT_CONSTRUCTION_DESPAWN (451). BOT_CONSTRUCTION_MASTER (449) has no class
    ///   in the client's module, so it is not sent;
    /// - per pump: CREATURE_VARIANT_ID 1689 / 1690 / 1692 / 1691 / 1883, CREATURE_LIFETIME_MS
    ///   900000 (15 min), CREATURE_LEVEL_DIFFERENCE 0 (P5: CREATURE_LEVEL 0),
    ///   MINION_HATE_TO_MASTER_PERCENT 50, MINION_HATE_FROM_MASTER_PERCENT 50;
    /// - the bodies are Ability_NeoBot_Flame / Rocket / Shield / Repair (25539-25542) and their
    ///   weapons Weapon_Creature_NeoBot_Flame (25546, WEAPON_ATTACK_NEOBOT_FLAME 1/261, fire),
    ///   _Missile (25545, 141/11, physical) and _Shield_Beam (25544, WEAPON_ATTACK_NEOBOT_BEAM
    ///   1/255, laser), their level-50 damage in the weapon class table;
    /// - "Multi Bot" is creature name 9633 and has no class of its own.
    ///
    /// The variant table is not in anything we have, so (<see cref="BotVariants"/>):
    /// - Flame, Rocket and Shield Bot fire their own weapon, scaled to the bot's level as the
    ///   spotter's is;
    /// - the Repair Bot has no weapon: it does not fight (a minion with no attack takes no part,
    ///   BehaviorManager.SetActionFighting) and restores BotRepairPercent of the maximum armour of
    ///   its owner and their squad within BotRepairRadius of it every BotRepairIntervalMs;
    /// - the Multi Bot is the Rocket Bot's body named Multi Bot, with all three weapons and the
    ///   Repair Bot's repairs.
    /// Everything else - level, health and armour from the player, the minion commands, assisting
    /// the owner, the hate shares, one at a time, the lifetime and despawn - is Spotter's
    /// (SummonMinion). Spotter and bot are counted apart: an engineer's bot does not send away a
    /// spotter.
    /// </summary>
    public partial class AbilityManager
    {
        public const string BotConstructionModule = "abilities.botconstruction";
        public const int BotMinionTypeId = 450;                  // BOT_CONSTRUCTION_MINION
        public const int BotDespawnTypeId = 451;                 // BOT_CONSTRUCTION_DESPAWN

        public const uint FlameBotWeaponClassId = 25546;         // Weapon_Creature_NeoBot_Flame
        public const uint RocketBotWeaponClassId = 25545;        // Weapon_Creature_NeoBot_Missile
        public const uint ShieldBotWeaponClassId = 25544;        // Weapon_Creature_NeoBot_Shield_Beam

        /// <summary>Chosen: the data has no repair numbers.</summary>
        public const int BotRepairPercent = 5;
        public const int BotRepairIntervalMs = 3000;
        public const float BotRepairRadius = 20f;

        public sealed class BotVariant
        {
            public string Name;
            public uint NameId;
            public uint CreatureClassId;
            public uint[] WeaponClassIds;
            public int RepairPercent;
        }

        /// <summary>CREATURE_VARIANT_ID → the bot that answers it.</summary>
        public static readonly Dictionary<int, BotVariant> BotVariants = new Dictionary<int, BotVariant>
        {
            // NameId 0: the client falls back to the class's own name - "Flame Bot" and so on.
            [1689] = new BotVariant { Name = "Flame Bot", CreatureClassId = 25539, WeaponClassIds = new[] { FlameBotWeaponClassId } },
            [1690] = new BotVariant { Name = "Rocket Bot", CreatureClassId = 25540, WeaponClassIds = new[] { RocketBotWeaponClassId } },
            [1692] = new BotVariant { Name = "Repair Bot", CreatureClassId = 25542, WeaponClassIds = new uint[0], RepairPercent = BotRepairPercent },
            [1691] = new BotVariant { Name = "Shield Bot", CreatureClassId = 25541, WeaponClassIds = new[] { ShieldBotWeaponClassId } },
            [1883] = new BotVariant { Name = "Multi Bot", NameId = 9633, CreatureClassId = 25540,
                                      WeaponClassIds = new[] { RocketBotWeaponClassId, ShieldBotWeaponClassId, FlameBotWeaponClassId }, RepairPercent = BotRepairPercent },
        };

        /// <summary>Builds the pump's bot beside the player, sending away the bot they had.</summary>
        private void ConstructBot(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var variantId = info.Get(AbilityProperty.CreatureVariantId);
            var weapons = new List<WeaponClassInfo>();

            if (BotVariants.TryGetValue(variantId, out var variant))
                foreach (var weaponClassId in variant.WeaponClassIds)
                {
                    var weapon = WeaponInfoOf(weaponClassId);

                    if (weapon != null)
                        weapons.Add(weapon);
                }

            if (variant == null || weapons.Count != variant.WeaponClassIds.Length)
            {
                Logger.WriteLog(LogType.Error, $"Bot Construction level {info.Level}: creature variant {variantId} or its weapons are not known; nothing built.");
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            var spec = new MinionSpec
            {
                Module = BotConstructionModule,
                Name = variant.Name,
                NameId = variant.NameId,
                CreatureClassId = variant.CreatureClassId,
                AppearanceWeaponClassId = variant.WeaponClassIds.Length > 0 ? variant.WeaponClassIds[0] : 0,
                MinionTypeId = BotMinionTypeId,
                DespawnTypeId = BotDespawnTypeId,
                RepairPercent = variant.RepairPercent
            };

            spec.Weapons.AddRange(weapons);

            SummonMinion(mapChannel, client, player, info, action, spec);
        }

        /// <summary>What a repair restores: percent of the maximum, never past it.</summary>
        public static int BotRepairAmount(int current, int max, int percent)
        {
            if (max <= 0 || current >= max || percent <= 0)
                return 0;

            return Math.Min(max - current, Math.Max(1, max * percent / 100));
        }

        /// <summary>A Repair or Multi Bot's repairs: its owner and their squad within BotRepairRadius of it.</summary>
        private static void RepairAround(MapChannel mapChannel, Spotter bot)
        {
            var owner = bot.Owner;

            if (owner == null || owner.MapContextId != bot.Creature.MapContextId)
                return;

            foreach (var member in SquadWithin(mapChannel, owner, 2 * BotRepairRadius + 100f))
            {
                if (member.State == CharacterState.Dead || Vector3.Distance(member.Position, bot.Creature.Position) > BotRepairRadius)
                    continue;

                if (!member.Attributes.TryGetValue(Attributes.Armor, out var armor))
                    continue;

                var amount = BotRepairAmount(armor.Current, armor.CurrentMax, bot.RepairPercent);

                if (amount <= 0)
                    continue;

                armor.Current += amount;
                CellManager.Instance.CellCallMethod(mapChannel, member, new UpdateArmorPacket(armor, bot.Creature.EntityId));
            }
        }
    }
}

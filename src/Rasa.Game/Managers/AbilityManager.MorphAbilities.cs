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
    /// Polymorph's combat actions: "The user will obtain all combat actions of an equal level
    /// enemy". Each pump's tooltip (uielement 2487-2491) names the weapon and the abilities, and
    /// each ability is a creature action whose player-facing level - the one with an icon, a name,
    /// a description and a power cost in the client's abilitydata - is 5 (the turret's is 3):
    ///
    /// - P1 Thrax Soldier: Kick, CR_THRAX_KICK 397/5 - abilities.knockback, 75-125 physical,
    ///   3 m, knockback 10 m. A direct damage ability like Force Blast.
    /// - P2 Thrax Technician: Mini Turret, CR_TECHNICIAN_TURRET 276/3 - TechnicianTurretAbility,
    ///   "Deploy a standalone automated turret that shoots enemies ... Turret Damage 21 ...
    ///   Duration: 30 seconds": the Turret ability's machine (PlaceTurret) at the pump's damage
    ///   for 30 s; and Repair, CR_TECHNICIAN_HEAL 275/5 - TechnicianHealAbility, "Repair the body
    ///   armor of an ally or the health and shields of a mechanical unit", HEAL_AMOUNT 200-300
    ///   within RADIUS_AROUND_SOURCE 12 m: the owner's and squad's armour, and friendly mechanical
    ///   creatures' health, announced as (healAmount, repairAmount) hits. (Jumpstart, 400/5, is a
    ///   revive and not given.)
    /// - P3 Bane Caretaker: Revitalize, CR_CARETAKER_HEAL 239/5 - CaretakerHealAbility, "Repair
    ///   damage to your Health and that of all nearby squadmates", 150-200 within 12 m,
    ///   announced as bare heal amounts; and Noxious Burst, CR_CARETAKER_ATTACK 395/5 -
    ///   CaretakerAttackAbility, 75-113 virulent at one enemy within 40 m (the tooltip does not
    ///   list it, but it is the caretaker's attack with the same player-facing level 5).
    ///   (Resuscitate, 242/5, is a revive and not given.)
    /// - P4 Kael: Smash, CR_KAEL_SMASH 433/5 - 188-250 physical to every enemy within 10 m; and
    ///   Ground Pound, CR_KAEL_GROUND_POUND 171/5 - 125-188 within 5 m and a 10 m knockback.
    /// - P5 Hominis Machina: Self Revive only, which waits on death; the drawer is empty.
    ///
    /// The attacks go through ResolveDirectDamage (DirectDamageModules), their hits written as
    /// the bare rawInfo the creature abilities' DoAbility reads (RawInfoModules); Noxious Burst's
    /// data has no DAMAGE_TYPE, so it is virulent as its description says (DefaultDamageTypeOf).
    /// The drawer is sent in the morph effect's abilityInfo, and a morphed player may perform
    /// exactly those (IsMorphAbility) - their skills stay locked.
    /// </summary>
    public partial class AbilityManager
    {
        public const string KaelSmashModule = "abilities.ai.kaelsmashability";
        public const string KaelGroundPoundModule = "abilities.ai.kaelgroundpoundability";
        public const string CaretakerAttackModule = "abilities.ai.caretakerattackability";
        public const string CaretakerHealModule = "abilities.ai.caretakerhealability";
        public const string TechnicianHealModule = "abilities.ai.technicianhealability";
        public const string TechnicianTurretModule = "abilities.ai.technicianturretability";

        /// <summary>The Mini Turret's "Duration: 30 seconds"; its data has no DURATION.</summary>
        public const int MiniTurretSeconds = 30;

        /// <summary>Direct damage whose client class reads each hit as the rawInfo itself.</summary>
        private static readonly HashSet<string> RawInfoModules = new HashSet<string> { KaelSmashModule, KaelGroundPoundModule, CaretakerAttackModule };

        /// <summary>The morph abilities that are not direct damage: the heals, the repair and the turret.</summary>
        private static readonly HashSet<string> MorphSupportModules = new HashSet<string> { CaretakerHealModule, TechnicianHealModule, TechnicianTurretModule };

        /// <summary>A direct damage module's type when its data gives none: Noxious Burst is virulent, the rest physical.</summary>
        public static DamageType DefaultDamageTypeOf(string module)
        {
            return module == CaretakerAttackModule ? DamageType.Virulent : DamageType.Physical;
        }

        /// <summary>Whether this is one of the combat actions the player's current morph gave them.</summary>
        public static bool IsMorphAbility(Manifestation player, ActionId actionId, uint level)
        {
            return player?.MorphWeapon != null && player.MorphAbilities.Any(a => a.ActionId == actionId && a.Level == level);
        }

        private void ResolveMorphSupport(MapChannel mapChannel, Client client, Manifestation player, ActionInfo actionInfo, ActionLevelInfo info, ActionData action)
        {
            switch (actionInfo.Module)
            {
                case TechnicianTurretModule:
                    PlaceTurret(mapChannel, player, info, action, false, MiniTurretSeconds);
                    CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                    return;

                case CaretakerHealModule:
                    CellManager.Instance.CellCallMethod(mapChannel, player, Revitalize(mapChannel, player, info, action));
                    return;

                case TechnicianHealModule:
                    CellManager.Instance.CellCallMethod(mapChannel, player, MechanicalRepair(mapChannel, player, info, action));
                    return;
            }
        }

        /// <summary>A heal's roll: HEAL_AMOUNT_MIN..MAX scaled to the performer's level as the ability's damage would be.</summary>
        private int RollHeal(Manifestation player, ActionLevelInfo info)
        {
            var min = info.Get(AbilityProperty.HealAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.HealAmountMax, min));

            return Math.Max(0, Scale(player.Level, _random.Next(min, max + 1), info.Get(AbilityProperty.DamageScaleType)));
        }

        /// <summary>Revitalize: health for the performer and their squad within RADIUS_AROUND_SOURCE.</summary>
        private AbilityRecoveryPacket Revitalize(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Heal);

            foreach (var ally in SquadWithin(mapChannel, player, info.Get(AbilityProperty.RadiusAroundSource, 12)))
            {
                var healed = ActorManager.Instance.Heal(ally, RollHeal(player, info), player.EntityId);

                recovery.Hits.Add(new AbilityHit { EntityId = ally.EntityId, Amount = healed });
            }

            return recovery;
        }

        /// <summary>
        /// Repair: the armour of the performer and their squad within RADIUS_AROUND_SOURCE, and
        /// the health of friendly mechanical creatures there - bots, turrets, a hacked machine.
        /// </summary>
        private AbilityRecoveryPacket MechanicalRepair(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.HealRepair);
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 12);

            foreach (var ally in SquadWithin(mapChannel, player, radius))
            {
                var repaired = ActorManager.Instance.RestoreArmor(ally, RollHeal(player, info), player.EntityId);

                recovery.Hits.Add(new AbilityHit { EntityId = ally.EntityId, Repair = repaired });
            }

            foreach (var machine in FriendlyMachinesWithin(mapChannel, player, radius))
            {
                var healed = ActorManager.Instance.Heal(machine, RollHeal(player, info), player.EntityId);

                recovery.Hits.Add(new AbilityHit { EntityId = machine.EntityId, Amount = healed });
            }

            return recovery;
        }

        /// <summary>Living FRIENDLY creatures that are MECHANICAL or MACHINA within radius of the player.</summary>
        private static List<Creature> FriendlyMachinesWithin(MapChannel mapChannel, Manifestation player, float radius)
        {
            var found = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var creature in cell.CreatureList)
                {
                    if (creature.TargetCategory != TargetCategory.Friendly || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                        continue;

                    if (Vector3.Distance(creature.Position, player.Position) > radius)
                        continue;

                    var flags = CreatureManager.CreatureFlagsOf(creature);

                    if (flags.Contains((int)CreatureFlag.Mechanical) || flags.Contains((int)CreatureFlag.Machina))
                        found.Add(creature);
                }

            return found;
        }
    }
}

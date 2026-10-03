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
    ///   creatures' health, announced as (healAmount, repairAmount) hits; and Jumpstart,
    ///   CR_TECHNICIAN_REVIVE 400/5 - TechnicianReviveAbility, the tooltip's "Revive (Machine
    ///   Only)": "Perform emergency repairs and restart a downed vehicle, turret, or other
    ///   mechanical ally. Targeting: Corpse - Friendly", within 5 m.
    /// - P3 Bane Caretaker: Revitalize, CR_CARETAKER_HEAL 239/5 - CaretakerHealAbility, "Repair
    ///   damage to your Health and that of all nearby squadmates", 150-200 within 12 m,
    ///   announced as bare heal amounts; and Noxious Burst, CR_CARETAKER_ATTACK 395/5 -
    ///   CaretakerAttackAbility, 75-113 virulent at one enemy within 40 m (the tooltip does not
    ///   list it, but it is the caretaker's attack with the same player-facing level 5); and
    ///   Resuscitate, CR_CARETAKER_REVIVE 242/5 - CaretakerReviveAbility, the tooltip's "Revive
    ///   (Biological Only)": "Restore a recently dead ally to life, repairing a portion of its
    ///   wounds. Targeting: Corpse - Friendly", within 40 m.
    /// - P4 Kael: Smash, CR_KAEL_SMASH 433/5 - 188-250 physical to every enemy within 10 m; and
    ///   Ground Pound, CR_KAEL_GROUND_POUND 171/5 - 125-188 within 5 m and a 10 m knockback.
    /// - P5 Hominis Machina: Self Revive only, POLY_SELF_RES 417/1 - SelfResAction, canDoWhileDead,
    ///   "Self Revive (useable once)": a Machina who dies keeps the morph, and with it the button,
    ///   and may get up where they fell (PlayerDeath.SelfRevive) once.
    ///
    /// The attacks go through ResolveDirectDamage (DirectDamageModules), their hits written as
    /// the bare rawInfo the creature abilities' DoAbility reads (RawInfoModules); Noxious Burst's
    /// data has no DAMAGE_TYPE, so it is virulent as its description says (DefaultDamageTypeOf).
    /// The drawer is sent in the morph effect's abilityInfo, and a morphed player may perform
    /// exactly those (IsMorphAbility) - their skills stay locked.
    ///
    /// The two revives (MorphRevive) are the creatures' own (CreatureSupport) aimed by a player,
    /// whose friends are not the Bane's. Both classes are TARGET_FRIENDLY and canTargetDead, so
    /// the client asks with a dead friend targeted; who that may be is IsMorphRevivable:
    ///
    /// - Resuscitate, on a dead player who is no enemy of the performer: CaretakerReviveAbility
    ///   keeps AnnounceRevive from a target that IsPlayer(), so the class was written with
    ///   players among its targets. They are offered the revive, as a Biotechnician's Cure
    ///   offers one (PlayerDeath.OfferRevive), and come back on accepting it with HEAL_AMOUNT
    ///   75-150 at the performer's level; the recovery names them with no healing, which draws
    ///   nothing until they do.
    /// - Either, on a dead FRIENDLY creature the world put there - a soldier, an AFS turret's
    ///   wreck - dead less than CreatureSupport.ReviveWindowMs ("recently dead"): Resuscitate the
    ///   ones that are not machines, Jumpstart the MECHANICAL and MACHINA ones. It gets up where
    ///   it lay with the HEAL_AMOUNT rolled (CreatureSupport.Revive), announced as a bare heal
    ///   amount as the creatures' revives are.
    /// - Not a summon that was killed - a bot, a clone, a turret. It is nobody's once it is dead
    ///   and has no place in the world to go back to; what put it there can put another.
    /// </summary>
    public partial class AbilityManager
    {
        public const string KaelSmashModule = "abilities.ai.kaelsmashability";
        public const string KaelGroundPoundModule = "abilities.ai.kaelgroundpoundability";
        public const string CaretakerAttackModule = "abilities.ai.caretakerattackability";
        public const string CaretakerHealModule = "abilities.ai.caretakerhealability";
        public const string TechnicianHealModule = "abilities.ai.technicianhealability";
        public const string TechnicianTurretModule = "abilities.ai.technicianturretability";
        public const string CaretakerReviveModule = "abilities.ai.caretakerreviveability";
        public const string TechnicianReviveModule = "abilities.ai.technicianreviveability";

        /// <summary>The Mini Turret's "Duration: 30 seconds"; its data has no DURATION.</summary>
        public const int MiniTurretSeconds = 30;

        /// <summary>Direct damage whose client class reads each hit as the rawInfo itself.</summary>
        private static readonly HashSet<string> RawInfoModules = new HashSet<string> { KaelSmashModule, KaelGroundPoundModule, CaretakerAttackModule };

        /// <summary>The morph abilities that are not direct damage: the heals, the repair, the turret and the revives.</summary>
        private static readonly HashSet<string> MorphSupportModules = new HashSet<string>
        {
            CaretakerHealModule, TechnicianHealModule, TechnicianTurretModule, CaretakerReviveModule, TechnicianReviveModule
        };

        /// <summary>Of those, the ones aimed at a dead friend: Resuscitate and Jumpstart.</summary>
        private static readonly HashSet<string> MorphReviveModules = new HashSet<string> { CaretakerReviveModule, TechnicianReviveModule };

        /// <summary>A direct damage module's type when its data gives none: Noxious Burst is virulent, the rest physical.</summary>
        public static DamageType DefaultDamageTypeOf(string module)
        {
            return module == CaretakerAttackModule ? DamageType.Virulent : DamageType.Physical;
        }

        public const string SelfResModule = "abilities.selfres";

        /// <summary>Whether the player's morph still has its Self Revive to use (Hominis Machina).</summary>
        public static bool CanSelfRevive(Manifestation player) => IsMorphAbility(player, ActionId.PolySelfRes, 1);

        /// <summary>The Self Revive is spent: "useable once".</summary>
        public static void SpendSelfRevive(Manifestation player)
        {
            player.MorphAbilities = player.MorphAbilities.Where(a => a.ActionId != ActionId.PolySelfRes).ToList();
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

                case CaretakerReviveModule:
                case TechnicianReviveModule:
                    CellManager.Instance.CellCallMethod(mapChannel, player, MorphRevive(mapChannel, player, actionInfo.Module, info, action));
                    return;
            }
        }

        /// <summary>
        /// Whether a morphed player's revive may be aimed at this target: a dead player who is
        /// no enemy of theirs and whom they may help (Resuscitate alone - a player is no
        /// machine), or a dead friendly creature of the kind the revive brings back
        /// (CreatureSupport.IsRevivableBy).
        /// </summary>
        internal static bool IsMorphRevivable(Manifestation player, string module, Actor target)
        {
            if (player == null || target == null || target == player || target.State != CharacterState.Dead)
                return false;

            if (target is Manifestation fallen)
                return module == CaretakerReviveModule && fallen.MapChannel != null
                       && !Pvp.AreEnemies(player, fallen) && Pvp.MayHelp(player, fallen);

            return target is Creature corpse && CreatureSupport.IsRevivableBy(player, corpse, machines: module == TechnicianReviveModule);
        }

        /// <summary>
        /// Resuscitate and Jumpstart: the dead friend targeted is brought back with HEAL_AMOUNT at
        /// the performer's level - a creature at once, a player when they accept the offer. One
        /// who got up, was taken or went away in the windup is not there to bring back: the
        /// ability is performed and nobody gets up, as it is for the creatures.
        /// </summary>
        private AbilityRecoveryPacket MorphRevive(MapChannel mapChannel, Manifestation player, string module, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Heal);
            var target = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) : null;

            if (!IsMorphRevivable(player, module, target))
                return recovery;

            var health = Math.Max(1, RollHeal(player, info));

            if (target is Manifestation fallen)
            {
                // Theirs to take or leave. With no healing on the hit the class draws nothing now;
                // Revived says it when they accept.
                if (PlayerDeath.OfferRevive(mapChannel, player, fallen, health))
                    recovery.Hits.Add(new AbilityHit { EntityId = fallen.EntityId, Amount = 0 });
            }
            else if (target is Creature corpse)
            {
                recovery.Hits.Add(new AbilityHit { EntityId = corpse.EntityId, Amount = CreatureSupport.Revive(mapChannel, corpse, health, player) });
            }

            return recovery;
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

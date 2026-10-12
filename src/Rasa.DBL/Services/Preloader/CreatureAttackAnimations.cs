using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Creature attacks the client has no animation for on the attacker's model, changed to an
    /// attack of the creature's own that it does animate (Fix_creature_attack_animations).
    ///
    /// The client plays an attack by its action and argument: actiondata.actionArguments names
    /// the windup and recovery animation families, and characteranimationmgr looks each up as
    /// animationFamilyData[(family, skeleton)], the skeleton being animationdata.meshSkeleton of
    /// the creature class's mesh. When the skeleton has no entry the creature stands still
    /// while the hit lands. A weapon attack's shot rides on that animation: the escorts aiming
    /// and dealing damage with nothing fired (Fix_bootcamp_soldier_weapon_attacks) was this.
    ///
    /// A creature's own abilities (CR_*) that its model does not animate stay: BaseActorAction
    /// attaches the ability's effect families whether or not an animation plays, and each is a
    /// mechanic the server runs (the Harvester's acid spit, the Granitour's bite, the Thraxus
    /// Machina's shrapnel, force blast and tectonic strike, the Grenadier bosses' necromite).
    /// </summary>
    public static class CreatureAttackAnimations
    {
        public const uint IdMin = 72001;
        public const uint IdMax = 72999;

        /// <summary>A creature_action row of its own for creatures that shared one with others who animate it.</summary>
        public sealed record NewAttack(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType, uint Replaced, uint[] Creatures);

        /// <summary>A creature's slot pointed at another row that already exists.</summary>
        public sealed record Repoint(uint Creature, int Slot, uint From, uint To);

        /// <summary>A row only the creatures it is wrong for use, changed where it stands.</summary>
        public sealed record Retarget(uint Id, uint FromAction, uint FromArg, uint ToAction, uint ToArg);

        /// <summary>
        /// Range, cooldown and damage are the replaced row's (row 8's for the mortar, row 2's for the
        /// rest); the Fithik's range is its bite's, 3 m.
        /// </summary>
        public static readonly NewAttack[] NewAttacks =
        {
            // Bane_Fithik_Winged: Weapon_Creature_Fithik, WEAPON_MELEE 34. Was the AFS light soldier's pistol (1/133).
            new(72001, "Bane Fithik Winged 1 weapon 174/34", 174, 34, 1, 3, 800, 0, 10, 15, 1, 2, new uint[] { 1 }),
            // Bane_Hominis_Machina: Weapon_Creature_Hominis_Machina, WEAPON_ATTACK 97. Was 1/133.
            new(72002, "Hominis Machina 8 weapon 1/97", 1, 97, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 8 }),
            // The Proving Grounds' AFS rifle soldiers hold Weapon_Avatar_Rifle_Physical_UNC_01_to_05 (27220),
            // WEAPON_ATTACK 134. 1/133 animates on them, as a pistol shot.
            new(72003, "Bootcamp AFS rifle soldier weapon 1/134", 1, 134, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 510217, 510227 }),
            // Emplacement_Bane_Turret_Standard: Weapon_Creature_Bane_Mortar_Launcher, WEAPON_GROUNDTARGET 1.
            // Was Emplacement_AFS_Turret_Mini's gun (1/242), row 8, which the AFS turret keeps.
            new(72004, "Wilderness Bane mortar weapon 411/1", 411, 1, 0, 50, 400, 400, 10, 20, 13, 8, new uint[] { 630076, 630077, 630078, 630079 }),
            // The Bot Construction minions (MinionCreaturePreloader), each its NeoBot weapon class's attack. Were 1/133.
            new(72005, "Flame Bot 600001 weapon 1/261", 1, 261, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 600001 }),    // Weapon_Creature_NeoBot_Flame
            new(72006, "Rocket Bot 600002 weapon 141/11", 141, 11, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 600002 }),  // Weapon_Creature_NeoBot_Missile
            new(72007, "Shield Bot 600003 weapon 1/255", 1, 255, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 600003 }),    // Weapon_Creature_NeoBot_Shield_Beam
            new(72008, "Repair Bot 600004 weapon 1/152", 1, 152, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 600004 }),    // no NeoBot repair weapon: the Arieki bots' gun
            // Warrior Aprika (Vendor_Forean_Gunner, skeleton 13904): the class is the goo gunner's, whose skeleton has
            // neither the AFS pistol (row 2) nor the spear she was given to hold (Weapon_Creature_NPC_Forean_Spear,
            // 174/11, family 792). She holds Weapon_Creature_NPC_Forean_GooGun (10530) instead, WEAPON_ATTACK 116.
            new(72009, "Warrior Aprika 43 weapon 1/116", 1, 116, 1, 20, 800, 0, 10, 15, 1, 2, new uint[] { 43 }),
        };

        /// <summary>
        /// The Foreans on WEAPON_ATTACK 305, the spear's ranged attack, whose recovery (785, Staff -
        /// Ranged) neither the spearman's skeleton nor the unarmed Forean's has. Row 5, "forean
        /// spearman melee", is Weapon_Creature_NPC_Forean_Spear's WEAPON_MELEE 11 with the same
        /// range, cooldown and damage as row 17. Ranger Milpas has no weapon: row 6,
        /// CR_FOREAN_LIGHTNING, which the spearmen of 50 and 139 cast already. Those two had row 5
        /// in slot 3 as well, so it moves to slot 1.
        /// </summary>
        public static readonly Repoint[] Repoints =
        {
            new(37, 1, 17, 5),          // NPC_Forean_Spearman
            new(50, 1, 17, 5),          // NPC_Forean_Spearman
            new(50, 3, 5, 0),
            new(98, 1, 17, 5),          // Ranger Anjuhi
            new(139, 1, 17, 5),         // Forean Tribal Leader Oingin
            new(139, 3, 5, 0),
            new(630001, 1, 17, 5),      // Alia escort ranger
            new(630010, 1, 17, 6),      // Ranger Milpas
        };

        /// <summary>Warrior Aprika's weapon slot (13): the spear to the goo gun her attack is.</summary>
        public const uint Aprika = 43;
        public const uint AprikaSpear = 10532;
        public const uint AprikaGooGun = 10530;

        public static readonly Retarget[] Retargets =
        {
            // Thrax Soldiers (Bane_Thrax_Soldier_Pistol_NoBeaminBirth, skeleton 13999): WEAPON_MELEE 48, the
            // Thrax melee soldier's swing (family 1155), to 46, Weapon_Creature_Thrax_Melee's (983, Melee - Pistols),
            // the Technicians' already. Each row is one region's own; 53046 is also the Simulated Thrax Rifleman's.
            new(53002, 174, 48, 174, 46), new(53013, 174, 48, 174, 46), new(53028, 174, 48, 174, 46),
            new(53038, 174, 48, 174, 46), new(53046, 174, 48, 174, 46), new(53061, 174, 48, 174, 46),
            new(53069, 174, 48, 174, 46), new(53085, 174, 48, 174, 46), new(53098, 174, 48, 174, 46),
            new(53115, 174, 48, 174, 46), new(53127, 174, 48, 174, 46), new(53138, 174, 48, 174, 46),
            new(53156, 174, 48, 174, 46),
            // Horntail (Bane_Caretaker_Boss): the Thrax pistol (1/1, family 1028) to Weapon_Holographic_Caretaker's 1/190.
            new(33, 1, 1, 1, 190),
            // Barb Ticks: Weapon_Creature_Barb_Tick's own pair, 174/17, plays the Tree Mite's swing (861), which the
            // tick's skeleton (13919) does not have; 1/153 is "Weapon - Arieki Barb Tick".
            new(55033, 174, 17, 1, 153), new(55043, 174, 17, 1, 153),
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var row in Retargets)
                migration.Sql($"update creature_action set action_id = {row.ToAction}, action_arg_id = {row.ToArg} where id = {row.Id};");

            foreach (var row in NewAttacks)
            {
                migration.Sql(
                    "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"values ({row.Id}, '{row.Description}', {row.ActionId}, {row.ArgId}, {F(row.RangeMin)}, {F(row.RangeMax)}, {row.Cooldown}, {row.Windup}, {row.MinDamage}, {row.MaxDamage}, {row.DamageType});");
                foreach (var creature in row.Creatures)
                    migration.Sql($"update creature set action1 = {row.Id} where id = {creature};");
            }

            foreach (var row in Repoints)
                migration.Sql($"update creature set action{row.Slot} = {row.To} where id = {row.Creature};");

            migration.Sql($"update creature_appearance set Class_id = {AprikaGooGun} where id = {Aprika} and slot_id = 13 and Class_id = {AprikaSpear};");
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"update creature_appearance set Class_id = {AprikaSpear} where id = {Aprika} and slot_id = 13 and Class_id = {AprikaGooGun};");

            foreach (var row in Repoints)
                migration.Sql($"update creature set action{row.Slot} = {row.From} where id = {row.Creature} and action{row.Slot} = {row.To};");

            foreach (var row in NewAttacks)
            {
                foreach (var creature in row.Creatures)
                    migration.Sql($"update creature set action1 = {row.Replaced} where id = {creature} and action1 = {row.Id};");
            }

            migration.Sql($"delete from creature_action where id between {IdMin} and {IdMax};");

            foreach (var row in Retargets)
                migration.Sql($"update creature_action set action_id = {row.FromAction}, action_arg_id = {row.FromArg} where id = {row.Id};");
        }

        private static string F(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}

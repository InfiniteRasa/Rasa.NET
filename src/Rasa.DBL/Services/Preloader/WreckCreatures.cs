using System;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The creatures that leave a wreck (the game server's AlternateMesh): the flags that say
    /// so, and creature rows for the four of the seven classes that had none. The rows of
    /// Add_wreck_creatures, which inserts them as SQL - it changes no table, so it carries no
    /// model for InsertData to take column types from.
    ///
    /// The client's creature flags name ALT_MESH (43) and ALT_MESH_DELAYED_3500 (103); it reads
    /// neither, so which class carries which was the 2009 server's to know and is reconstructed.
    /// A class is here if its model has a "_destroyed" twin in the client's string table. The
    /// five turrets, which have no death animation, are their wreck the moment they die (43);
    /// the two walkers, which have one, once it has played (103).
    ///
    /// The four new rows are at level 42, where a player's base health is 10 000, as the
    /// summoned creatures' rows are (CreatureSummonPreloader), and no spawn pool has them: they
    /// are for ".creature" and for pools placed later, which will want a row per zone as the AFS
    /// turrets have. No name id - the client names each from its class: "Bane Mortar",
    /// "Bane Light Mortar", "Brann Turret", "Ravager".
    ///
    ///  - Bane Mortar and Bane Light Mortar (Emplacement_Bane_Turret_Standard and _Mini): HOSTILE,
    ///    no speed, a lieutenant and a thug by the open-country rules (RegionCreaturePreloader) -
    ///    health 1.75x and 1.0x the base, armour (10 x level - 20) x 1.0 and 0.75, a shot 8% and
    ///    6% of the base +-20%. Both fire Weapon_Creature_Bane_Mortar_Launcher's pair, 411/1
    ///    (WEAPON_GROUNDTARGET), Physical, 0 to 60 m as the client's row reaches, once a second.
    ///  - Brann Turret (Emplacement_AFS_Turret_Brann): FRIENDLY, as the AFS Turret
    ///    (TurretCreaturePreloader) - health 3x the base, armour 10 x level - 20, a shot 8% -
    ///    with Weapon_Creature_AFS_Turret_Brann's pair, 1/252, Electrical, 0 to 60 m.
    ///  - Ravager (Vehicle_Bane_Ravager): HOSTILE, a lieutenant, a Stalker's speeds, with
    ///    Weapon_Creature_Ravager's pair, 1/299, EMP, 0 to 30 m as the client's row reaches. Its
    ///    Deathray ability is not wired.
    ///
    /// Each has its gun in the weapon slot (13), so the client has the weapon its attack fires.
    /// </summary>
    public static class WreckCreatures
    {
        public const uint BaneMortarId = 590001;
        public const uint BaneLightMortarId = 590002;
        public const uint BrannTurretId = 590003;
        public const uint RavagerId = 590004;

        public const uint FirstId = BaneMortarId;
        public const uint LastId = RavagerId;

        public const uint FirstActionId = 71001;
        public const uint LastActionId = 71004;

        public const int AltMesh = 43;
        public const int AltMeshDelayed3500 = 103;

        private static readonly string[] ClassFlagColumns = { "class_id", "flag_id" };

        private static readonly object[][] ClassFlags =
        {
            new object[] { 3902, AltMeshDelayed3500 },      // Vehicle_Bane_Predator
            new object[] { 4064, AltMesh },                 // Emplacement_AFS_Turret_Standard
            new object[] { 7482, AltMesh },                 // Emplacement_Bane_Turret_Standard
            new object[] { 10509, AltMesh },                // Emplacement_Bane_Turret_Mini
            new object[] { 11302, AltMesh },                // Emplacement_AFS_Turret_Mini
            new object[] { 23902, AltMesh },                // Emplacement_AFS_Turret_Brann
            new object[] { 30080, AltMeshDelayed3500 }      // Vehicle_Bane_Ravager
        };

        private static readonly string[] ActionColumns =
        {
            "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage", "damage_type"
        };

        private static readonly object[][] Actions =
        {
            new object[] { 71001, "Bane Mortar weapon 411/1", 411, 1, 0.0, 60.0, 1000, 0, 640, 960, 1 },
            new object[] { 71002, "Bane Light Mortar weapon 411/1", 411, 1, 0.0, 60.0, 1000, 0, 480, 720, 1 },
            new object[] { 71003, "Brann Turret weapon 1/252", 1, 252, 0.0, 60.0, 1000, 0, 640, 960, 13 },
            new object[] { 71004, "Ravager weapon 1/299", 1, 299, 0.0, 30.0, 1000, 0, 640, 960, 5 }
        };

        private static readonly string[] CreatureColumns =
        {
            "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
            "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
        };

        private static readonly object[][] Creatures =
        {
            new object[] { BaneMortarId, "Bane Mortar", 7482, 0, 42, 17500, 0, 0, 0, 71001, 0, 0, 0, 0, 0, 0, 0 },
            new object[] { BaneLightMortarId, "Bane Light Mortar", 10509, 0, 42, 10000, 0, 0, 0, 71002, 0, 0, 0, 0, 0, 0, 0 },
            new object[] { BrannTurretId, "Brann Turret", 23902, 1, 42, 30000, 0, 0, 0, 71003, 0, 0, 0, 0, 0, 0, 0 },
            new object[] { RavagerId, "Ravager", 30080, 0, 42, 17500, 0, 9, 5, 71004, 0, 0, 0, 0, 0, 0, 0 }
        };

        private static readonly string[] StatColumns = { "id", "body", "mind", "spirit", "health", "armor" };

        private static readonly object[][] Stats =
        {
            new object[] { BaneMortarId, 15, 15, 15, 17500, 400 },
            new object[] { BaneLightMortarId, 15, 15, 15, 10000, 300 },
            new object[] { BrannTurretId, 15, 15, 15, 30000, 400 },
            new object[] { RavagerId, 15, 15, 15, 17500, 400 }
        };

        private static readonly string[] AppearanceColumns = { "id", "slot_id", "Class_id", "color" };

        private static readonly object[][] Appearances =
        {
            new object[] { BaneMortarId, 13, 10604, 1 },        // Weapon_Creature_Bane_Mortar_Launcher
            new object[] { BaneLightMortarId, 13, 10604, 1 },
            new object[] { BrannTurretId, 13, 26824, 1 },       // Weapon_Creature_AFS_Turret_Brann
            new object[] { RavagerId, 13, 30324, 1 }            // Weapon_Creature_Ravager
        };

        /// <summary>The flag rows again, as a condition: what Down deletes, and nothing a later migration flags.</summary>
        public static string ClassFlagRows =>
            string.Join(" or ", ClassFlags.Select(row => $"(class_id = {Literal(row[0])} and flag_id = {Literal(row[1])})"));

        /// <summary>The inserts, in the order they are run: flags, guns, creatures, stats, weapon appearances.</summary>
        public static string[] InsertStatements => new[]
        {
            Insert(CreatureClassFlagEntry.TableName, ClassFlagColumns, ClassFlags),
            Insert(CreatureActionEntry.TableName, ActionColumns, Actions),
            Insert(CreatureEntry.TableName, CreatureColumns, Creatures),
            Insert(CreatureStatEntry.TableName, StatColumns, Stats),
            Insert(CreatureAppearanceEntry.TableName, AppearanceColumns, Appearances)
        };

        private static string Insert(string table, string[] columns, object[][] rows) =>
            $"insert into {table} ({string.Join(", ", columns)}) "
            + string.Join(" union all ", rows.Select(row => "select " + string.Join(", ", row.Select(Literal))))
            + ";";

        private static string Literal(object value) => value switch
        {
            string text => "'" + text.Replace("'", "''") + "'",
            double number => number.ToString("0.0###", CultureInfo.InvariantCulture),
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"No SQL literal for {value?.GetType().Name ?? "null"}.")
        };
    }
}

using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job W (the full-suite run of the PR copy, 2026-11-14), a delta on 20271110002190 (fix V2): three Ashen bosses get
    /// attacks their client model animates, and two 1499 scenario names fit MySQL's 64 characters.
    ///  - Wendy 790376 (1293; Creature_Granitour_Adult_Boss 25620, mesh 39866, skeleton 14024): her spit 55049 is the young
    ///    Granitour's CR_GRANITOUR_V1_SPIT 472/1, whose families (1340) skeleton 14024 has no animation for. A new row
    ///    <see cref="WendySpit"/> is the adult's CR_GRANITOUR_V2_SPIT 470/1 (windup 1332, recovery 1333, on 14024), with 55049's
    ///    numbers. Her bite 55048 (471/1, recovery 1334, on 14024) stays.
    ///  - The Atta Hive Queen 790664 (1551; Creature_Atta_Queen 24087, mesh 17409, skeleton 13916): 54014 (CR_ATTA_SOLDIER_MELEE
    ///    478/1, family 1379) and 54015 (CR_ATTA_SOLDIER_ACID_SPIT 479/1, family 1378) are Atta Soldier abilities, not on 13916.
    ///    A new row <see cref="QueenClaw"/> is the WEAPON_MELEE 174/25 claw (recovery 875, on 13916) that Queen Kardash 781024
    ///    attacks with (row 781113), with 54014's numbers. Action 2 is the rock throw 54016 (292/1, 1402/1401, on 13916), and
    ///    action 3 is 0.
    ///  - Enslaver Xahn 790666 (1501; Boss_Enslaver 11134, mesh 28518, skeleton 13989): none of Overseer Mitecht's six actions
    ///    (34 1/79, 57008, 61003, 61004, 68003, 68004: Thrax families 512, 1342-1345) are on 13989. A new row
    ///    <see cref="XahnWeapon"/> is WEAPON_ATTACK_BOSS_ENSLAVER 1/241 (recovery 1092, on 13989), the attack of the client's
    ///    Weapon_Creature_Boss_Enslaver 11135 (weaponclass 11135: 1/241), with row 34's numbers; the other slots are 0. The
    ///    Enslaver's cage (CR_ENSLAVER_CAGE 306/1) and the debuff 524/1 (CR_ENSLAVER_DEBUFF_TEST) stay out: the server has no
    ///    mechanic for the cage, and the debuff is a client test action.
    ///  - 1499's scenarios 6 and 7 (<see cref="AshenIntoTheLionsDenV1.PitSaved"/>, <see cref="AshenIntoTheLionsDenV1.CellsOpen"/>)
    ///    had names (and comments, the same text) of 90 and 73 characters; mission_scenario.name and comment are varchar(64) on
    ///    MySQL, which rejects them in set 20's migration. AshenIntoTheLionsDenV1 now writes the short names (as the Plateau's
    ///    texts were shortened in place, 9894b1a6), so a new database gets them there; this migration renames the rows of a database
    ///    that ran set 20 with the long names (the live SQLite one). Its Down leaves the short names: they are also what set 20
    ///    writes now, so a database rolled back to fix V2 holds what one migrated only to fix V2 holds.
    /// Sources: client actiondata.actionArguments (fields 1 and 3, windup and recovery families) and animationdata
    /// (meshSkeleton, animationFamilyData), decoded with tools/mission-crossref/pyo24.py, 1.0; World DB weaponclass 11135.
    /// ESTIMATE: every damage number, range, cooldown and windup below (copied from 55049, 54014 and row 34, no source for these
    /// bosses). The UPDATEs are guarded on the exact live values (2026-11-14 copy of the live World DB); Down puts the old actions
    /// back and deletes the three rows.
    /// </summary>
    public static class AshenFixWV1
    {
        public const uint Wendy = AshenDesignerHidesNpcsV1.Wendy;           // 790376
        public const uint Queen = AshenConscriptionQueenNpcsV1.Queen;       // 790664
        public const uint Xahn = AshenConscriptionQueenNpcsV1.Xahn;         // 790666

        /// <summary>The new creature_action rows, each insert guarded (CrucibleSql.Guard) so a second Up adds nothing (the Ashen block; 790243-790245 free in creature_action, 2026-11-14).</summary>
        public const uint WendySpit = 790243, QueenClaw = 790244, XahnWeapon = 790245;

        /// <summary>Wendy's actions 1-8, before and after.</summary>
        public static readonly uint[] WendyBefore = { 55048, 55049, 0, 0, 0, 0, 0, 0 };
        public static readonly uint[] WendyNow = { 55048, WendySpit, 0, 0, 0, 0, 0, 0 };

        /// <summary>The Atta Hive Queen's actions 1-8, before and after.</summary>
        public static readonly uint[] QueenBefore = { 54014, 54015, 54016, 0, 0, 0, 0, 0 };
        public static readonly uint[] QueenNow = { QueenClaw, 54016, 0, 0, 0, 0, 0, 0 };

        /// <summary>Enslaver Xahn's actions 1-8 (Overseer Mitecht's), before and after.</summary>
        public static readonly uint[] XahnBefore = { 34, 57008, 61003, 61004, 68003, 68004, 0, 0 };
        public static readonly uint[] XahnNow = { XahnWeapon, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>The new rows: id, description, action, argument, range, cooldown, windup, damage, damage type.</summary>
        public static readonly (uint Id, string Description, uint Action, uint Arg, double Min, double Max, uint Cooldown, uint Windup,
            uint DamageMin, uint DamageMax, uint Type)[] Actions =
        {
            // 55049's numbers (ESTIMATE): 1-20 m, 4000 ms, windup 466 ms, 332-550, type 2.
            (WendySpit, "Wendy Ashen Desert granitour_v2_spit 470/1", 470, 1, 1.0, 20.0, 4000, 466, 332, 550, 2),
            // 54014's numbers (ESTIMATE): 0.5-4 m, 1000 ms, no windup, 1037-2075, type 1.
            (QueenClaw, "Atta Hive Queen claw 174/25", 174, 25, 0.5, 4.0, 1000, 0, 1037, 2075, 1),
            // Row 34's numbers (ESTIMATE): 0.5-30 m, 800 ms, no windup, 15-25, type 1.
            (XahnWeapon, "Enslaver Xahn weapon 1/241", 1, 241, 0.5, 30.0, 800, 0, 15, 25, 1),
        };

        /// <summary>1499's two scenario names (and comments) before and after.</summary>
        public const string PitSavedBefore = "Humphries saved: the ledge's NPCs and switch go, the fighters still standing fall (3 done)";
        public const string PitSavedNow = "Humphries saved: ledge NPCs, switch go, the rest fall (3 done)";
        public const string CellsOpenBefore = "The cells open: Humphries goes, the fighters still standing fall (4 done)";
        public const string CellsOpenNow = "The cells open: Humphries goes, the rest fall (4 done)";

        public static IEnumerable<string> ActionSql()
        {
            foreach (var row in Actions)
                yield return Crucible.CrucibleSql.Guard(Action(row.Id, row.Description, row.Action, row.Arg, row.Min, row.Max,
                    row.Cooldown, row.Windup, row.DamageMin, row.DamageMax, row.Type));
        }

        /// <summary>The guarded UPDATEs of Up: each runs again without a change.</summary>
        public static IEnumerable<string> UpdateSql()
        {
            yield return CreatureSql(Wendy, WendyNow, WendyBefore);
            yield return CreatureSql(Queen, QueenNow, QueenBefore);
            yield return CreatureSql(Xahn, XahnNow, XahnBefore);
            yield return ScenarioSql(AshenIntoTheLionsDenV1.PitSaved, PitSavedNow, PitSavedBefore);
            yield return ScenarioSql(AshenIntoTheLionsDenV1.CellsOpen, CellsOpenNow, CellsOpenBefore);
        }

        public static IEnumerable<string> UpSql()
        {
            foreach (var sql in ActionSql())
                yield return sql;
            foreach (var sql in UpdateSql())
                yield return sql;
        }

        public static IEnumerable<string> DownSql()
        {
            yield return CreatureSql(Wendy, WendyBefore, WendyNow);
            yield return CreatureSql(Queen, QueenBefore, QueenNow);
            yield return CreatureSql(Xahn, XahnBefore, XahnNow);
            foreach (var row in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {row.Id} AND description = '{row.Description}' AND action_id = {row.Action} " +
                             $"AND action_arg_id = {row.Arg};";
        }

        public static void Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
        }

        public static void Down(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migration)
        {
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string CreatureSql(uint id, uint[] to, uint[] from)
        {
            var set = new List<string>();
            var where = new List<string>();
            for (var slot = 0; slot < 8; slot++)
            {
                set.Add($"action{slot + 1} = {to[slot]}");
                where.Add($"action{slot + 1} = {from[slot]}");
            }
            return $"UPDATE creature SET {string.Join(", ", set)} WHERE id = {id} AND {string.Join(" AND ", where)};";
        }

        private static string ScenarioSql(uint scenario, string to, string from) =>
            $"UPDATE mission_scenario SET name = '{Q(to)}', comment = '{Q(to)}' WHERE mission_id = {AshenIntoTheLionsDenV1.MissionId} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND scenario_id = {scenario} AND name = '{Q(from)}' AND comment = '{Q(from)}';";

        /// <summary>A quote doubled, as SQLite and MySQL both read it.</summary>
        private static string Q(string text) => text.Replace("'", "''");
    }
}

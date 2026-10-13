using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The family abilities that spawned creatures were missing (Family_abilities): each gets the
    /// client's CR_* actions its kin already use.
    ///
    ///  - Boargar: the stun and the knockback (182/1, 235/1) for the Young Forest Boargar (44),
    ///    the NPC-class Boargar (58) and Shahrbaraz (520044); the knockback for the Palisades
    ///    Boargar (540007), which had the stun.
    ///  - Fithik: the lunge and acid spray at argument 1 (MINION) for the Wilderness Fithik (1);
    ///    the lunge, acid spray and self-destruct at 2 (BOSS) for the Hive Monarch (89) and the
    ///    Hive Master (520038), which only bit.
    ///  - Amoeboid: the melee and the V1 vomit at 1 for the Wilderness Amoeboid (33), the vomit
    ///    for the Abyss's (540033); Splatter (520018) and Goriam (520056), which had a melee at
    ///    30-60, the melee, slime and V1 vomit at 2 (2_BOSS).
    ///  - Miasma: the gas cloud (row 56011) for the Plateau, Pools and one Howling Maw Miasma
    ///    (540015, 540004, 540034).
    ///  - Beam Manta: the lightning at 1 (SHARED_MINION) for the Plains, Mires and Thunderhead
    ///    Mantas, as their boss has it at 2.
    ///  - Shield Drone: the heal (row 45) for the ten zone drones (531xxx) and Cavalon (520055),
    ///    whose attack, at 15-20, becomes the drone attack at its level.
    ///  - Linker: the channel (row 66001) for Atropos (78).
    ///  - Caretaker: the attack at 3 (3_BOSS_SHARED) for Horntail (79), which had none, and for
    ///    Mordra (520015) and Fik (520028), which had it at 1 for 10-15.
    ///  - Thrax: the kick at 1 for the Wilderness Thrax Soldier (3) and at 3 for the Divide's
    ///    Thrax Commander (530005), as their kin have it; the Thrax Overseers' kit - force blast,
    ///    lightning, rage (row 61001), scourge, shrapnel - for Old Scratch (80) and Overseers
    ///    Glognar, Phlegg, Rankash and Graal (82, 83, 84, 90); the grenadier bosses' kit -
    ///    explosive nanites, necromite (row 68059), tectonic strike - for the Archfiend Grenadier
    ///    (75).
    ///  - Treeback: the stomp, howl and swarm for the Wilderness's non-aggressive herd (630043),
    ///    which had no attack at all. It still only fights when attacked.
    ///  - Machina: the revive (row 62059) for the Wilderness Forean Machina (630100).
    ///
    /// Each new row takes its range, reuse, windup and damage type from a row of its kin for the
    /// same action (the V1 vomit's windup is the client's 3600 ms), and damage as the region rows
    /// have it: the argument's DAMAGE_AMOUNT_MIN/MAX x 2^((level - 1) / 8) x 0.25, or x 0.5 for a
    /// boss on an argument not named for one (Shahrbaraz, Cavalon). An action with no damage
    /// (gas cloud, heal, channel, rage, necromite, revive) is the kin's own row.
    ///
    /// Guarded: a row is inserted once, and only with its creature; a slot is filled only while it is empty, and a slot is
    /// changed only from the row it had. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class FamilyAbilities
    {
        public const uint IdMin = 76001;
        public const uint IdMax = 76058;

        /// <summary>A new creature_action row, made for the creature it is given to.</summary>
        public sealed record Row(uint Creature, uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>An empty action slot (1-8) of a creature given a row.</summary>
        public sealed record Slot(uint Creature, int Index, uint Row);

        /// <summary>A slot whose row is replaced.</summary>
        public sealed record Swap(uint Creature, int Index, uint Was, uint Row);

        public static readonly Row[] Rows =
        {
            new(44, 76001, "Young Forest Boargar 44 boargar_stun", 182, 1, 0.5, 3.0, 4000, 1500, 11, 22, 7),
            new(44, 76002, "Young Forest Boargar 44 boargar_knockback", 235, 1, 0.5, 3.0, 10000, 166, 11, 22, 1),
            new(58, 76003, "Boargar (NPC class) 58 boargar_stun", 182, 1, 0.5, 3.0, 4000, 1500, 14, 29, 7),
            new(58, 76004, "Boargar (NPC class) 58 boargar_knockback", 235, 1, 0.5, 3.0, 10000, 166, 14, 29, 1),
            new(540007, 76005, "Boargar 540007 boargar_knockback", 235, 1, 0.5, 3.0, 10000, 166, 52, 106, 1),
            new(520044, 76006, "Shahrbaraz 520044 boargar_stun", 182, 1, 0.5, 3.0, 4000, 1500, 161, 327, 7),
            new(520044, 76007, "Shahrbaraz 520044 boargar_knockback", 235, 1, 0.5, 3.0, 10000, 166, 161, 327, 1),
            new(1, 76008, "Bane Fithik 1 fithik_lunge", 405, 1, 1.0, 30.0, 15000, 1200, 18, 22, 1),
            new(1, 76009, "Bane Fithik 1 fithik_acid_spray", 201, 1, 0.5, 5.0, 10000, 0, 13, 22, 1),
            new(89, 76010, "Fithik Hive Monarch 89 fithik_lunge", 405, 2, 1.0, 30.0, 15000, 1200, 48, 62, 1),
            new(89, 76011, "Fithik Hive Monarch 89 fithik_acid_spray", 201, 2, 0.5, 5.0, 10000, 0, 34, 62, 1),
            new(89, 76012, "Fithik Hive Monarch 89 fithik_self_destruct", 180, 2, 0.0, 0.0, 0, 4333, 123, 164, 1),
            new(520038, 76013, "Fithik Hive Master 520038 fithik_lunge", 405, 2, 1.0, 30.0, 15000, 1200, 228, 293, 1),
            new(520038, 76014, "Fithik Hive Master 520038 fithik_acid_spray", 201, 2, 0.5, 5.0, 10000, 0, 163, 293, 1),
            new(520038, 76015, "Fithik Hive Master 520038 fithik_self_destruct", 180, 2, 0.0, 0.0, 0, 4333, 584, 778, 1),
            new(33, 76016, "Bane Amoeboid 33 amoeboid_melee", 431, 1, 0.5, 3.0, 1000, 0, 19, 24, 1),
            new(33, 76017, "Bane Amoeboid 33 amoeboid_v1_vomit", 274, 1, 3.0, 14.0, 10000, 3600, 0, 0, 1),
            new(540033, 76018, "Amoeboid 540033 amoeboid_v1_vomit", 274, 1, 3.0, 14.0, 10000, 3600, 0, 0, 1),
            new(520018, 76019, "Splatter 520018 amoeboid_melee", 431, 2, 0.5, 3.0, 1000, 0, 130, 162, 1),
            new(520018, 76020, "Splatter 520018 amoeboid_slime", 211, 2, 0.5, 3.0, 4000, 1800, 97, 130, 1),
            new(520018, 76021, "Splatter 520018 amoeboid_v1_vomit", 274, 2, 3.0, 14.0, 10000, 3600, 0, 0, 1),
            new(520056, 76022, "Goriam 520056 amoeboid_melee", 431, 2, 0.5, 3.0, 1000, 0, 476, 595, 1),
            new(520056, 76023, "Goriam 520056 amoeboid_slime", 211, 2, 0.5, 3.0, 4000, 1800, 357, 476, 1),
            new(520056, 76024, "Goriam 520056 amoeboid_v1_vomit", 274, 2, 3.0, 14.0, 10000, 3600, 0, 0, 1),
            new(540026, 76025, "Beam Manta 540026 beammanta_lightning", 248, 1, 1.0, 20.0, 4000, 2966, 119, 119, 13),
            new(540021, 76026, "Beam Manta 540021 beammanta_lightning", 248, 1, 1.0, 20.0, 4000, 2966, 130, 130, 13),
            new(540040, 76027, "Beam Manta 540040 beammanta_lightning", 248, 1, 1.0, 20.0, 4000, 2966, 183, 183, 13),
            new(520055, 76028, "Cavalon 520055 shield_drone_attack", 396, 1, 0.5, 3.0, 1000, 0, 238, 714, 1),
            new(79, 76029, "Horntail 79 caretaker_attack", 395, 3, 1.0, 40.0, 1000, 700, 42, 63, 1),
            new(520015, 76030, "Caretaker Mordra 520015 caretaker_attack", 395, 3, 1.0, 40.0, 1000, 700, 65, 97, 1),
            new(520028, 76031, "Caretaker Fik 520028 caretaker_attack", 395, 3, 1.0, 40.0, 1000, 700, 308, 463, 1),
            new(3, 76032, "Thrax Soldier 3 thrax_kick", 397, 1, 0.5, 3.0, 10000, 333, 11, 15, 1),
            new(530005, 76033, "Thrax Commander 530005 thrax_kick", 397, 3, 0.5, 3.0, 10000, 333, 27, 41, 1),
            new(80, 76034, "Old Scratch 80 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 53, 105, 7),
            new(80, 76035, "Old Scratch 80 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 53, 105, 13),
            new(80, 76036, "Old Scratch 80 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 3, 8, 1),
            new(80, 76037, "Old Scratch 80 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 26, 53, 1),
            new(82, 76038, "Overseer Glognar 82 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 24, 48, 7),
            new(82, 76039, "Overseer Glognar 82 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 24, 48, 13),
            new(82, 76040, "Overseer Glognar 82 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 2, 3, 1),
            new(82, 76041, "Overseer Glognar 82 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 12, 24, 1),
            new(83, 76042, "Overseer Phlegg 83 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 24, 48, 7),
            new(83, 76043, "Overseer Phlegg 83 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 24, 48, 13),
            new(83, 76044, "Overseer Phlegg 83 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 2, 3, 1),
            new(83, 76045, "Overseer Phlegg 83 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 12, 24, 1),
            new(84, 76046, "Overseer Rankash 84 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 24, 48, 7),
            new(84, 76047, "Overseer Rankash 84 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 24, 48, 13),
            new(84, 76048, "Overseer Rankash 84 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 2, 3, 1),
            new(84, 76049, "Overseer Rankash 84 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 12, 24, 1),
            new(90, 76050, "Overseer Graal 90 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 34, 68, 7),
            new(90, 76051, "Overseer Graal 90 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 34, 68, 13),
            new(90, 76052, "Overseer Graal 90 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 2, 5, 1),
            new(90, 76053, "Overseer Graal 90 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 17, 34, 1),
            new(75, 76054, "Archfiend Grenadier 75 thrax_explosive_nanites", 457, 1, 1.0, 30.0, 15000, 500, 11, 26, 6),
            new(75, 76055, "Archfiend Grenadier 75 thrax_tectonic_strike", 458, 1, 1.0, 40.0, 10000, 500, 53, 105, 1),
            new(630043, 76056, "Treeback herd 630043 treeback_stomp", 190, 1, 1.0, 20.0, 30000, 3299, 79, 105, 7),
            new(630043, 76057, "Treeback herd 630043 treeback_howl", 192, 1, 1.0, 40.0, 4000, 1000, 40, 53, 7),
            new(630043, 76058, "Treeback herd 630043 treeback_swarm", 191, 1, 1.0, 40.0, 20000, 2799, 5, 11, 4)
        };

        public static readonly Slot[] Slots =
        {
            new(44, 2, 76001),
            new(44, 3, 76002),
            new(58, 2, 76003),
            new(58, 3, 76004),
            new(540007, 3, 76005),
            new(520044, 2, 76006),
            new(520044, 3, 76007),
            new(1, 3, 76008),
            new(1, 4, 76009),
            new(89, 2, 76010),
            new(89, 3, 76011),
            new(89, 4, 76012),
            new(520038, 2, 76013),
            new(520038, 3, 76014),
            new(520038, 4, 76015),
            new(33, 2, 76016),
            new(33, 3, 76017),
            new(540033, 3, 76018),
            new(520018, 2, 76020),
            new(520018, 3, 76021),
            new(520056, 2, 76023),
            new(520056, 3, 76024),
            new(540015, 3, 56011),
            new(540004, 3, 56011),
            new(540034, 3, 56011),
            new(540026, 2, 76025),
            new(540021, 2, 76026),
            new(540040, 2, 76027),
            new(531072, 2, 45),
            new(531087, 2, 45),
            new(531094, 2, 45),
            new(531050, 2, 45),
            new(531079, 2, 45),
            new(531060, 2, 45),
            new(531013, 2, 45),
            new(531106, 2, 45),
            new(531006, 2, 45),
            new(531034, 2, 45),
            new(520055, 2, 45),
            new(78, 5, 66001),
            new(79, 4, 76029),
            new(3, 2, 76032),
            new(530005, 2, 76033),
            new(80, 2, 76034),
            new(80, 3, 76035),
            new(80, 4, 61001),
            new(80, 5, 76036),
            new(80, 6, 76037),
            new(82, 2, 76038),
            new(82, 3, 76039),
            new(82, 4, 61001),
            new(82, 5, 76040),
            new(82, 6, 76041),
            new(83, 2, 76042),
            new(83, 3, 76043),
            new(83, 4, 61001),
            new(83, 5, 76044),
            new(83, 6, 76045),
            new(84, 2, 76046),
            new(84, 3, 76047),
            new(84, 4, 61001),
            new(84, 5, 76048),
            new(84, 6, 76049),
            new(90, 2, 76050),
            new(90, 3, 76051),
            new(90, 4, 61001),
            new(90, 5, 76052),
            new(90, 6, 76053),
            new(75, 3, 76054),
            new(75, 4, 68059),
            new(75, 5, 76055),
            new(630043, 1, 76056),
            new(630043, 2, 76057),
            new(630043, 3, 76058),
            new(630100, 2, 62059)
        };

        public static readonly Swap[] Swaps =
        {
            new(520018, 1, 11, 76019),
            new(520056, 1, 11, 76022),
            new(520055, 1, 42, 76028),
            new(520015, 1, 16, 76030),
            new(520028, 1, 16, 76031)
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var row in Rows)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {row.Id}, '{row.Description}', {row.ActionId}, {row.ArgId}, {F(row.RangeMin)}, {F(row.RangeMax)}, {row.Cooldown}, {row.Windup}, {row.MinDamage}, {row.MaxDamage}, {row.DamageType} " +
                    $"FROM creature c WHERE c.id = {row.Creature} AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {row.Id});";

            foreach (var slot in Slots)
                yield return $"UPDATE creature SET action{slot.Index} = {slot.Row} WHERE id = {slot.Creature} AND action{slot.Index} = 0 " +
                    $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {slot.Row});";

            foreach (var swap in Swaps)
                yield return $"UPDATE creature SET action{swap.Index} = {swap.Row} WHERE id = {swap.Creature} AND action{swap.Index} = {swap.Was} " +
                    $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {swap.Row});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var swap in Swaps)
                yield return $"UPDATE creature SET action{swap.Index} = {swap.Was} WHERE id = {swap.Creature} AND action{swap.Index} = {swap.Row};";

            foreach (var slot in Slots)
                yield return $"UPDATE creature SET action{slot.Index} = 0 WHERE id = {slot.Creature} AND action{slot.Index} = {slot.Row};";

            yield return $"DELETE FROM creature_action WHERE id BETWEEN {IdMin} AND {IdMax};";
        }

        private static string F(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}

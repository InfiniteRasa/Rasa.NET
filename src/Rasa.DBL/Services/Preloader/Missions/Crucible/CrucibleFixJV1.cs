using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job J (real client 2026-11-08 08:09-08:37, to-fix row 75; NOTES-crucible-triage mechanics item
    /// "Fix J"), a delta on migration 20271108000400 (set 4, CrucibleMinefieldAndSalvageNpcsV1), whose data class stays as it is.
    ///
    /// The play (visible, not immune): the Smart Mines (770014) came up within about 4 m, hit the player 170 times and died by
    /// themselves 5-10 s after they rose; no shot killed one, the count stayed 3/20. client_mod.log: two bands of "Necromite
    /// hits you for N points of Physical damage", 80-120 (the mine's weapon 1/109, row 770014, 80-120; CreatureWeaponDamage
    /// 1/109 Physical) and 146-300 (its necromite_self_destruct 489/1, row 770016, 150-300; the ability data has DAMAGE_TYPE 1,
    /// Physical), and three of 318, 347 and 431 (ESTIMATE: crits, or the blasts of two mines at once). The self-destruct is
    /// action 2 of the mine (range 0-5 m, 1 s cooldown): in the fight, the mine uses it as soon as the player is within 5 m
    /// (BehaviorManager -> MissileManager -> CreatureEffectAttacks.OnHit, GroundBlast -> CreatureBombs.GroundBlast), and
    /// CreatureBombs.Spend kills the mine at once (since fix job F, 20271108001000, as nobody's kill: no count).
    ///
    /// Retail: TaRapedia Minesweeper walkthrough "The mines don't actually explode and they do very little damage"; TaRapedia
    /// Necromite ("Bane Smart Mines"): they follow the player and deal a small virulent DOT. The client briefing 9809 ("you'll
    /// have a few seconds to shoot it out of the air before it explodes") is the design text; the walkthrough is the live game.
    ///
    /// Mechanics check (no new mechanic): the mine keeps its row 770014 and loses its action 2 (the self-destruct row 770016
    /// stays in the table, on no creature). Row 770014 becomes the existing creature damage over time of CreatureEffectAttacks
    /// (Kind.DamageOverTime, "abilities.decay": no hit of its own, DECAY 82 on the player, the row's damage a tick): the Bane
    /// Thrax's CR_THRAX_DECAY 450/1 (client action_property: DURATION 10 s, INTERVAL 2 s, DAMAGE_TYPE 4 Virulent; action_level
    /// windup 500 ms), as upstream's Thrax Technician bosses and Tharvox use it (rows 56013-56025, 78009). The Thrax's own
    /// Necromite 570003 (upstream 20261011000000) has no damage over time: weapon 1/109 Physical, the self-destruct and the
    /// corpse explosion. Nothing else changes: the mines lie buried until a player is about 8 m off (MawLurk), rise, follow
    /// the player (the fight's chase: a mine out of its 3 m goes after the player), sink back after the fight, and a player's
    /// kill counts (CreatureKilled 770014, 20; CrucibleMinesweeperV1).
    ///
    /// No code change: CreatureBombs.Spend keeps fix job F's "a lurking mine's detonation is nobody's kill" as a guard against
    /// a data rollback (no lurking mine detonates now). The rest is tests and an evidence row for 1232.
    ///
    /// Reconstructed (ESTIMATE): the tick 20-30 Virulent (5 ticks in 10 s: 100-150 a mine; "very little damage", under the
    /// 80-120 Physical every 3 s of the weapon it replaces); the reach 0.5-3 m (the Necromite weapon's: it follows to 3 m); the
    /// cooldown 10 s (the DOT's duration: one on the player from each mine at a time); the windup 500 ms (the client's
    /// action_level 450/1). That the Necromite's client class plays the Thrax's decay with no animation of its own is not
    /// checked (real client).
    /// </summary>
    public static class CrucibleFixJV1
    {
        public const uint SmartMine = CrucibleMinefieldAndSalvageNpcsV1.SmartMine;
        public const uint MineAttack = CrucibleMinefieldAndSalvageNpcsV1.MineWeapon;
        public const uint SelfDestruct = CrucibleMinefieldAndSalvageNpcsV1.MineSelfDestruct;

        /// <summary>A creature_action row's values: action, argument, reach, cooldown, windup, damage, damage_type, description.</summary>
        public sealed record Row(string Description, uint Action, uint Arg, double RangeMin, double RangeMax, uint Cooldown,
            uint Windup, uint DamageMin, uint DamageMax, uint DamageType);

        /// <summary>Row 770014 as set 4 wrote it (CrucibleMinefieldAndSalvageNpcsV1.Up): the Necromite's weapon 1/109.</summary>
        public static readonly Row Before = new("Smart Mine Crucible weapon 1/109", 1, 109, 0.5, 3, 3000, 0, 80, 120,
            CrucibleMinefieldAndSalvageNpcsV1.WeaponTableDamage);

        /// <summary>CR_THRAX_DECAY 450/1, the client's Virulent damage over time (DAMAGE_TYPE 4).</summary>
        public const uint ThraxDecay = 450, ThraxDecayArg = 1, Virulent = 4;

        /// <summary>Row 770014 now: CR_THRAX_DECAY 450/1, a tick of 20-30 Virulent every 2 s for 10 s (ESTIMATE: the numbers).</summary>
        public static readonly Row Now = new("Smart Mine Crucible thrax_decay 450/1 (fix J)", ThraxDecay, ThraxDecayArg, 0.5, 3, 10000, 500,
            20, 30, Virulent);

        /// <summary>The mine's action 2: set 4's self-destruct, none now.</summary>
        public const uint Action2Before = SelfDestruct, Action2Now = 0;

        /// <summary>1232's evidence row (set 4 wrote 1-3, fix job F 4).</summary>
        public const uint Evidence = 5;

        public const string Source = "repository:CrucibleFixJV1";

        public const string Note =
            "Fix job J (to-fix row 75): the Smart Mines rose, hit for 80-120 (weapon 1/109) and 146-300 Physical (necromite_self_destruct " +
            "489) and died by themselves 5-10 s up, no count. TaRapedia: the mines don't explode, very little damage; Necromite: a small " +
            "virulent DOT. Now no self-destruct; the attack the Thrax's decay 450/1 (Virulent, a tick every 2 s for 10 s) at 20-30 a " +
            "tick, reach 0.5-3 m, every 10 s (ESTIMATE: the numbers). They follow the player; a kill counts.";

        public static IEnumerable<string> UpSql()
        {
            yield return Set(MineAttack, Now);
            yield return $"UPDATE creature SET action2 = {Action2Now} WHERE id = {SmartMine};";
        }

        public static IEnumerable<string> DownSql()
        {
            yield return $"UPDATE creature SET action2 = {Action2Before} WHERE id = {SmartMine};";
            yield return Set(MineAttack, Before);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            CrucibleMissionDataV1.Existing(migration, CrucibleMinesweeperV1.MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {CrucibleMinesweeperV1.MissionId} " +
                          $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string Set(uint id, Row row) =>
            $"UPDATE creature_action SET description = '{row.Description}', action_id = {row.Action}, action_arg_id = {row.Arg}, " +
            $"range_min = {N(row.RangeMin)}, range_max = {N(row.RangeMax)}, cooldown = {row.Cooldown}, windup = {row.Windup}, " +
            $"min_damage = {row.DamageMin}, max_damage = {row.DamageMax}, damage_type = {row.DamageType} WHERE id = {id};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}

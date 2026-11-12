using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job F (real client pass 2026-11-07 15:50; NOTES-crucible-triage item 13; to-fix rows 44, 50, 59, 60),
    /// a delta on migrations 20271108000200 (set 3: the Warden Bot and Lasher pools, 1157's markers), 20271108000300 (pool
    /// 540141 and 1149's marker), 20271108000400 (set 4: the mine and Reconstructor pools, Minesweeper 1232), 20271108000600
    /// (set 5: Shield Drone pool 770024, Molitor's pool 770029 and their markers) and 20271108000800 (Warden Bot pool 770011
    /// and its markers). The live World DB has applied them: their data classes stay as they are. The rest is code
    /// (CreatureBombs.Spend) and tests; the data: twelve pools made points (radius 0), six of them moved off lava with their
    /// markers, and an evidence row for each mission concerned.
    ///
    ///  - 1232, self-detonation: a Smart Mine that comes up and goes off at the player died of it and counted as destroyed
    ///    (3/20 in 2 s): CreatureBombs.Spend gave the kill to the target. Now a minefield mine (MawLurk.Lurks) that goes off
    ///    is nobody's kill (code). Client 1232 text 9809 (1.0): "When one pops up, you'll have a few seconds to shoot it out of
    ///    the air before it explodes".
    ///  - 1232, the name: the mines show "Necromite" (name 433). The client tables have no Smart Mine name or class:
    ///    creaturenamelanguage has no "Smart Mine" (its "Mine" names are 8737 Fluxite Mine Guard, 8763 Thrax Mine Layer, 9482
    ///    Mine Foreman Esoobac, 10363 Mine Operations Technician), physicalentityclassnamelanguage's "Mine" (22343, 22806) are
    ///    Eloh logos objects, and class 7528 is Ability_Bane_Necromite; "Smart Mine" is only in mission and tooltip texts
    ///    (1232's 9808-9814, 14635/14636, tooltip 1847). The name stays 433, a deviation (NOTES-playtest.md).
    ///  - 1232, pools 1 and 3 "empty": the mines respawn (run/game.log: the same entities killed again 120 s and more apart,
    ///    12:37-12:44) under the ground (MawLurk) and come up only for a player within 8 m (ESTIMATE) of where they lie. A
    ///    pool with a radius sets its creatures down at NavMeshQuery.RandomPointAround (Detour's findRandomPointAroundCircle:
    ///    a point anywhere in a navmesh polygon the circle touches), which in the open trench is 16 to 22 m from the pool's
    ///    point (probe 2026-11-07, pool 770014 r6): outside the 8 m from the marker's centre, so a player there found none (the
    ///    real client stood 10.4 m and more from pool 1's point, client_mod.log 15:51-15:53). Now the mine pools are points
    ///    (radius 0): SpawnPoolManager sets the four down one after the other on the places round the point (Spacing.OnRings:
    ///    the point, then the first ring 3.3 m out, the second 6.6 m), each clear of the ones already there; Spacing.Taken
    ///    counts the buried pool-mates too (code, fix job F: a buried mine is in no cell, and all four were put on the point).
    ///    All four lie within 8 m of the point.
    ///  - To-fix row 44: the same draw puts a bot of Warden pools 770009, 770011 and Reconstructor pools 770017, 770018 up to
    ///    36 m from its point (2000 draws, 2026-11-07); a chase of MaxChaseDistance (60 m) and one think from such a home
    ///    came 7.8, 4.9, -10.8 and 7.7 m outside a link's circle (770017: into the Chaukas door, link 98). Now they are points
    ///    too (radius 0, the rings): every chase stays 10 m or more outside every link's circle (CrucibleGuardTests).
    ///  - To-fix row 50: the fall by the east stairs (-178.8, 194.7, 355.4), on the walk from the lower office teleporter to
    ///    Donal, added to the Staal holes (<see cref="Holes"/>) as the tester's avoid circle (-178, 196, 355) r6
    ///    (RASABOT_NAV_AVOID); upstream N10. The server navmesh takes four walks (the lower teleporter to and from Donal and to
    ///    Nema, Donal to Munit) up those stairs; no NPC or marker move takes them off it: listed, not moved.
    ///  - To-fix row 60: the two Lasher pools of Whiplash (1157) stood on the lava lake (-832.9..-768.9, 372.7..436.7), y 284
    ///    (LavaSurfaces, from the client's .map; LavaDamage.InLava burns a player on it: 185 and 249 of 317 navmesh points
    ///    within 20 m of them). Moved south-east onto the y 286 ground at (-752, 285.6, 368) and (-780, 286.04, 356) (30.5 m
    ///    apart, as before), no lava within 24 m, walked to from the Awol Camp waypoint, 67 m and more from every other pool's
    ///    circle (probe 2026-11-07); 1157's two markers on them. The lava guard (CrucibleGuardTests) found four more of our
    ///    pools with lava where their creatures stand or a player fights them: Flaregasher pool 540141 (1149; on a lava flow),
    ///    Warden Bot pool 770011 (1147, 1203, 1204; on the lake (-626..-562, 504..568), y 248), Shield Drone pool 770024 (1208;
    ///    the lake (346..410, 48..112) within 20 m) and Molitor's pool 770029 (1185; the lake (-912..-848, 364..428) at its
    ///    edge). Each moved to the nearest navmesh point with no lava within 20 m, walked to from its old point, 30 m and more
    ///    from every other pool's circle (770011: 79 m and more from every link's circle), and its markers with it.
    ///  - To-fix row 59: the server navmesh walk from the S5 cave mouth to DeLisi crosses the lava lakes at y 184 and the lake
    ///    at (463..527, 707..771), y 161.9 (upstream, NOTES-upstream-bugs N12). No marker is on it: nothing moved.
    /// </summary>
    public static class CrucibleFixFV1
    {
        /// <summary>The evidence row 1232 gets (its next free evidence id).</summary>
        public const uint MinesweeperEvidence = 4;

        /// <summary>The Staal holes: CrucibleStaalWalksV1's two (to-fix rows 32 and 35) and the fall by the east stairs (row 50).</summary>
        public static readonly ((double X, double Y, double Z) At, double Radius)[] Holes =
        {
            CrucibleStaalWalksV1.Holes[0], CrucibleStaalWalksV1.Holes[1], ((-178, 196, 355), 6)
        };

        /// <summary>A point pool's radius.</summary>
        public const uint PointRadius = 0;

        /// <summary>
        /// The pools made points (radius 0), with the radius they had: the three mine pools (1232), the bot pools whose chase
        /// reached a door (row 44), and the pools moved off the lava (row 60).
        /// </summary>
        public static readonly (uint Pool, uint Before)[] Points =
        {
            (CrucibleMinefieldAndSalvageNpcsV1.MinePools[0].Pool, CrucibleMinefieldAndSalvageNpcsV1.PoolRadius),
            (CrucibleMinefieldAndSalvageNpcsV1.MinePools[1].Pool, CrucibleMinefieldAndSalvageNpcsV1.PoolRadius),
            (CrucibleMinefieldAndSalvageNpcsV1.MinePools[2].Pool, CrucibleMinefieldAndSalvageNpcsV1.PoolRadius),
            (CrucibleWardensAndLashersNpcsV1.WardenPools[0].Pool, CrucibleWardensAndLashersNpcsV1.PoolRadius),
            (CrucibleWardensAndLashersNpcsV1.WardenPools[2].Pool, CrucibleWardensAndLashersNpcsV1.PoolRadius),
            (CrucibleMinefieldAndSalvageNpcsV1.ReconstructorPools[0].Pool, CrucibleMinefieldAndSalvageNpcsV1.PoolRadius),
            (CrucibleMinefieldAndSalvageNpcsV1.ReconstructorPools[1].Pool, CrucibleMinefieldAndSalvageNpcsV1.PoolRadius),
            (CrucibleWardensAndLashersNpcsV1.LasherPools[0].Pool, CrucibleWardensAndLashersNpcsV1.PoolRadius),
            (CrucibleWardensAndLashersNpcsV1.LasherPools[1].Pool, CrucibleWardensAndLashersNpcsV1.PoolRadius),
            (FlaregasherPool, FlaregasherRadius),
            (CrucibleStaalSigalsNpcsV1.DronePools[0].Pool, CrucibleStaalSigalsNpcsV1.PoolRadius),
            (CrucibleStaalSigalsNpcsV1.MolitorPool, CrucibleStaalSigalsNpcsV1.MolitorPoolRadius)
        };

        /// <summary>Upstream's Flaregasher pool 540141 (radius 15, upstream's), which 20271108000300 moved; 1149's marker 3 on it.</summary>
        public const uint FlaregasherPool = 540141, FlaregasherRadius = 15;

        /// <summary>The pools moved off the lava (row 60): pool, the place before, the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (CrucibleWardensAndLashersNpcsV1.LasherPools[0].Pool, CrucibleWardensAndLashersNpcsV1.LasherPools[0].At, (-752, 285.6, 368)),
            (CrucibleWardensAndLashersNpcsV1.LasherPools[1].Pool, CrucibleWardensAndLashersNpcsV1.LasherPools[1].At, (-780, 286.04, 356)),
            (FlaregasherPool, CrucibleMarkersClearOfLinksV2.Pools.Single(pool => pool.Pool == FlaregasherPool).Now, (-368, 249.68, 70)),
            (CrucibleWardensAndLashersNpcsV1.WardenPools[2].Pool, CrucibleWardenPoolsAndVargoV1.Now(CrucibleWardensAndLashersNpcsV1.WardenPools[2].Pool).Value,
                (-636, 249.76, 543)),
            (CrucibleStaalSigalsNpcsV1.DronePools[0].Pool, CrucibleStaalSigalsNpcsV1.DronePools[0].At, (415, 170.08, 113)),
            (CrucibleStaalSigalsNpcsV1.MolitorPool, CrucibleStaalSigalsNpcsV1.MolitorAt, (-853, 286.64, 359))
        };

        /// <summary>Where a pool stands after this migration: a moved one's new place, else null.</summary>
        public static (double X, double Y, double Z)? Now(uint pool) =>
            Moves.Any(move => move.Pool == pool) ? Moves.Single(move => move.Pool == pool).Now : null;

        /// <summary>Every Crucible marker on a moved pool: mission, objective, indicator, pool.</summary>
        public static IEnumerable<(uint Mission, uint Objective, uint Indicator, uint Pool)> Markers
        {
            get
            {
                var lashers = CrucibleWardensAndLashersNpcsV1.LasherPools;
                yield return (CrucibleWhiplashV1.MissionId, CrucibleWhiplashV1.Glands, MissionIndicator.UnnamedFrom + 1, lashers[0].Pool);
                yield return (CrucibleWhiplashV1.MissionId, CrucibleWhiplashV1.Glands, MissionIndicator.UnnamedFrom + 2, lashers[1].Pool);
                yield return (CrucibleJunkyardDogsV1.MissionId, CrucibleJunkyardDogsV1.Kill, MissionIndicator.UnnamedFrom + 3, FlaregasherPool);
                foreach (var marker in CrucibleWardenPoolsAndVargoV1.Markers.Where(marker => marker.Pool == CrucibleWardensAndLashersNpcsV1.WardenPools[2].Pool))
                    yield return marker;
                yield return (CrucibleOutOfSightV1.MissionId, CrucibleOutOfSightV1.Processors, MissionIndicator.UnnamedFrom + 1,
                    CrucibleStaalSigalsNpcsV1.DronePools[0].Pool);
                yield return (CrucibleIrreconcilableDifferencesV1.MissionId, CrucibleIrreconcilableDifferencesV1.Necklace, MissionIndicator.UnnamedFrom + 2,
                    CrucibleStaalSigalsNpcsV1.MolitorPool);
            }
        }

        /// <summary>The evidence row each mission gets (its next free evidence id).</summary>
        public static readonly (uint Mission, uint Evidence)[] EvidenceIds =
        {
            (CrucibleMinesweeperV1.MissionId, MinesweeperEvidence), (CrucibleGameWardensV1.MissionId, 6), (CrucibleSalvageDutyV1.MissionId, 5),
            (CrucibleSalvageDutyV1.MoreMissionId, 5), (CrucibleArchitectsOfDestructionV1.MissionId, 5), (CrucibleWhiplashV1.MissionId, 4),
            (CrucibleJunkyardDogsV1.MissionId, 5), (CrucibleOutOfSightV1.MissionId, 4), (CrucibleIrreconcilableDifferencesV1.MissionId, 4)
        };

        public const string Note =
            "Fix job F: a Smart Mine that comes up and goes off at the player (necromite_self_destruct) is nobody's kill: no count, " +
            "experience or loot (client 9809: shoot it before it explodes); only one shot down counts. The name stays Necromite 433: " +
            "the client has no Smart Mine creature name or class (creaturenamelanguage, physicalentityclassnamelanguage; class 7528 " +
            "Ability_Bane_Necromite), a deviation. The mine pools are points now (radius 0): a radius set the mines down 16-22 m from " +
            "the point (navmesh random point), out of the 8 m (ESTIMATE) a buried mine comes up at; now the four lie within 6.6 m, " +
            "and a player at the marker's centre brings all four up. They respawn after 120 s under the ground.";

        public const string BotNote =
            "Fix job F (to-fix row 44): a pool with a radius sets its bots down anywhere in a navmesh polygon its circle touches, up to " +
            "36 m from its point, and a chase from there reached the Chaukas Robotics Facility door; Warden Bot pools 770009, 770011 " +
            "and Reconstructor pools 770017, 770018 are points now (radius 0, the bots on rings within 6.6 m), every chase 10 m or " +
            "more outside every link's circle. Row 60: Warden Bot pool 770011 stood on the lava lake (-626..-562, 504..568), y 248; " +
            "moved to (-636, 249.76, 543), no lava within 20 m, its markers on it.";

        public const string LasherNote =
            "Fix job F (to-fix row 60): both Lasher pools stood on the lava lake at (-832.9..-768.9, 372.7..436.7), y 284 " +
            "(LavaSurfaces; the real client took 192 Fire hits there); moved to (-752, 285.6, 368) and (-780, 286.04, 356), 24 m and " +
            "more from any lava the server burns on (LavaDamage.InLava), points (radius 0); the two markers on them.";

        public const string FlaregasherNote =
            "Fix job F (to-fix row 60, lava guard): Flaregasher pool 540141 at (-380, 248.05, 46) stood on lava (LavaDamage.InLava); " +
            "moved to (-368, 249.68, 70), no lava within 20 m, a point (radius 0); marker 3 on it.";

        public const string DroneNote =
            "Fix job F (to-fix row 60, lava guard): the lava lake (346..410, 48..112), y 168, came within 20 m of Shield Drone pool " +
            "770024 at (415, 170.08, 110); moved to (415, 170.08, 113), no lava within 20 m, a point (radius 0); its marker on it.";

        public const string MolitorNote =
            "Fix job F (to-fix row 60, lava guard): Molitor's pool 770029 at (-856, 286.83, 371) had the lava lake (-912..-848, " +
            "364..428), y 284, at its edge; moved to (-853, 286.64, 359), no lava within 20 m, a point (radius 0); marker 2 on it.";

        /// <summary>The note of a mission's evidence row.</summary>
        public static string NoteOf(uint mission) => mission switch
        {
            CrucibleMinesweeperV1.MissionId => Note,
            CrucibleWhiplashV1.MissionId => LasherNote,
            CrucibleJunkyardDogsV1.MissionId => FlaregasherNote,
            CrucibleOutOfSightV1.MissionId => DroneNote,
            CrucibleIrreconcilableDifferencesV1.MissionId => MolitorNote,
            _ => BotNote
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (pool, _) in Points)
                migration.Sql(Radius(pool, PointRadius));
            foreach (var (pool, _, now) in Moves)
                migration.Sql(Place(pool, now));
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Now(pool).Value));
            const string Source = "repository:CrucibleFixFV1";
            foreach (var (mission, evidence) in EvidenceIds)
                CrucibleMissionDataV1.Existing(migration, mission).Evidence(evidence, MissionEvidenceSourceKind.Reconstruction, Source,
                    NoteOf(mission), 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (mission, evidence) in EvidenceIds)
                migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' " +
                              $"AND evidence_id = {evidence};");
            foreach (var (mission, objective, indicator, pool) in Markers)
                migration.Sql(Marker(mission, objective, indicator, Moves.Single(move => move.Pool == pool).Before));
            foreach (var (pool, before, _) in Moves)
                migration.Sql(Place(pool, before));
            foreach (var (pool, before) in Points)
                migration.Sql(Radius(pool, before));
        }

        private static string Radius(uint pool, uint radius) => $"UPDATE spawnpool SET radius = {radius} WHERE id = {pool};";

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string Marker(uint mission, uint objective, uint indicator, (double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} " +
            $"WHERE mission_id = {mission} AND content_revision = '{CrucibleMissionDataV1.Revision}' AND objective_id = {objective} " +
            $"AND indicator_id = {indicator};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ashen fix job M (to-fix row 48), a delta on 20271110001700 (set 17, live) and 20271110001800 (set 18, live), Avernus
    /// Outpost (2110). A creature notices a player within 18 m (Creature.AggroRange, 3D) of where it stands, and a pool with a
    /// circle stands its creatures anywhere on the navmesh polygons that touch the circle (NavMeshManager.RandomPointAround):
    /// the places of sets 17 and 18 were kept 34 m outside the circles on paper, but some stood on the pools' measured spawn
    /// ground (AshenSpawnReach, 1500 seeded draws a pool). The real client died on the walk from pad 916 to Captain Petrovich,
    /// and found Petrovich (a plain pool NPC, 4500 HP) gone with six Atta round his place: his pool comes back only after the
    /// squad copy's 45 minutes.
    ///
    ///  - Petrovich in the AFS Graveyard 790642 from (909, 273.86, -246), 0.6 m from upstream pool 531050's draws (r 20, its
    ///    draws up to 67 m from its middle), to the graveyard's north-west shelf (873, 284.11, -219): 40.8 m from every hostile
    ///    pool's draws, 36 m outside 531049's circle. 1533's hidden area (15 m) with him; 1535's scene copy of him and his squad
    ///    (officer, two soldiers) round him.
    ///  - Pad 916 (the graveyard's Atta Teleporter) from (890, 278.11, -232), 19.7 m from 531050's draws, to (888, 281.86,
    ///    -212): 30.9 m; its ramp down to the south, then 15 m west to Petrovich (the arrival 16.6 m from him, outside 1533's area).
    ///  - Pad 915 (Drazen's) from (64, 480.73, -312), whose south (ramp) side is a 42-45 degree hillside and whose west side the
    ///    real client does not let a player step off (row 48, 2026-11-12), to the flat floor south-west of Drazen (48, 477.12,
    ///    -318): its ramp foot (48, 477.49, -324.5) on the same floor; 26.8 m (3D) outside 531061's circle on paper (a break of
    ///    the 34 m pad rule; far from every pool's draws, AshenFixMTests).
    ///  - Petrovich 790642, Commander Drazen 790641 (1533's giver) named as public-spawn actors of 1533's scene and Petrovich at
    ///    the top 790643 of 1535's: a named pool comes back on its own respawn time (2 s) in a squad's copy, not after 45 min
    ///    (SpawnPoolManager.ReturnsOnOwnTime), as Tananda (set 18). An NPC of a plain pool cannot be made unhurt by data (only a
    ///    scene's role is, CreatureGameplayRules.IsInvulnerable); Karter (106 m from every pool's draws) is left as he is.
    ///  - Colonel Vandeweghe (1537's scene) from (349, 233.11, -260), 0.5 m from upstream pool 531058's draws, and the queen pad
    ///    918 (its pair 917 kept) from (340, 238.11, -252), 4.0 m, to the hollow down the Queen's Chamber tunnel: Vandeweghe
    ///    (294, 216.61, -330), 26.5 m from every hostile pool's draws; the pad (306, 218.11, -326), 26.4 m, its arrival 12.6 m
    ///    from him (his area 6 m). 1537's area 2 and markers 2 and 3 with him; his lead down starts at his place (the route's
    ///    first point (316, 224.28, -332), back up toward 531058, dropped).
    ///  - Placements off the navmesh (MissionGroundTests, found by fix L's run): 1535's first ambush man of both waves from (745,
    ///    329.96, -135), 1.81 m sideways from the navmesh, to its nearest point (745.2, 330.48, -136.8); 1497's front field from
    ///    (6.5, 129.5, -405), 6.87 m from the navmesh (an object's 6 m), up to (6.5, 131, -405): its foot still 0.5 m under the
    ///    tunnel's lowest floor (131.5, the east gutter), so nobody walks under it.
    ///  - 1498's lab charge (a delta on 20271110001920, fix L, which owns 1498's scene) from the Atta Laboratory's door (-216,
    ///    96.01, -160), 13.6 m from the Atta Imperial Guard 520002's draws, 9 m south along the hall to (-213, 96.01, -169):
    ///    21.7 m from 520002's draws (31.4 m from its point), 23.6-24.8 m from the lab's own 531064 and 531066 and 25 m and more
    ///    from every other pool's; 20.6 and 20.4 m outside 531064's and 531066's circles (set 19's 20 m rule); objective 5's
    ///    marker with it.
    /// All new places ESTIMATE (a client-floor and navmesh probe, 2026-11-12).
    /// </summary>
    public static class AshenFixMV1
    {
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint LastStand = AshenPetrovichsLastStandV1.MissionId, Run = AshenPetrovichsRunV1.MissionId, Queen = AshenGodSaveTheQueenV1.MissionId,
            Raid = AshenRaidTheConscriptionFacilityV1.MissionId, Blast = AshenBlastTheBiolabV1.MissionId;
        public const uint Petrovich = AshenAvernusOutpostNpcsV1.Petrovich, Drazen = AshenAvernusOutpostNpcsV1.Drazen, PetrovichTop = AshenAvernusOutpostNpcsV1.PetrovichTop;

        public static readonly (double X, double Y, double Z) PetrovichBefore = AshenAvernusOutpostNpcsV1.PetrovichAt, PetrovichAt = (873, 284.11, -219);
        public static readonly ScenePosition OfficerAt = new(871f, 284.61f, -216f);
        public static readonly ScenePosition[] SoldiersAt = { new(872f, 284.36f, -221f), new(875f, 283.86f, -217f) };

        public const uint GraveyardPad = AshenAvernusOutpostNpcsV1.GraveyardPad, CavernPad = AshenAvernusOutpostNpcsV1.CavernPad, QueenPad = AshenAvernusHiveNpcsV1.QueenPad;
        public static readonly (double X, double Y, double Z) CavernPadBefore = AshenAvernusOutpostNpcsV1.CavernPadAt, CavernPadAt = (48, 477.12, -318);
        public static readonly (double X, double Y, double Z) GraveyardPadBefore = AshenAvernusOutpostNpcsV1.GraveyardPadAt, GraveyardPadAt = (888, 281.86, -212);
        public static readonly (double X, double Y, double Z) QueenPadBefore = AshenAvernusHiveNpcsV1.QueenPadAt, QueenPadAt = (306, 218.11, -326);
        public static readonly (double X, double Y, double Z) VandewegheBefore = AshenAvernusHiveNpcsV1.VandewegheAt, VandewegheAt = (294, 216.61, -330);

        public const uint GraveyardArea = AshenPetrovichsLastStandV1.GraveyardArea, ColonelArea = AshenGodSaveTheQueenV1.ColonelArea;
        public const uint ColonelPlaceObjective = AshenGodSaveTheQueenV1.TeleportToTheChamber, ColonelPlaceMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 2;
        public const uint ColonelObjective = AshenGodSaveTheQueenV1.Colonel, ColonelMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 3;

        /// <summary>The public-spawn roles fix M names (a named pool comes back on its own time in a squad's copy).</summary>
        public const string PetrovichRole = "petrovich-in-the-graveyard", DrazenRole = "drazen", PetrovichTopRole = "petrovich-at-the-top";

        /// <summary>1535's ambush men (both waves): the first on the navmesh (set 17's 1.81 m off it), the second set 17's.</summary>
        public static readonly ScenePosition[] AmbushAt = { new(745.2f, 330.48f, -136.8f), AshenPetrovichsRunV1.AmbushAt[1] };

        /// <summary>1497's front field, raised to within an object's reach of the navmesh (set 19's (6.5, 129.5, -405)).</summary>
        public static readonly (double X, double Y, double Z) FieldBefore = AshenConscriptionEntryNpcsV1.FieldAt, FieldAt = (6.5, 131, -405);

        /// <summary>1498's lab charge, 9 m south of the lab's door along the hall (fix L's scene, set 19's place), and its marker.</summary>
        public static readonly (double X, double Y, double Z) LabChargeBefore = (AshenBlastTheBiolabV1.ChargeAt.X, AshenBlastTheBiolabV1.ChargeAt.Y, AshenBlastTheBiolabV1.ChargeAt.Z),
            LabChargeAt = (-213, 96.01, -169);
        public const uint LabChargeObjective = AshenBlastTheBiolabV1.DestroyTheLab, LabChargeMarker = Rasa.Structures.MissionIndicator.UnnamedFrom + 4;

        /// <summary>Vandeweghe's way down: set 18's without its first point.</summary>
        public static readonly ScenePosition[] DownWay = AshenGodSaveTheQueenV1.DownWay.Skip(1).ToArray();

        public const uint Evidence = 4;
        /// <summary>1498's fix M row (fix L has its 4).</summary>
        public const uint BlastEvidence = 5;
        public const string Source = "repository:AshenFixMV1";
        public const string LastStandNote =
            "Fix job M (to-fix row 48): Captain Petrovich 790642 from (909, 273.86, -246), on upstream pool 531050's spawn ground, to " +
            "(873, 284.11, -219), 40.8 m from every hostile pool's draws; the 15 m area with him; pad 916 to (888, 281.86, -212), pad 915 " +
            "to (48, 477.12, -318) (walked onto up its south ramp). " +
            "Petrovich and Drazen 790641 named public-spawn actors (back on their own respawn time in a squad's copy). ESTIMATE.";
        public const string RunNote =
            "Fix job M (to-fix row 48): Petrovich's scene copy and his squad with him to (873, 284.11, -219); Petrovich at the top " +
            "790643 a named public-spawn actor (back on his own respawn time in a squad's copy); the first ambush man of both waves " +
            "from (745, 329.96, -135) onto the navmesh (745.2, 330.48, -136.8). ESTIMATE.";
        public const string RaidNote =
            "Fix job M (MissionGroundTests): the front field from (6.5, 129.5, -405), 6.87 m from the navmesh, up to (6.5, 131, -405); " +
            "its foot still 0.5 m under the tunnel's lowest floor (131.5). ESTIMATE.";
        public const string BlastNote =
            "Fix job M (to-fix row 48): the lab's charge from the Atta Laboratory's door (-216, 96.01, -160), 13.6 m from the Atta " +
            "Imperial Guard 520002's spawn draws, 9 m south along the hall to (-213, 96.01, -169), 21.7 m; objective 5's marker " +
            "with it. ESTIMATE.";
        public const string QueenNote =
            "Fix job M (to-fix row 48): Vandeweghe from (349, 233.11, -260), on upstream pool 531058's spawn ground, to (294, 216.61, " +
            "-330), 26.5 m from every hostile pool's draws; area 2 and markers 2-3 with him; pad 918 to (306, 218.11, -326); his lead " +
            "down starts at his place. ESTIMATE.";

        /// <summary>1533's scene as set 17 enabled it (AshenPetrovichsLastStandV1.Up).</summary>
        public static MissionSceneDefinition LastStandSceneBefore()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = Outpost,
                HiddenObjectiveIds = new() { AshenPetrovichsLastStandV1.InTheGraveyard }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static MissionSceneDefinition LastStandScene()
        {
            var scene = LastStandSceneBefore();
            scene.Actors[PetrovichRole] = new SceneActorDefinition(PetrovichRole, SceneActorKind.PublicSpawn, Petrovich);
            scene.Actors[DrazenRole] = new SceneActorDefinition(DrazenRole, SceneActorKind.PublicSpawn, Drazen);
            return scene;
        }

        public static MissionSceneDefinition RunScene()
        {
            var scene = AshenPetrovichsRunV1.Scene();
            var role = AshenPetrovichsRunV1.PetrovichRole;
            scene.Actors[role] = scene.Actors[role] with { Position = new ScenePosition((float)PetrovichAt.X, (float)PetrovichAt.Y, (float)PetrovichAt.Z) };
            scene.Actors[AshenPetrovichsRunV1.OfficerRole] = scene.Actors[AshenPetrovichsRunV1.OfficerRole] with { Position = OfficerAt };
            for (var index = 0; index < SoldiersAt.Length; index++)
            {
                var soldier = AshenPetrovichsRunV1.SoldierRole(index);
                scene.Actors[soldier] = scene.Actors[soldier] with { Position = SoldiersAt[index] };
            }
            scene.Actors[PetrovichTopRole] = new SceneActorDefinition(PetrovichTopRole, SceneActorKind.PublicSpawn, PetrovichTop);
            for (var wave = 1; wave <= 2; wave++)
                for (var member = 0; member < AmbushAt.Length; member++)
                {
                    var ambush = AshenPetrovichsRunV1.AmbushRole(wave, member + 1);
                    scene.Actors[ambush] = scene.Actors[ambush] with { Position = AmbushAt[member] };
                }
            return scene;
        }

        public static MissionSceneDefinition QueenScene()
        {
            var scene = AshenGodSaveTheQueenV1.Scene();
            var role = AshenGodSaveTheQueenV1.ColonelRole;
            scene.Actors[role] = scene.Actors[role] with { Position = new ScenePosition((float)VandewegheAt.X, (float)VandewegheAt.Y, (float)VandewegheAt.Z) };
            var route = AshenGodSaveTheQueenV1.DownRoute;
            scene.Routes[route] = scene.Routes[route] with { Points = DownWay.Select(point => new SceneWaypoint(point)).ToArray() };
            return scene;
        }

        public static MissionSceneDefinition RaidScene()
        {
            var scene = AshenRaidTheConscriptionFacilityV1.Scene();
            var role = AshenRaidTheConscriptionFacilityV1.FieldRole;
            scene.Actors[role] = scene.Actors[role] with { Position = new ScenePosition((float)FieldAt.X, (float)FieldAt.Y, (float)FieldAt.Z) };
            return scene;
        }

        /// <summary>1498's scene as fix L left it, the charge moved.</summary>
        public static MissionSceneDefinition BlastScene()
        {
            var scene = AshenFixLV1.BlastScene();
            var role = AshenBlastTheBiolabV1.ChargeRole;
            scene.Actors[role] = scene.Actors[role] with { Position = new ScenePosition((float)LabChargeAt.X, (float)LabChargeAt.Y, (float)LabChargeAt.Z) };
            return scene;
        }

        public static string BlastBefore() => AshenFixLV1.BlastSceneNow();
        public static string BlastNow() => Json(BlastScene());

        public static string RaidBefore() => Json(AshenRaidTheConscriptionFacilityV1.Scene());
        public static string RaidNow() => Json(RaidScene());

        public static string LastStandBefore() => Json(LastStandSceneBefore());
        public static string LastStandNow() => Json(LastStandScene());
        public static string RunBefore() => Json(AshenPetrovichsRunV1.Scene());
        public static string RunNow() => Json(RunScene());
        public static string QueenBefore() => Json(AshenGodSaveTheQueenV1.Scene());
        public static string QueenNow() => Json(QueenScene());

        public static IEnumerable<string> UpSql()
        {
            yield return PoolSql(Petrovich, PetrovichAt, PetrovichBefore);
            yield return PadSql(GraveyardPad, GraveyardPadAt, GraveyardPadBefore);
            yield return PadSql(CavernPad, CavernPadAt, CavernPadBefore);
            yield return PadSql(QueenPad, QueenPadAt, QueenPadBefore);
            yield return AreaSql(LastStand, GraveyardArea, PetrovichAt, PetrovichBefore);
            yield return AreaSql(Queen, ColonelArea, VandewegheAt, VandewegheBefore);
            yield return MarkerSql(Queen, ColonelPlaceObjective, ColonelPlaceMarker, VandewegheAt, VandewegheBefore);
            yield return MarkerSql(Queen, ColonelObjective, ColonelMarker, VandewegheAt, VandewegheBefore);
            yield return SceneSql(LastStand, LastStandNow(), LastStandBefore());
            yield return SceneSql(Run, RunNow(), RunBefore());
            yield return SceneSql(Queen, QueenNow(), QueenBefore());
            yield return SceneSql(Raid, RaidNow(), RaidBefore());
            yield return MarkerSql(Blast, LabChargeObjective, LabChargeMarker, LabChargeAt, LabChargeBefore);
            yield return SceneSql(Blast, BlastNow(), BlastBefore());
        }

        public static IEnumerable<string> DownSql()
        {
            yield return SceneSql(Blast, BlastBefore(), BlastNow());
            yield return MarkerSql(Blast, LabChargeObjective, LabChargeMarker, LabChargeBefore, LabChargeAt);
            yield return SceneSql(Raid, RaidBefore(), RaidNow());
            yield return SceneSql(Queen, QueenBefore(), QueenNow());
            yield return SceneSql(Run, RunBefore(), RunNow());
            yield return SceneSql(LastStand, LastStandBefore(), LastStandNow());
            yield return MarkerSql(Queen, ColonelObjective, ColonelMarker, VandewegheBefore, VandewegheAt);
            yield return MarkerSql(Queen, ColonelPlaceObjective, ColonelPlaceMarker, VandewegheBefore, VandewegheAt);
            yield return AreaSql(Queen, ColonelArea, VandewegheBefore, VandewegheAt);
            yield return AreaSql(LastStand, GraveyardArea, PetrovichBefore, PetrovichAt);
            yield return PadSql(QueenPad, QueenPadBefore, QueenPadAt);
            yield return PadSql(CavernPad, CavernPadBefore, CavernPadAt);
            yield return PadSql(GraveyardPad, GraveyardPadBefore, GraveyardPadAt);
            yield return PoolSql(Petrovich, PetrovichBefore, PetrovichAt);
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            AshenMissionDataV1.Existing(migration, LastStand).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, LastStandNote, 0.3);
            AshenMissionDataV1.Existing(migration, Run).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, RunNote, 0.3);
            AshenMissionDataV1.Existing(migration, Queen).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, QueenNote, 0.3);
            AshenMissionDataV1.Existing(migration, Raid).Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, RaidNote, 0.3);
            AshenMissionDataV1.Existing(migration, Blast).Evidence(BlastEvidence, MissionEvidenceSourceKind.Reconstruction, Source, BlastNote, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id IN ({LastStand}, {Run}, {Queen}, {Raid}) " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {Evidence} AND source_uri = '{Source}';");
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {Blast} " +
                          $"AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {BlastEvidence} AND source_uri = '{Source}';");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Json(MissionSceneDefinition scene)
        {
            var json = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            if (json.Contains('\'') || json.Contains('\\'))
                throw new InvalidOperationException("The scene JSON holds a quote or a backslash: escape it for SQLite and MySQL alike.");
            return json;
        }

        private static string Near((double X, double Y, double Z) at) =>
            $"ABS(pos_x - {F(at.X)}) < 0.001 AND ABS(pos_y - {F(at.Y)}) < 0.001 AND ABS(pos_z - {F(at.Z)}) < 0.001";

        private static string Set((double X, double Y, double Z) to) => $"pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}";

        private static string SceneSql(uint mission, string to, string from) =>
            $"UPDATE mission_scene_binding SET bindings = '{to}' WHERE mission_id = {mission} " +
            $"AND content_revision = '{AshenMissionDataV1.Revision}' AND script_key = 'data.sequence' AND bindings = '{from}';";

        private static string PoolSql(uint pool, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE spawnpool SET {Set(to)} WHERE id = {pool} AND map_context_id = {Outpost} AND {Near(from)};";

        private static string PadSql(uint pad, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE teleporter SET {Set(to)} WHERE id = {pad} AND map_context_id = {Outpost} AND type = {AshenAvernusOutpostNpcsV1.LocalTeleporter} AND {Near(from)};";

        private static string AreaSql(uint mission, uint area, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_area SET {Set(to)} WHERE mission_id = {mission} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND area_id = {area} AND map_context_id = {Outpost} AND {Near(from)};";

        private static string MarkerSql(uint mission, uint objective, uint indicator, (double X, double Y, double Z) to, (double X, double Y, double Z) from) =>
            $"UPDATE mission_indicator SET {Set(to)} WHERE mission_id = {mission} AND content_revision = '{AshenMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND indicator_id = {indicator} AND {Near(from)};";
    }
}

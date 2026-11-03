using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Rasa.Missions.Content;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Pools missions played in a squad instance - the Turpis Refinery (1429), the Lamna Armory (1465) and the
    /// Retread Caves (1694) - had their scene's MapContextId (world effects refused off that map) but not its
    /// BoundMapContextId, which the Plateau's instance missions have. Unbound, a run stands only on the copy it was
    /// started in (SceneApplication.CanStandOn: the row's map key, map and instance id): after a server restart, or in
    /// a squad's later copy, the squad instance comes back under a new instance id, the run stays on the old key, and
    /// nothing of the scene is put in the copy. Operation Overlord (898, playtest 2026-11-01): the keys were looted
    /// after a restart, but the key slots and the switch were not in the copy, so the mission could not go on.
    ///
    /// Bound to its instance's map, a run goes with its owner to any copy of it, and a mission taken outside waits for
    /// its owner to come in, as the Plateau's do (MissionSceneDefinition.BoundMapContextId).
    ///
    /// Data only: each scene is the one its V1 wrote (taken from what the V1's Up would insert), with the bound map
    /// set; Down writes it back without. The missions' revision does not change, so the missions held stay as they are.
    /// </summary>
    public static class PoolsInstanceScenesBoundV1
    {
        public static readonly (uint MissionId, uint MapContextId, Action<MigrationBuilder> Up)[] Missions =
        {
            (PoolsMadBomberV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsMadBomberV1.Up),
            (PoolsThingsThatBlowUpV1.MissionId, PoolsTurpisRefineryNpcsV1.MapContextId, PoolsThingsThatBlowUpV1.Up),
            (PoolsHardCoreV1.MissionId, PoolsTurpisRefineryNpcsV1.MapContextId, PoolsHardCoreV1.Up),
            (PoolsChemistry101V1.MissionId, PoolsTurpisRefineryNpcsV1.MapContextId, PoolsChemistry101V1.Up),
            (PoolsTheBadDoctorV1.MissionId, PoolsTurpisRefineryNpcsV1.MapContextId, PoolsTheBadDoctorV1.Up),
            (PoolsBaneBattlePlansV1.MissionId, PoolsLamnaArmoryNpcsV1.MapContextId, PoolsBaneBattlePlansV1.Up),
            (PoolsOperationOverlordV1.MissionId, PoolsLamnaArmoryNpcsV1.MapContextId, PoolsOperationOverlordV1.Up),
            (PoolsBlowThemAllUpV1.MissionId, PoolsLamnaArmoryNpcsV1.MapContextId, PoolsBlowThemAllUpV1.Up),
            (PoolsCostOfDoingBusinessV1.MissionId, PoolsTurpisRefineryNpcsV1.MapContextId, PoolsCostOfDoingBusinessV1.Up),
            (PoolsMiasmaMutationsV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsMiasmaMutationsV1.Up),
            (PoolsMoxOnBothOurHousesV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsMoxOnBothOurHousesV1.Up),
            (PoolsDeadlyMoxinsV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsDeadlyMoxinsV1.Up),
            (PoolsMoxShouldntSmokeV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsMoxShouldntSmokeV1.Up),
            (PoolsMiasmaTestingV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsMiasmaTestingV1.Up),
            (PoolsTotalSlaughterV1.MissionId, PoolsRetreadCavesNpcsV1.MapContextId, PoolsTotalSlaughterV1.Up),
        };

        /// <summary>The scene a mission's V1 writes: the bindings of its mission_scene_binding insert.</summary>
        public static MissionSceneDefinition SceneOf(Action<MigrationBuilder> up, uint missionId, string provider)
        {
            var scratch = new MigrationBuilder(provider);
            up(scratch);
            var insert = scratch.Operations.OfType<InsertDataOperation>().Single(operation => operation.Table == "mission_scene_binding");
            var column = Array.IndexOf(insert.Columns, "bindings");
            var mission = Array.IndexOf(insert.Columns, "mission_id");
            if (Convert.ToUInt32(insert.Values[0, mission]) != missionId)
                throw new InvalidOperationException($"mission {missionId}: its V1 writes the scene of {insert.Values[0, mission]}");
            return JsonSerializer.Deserialize<MissionSceneDefinition>((string)insert.Values[0, column], MissionContentCodec.Options);
        }

        public static MissionSceneDefinition Bound(uint missionId, string provider)
        {
            var (_, map, up) = Missions.Single(entry => entry.MissionId == missionId);
            var scene = SceneOf(up, missionId, provider);
            if (scene.MapContextId != map)
                throw new InvalidOperationException($"mission {missionId}: its scene is on map {scene.MapContextId}, not {map}");
            scene.BoundMapContextId = map;
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (missionId, _, _) in Missions)
                MissionDataMigration.UpdateSceneBindings(migration, missionId, PoolsMissionDataV1.Revision, Bound(missionId, migration.ActiveProvider));
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var (missionId, _, up) in Missions)
                MissionDataMigration.UpdateSceneBindings(migration, missionId, PoolsMissionDataV1.Revision, SceneOf(up, missionId, migration.ActiveProvider));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// To-fix #55: in a squad's copy of Maligo Base a killed pool comes back only after the copy's respawn
    /// (SquadInstanceState, 45 minutes), not after its own respawn time. Squad Leader Altera (pool 750083) is Coup's
    /// (878) talk NPC for objectives 1 and 5 (package 980) and its receiver: a squad member who comes in after she was
    /// killed found nobody to talk to.
    ///
    /// As Zyrutha and Kythura (PlateauMaligoOfficerSquadRespawnV1, to-fix #18): Coup's scene names her pool as a
    /// public-spawn actor (<see cref="AlteraRole"/>); a pool a mission scene names so comes back on its own respawn time
    /// in a squad copy (SpawnPoolManager.ReturnsOnOwnTime). The actor is only named: no sequence uses it, so her talks,
    /// her turn-in and her pool are as before.
    ///
    /// To-fix #55 (second part): to-fix #16 (PlateauContactsOutOfAggroV1) put her at (-139, -96.78, 137), 1.9 m off the
    /// navmesh (nearest polygon at -137.1, -96.4, 137) inside the side of the room's ramp. The client has no floor there:
    /// a player sent there fell through to the level below, and from beside her the use gave "You cannot see your target
    /// from here". She moves to <see cref="AlteraAt"/>, 3 m south-east: on the navmesh (region 1, the room's), with the
    /// client's floor of the room piece (arch_bane_industrial_room_control_24m_v06) under it, and the place the playtest
    /// stood on. 21.8 m (flat) from Bane pool 531633's ground (radius 5 at -111.6, 129.1) and 26.8 m from Overseer
    /// Kythura's (750089), over the aggro range (18 m). Her markers (878 objectives 1 and 5) move with her.
    ///
    /// Data only: the scene binding of 878 written again, her pool and markers moved. Down puts back the scene without
    /// the actor (PlateauCoupV1.Scene) and her place of PlateauContactsOutOfAggroV1.
    /// </summary>
    public static class PlateauCoupAlteraRespawnV1
    {
        private const string Revision = PlateauMissionDataV1.Revision;

        public const string AlteraRole = "altera";

        public static readonly (double X, double Y, double Z) AlteraAt = (-136.5, -95.7, 139);

        public static MissionSceneDefinition Coup()
        {
            var scene = PlateauCoupV1.Scene();
            scene.Actors[AlteraRole] = new SceneActorDefinition(AlteraRole, SceneActorKind.PublicSpawn, PlateauMaligoBaseV1.Altera);
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, PlateauCoupV1.MissionId, Revision, Coup());
            Place(migration, AlteraAt);
        }

        public static void Down(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateSceneBindings(migration, PlateauCoupV1.MissionId, Revision, PlateauCoupV1.Scene());
            Place(migration, PlateauContactsOutOfAggroV1.AlteraAt);
        }

        private static void Place(MigrationBuilder migration, (double X, double Y, double Z) at)
        {
            migration.Sql($"UPDATE spawnpool SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} WHERE id = {PlateauMaligoBaseV1.Altera};");
            foreach (var (mission, indicator) in PlateauContactsOutOfAggroV1.AlteraMarkers)
                migration.Sql($"UPDATE mission_indicator SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} WHERE mission_id = {mission} " +
                    $"AND content_revision = '{Revision}' AND indicator_id = {MissionIndicator.UnnamedFrom + indicator};");
        }
    }
}

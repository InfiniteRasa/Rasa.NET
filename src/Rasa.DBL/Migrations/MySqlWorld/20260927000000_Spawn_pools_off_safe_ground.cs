using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// The mined spawn areas kept off safe ground.
    ///
    /// 50 of the pools Add_spawn_areas and Add_world_spawn_areas placed sit on a control point:
    /// within 30 m of the point's marker, or of a hospital, token banker, prestige vendor or
    /// waypoint the client labels "(Control Point)". At a control point the Bane camp is the
    /// point itself, and those services are what replaces it once AFS takes it - the server
    /// already seeds them, so the two stood together. Mode 1 (control point): dormant until the
    /// server has control point ownership, the garrison of a point AFS holds.
    ///
    /// 5 more had their centre on a friendly NPC: the Torcastra Prison field medic, Purgas
    /// Station's Level 01 control room medic, Doctor Splicer in Cuthah Base's bioresearch wing,
    /// the medic at the Ojasa hive entrance and the Kardash colony's medical vendor. Moving one
    /// blind inside an instance could put it in a wall: mode 2 (scripted), not spawned
    /// automatically, left to place by hand.
    ///
    /// 4 came within 15 m of a friendly NPC or a hospital and have their radius pulled back so
    /// the edge keeps 15 m.
    ///
    /// spawn_safety.csv lists every one with the marker or NPC that decided it; gen_spawn_safety.py
    /// made it. SpawnPoolManager.ValidatePools reports any pool that is still too close at startup.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Spawn_pools_off_safe_ground : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // control points: the Bane camp is the point itself, AFS's services replace it (50 pools)
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set mode = 1 where id in (530001, 530027, 531003, 531004, 531033, 531098, 531136, 531137, 531143, 531145, 531146, 531151, 531152, 531161, 531164, 531168, 531171, 531187, 531202, 531203, 531204, 531235, 531238, 531247, 531279, 531284, 531287, 531381, 531385, 531395, 531462, 531463, 531466, 531469, 531470, 531472, 531477, 531478, 531543, 531608, 531609, 531661, 531664, 531695, 531700, 531714, 531716, 531717, 531720, 531726);");

            // their centre is a place players revive at or arrive on; not moved blind (5 pools)
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set mode = 2 where id in (530039, 530060, 531223, 531256, 531590);");

            // the area pulled back to keep 15 m clear of a friendly NPC, hospital or waypoint (4 pools)
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 5.9 where id = 531021;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 32.1 where id = 531210;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 18.0 where id = 531291;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 9.1 where id = 531659;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set mode = 0 where id in (530001, 530027, 531003, 531004, 531033, 531098, 531136, 531137, 531143, 531145, 531146, 531151, 531152, 531161, 531164, 531168, 531171, 531187, 531202, 531203, 531204, 531235, 531238, 531247, 531279, 531284, 531287, 531381, 531385, 531395, 531462, 531463, 531466, 531469, 531470, 531472, 531477, 531478, 531543, 531608, 531609, 531661, 531664, 531695, 531700, 531714, 531716, 531717, 531720, 531726, 530039, 530060, 531223, 531256, 531590);");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 15.2 where id = 531021;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 39.9 where id = 531210;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 20.1 where id = 531291;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set radius = 12.6 where id = 531659;");
        }
    }
}

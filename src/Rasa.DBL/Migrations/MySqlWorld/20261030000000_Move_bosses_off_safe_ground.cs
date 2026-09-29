using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Five boss pools that Add_boss_spawns put on safe ground, moved to where their missions send
    /// the player. Each had fallen back on a map label, and the label was the friendly post, NPC or
    /// hospital that shares the boss's place name; SpawnPoolManager.ValidatePools reported all five.
    ///
    ///  - 520020 Master Gas Harvester Phuumz sat on Recon Commander McReddy (Dia Toma region
    ///    label). "Go north of the recon squad, into the devastated Forean village of Dia Toma":
    ///    the Dia Toma POI label, 44 m north.
    ///  - 520023 Overseer Nyxroq sat 11 m from the Nyxroq Post trainer (Nyxroq Post POI label).
    ///    Foletto: "a pill box about a half click from here", with the power supply encampment
    ///    near it. The one-prop Bane post (531239) on the edge of the Thrax camp (531233) north-west
    ///    of Nyxroq Trench, 300 m from Foletto. The mission names no place; this one is inferred.
    ///  - 520038 Fithik Hive Master sat on Hospital: Devil's Den. The client's "Hive Master
    ///    Hatchery" POI label, beside the Fithik lair (531518).
    ///  - 520048 Overseer Quarm sat on the Irendas Colony military surplus vendor. "The southern
    ///    most mining facility", south of Mt. Hellas Outpost: Ten Ton Hammer's Mount Hellas guide
    ///    gives -984.9, 431.0, -218.8, west of the lava bridge by the Bane Drill Control Outpost,
    ///    which lands on the navmesh.
    ///  - 520062 Overseer Prysiam sat on Corporal Orton (Obelisk POI label). "Somewhere near the
    ///    Obelisk itself": the centre of the Thrax camp (531670) 54 m from it.
    ///
    /// Every new position is on the walkable navmesh, at least 44 m from friendly NPCs, hospitals
    /// and waypoints and outside every turret's scan.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Move_bosses_off_safe_ground : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = 388.3, pos_y = 212.2, pos_z = 152.6 where id = 520020;");   // Phuumz: Dia Toma village
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -464.0, pos_y = 254.6, pos_z = -61.0 where id = 520023;");  // Nyxroq: the pillbox by the camp
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -80.3, pos_y = 170.0, pos_z = 327.5 where id = 520038;");   // Hive Master: Hive Master Hatchery
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -984.9, pos_y = 431.4, pos_z = -218.8 where id = 520048;"); // Quarm: the southern mining facility
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -837.2, pos_y = 791.2, pos_z = -14.3 where id = 520062;");  // Prysiam: the camp by the Obelisk
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = 382.4, pos_y = 216.1, pos_z = 108.7 where id = 520020;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -185.3, pos_y = 234.9, pos_z = -226.6 where id = 520023;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = 14.6, pos_y = 107.0, pos_z = -376.5 where id = 520038;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = 375.0, pos_y = 448.6, pos_z = -91.0 where id = 520048;");
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set pos_x = -859.6, pos_y = 791.2, pos_z = 35.2 where id = 520062;");
        }
    }
}

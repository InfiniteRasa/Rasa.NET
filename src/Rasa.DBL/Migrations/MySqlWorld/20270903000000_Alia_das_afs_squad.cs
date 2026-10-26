using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// What a squad's route and spawn say about how it goes (NpcSquads): a point's leg run or
    /// walked (npc_squad_path_point.run) and how far the squad looks for a fight going to it and
    /// at it (engage_range, 0 for its type's); a spawn's arrival, there or teleported in
    /// (npc_squad_spawn.arrival). All 0 for the rows there are, as they went before.
    ///
    /// And the first squad: Alia Das's AFS infantry, teleported in on the waypoint pad and sent
    /// down to the Lower Eloh Creek valley to fight the Bane there (AliaDasAfsSquad): three
    /// creature rows, the squad type and its members, the route and the spawn.
    ///
    /// Down takes the rows out and drops the columns.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270903000000_Alia_das_afs_squad")]
    public class Alia_das_afs_squad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "run",
                table: NpcSquadPathPointEntry.TableName,
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<double>(
                name: "engage_range",
                table: NpcSquadPathPointEntry.TableName,
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<byte>(
                name: "arrival",
                table: NpcSquadSpawnEntry.TableName,
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            foreach (var sql in AliaDasAfsSquad.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasAfsSquad.Down())
                migrationBuilder.Sql(sql);

            migrationBuilder.DropColumn(name: "arrival", table: NpcSquadSpawnEntry.TableName);
            migrationBuilder.DropColumn(name: "engage_range", table: NpcSquadPathPointEntry.TableName);
            migrationBuilder.DropColumn(name: "run", table: NpcSquadPathPointEntry.TableName);
        }
    }
}

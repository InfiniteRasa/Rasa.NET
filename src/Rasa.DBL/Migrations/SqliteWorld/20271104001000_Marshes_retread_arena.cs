using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 11, the Retread Arena: Retread Karl, Retread Foster, the five arena fighters and a Forean Machina
    /// pool and the dome's way-out teleporter pads (MarshesRetreadArenaNpcsV1), with Beat Street, Smackdown and Legend or Fall (1085, 1087, 1088,
    /// MarshesRetreadArenaV1) and Calling all Parts (1084, MarshesCallingAllPartsV1). Data only.
    /// </summary>
    public partial class Marshes_retread_arena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesRetreadArenaNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesRetreadArenaV1.Up(migrationBuilder);
            MarshesCallingAllPartsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesCallingAllPartsV1.Down(migrationBuilder);
            MarshesRetreadArenaV1.Down(migrationBuilder);
            foreach (var sql in MarshesRetreadArenaNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}

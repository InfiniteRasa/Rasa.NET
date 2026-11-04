using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 3, Harvesters: Lieutenant Brazos at Paludos (MarshesHarvestersNpcsV1), with Researcher Vetter's
    /// Reaping the Harvest (881, MarshesReapingTheHarvestV1) and what follows it: Retread Kline's Hassling the Harvesters
    /// (890, MarshesHasslingTheHarvestersV1), Brazos's Beware: Buried Gas Lines (913, MarshesBuriedGasLinesV1) and Ranger
    /// Taavik's A Butcher, a Baker, and a Caretaker (968, MarshesButcherBakerCaretakerV1). Data only.
    /// </summary>
    public partial class Marshes_harvesters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesHarvestersNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesReapingTheHarvestV1.Up(migrationBuilder);
            MarshesHasslingTheHarvestersV1.Up(migrationBuilder);
            MarshesBuriedGasLinesV1.Up(migrationBuilder);
            MarshesButcherBakerCaretakerV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesButcherBakerCaretakerV1.Down(migrationBuilder);
            MarshesBuriedGasLinesV1.Down(migrationBuilder);
            MarshesHasslingTheHarvestersV1.Down(migrationBuilder);
            MarshesReapingTheHarvestV1.Down(migrationBuilder);
            foreach (var sql in MarshesHarvestersNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}

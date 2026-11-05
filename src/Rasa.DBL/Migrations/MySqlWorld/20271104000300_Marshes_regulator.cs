using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 4, the Regulator: Retread Wilson, Conscript Kaav and a Strider pool (MarshesRegulatorNpcsV1), with
    /// Retread McCormick's The Regulator: Part I (925, MarshesRegulatorPartOneV1) and the three that follow it: Wilson's Part II
    /// (1028, MarshesRegulatorPartTwoV1), Kaav's Part III (1029, MarshesRegulatorPartThreeV1) and Wilson's Part IV (1030,
    /// MarshesRegulatorPartFourV1). Data only.
    /// </summary>
    public partial class Marshes_regulator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesRegulatorNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesRegulatorPartOneV1.Up(migrationBuilder);
            MarshesRegulatorPartTwoV1.Up(migrationBuilder);
            MarshesRegulatorPartThreeV1.Up(migrationBuilder);
            MarshesRegulatorPartFourV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesRegulatorPartFourV1.Down(migrationBuilder);
            MarshesRegulatorPartThreeV1.Down(migrationBuilder);
            MarshesRegulatorPartTwoV1.Down(migrationBuilder);
            MarshesRegulatorPartOneV1.Down(migrationBuilder);
            foreach (var sql in MarshesRegulatorNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}

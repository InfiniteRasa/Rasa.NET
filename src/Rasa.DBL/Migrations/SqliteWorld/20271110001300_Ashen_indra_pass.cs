using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 13, Indra Pass: Surveyor Malya Odesha and Researcher Endala Berish new by the Indra Pass waypoint
    /// (AshenIndraPassNpcsV1), with Missing Scientists (1280, AshenMissingScientistsV1) and its two exclusive follow-ups God of
    /// Storms (1281, AshenGodOfStormsV1) and The Bonds of Blood (1282, AshenBondsOfBloodV1).
    /// </summary>
    public partial class Ashen_indra_pass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenIndraPassNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenMissingScientistsV1.Up(migrationBuilder);
            AshenGodOfStormsV1.Up(migrationBuilder);
            AshenBondsOfBloodV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenBondsOfBloodV1.Down(migrationBuilder);
            AshenGodOfStormsV1.Down(migrationBuilder);
            AshenMissingScientistsV1.Down(migrationBuilder);
            foreach (var sql in AshenIndraPassNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

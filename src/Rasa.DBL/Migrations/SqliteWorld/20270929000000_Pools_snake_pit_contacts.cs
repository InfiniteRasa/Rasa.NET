using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Spymaster Timlin, Specialist Allen and Lieutenant Feldman (PoolsSnakePitContactsV1), with Looking For Logos? (1308,
    /// PoolsLookingForLogosV1), Mapping The Pools (1025, PoolsMappingThePoolsV1) and Bane Investigation (932,
    /// PoolsBaneInvestigationV1). Data only.
    /// </summary>
    public partial class Pools_snake_pit_contacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsSnakePitContactsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsLookingForLogosV1.Up(migrationBuilder);
            PoolsMappingThePoolsV1.Up(migrationBuilder);
            PoolsBaneInvestigationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsBaneInvestigationV1.Down(migrationBuilder);
            PoolsMappingThePoolsV1.Down(migrationBuilder);
            PoolsLookingForLogosV1.Down(migrationBuilder);
            foreach (var sql in PoolsSnakePitContactsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

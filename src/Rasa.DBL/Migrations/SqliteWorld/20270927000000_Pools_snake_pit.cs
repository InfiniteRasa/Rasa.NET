using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Snake Pit's Supply Chief Riggins and Dr. Hester (PoolsSnakePitNpcsV1), Riggins's Building Materials (1314,
    /// PoolsBuildingMaterialsV1) and Hester's Medicinal Measures (1066, PoolsMedicinalMeasuresV1). Data only.
    /// </summary>
    public partial class Pools_snake_pit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsSnakePitNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsBuildingMaterialsV1.Up(migrationBuilder);
            PoolsMedicinalMeasuresV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsMedicinalMeasuresV1.Down(migrationBuilder);
            PoolsBuildingMaterialsV1.Down(migrationBuilder);
            foreach (var sql in PoolsSnakePitNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

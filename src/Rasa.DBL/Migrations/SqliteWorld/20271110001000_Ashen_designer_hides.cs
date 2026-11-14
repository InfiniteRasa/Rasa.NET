using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 10, Ida Hayner's orders: the bosses Sally, Molly, Trudy and Wendy and the Lasher, Beam Manta and
    /// Granitour pools new, Ida Hayner moved off the weapon vendor (AshenDesignerHidesNpcsV1), with her Designer Jewelry Gone
    /// Wrong (1287, AshenDesignerJewelryV1), All the Latest Rage! (1293, AshenLatestRageV1), I Wouldn't Wear It, But... (1289,
    /// AshenWouldntWearItV1) and A New Fad (1291, AshenNewFadV1).
    /// </summary>
    public partial class Ashen_designer_hides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenDesignerHidesNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenDesignerJewelryV1.Up(migrationBuilder);
            AshenLatestRageV1.Up(migrationBuilder);
            AshenWouldntWearItV1.Up(migrationBuilder);
            AshenNewFadV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenNewFadV1.Down(migrationBuilder);
            AshenWouldntWearItV1.Down(migrationBuilder);
            AshenLatestRageV1.Down(migrationBuilder);
            AshenDesignerJewelryV1.Down(migrationBuilder);
            foreach (var sql in AshenDesignerHidesNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 3, the Sheitan Highway: the Thrax Ravager name on the Ashen Thrax Grenadier 531009, Thrax Ravager
    /// and Thrax Soldier pools by the Bane camp on the highway, the Desert Predator new on two beats along the road, and the
    /// highway's four Bane Mortar pools each given its own copy of the mortar (AshenSheitanHighwayNpcsV1), with Captain Sera
    /// Karter's Highway to Hell (1247, AshenHighwayToHellV1), Lt. Colonel Trent Ivan's Air Superiority (1248,
    /// AshenAirSuperiorityV1) and Lt. Colonel Dania Caldwell's Artillery Annihilation (1249, AshenArtilleryAnnihilationV1).
    /// Data only, plain SQL (CrucibleSql).
    /// </summary>
    public partial class Ashen_sheitan_highway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenSheitanHighwayNpcsV1.Up())
                migrationBuilder.Sql(AshenSheitanHighwayNpcsV1.Guarded(sql));
            AshenHighwayToHellV1.Up(migrationBuilder);
            AshenAirSuperiorityV1.Up(migrationBuilder);
            AshenArtilleryAnnihilationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenArtilleryAnnihilationV1.Down(migrationBuilder);
            AshenAirSuperiorityV1.Down(migrationBuilder);
            AshenHighwayToHellV1.Down(migrationBuilder);
            foreach (var sql in AshenSheitanHighwayNpcsV1.Down())
                migrationBuilder.Sql(AshenSheitanHighwayNpcsV1.Guarded(sql));
        }
    }
}

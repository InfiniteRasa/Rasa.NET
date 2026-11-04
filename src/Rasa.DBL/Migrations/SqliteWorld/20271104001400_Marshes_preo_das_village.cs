using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 15, P'reo Das (part 2): Forean Warrior Tola, Forean Elder Kirii, Forean Shaman Rakla and the Forean
    /// Prisoners of the three camps in the Village Ruins (MarshesPreoDasVillageNpcsV1), with Never Surrender (891,
    /// MarshesNeverSurrenderV1), Picking Up the Pieces (892, MarshesPickingUpThePiecesV1) and Cleaning House (894,
    /// MarshesCleaningHouseV1). Data only.
    /// </summary>
    public partial class Marshes_preo_das_village : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesPreoDasVillageNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesNeverSurrenderV1.Up(migrationBuilder);
            MarshesPickingUpThePiecesV1.Up(migrationBuilder);
            MarshesCleaningHouseV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesCleaningHouseV1.Down(migrationBuilder);
            MarshesPickingUpThePiecesV1.Down(migrationBuilder);
            MarshesNeverSurrenderV1.Down(migrationBuilder);
            foreach (var sql in MarshesPreoDasVillageNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}

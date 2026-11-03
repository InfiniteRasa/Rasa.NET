using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 16, the Bane Supply Depot: C-6801 Copilot Ortega at the Survivor's Camp (MarshesBaneSupplyDepotNpcsV1),
    /// with Survivors (664, MarshesSurvivorsV1), Beinvenidos del Voro (919, MarshesBeinvenidosDelVoroV1) and Insertion Point
    /// (663, MarshesInsertionPointV1). Data only.
    /// </summary>
    public partial class Marshes_bane_supply_depot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesBaneSupplyDepotNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesSurvivorsV1.Up(migrationBuilder);
            MarshesBeinvenidosDelVoroV1.Up(migrationBuilder);
            MarshesInsertionPointV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesInsertionPointV1.Down(migrationBuilder);
            MarshesBeinvenidosDelVoroV1.Down(migrationBuilder);
            MarshesSurvivorsV1.Down(migrationBuilder);
            foreach (var sql in MarshesBaneSupplyDepotNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

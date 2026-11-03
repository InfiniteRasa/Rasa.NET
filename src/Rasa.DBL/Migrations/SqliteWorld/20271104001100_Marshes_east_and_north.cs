using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 12, east and north: Elder Bargas, two Forean Machina pools, the Kael prison caravan (a squad),
    /// Lieutenant Shaw with the creatures of his defence and Ranger Meeko (MarshesEastAndNorthNpcsV1), with Bring Down the
    /// Kael (542, MarshesBringDownTheKaelV1), Bringing the Spirit Home (864, MarshesBringingTheSpiritHomeV1), Head East, Young
    /// Man (920, MarshesHeadEastV1) and Soyuz Salvage: Marshes (2009, MarshesSoyuzSalvageV1). Data only.
    /// </summary>
    public partial class Marshes_east_and_north : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesEastAndNorthNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesBringDownTheKaelV1.Up(migrationBuilder);
            MarshesBringingTheSpiritHomeV1.Up(migrationBuilder);
            MarshesHeadEastV1.Up(migrationBuilder);
            MarshesSoyuzSalvageV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesSoyuzSalvageV1.Down(migrationBuilder);
            MarshesHeadEastV1.Down(migrationBuilder);
            MarshesBringingTheSpiritHomeV1.Down(migrationBuilder);
            MarshesBringDownTheKaelV1.Down(migrationBuilder);
            foreach (var sql in MarshesEastAndNorthNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

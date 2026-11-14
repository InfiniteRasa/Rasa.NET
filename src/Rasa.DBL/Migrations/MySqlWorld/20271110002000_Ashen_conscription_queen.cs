using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 20, the Bane Conscription Facility (squad instance 1988), the Atta queen part: the Enraged Atta,
    /// the Atta Hive Queen, Enslaver Xahn, Lt. Humphries, Lukaz, the Brann Prisoner, Commander Jawan (Thunderhead) and two Bane
    /// teleporter pairs new (AshenConscriptionQueenNpcsV1), with What Makes Them Tick (1551, AshenWhatMakesThemTickV1), Into
    /// the Lion's Den (1499, AshenIntoTheLionsDenV1) and Save the Queen (1501, AshenSaveTheQueenV1). Sorts after set 19
    /// (20271110001900). It updates no row it did not insert itself.
    /// </summary>
    public partial class Ashen_conscription_queen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenConscriptionQueenNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            foreach (var sql in AshenConscriptionQueenNpcsV1.PadsUp())
                migrationBuilder.Sql(sql);
            AshenWhatMakesThemTickV1.Up(migrationBuilder);
            AshenIntoTheLionsDenV1.Up(migrationBuilder);
            AshenSaveTheQueenV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenSaveTheQueenV1.Down(migrationBuilder);
            AshenIntoTheLionsDenV1.Down(migrationBuilder);
            AshenWhatMakesThemTickV1.Down(migrationBuilder);
            foreach (var sql in AshenConscriptionQueenNpcsV1.PadsDown())
                migrationBuilder.Sql(sql);
            foreach (var sql in AshenConscriptionQueenNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

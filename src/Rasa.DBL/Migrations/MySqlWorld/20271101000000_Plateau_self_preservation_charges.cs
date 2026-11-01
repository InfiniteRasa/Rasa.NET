using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Self Preservation (879) with an Explosive Charge beside each Security CPU (PlateauSelfPreservationV2): the real
    /// client does not take the CPUs as weapon targets (to-fix #54), so each room's switch makes its charge usable and the
    /// charge's use blows the CPU and counts it. The mission is written again (its rows hold the shot CPUs), and with it
    /// the fields' take-downs on the switches' objectives (PlateauSelfPreservationFieldsV1), which go with the mission's
    /// rows. Down writes the first mission (PlateauSelfPreservationV1) again. Data only.
    /// </summary>
    public partial class Plateau_self_preservation_charges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSelfPreservationFieldsV1.Down())
                migrationBuilder.Sql(sql);
            PlateauSelfPreservationV1.Down(migrationBuilder);
            PlateauSelfPreservationV2.Up(migrationBuilder);
            foreach (var sql in PlateauSelfPreservationFieldsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSelfPreservationFieldsV1.Down())
                migrationBuilder.Sql(sql);
            PlateauSelfPreservationV2.Down(migrationBuilder);
            PlateauSelfPreservationV1.Up(migrationBuilder);
            foreach (var sql in PlateauSelfPreservationFieldsV1.Up())
                migrationBuilder.Sql(sql);
        }
    }
}

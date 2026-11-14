using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 19, the Bane Conscription Facility (squad instance 1988), the entry and the biolab: Capt.
    /// Peacock, Slocombe and the Bane teleporter pads new (AshenConscriptionEntryNpcsV1; the front force field is each
    /// member's own 1497 scene object, no world_destructible row), with
    /// Lockdown the Conscription Facility (1500, AshenLockdownTheConscriptionFacilityV1), Raid the Conscription Facility (1497,
    /// AshenRaidTheConscriptionFacilityV1) and Blast the Biolab (1498, AshenBlastTheBiolabV1). Sorts after set 18
    /// (20271110001800). It updates no row it did not insert itself.
    /// </summary>
    public partial class Ashen_conscription_entry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenConscriptionEntryNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            foreach (var sql in AshenConscriptionEntryNpcsV1.PadsUp())
                migrationBuilder.Sql(sql);
            AshenLockdownTheConscriptionFacilityV1.Up(migrationBuilder);
            AshenRaidTheConscriptionFacilityV1.Up(migrationBuilder);
            AshenBlastTheBiolabV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenBlastTheBiolabV1.Down(migrationBuilder);
            AshenRaidTheConscriptionFacilityV1.Down(migrationBuilder);
            AshenLockdownTheConscriptionFacilityV1.Down(migrationBuilder);
            foreach (var sql in AshenConscriptionEntryNpcsV1.PadsDown())
                migrationBuilder.Sql(sql);
            foreach (var sql in AshenConscriptionEntryNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

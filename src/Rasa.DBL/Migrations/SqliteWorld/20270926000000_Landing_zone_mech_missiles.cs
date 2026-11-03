using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Landing Zone's AFS Mech Defenders given the Mech's missiles beside its minigun
    /// (LandingZoneMechMissiles). Data only, in plain SQL: no model of its own.
    ///
    /// It was 20270925000000, the timestamp Gate_field_nooks has. A World that ran it under that
    /// id runs it again under this one, which the guard on the empty slot leaves as it is, and
    /// its history row for the old id is removed.
    ///
    /// Down takes the missiles off them again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270926000000_Landing_zone_mech_missiles")]
    public class Landing_zone_mech_missiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneMechMissiles.Up())
                migrationBuilder.Sql(sql);

            migrationBuilder.Sql(LandingZoneMechMissiles.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneMechMissiles.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

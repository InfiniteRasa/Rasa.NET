using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 6, the AWOL Camp: Awol Captain Cheung, Awol Lieutenant Deirdre, Lieutenant Sherer and Intelligence
    /// Officer D'Ambrose moved out of the upstream placeholder clusters (CrucibleAwolCampNpcsV1), with Sherer's Calendar Girl
    /// (1144, CrucibleCalendarGirlV1), D'Ambrose's Stray Cats (1152, CrucibleStrayCatsV1), Breiburg's The Other Side Of The
    /// Coin (1153, CrucibleOtherSideOfTheCoinV1), Making Amends (1155) and Sweet Revenge (1205, CrucibleMakingAmendsV1). Data
    /// only, plain SQL.
    /// </summary>
    public partial class Crucible_awol_camp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleAwolCampNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleCalendarGirlV1.Up(migrationBuilder);
            CrucibleStrayCatsV1.Up(migrationBuilder);
            CrucibleOtherSideOfTheCoinV1.Up(migrationBuilder);
            CrucibleMakingAmendsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleMakingAmendsV1.Down(migrationBuilder);
            CrucibleOtherSideOfTheCoinV1.Down(migrationBuilder);
            CrucibleStrayCatsV1.Down(migrationBuilder);
            CrucibleCalendarGirlV1.Down(migrationBuilder);
            foreach (var sql in CrucibleAwolCampNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

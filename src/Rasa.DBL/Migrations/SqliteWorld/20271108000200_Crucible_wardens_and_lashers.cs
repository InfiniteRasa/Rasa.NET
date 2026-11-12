using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 3, wardens and lashers: Medical Technician Halprin, the Warden Bots by the Chaukas Robotics
    /// Facility's door and the Lashers north-east of the Awol Camp (CrucibleWardensAndLashersNpcsV1), with Viddia Grupa
    /// Donal's Game Wardens (1147, CrucibleGameWardensV1), Halprin's Whiplash (1157, CrucibleWhiplashV1) and Kappa Grupa
    /// Lohen's Earning Respect (1170, CrucibleEarningRespectV1) and Business Proposal (1229, CrucibleBusinessProposalV1).
    /// Data only, plain SQL.
    /// </summary>
    public partial class Crucible_wardens_and_lashers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleWardensAndLashersNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleGameWardensV1.Up(migrationBuilder);
            CrucibleWhiplashV1.Up(migrationBuilder);
            CrucibleEarningRespectV1.Up(migrationBuilder);
            CrucibleBusinessProposalV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleBusinessProposalV1.Down(migrationBuilder);
            CrucibleEarningRespectV1.Down(migrationBuilder);
            CrucibleWhiplashV1.Down(migrationBuilder);
            CrucibleGameWardensV1.Down(migrationBuilder);
            foreach (var sql in CrucibleWardensAndLashersNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

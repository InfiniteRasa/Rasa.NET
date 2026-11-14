using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 5, the Staal Sigals: Viddia Sigal Nema, Top Khana Sigal Pico and Trista in Staal, two more
    /// Reconstructor Bot pools by the Chaukas Robotics Facility, Bane Shield Drones north of Khulago Base and Preceptor Molitor
    /// with his pack north-east of the Awol Camp (CrucibleStaalSigalsNpcsV1), with Nema's Architects Of Destruction (1202,
    /// CrucibleArchitectsOfDestructionV1), Pico's Out Of Sight (1208, CrucibleOutOfSightV1) and Trista's Irreconcilable
    /// Differences (1185, CrucibleIrreconcilableDifferencesV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_staal_sigals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleStaalSigalsNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleArchitectsOfDestructionV1.Up(migrationBuilder);
            CrucibleOutOfSightV1.Up(migrationBuilder);
            CrucibleIrreconcilableDifferencesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleIrreconcilableDifferencesV1.Down(migrationBuilder);
            CrucibleOutOfSightV1.Down(migrationBuilder);
            CrucibleArchitectsOfDestructionV1.Down(migrationBuilder);
            foreach (var sql in CrucibleStaalSigalsNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

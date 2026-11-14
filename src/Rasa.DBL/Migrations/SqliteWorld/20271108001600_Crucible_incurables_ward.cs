using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 11a, the Incurables Ward: Mechanic Shanul, Engineer Dhimand, the Incurables, Jaikev and Parvatee, with
    /// Supervisor Sheira and Larai Sigal Tempa moved (CrucibleIncurablesWardNpcsV1), with Nema's Sadistic Sabotage (1445,
    /// CrucibleSadisticSabotageV1), Sheira's Lost in the Asylum (1444, CrucibleLostInTheAsylumV1) and Tempa's Rehabilitation at
    /// Gunpoint (1402, CrucibleRehabilitationAtGunpointV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_incurables_ward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleIncurablesWardNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleSadisticSabotageV1.Up(migrationBuilder);
            CrucibleLostInTheAsylumV1.Up(migrationBuilder);
            CrucibleRehabilitationAtGunpointV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleRehabilitationAtGunpointV1.Down(migrationBuilder);
            CrucibleLostInTheAsylumV1.Down(migrationBuilder);
            CrucibleSadisticSabotageV1.Down(migrationBuilder);
            foreach (var sql in CrucibleIncurablesWardNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 8b, Khulago Base for Larai Zupa Madias at Prometheus Outpost: the Bane Juggernauts and Maxxum Ugoretz
    /// (CruciblePrometheusOutpostNpcsV1), with Iron Giants (1213, CrucibleIronGiantsV1), Who's In Charge Here? (1215,
    /// CrucibleWhosInChargeHereV1) and The Big Kahuna (1216, CrucibleBigKahunaV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_prometheus_outpost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CruciblePrometheusOutpostNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleIronGiantsV1.Up(migrationBuilder);
            CrucibleWhosInChargeHereV1.Up(migrationBuilder);
            CrucibleBigKahunaV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleBigKahunaV1.Down(migrationBuilder);
            CrucibleWhosInChargeHereV1.Down(migrationBuilder);
            CrucibleIronGiantsV1.Down(migrationBuilder);
            foreach (var sql in CruciblePrometheusOutpostNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

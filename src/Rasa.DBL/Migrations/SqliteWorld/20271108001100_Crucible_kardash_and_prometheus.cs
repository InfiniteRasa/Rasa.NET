using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 8a, the Kardash obelisk and Prometheus Outpost: Professor Tiernan (Crucible) and Captain Amandla Botha
    /// (Ashen Desert), Major Keplinger and Larai Zupa Madias moved off the Prometheus Outpost control point
    /// (CrucibleKardashAndPrometheusNpcsV1), with DeLisi's Prometheus Rising (1210, CruciblePrometheusRisingV1), Madias'
    /// Confirmed Kills (1212, CrucibleConfirmedKillsV1), Cheung's Point Of Impact (1217, CruciblePointOfImpactV1) and the
    /// obelisk's It Came From Erdas (1218, CrucibleItCameFromErdasV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_kardash_and_prometheus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleKardashAndPrometheusNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CruciblePrometheusRisingV1.Up(migrationBuilder);
            CrucibleConfirmedKillsV1.Up(migrationBuilder);
            CruciblePointOfImpactV1.Up(migrationBuilder);
            CrucibleItCameFromErdasV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleItCameFromErdasV1.Down(migrationBuilder);
            CruciblePointOfImpactV1.Down(migrationBuilder);
            CrucibleConfirmedKillsV1.Down(migrationBuilder);
            CruciblePrometheusRisingV1.Down(migrationBuilder);
            foreach (var sql in CrucibleKardashAndPrometheusNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

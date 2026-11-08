using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 13, the Thrax hybrid arc: Special Agent Stark, the two Skeevs of the Khulago laboratory, its two pads,
    /// and Capriulo and Eugin moved (CrucibleHybridArcNpcsV1), with Dark Genesis (1856, CrucibleDarkGenesisV1), The Gene Machine
    /// (1857, CrucibleGeneMachineV1), Designer Genes (1881, CrucibleDesignerGenesV1) and Genome Sweet Genome (1899,
    /// CrucibleGenomeSweetGenomeV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_hybrid_arc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleHybridArcNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleDarkGenesisV1.Up(migrationBuilder);
            CrucibleGeneMachineV1.Up(migrationBuilder);
            CrucibleDesignerGenesV1.Up(migrationBuilder);
            CrucibleGenomeSweetGenomeV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleGenomeSweetGenomeV1.Down(migrationBuilder);
            CrucibleDesignerGenesV1.Down(migrationBuilder);
            CrucibleGeneMachineV1.Down(migrationBuilder);
            CrucibleDarkGenesisV1.Down(migrationBuilder);
            foreach (var sql in CrucibleHybridArcNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

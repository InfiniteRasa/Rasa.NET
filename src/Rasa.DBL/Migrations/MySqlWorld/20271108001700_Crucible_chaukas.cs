using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 12a, the Chaukas Robotics Facility: Information Analyst Ruben and Cipher Specialist Brian
    /// (CrucibleChaukasNpcsV1), with Ruben's Get to Chaukas (1577, CrucibleGetToChaukasV1) and Brian's Control the Brain
    /// (1521, CrucibleControlTheBrainV1) and Fix the Factory (1522, CrucibleFixTheFactoryV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_chaukas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleChaukasNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleGetToChaukasV1.Up(migrationBuilder);
            CrucibleControlTheBrainV1.Up(migrationBuilder);
            CrucibleFixTheFactoryV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleFixTheFactoryV1.Down(migrationBuilder);
            CrucibleControlTheBrainV1.Down(migrationBuilder);
            CrucibleGetToChaukasV1.Down(migrationBuilder);
            foreach (var sql in CrucibleChaukasNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

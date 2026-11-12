using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 7, the Staal Junkyard: Cirkin, the three Thrax officers and their squad, Kalamit, Lamontix, the
    /// Junkyard's Thrax Technicians and the Ultra Reconstructor (CrucibleStaalJunkyardNpcsV1), with Melching's Heads Will Roll
    /// (1206, CrucibleHeadsWillRollV1), DeLisi's Careful Negotiation (1519, CrucibleCarefulNegotiationV1), Cirkin's Your Part of
    /// the Bargain (1529, CruciblePartOfTheBargainV1) and Cheung's Not My Junk (1538, CrucibleNotMyJunkV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_staal_junkyard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleStaalJunkyardNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleHeadsWillRollV1.Up(migrationBuilder);
            CrucibleCarefulNegotiationV1.Up(migrationBuilder);
            CruciblePartOfTheBargainV1.Up(migrationBuilder);
            CrucibleNotMyJunkV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleNotMyJunkV1.Down(migrationBuilder);
            CruciblePartOfTheBargainV1.Down(migrationBuilder);
            CrucibleCarefulNegotiationV1.Down(migrationBuilder);
            CrucibleHeadsWillRollV1.Down(migrationBuilder);
            foreach (var sql in CrucibleStaalJunkyardNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

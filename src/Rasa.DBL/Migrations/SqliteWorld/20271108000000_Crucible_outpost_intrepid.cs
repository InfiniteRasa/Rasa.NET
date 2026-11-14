using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 1, Outpost Intrepid: Corporal Shandy, Recon Sergeant Mullins and Top Khana Grupa Munit, and the
    /// upstream rows set 1 uses moved to their sourced places (DeLisi, Drake, Vitto, Donal, Rigs) with the client's packages
    /// (Rigs 1231, D'Ambrose 1301) (CrucibleOutpostIntrepidNpcsV1), with Corporal Shandy's The Last Men Standing (1142,
    /// CrucibleLastMenStandingV1), Captain DeLisi's A Fort Called Intrepid (1150, CrucibleFortCalledIntrepidV1) and The Wire
    /// (1145, CrucibleWireV1), and Private Drake's The Departed (1143, CrucibleDepartedV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_outpost_intrepid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleOutpostIntrepidNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleLastMenStandingV1.Up(migrationBuilder);
            CrucibleDepartedV1.Up(migrationBuilder);
            CrucibleFortCalledIntrepidV1.Up(migrationBuilder);
            CrucibleWireV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleWireV1.Down(migrationBuilder);
            CrucibleFortCalledIntrepidV1.Down(migrationBuilder);
            CrucibleDepartedV1.Down(migrationBuilder);
            CrucibleLastMenStandingV1.Down(migrationBuilder);
            foreach (var sql in CrucibleOutpostIntrepidNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

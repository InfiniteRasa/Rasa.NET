using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 2, the Grupa chiefs: Bunko, Merchant Yeg and Roscoe, the upstream rows set 2 uses moved to their
    /// places (Kappa Grupa Lohen into his office, Merchant Cugin onto the Staal Centrum floor, Corporal Fiorella into
    /// Outpost Intrepid, Labbna Sigal Haggus into the Labbna Quarter) and the office's two local teleporters
    /// (CrucibleGrupaChiefsNpcsV1), with Captain DeLisi's The Boss Of All Bosses (1161, CrucibleBossOfAllBossesV1), Larai
    /// Grupa Vitto's The Protection Racket (1146, CrucibleProtectionRacketV1), Top Khana Grupa Munit's Smoking Guns (1148,
    /// CrucibleSmokingGunsV1) and Labbna Grupa Rigs' Junkyard Dogs (1149, CrucibleJunkyardDogsV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_grupa_chiefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleGrupaChiefsNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleBossOfAllBossesV1.Up(migrationBuilder);
            CrucibleProtectionRacketV1.Up(migrationBuilder);
            CrucibleSmokingGunsV1.Up(migrationBuilder);
            CrucibleJunkyardDogsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleJunkyardDogsV1.Down(migrationBuilder);
            CrucibleSmokingGunsV1.Down(migrationBuilder);
            CrucibleProtectionRacketV1.Down(migrationBuilder);
            CrucibleBossOfAllBossesV1.Down(migrationBuilder);
            foreach (var sql in CrucibleGrupaChiefsNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

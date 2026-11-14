using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 7, Latan's side of Formal Complaints (player flag 250): the Nitroglazer and the Hominis Machina Mk XI
    /// new in the Boneyard (AshenScavengerNpcsV1), with Scavenger Vakiel Latan's Robotic Salvage Recovery (1264,
    /// AshenRoboticSalvageV1), Weapon Salvage Recovery (1266, AshenWeaponSalvageV1) and Heavy Salvage Recovery (1265,
    /// AshenHeavySalvageV1). Data only, plain SQL (CrucibleSql); no upstream row changes.
    /// </summary>
    public partial class Ashen_scavenger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenScavengerNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenRoboticSalvageV1.Up(migrationBuilder);
            AshenWeaponSalvageV1.Up(migrationBuilder);
            AshenHeavySalvageV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenHeavySalvageV1.Down(migrationBuilder);
            AshenWeaponSalvageV1.Down(migrationBuilder);
            AshenRoboticSalvageV1.Down(migrationBuilder);
            foreach (var sql in AshenScavengerNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

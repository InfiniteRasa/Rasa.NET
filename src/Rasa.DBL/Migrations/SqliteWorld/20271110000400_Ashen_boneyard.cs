using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 4, the Boneyard: Lieutenant Gabriel Infanger new at Boneyard Outpost, Xanx pools and the Desert
    /// Stalker (new) in the Boneyard (AshenBoneyardNpcsV1), with Lieutenant Jan Blood's Xanxless Job (1262, AshenXanxlessJobV1),
    /// Captain Akio Yamamoto's Fallen Soldiers (1263, AshenFallenSoldiersV1) and Infanger's Stalking the Stalkers (1272,
    /// AshenStalkingTheStalkersV1). Data only, plain SQL (CrucibleSql).
    /// </summary>
    public partial class Ashen_boneyard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenBoneyardNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenXanxlessJobV1.Up(migrationBuilder);
            AshenFallenSoldiersV1.Up(migrationBuilder);
            AshenStalkingTheStalkersV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenStalkingTheStalkersV1.Down(migrationBuilder);
            AshenFallenSoldiersV1.Down(migrationBuilder);
            AshenXanxlessJobV1.Down(migrationBuilder);
            foreach (var sql in AshenBoneyardNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

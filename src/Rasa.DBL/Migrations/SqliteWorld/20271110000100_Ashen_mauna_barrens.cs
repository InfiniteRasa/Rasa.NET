using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 2, the Mauna Barrens: Lieutenant Simon Kuang, Doctor Jung Kwon and Captain Sera Karter new at
    /// Shadow's Edge Post, the Ashen Atta Soldiers, Ashen Atta Workers and Mutated Atta Soldiers new in six Mauna Barrens pools,
    /// and Thrax Overseer Exing's pool moved off its navmesh island (AshenMaunaBarrensNpcsV1), with Kuang's Angry Angry Atta
    /// (1242, AshenAngryAngryAttaV1), Harmful Mutations (1243, AshenHarmfulMutationsV1) and Chemical Oversight (1244,
    /// AshenChemicalOversightV1) and Karter's Eyes on the Ground (1245, AshenEyesOnTheGroundV1). Data only, plain SQL
    /// (CrucibleSql).
    /// </summary>
    public partial class Ashen_mauna_barrens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenMaunaBarrensNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenAngryAngryAttaV1.Up(migrationBuilder);
            AshenHarmfulMutationsV1.Up(migrationBuilder);
            AshenChemicalOversightV1.Up(migrationBuilder);
            AshenEyesOnTheGroundV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenEyesOnTheGroundV1.Down(migrationBuilder);
            AshenChemicalOversightV1.Down(migrationBuilder);
            AshenHarmfulMutationsV1.Down(migrationBuilder);
            AshenAngryAngryAttaV1.Down(migrationBuilder);
            foreach (var sql in AshenMaunaBarrensNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

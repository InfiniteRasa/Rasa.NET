using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 12b, the Chaukas Robotics Facility's factory: the Refitted Warden Bot row
    /// (CrucibleFactoryFortificationV1.NpcUp), Brian's Factory Fortification (1523, CrucibleFactoryFortificationV1) and
    /// Information is Key (1525, CrucibleInformationIsKeyV1, handed in at Ruben). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_chaukas_factory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleFactoryFortificationV1.NpcUp())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleFactoryFortificationV1.Up(migrationBuilder);
            CrucibleInformationIsKeyV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleInformationIsKeyV1.Down(migrationBuilder);
            CrucibleFactoryFortificationV1.Down(migrationBuilder);
            foreach (var sql in CrucibleFactoryFortificationV1.NpcDown())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}

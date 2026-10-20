using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Medic Schramm, a medical vendor in the teal officer's uniform, at the Proving Grounds'
    /// fort (BootcampFortMedic). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes her out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270109000000_Bootcamp_fort_medic")]
    public class Bootcamp_fort_medic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampFortMedic.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampFortMedic.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

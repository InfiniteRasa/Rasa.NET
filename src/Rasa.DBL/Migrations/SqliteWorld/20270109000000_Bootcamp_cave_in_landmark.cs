using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The rocks in the Proving Grounds' cavern doorwell seen from across the bridge
    /// (BootcampCaveInLandmarkV10): the scenes' pile is given a view radius. Data only: no model
    /// of its own.
    ///
    /// Down takes it away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270109000000_Bootcamp_cave_in_landmark")]
    public class Bootcamp_cave_in_landmark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => BootcampCaveInLandmarkV10.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => BootcampCaveInLandmarkV10.Down(migrationBuilder);
    }
}

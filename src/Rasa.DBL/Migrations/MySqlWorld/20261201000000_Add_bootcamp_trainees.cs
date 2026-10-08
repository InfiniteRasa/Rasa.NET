using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The drill sergeant's recruits at the Proving Grounds (BootcampTrainees): two squads of six,
    /// ambient_npc 16 and 17, in a rectangle between his line and the gate wall, facing him. No
    /// table changes.
    ///
    /// Down deletes the two rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261201000000_Add_bootcamp_trainees")]
    public partial class Add_bootcamp_trainees : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in BootcampTrainees.UpStatements())
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in BootcampTrainees.DownStatements())
                migrationBuilder.Sql(statement);
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Alia Das and three other Wilderness posts placed and dressed from gameplay video: new NPCs,
    /// moves, looks, poses, patrols, greetings and vendor stock, the six class trainers and the
    /// unnamed weapons vendor taken away, the south gate turret and the crafting station turned
    /// (AliaDasNpcs). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261231000000_Alia_das_npcs")]
    public class Alia_das_npcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasNpcs.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasNpcs.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

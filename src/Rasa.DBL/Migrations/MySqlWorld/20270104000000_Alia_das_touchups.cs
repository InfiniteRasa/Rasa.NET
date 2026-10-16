using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Alia Das put right after a look round in game: clothes, hair and helmets, the command
    /// tent's turret turned, a turret on the meadow tripod, the gunner trainee's circle on the
    /// ground, four footlockers, Supply Clerk Bradshaw taken away and Caruso selling his armour,
    /// and a pad at each end of the CELLAR Arena link (AliaDasTouchups). Data only, in plain SQL:
    /// no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270104000000_Alia_das_touchups")]
    public class Alia_das_touchups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasTouchups.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AliaDasTouchups.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

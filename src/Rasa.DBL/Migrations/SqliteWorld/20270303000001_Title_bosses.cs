using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The 29 named bosses the boss titles ask for and nothing spawned, each on a point pool of
    /// its own on a map of its title (TitleBossesSeed). Data only, in plain SQL: no model of its
    /// own.
    ///
    /// Down takes out exactly the rows Up put in.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270303000001_Title_bosses")]
    public class Title_bosses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TitleBossesSeed.UpStatements)
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TitleBossesSeed.DownStatements)
                migrationBuilder.Sql(sql);
        }
    }
}

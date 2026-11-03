using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Kardash Atta Colony's people, Atta, Bane and ten missions, and the Irendas mission
    /// givers moved to where they stand (KardashAttaColonyContent). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down takes the missions out, then the rows, and puts back the pools and people it changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271015000000_Kardash_atta_colony")]
    public class Kardash_atta_colony : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in KardashAttaColonyContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in KardashAttaColonyContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

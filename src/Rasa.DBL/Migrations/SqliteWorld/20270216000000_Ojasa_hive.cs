using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Ojasa Atta Hive's people, Atta and missions (OjasaHiveContent). Data only, in plain
    /// SQL: no model of its own.
    ///
    /// Down takes the rows out again and gives the hive's pools back the Incline's Atta.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270216000000_Ojasa_hive")]
    public class Ojasa_hive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OjasaHiveContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OjasaHiveContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

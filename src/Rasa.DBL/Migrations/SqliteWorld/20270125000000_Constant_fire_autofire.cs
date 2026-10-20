using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The constant-fire weapons (propellant guns, chainguns and the Series 2 and 3 laser pistols,
    /// leech guns, polarity guns) get windup -1, which the client's tooltip shows as "Autofire"
    /// (ConstantFireAutofire). Data only; rows a game master has changed from the 800 placeholder
    /// are left alone.
    ///
    /// Down puts 800 back on those weapons' rows that are -1.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270125000000_Constant_fire_autofire")]
    public partial class Constant_fire_autofire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConstantFireAutofire.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConstantFireAutofire.Down())
                migrationBuilder.Sql(sql);
        }
    }
}

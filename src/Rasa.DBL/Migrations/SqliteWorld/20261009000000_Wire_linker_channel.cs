using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the world's Linkers (Abyss 531007, Ashen Desert 531014) CR_LINKER_CHANNEL 410/1
    /// (_SHARED): creature_action rows 66001-66002, in each one's fourth slot. A Linker feeds
    /// another Linker in its fight, doubling or tripling its attacks (CreatureBuffs). Range 40 m
    /// and windup 5.7 s are the client's; reuse 20 s is ours, the client giving 0. No damage.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_linker_channel : Migration
    {
        private const uint IdMin = 66001;
        private const uint IdMax = 66999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureLinkerChannelPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action4 = 66001 where id = 531007;");   // Linker Abyss
            migrationBuilder.Sql($"update {creatures} set action4 = 66002 where id = 531014;");   // Linker Ashen Desert
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531007 and action4 = 66001;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531014 and action4 = 66002;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}

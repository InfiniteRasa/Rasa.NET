using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives the summoned Necromite (570003) CR_NECROMITE_CORPSE_EXPLOSION 490/1 in slot 3:
    /// creature_action row 69001, at level 42 as its other actions are, scaled to its Thrax's
    /// when it comes. Damage is the client base x2 every 8 levels x0.25; windup the client's
    /// (0.8 s); reuse ours, 10 s, the client giving none. Its range is the fighting loop's (40 m,
    /// the fight it is in): how near a body has to be is the argument's own 5 m
    /// (CreatureBombs.StartCorpseExplosion).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_necromite_corpse_explosion : Migration
    {
        private const uint ActionIdMin = 69001;
        private const uint ActionIdMax = 69999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureNecromiteCorpsePreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action3 = 69001 where id = 570003;");   // Necromite (Thrax's)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 570003 and action3 = 69001;");

            migrationBuilder.Sql($"delete from {actions} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}

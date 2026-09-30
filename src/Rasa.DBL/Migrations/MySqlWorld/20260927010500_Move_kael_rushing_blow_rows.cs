using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Structures.World;

    /// <summary>
    /// Moves the Kael rushing blow rows out of the region spawns' id block, before that block is
    /// filled.
    ///
    /// Add_kael_rushing_blow and Add_region_spawns were written apart and both took creature_action
    /// ids from 55001: the rushing blow 55001-55003, the region grounds and packs 55001-55062. With
    /// both in the tree, Add_region_spawns stopped on the duplicate key. This runs between the two
    /// (its timestamp is after Add_region_volumes and before Add_region_spawns), so it takes the
    /// same path on a fresh database and on one where Add_region_spawns failed: the three rushing
    /// blow rows become 58001-58003 and the three Kael slots follow them, and the region rows then
    /// go in where they were written to go.
    ///
    /// Only rows that are the rushing blow are moved (action_id 496), so running it where 55001 is
    /// already something else changes nothing.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Move_kael_rushing_blow_rows : Migration
    {
        private static readonly (uint From, uint To, uint Creature)[] Moves =
        {
            (55001, 58001, 520051),     // Goliath - Plateau
            (55002, 58002, 520063),     // Painrox - Thunderhead
            (55003, 58003, 531088)      // Kael, Plateau
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            foreach (var (from, to, creature) in Moves)
            {
                migrationBuilder.Sql($"update {actions} set id = {to} where id = {from} and action_id = 496;");
                migrationBuilder.Sql($"update {creatures} set action4 = {to} where id = {creature} and action4 = {from};");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            foreach (var (from, to, creature) in Moves)
            {
                migrationBuilder.Sql($"update {actions} set id = {from} where id = {to} and action_id = 496;");
                migrationBuilder.Sql($"update {creatures} set action4 = {from} where id = {creature} and action4 = {to};");
            }
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// Gives the Kael their rushing blow (CR_KAEL_RUSHING_BLOW 496/1): the two Kael bosses,
    /// Goliath and Painrox, and the Plateau's Kael, in their fourth slot.
    ///
    /// The blow is a charge (KaelRushingBlow): picked 8-50 m from a target the Kael can see, it
    /// carries the Kael to where the target stood and lands on everyone within 15 m of that spot.
    /// 50 m is the client's range; 8 m and the 15 second reuse are ours - the client gives a
    /// reuse of 0 and a charge from arm's length is not one, and at 15 s it is the Kael's way
    /// back into a fight it has been kited out of rather than its every move. Between charges
    /// the Kael walks in and uses its fists (BehaviorManager.ClosesInWhileCooling).
    ///
    /// Damage as Wire_creature_attacks sets it: the argument's 60-80 x 2^((level - 1) / 8) x
    /// 0.25, a boss x 0.5. The windup column is 0: the charge's windup is its length over 30 m/s,
    /// worked out when it starts, as the client works it out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_kael_rushing_blow : Migration
    {
        private const uint IdMin = 55001;
        private const uint IdMax = 55003;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;
            const string columns = "(id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type)";

            migrationBuilder.Sql($"insert into {actions} {columns} values (55001, 'Goliath (Kael boss) kael_rushing_blow', 496, 1, 8.0, 50.0, 15000, 0, 440, 587, 1);");
            migrationBuilder.Sql($"insert into {actions} {columns} values (55002, 'Painrox (Kael boss) kael_rushing_blow', 496, 1, 8.0, 50.0, 15000, 0, 1358, 1810, 1);");
            migrationBuilder.Sql($"insert into {actions} {columns} values (55003, 'Kael Plateau kael_rushing_blow', 496, 1, 8.0, 50.0, 15000, 0, 156, 207, 1);");

            migrationBuilder.Sql($"update {creatures} set action4 = 55001 where id = 520051;");   // Goliath - Plateau
            migrationBuilder.Sql($"update {creatures} set action4 = 55002 where id = 520063;");   // Painrox - Thunderhead
            migrationBuilder.Sql($"update {creatures} set action4 = 55003 where id = 531088;");   // Kael, Plateau
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520051 and action4 = 55001;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 520063 and action4 = 55002;");
            migrationBuilder.Sql($"update {creatures} set action4 = 0 where id = 531088 and action4 = 55003;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}

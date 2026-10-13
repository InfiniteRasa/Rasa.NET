using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The creature rows whose speeds were wrong, and the migration that puts them right. Every
    /// world database is seeded by CreaturePreloader in an early migration and then comes through
    /// this one, so the preloader keeps the rows as they were and this migration corrects new and
    /// existing databases alike; Down puts back exactly what the seed wrote, which the rollback
    /// check (MigrationConsolidationTests) holds it to.
    ///
    /// Six Wilderness bosses could not reach anyone: Proctor Fulgor (76), Arioch (77), Atropos
    /// (78), the Fithik Hive Monarch (89) and Overseer Graal (90) had a run and a walk speed of
    /// 0, and the Archfiend Grenadier (75) 1 and 1, a crawl. A creature with no running speed
    /// never chases (BehaviorManager), so each stood at its spawn and fired at whatever came
    /// within reach of its attacks and nothing else. They are given the speeds of the bosses in
    /// the rows beside them (79-84: Horntail, Old Scratch, the Hunter boss, the Overseers): a
    /// run of 9, and no walk, so they hold their ground until a fight starts.
    ///
    /// Rows 69 and 70, two of Council Elder Solis, carried 6707 and 6708 in run_speed - the
    /// name ids of Luminary Doyan and Advisor Todae (rows 91 and 92), a column out of place -
    /// so the elders, who stand, "ran" at 6,707 m/s. They have 0, as the other elders do.
    /// </summary>
    public static class WildernessBossSpeeds
    {
        /// <summary>Creature id, the speeds it had, the speeds it has: run then walk.</summary>
        public static readonly (uint Id, uint OldRun, uint OldWalk, uint Run, uint Walk)[] Rows =
        {
            (69, 6707, 0, 0, 0),
            (70, 6708, 0, 0, 0),
            (75, 1, 1, 9, 0),
            (76, 0, 0, 9, 0),
            (77, 0, 0, 9, 0),
            (78, 0, 0, 9, 0),
            (89, 0, 0, 9, 0),
            (90, 0, 0, 9, 0),
        };

        private static readonly string[] Columns = { "run_speed", "walk_speed" };

        public static void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _, _, run, walk) in Rows)
                migrationBuilder.UpdateData(CreatureEntry.TableName, "id", id, Columns, new object[] { run, walk });
        }

        public static void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, oldRun, oldWalk, _, _) in Rows)
                migrationBuilder.UpdateData(CreatureEntry.TableName, "id", id, Columns, new object[] { oldRun, oldWalk });
        }
    }
}

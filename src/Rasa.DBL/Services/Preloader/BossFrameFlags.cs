using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// UI_INDICATOR_BOSS (56) on the seven boss classes the flag editor left without it
    /// (CreatureFlagEdits gives it to the other boss classes it edits): the client draws the boss
    /// border on the target frame and the overhead name from that flag alone
    /// (targetinfowindow.py, overheadwindow.py), so the Bane Hunter Boss and Invasion Boss, the
    /// Arioch Xanx Boss, the Plateau's Mox Alpha Male, Hominis Machina Lt. Casper and Rotting Sal
    /// were shown in an ordinary creature's frame. The other three have no creature row today and
    /// get it for the day they do.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: the flag goes in only where the class has not
    /// got it, and Down takes it out again (none of the seven had it before).
    /// </summary>
    public static class BossFrameFlags
    {
        public const uint UiIndicatorBoss = 56;

        /// <summary>The classes, with the creature rows that use them today.</summary>
        public static readonly (uint ClassId, string Name)[] Classes =
        {
            (10327, "Bane_Hunter_Boss"),        // 81 Bane Hunter Boss, 49 Bane Hunter Invasion Boss
            (10523, "Bane_Xanx_Boss"),          // 77 Arioch Xanx Boss
            (22977, "Bane_Mox_Standard_Boss"),  // 520053 Mox Alpha Male - Plateau
            (21474, "Bane_Hominis_Machina_Boss"),   // 520012 Hominis Machina Lt. Casper, 520017 Rotting Sal
            (22975, "Creature_Mox_Boss"),
            (10222, "Boss_Eloh"),
            (11134, "Boss_Enslaver")
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (classId, _) in Classes)
                yield return "INSERT INTO creature_class_flag (class_id, flag_id) SELECT e.id, " + UiIndicatorBoss + " FROM entityclass e " +
                    $"WHERE e.id = {classId} AND NOT EXISTS (SELECT 1 FROM creature_class_flag x WHERE x.class_id = e.id AND x.flag_id = {UiIndicatorBoss});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_class_flag WHERE flag_id = {UiIndicatorBoss} AND class_id IN ({string.Join(", ", Classes.Select(c => c.ClassId))});";
        }
    }
}

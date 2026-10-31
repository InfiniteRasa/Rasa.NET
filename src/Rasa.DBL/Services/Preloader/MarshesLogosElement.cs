using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Two Logos shrines put right (Marshes_logos_element).
    ///
    /// The Valverde Marshes shrine at (132.86, 219.70, 51.73), on the Eloh dispenser base the map
    /// stands there, is a UsableItemDispElohLogosElementV01 (21271): its mesh is
    /// arch_eloh_logos_logoselement_v01, the Logos "Logos Element" (LOGOS_ELEMENT, 215), whose
    /// stone is ItemElohLogosLogos_element (22796) and whose sign is ArchElohLogosSignLogosElement
    /// (22329), all of that era's classes. The shrine's row had 405 (ELEMENT, "Element"), a later
    /// Logos with no stone, whose own shrine and sign are the later 25816 and 25801. A player
    /// used it and learned Element; Logos Element, the one it shows, never came, and nothing on
    /// the Tabula is lit for it. The row is 215 now, named as the client names it.
    ///
    /// Crucible's Everyone (348) stood as UsableItemDispElohLogosEveryonesV01 (21202), the
    /// shrine of Everyones (156) in Howling Maw: it taught Everyone and showed the glyph of
    /// Everyones. It is UsableItemDispElohLogosEveryoneV01 (21420), Everyone's own.
    ///
    /// A character who learned Element at the Marshes shrine keeps it; using the shrine again
    /// teaches them Logos Element.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: each row changes only while it is as the logos
    /// table had it, and Down changes it back only while it is as Up left it. A row of 215
    /// already there stops Up with its key, rather than leave the Marshes without its shrine.
    /// </summary>
    public static class MarshesLogosElement
    {
        public const uint Marshes = 1454, Crucible = 1993;
        public const uint ShrineClass = 21271;
        public const uint WasLogos = 405, NowLogos = 215;
        public const string WasName = "Element", NowName = "Logos Element";

        public const uint Everyone = 348;
        public const uint WasEveryoneClass = 21202, NowEveryoneClass = 21420;

        public static IEnumerable<string> Up()
        {
            yield return Relabel(WasLogos, WasName, NowLogos, NowName);
            yield return Reclass(WasEveryoneClass, NowEveryoneClass);
        }

        public static IEnumerable<string> Down()
        {
            yield return Relabel(NowLogos, NowName, WasLogos, WasName);
            yield return Reclass(NowEveryoneClass, WasEveryoneClass);
        }

        private static string Relabel(uint fromId, string fromName, uint toId, string toName) =>
            $"UPDATE logos SET id = {toId}, name = '{toName}' WHERE id = {fromId} AND name = '{fromName}' AND class_id = {ShrineClass} " +
            $"AND map_context_id = {Marshes} AND ABS(pos_x - 132.85547) < 0.01 AND ABS(pos_z - 51.726562) < 0.01;";

        private static string Reclass(uint fromClass, uint toClass) =>
            $"UPDATE logos SET class_id = {toClass} WHERE id = {Everyone} AND class_id = {fromClass} AND map_context_id = {Crucible};";
    }
}

using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The voiced barks of the named NPCs who stand in our world (npc_bark). Of the client's 859
    /// barks, 42 have a recording, and the recording's file name says whose voice it is; seven are
    /// named NPCs': Information Spec. Johnson (22, ITSpecJohnson.ogg), Field Lt. Yates (23),
    /// Commander Elvers (27, 205, 206, 207), Field Engineer Hernandez (38), Tribal Leader Ryosa
    /// (40), Comm. Officer Jennings (43) and Major McAllister (852,
    /// boot_camp_major_mcallister_bark.ogg).
    ///
    /// Only Major McAllister stands anywhere: creature 510203, by the Eloh hologram in Proving
    /// Grounds (BootcampWorldContentSeedData). The others have no creature row, and Comm. Officer
    /// Jennings (name 5258) is not Recon. Spec. Jennings (3075, creature 135). A row for one of
    /// them goes in with the NPC.
    ///
    /// Each row goes in only where its creature is and the row is not there already; Down takes
    /// out only these rows.
    /// </summary>
    public static class NamedNpcBarks
    {
        public const uint MajorMcAllister = BootcampWorldContentSeedData.MajorMcAllisterCreatureId;

        /// <summary>"Soldier! If you're done communing with your alien buddies, we could use a hand saving our asses around here!"</summary>
        public const uint McAllisterAlienBuddies = 852;

        /// <summary>The rows, as (creature id, bark id).</summary>
        public static IReadOnlyList<(uint Creature, uint Bark)> Rows { get; } = new[]
        {
            (MajorMcAllister, McAllisterAlienBuddies)
        };

        /// <summary>One statement per row, valid on SQLite and MySQL alike.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var (creature, bark) in Rows)
                yield return $"INSERT INTO {NpcBarkEntry.TableName} (creature_id, bark_id) SELECT {creature}, {bark} FROM creature " +
                             $"WHERE id = {creature} AND NOT EXISTS (SELECT 1 FROM {NpcBarkEntry.TableName} WHERE creature_id = {creature} AND bark_id = {bark});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (creature, bark) in Rows)
                yield return $"DELETE FROM {NpcBarkEntry.TableName} WHERE creature_id = {creature} AND bark_id = {bark};";
        }
    }
}

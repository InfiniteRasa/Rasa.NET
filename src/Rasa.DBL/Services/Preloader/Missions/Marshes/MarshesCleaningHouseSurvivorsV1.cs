using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The survivors of Cleaning House's three camps (894) as their own creature rows (to-fix 16, 2026-11-03), so that a talk
    /// can name the one creature that speaks it (MissionDialogueTopicDefinition.SourceCreatureId, which content validation
    /// checks against npc_package: the creature's row and its package). Ids 760383-760385 (block 760300-760399; checked
    /// 2026-11-03 against the World DB copy and the code of all worktrees: free): each a copy of its camp's prisoner row
    /// (760379-760381: the Torcastra prisoner's look, neutral, level 35) with the same client package (1158, 1159, 1160).
    /// </summary>
    public static class MarshesCleaningHouseSurvivorsV1
    {
        public static readonly uint[] Survivors = { 760383, 760384, 760385 };

        private static readonly (uint Prisoner, uint Package, string Camp)[] Camps =
        {
            (MarshesPreoDasVillageNpcsV1.RaaPrisoner, MarshesPreoDasVillageNpcsV1.RaaPackage, "Ra''a Das"),
            (MarshesPreoDasVillageNpcsV1.EbaePrisoner, MarshesPreoDasVillageNpcsV1.EbaePackage, "E''bae Das"),
            (MarshesPreoDasVillageNpcsV1.HekalPrisoner, MarshesPreoDasVillageNpcsV1.HekalPackage, "He''kal Das")
        };

        public static IEnumerable<string> Up()
        {
            for (var index = 0; index < Camps.Length; index++)
            {
                var (prisoner, package, camp) = Camps[index];
                var id = Survivors[index];
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {id}, 'Forean Prisoner - Village Ruins, {camp}, the survivor (894)', class_id, faction, level, max_hp, name_id, " +
                    $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {prisoner};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, 'Forean survivor - {camp} (894)');";
            }
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM npc_package WHERE id IN ({string.Join(", ", Survivors)});";
            yield return $"DELETE FROM creature WHERE id IN ({string.Join(", ", Survivors)});";
        }
    }
}

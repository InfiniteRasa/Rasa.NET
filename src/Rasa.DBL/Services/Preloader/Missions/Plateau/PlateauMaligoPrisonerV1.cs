using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Out of Thin Air's NPCs in Ustor Yard (squad instance 1502): the Maligo Base Prisoner on her rock, and the
    /// template of the small Thrax patrol that spawns when the mission is taken. The server had neither.
    ///
    /// Sourced: name 9168 "Maligo Base Prisoner" (creaturenamelanguage), package 1307 (objectiveconversation 1190,3);
    /// "Give Her" (client objective 3 9238): a woman. TaRapedia NPC list: Ustor Yard, /loc 19, 178, 121. Client 1190
    /// text 9144: "Without armor, I'm dead if I step a foot off this rock."
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24): she stands at TaRapedia's /loc read as (x, y, z),
    /// on the navmesh, in the Downed Prisoner's outfit (Rapp's, PlateauMaligoBaseV1), level 32, with the server's
    /// escort health and armour (EscortNpcStats). The patrol: Thrax as Ustor Yard's security (level 31).
    ///
    /// Ids: creatures 650105-650106, the prisoner's pool 650105, free before. Plain SQL, valid on SQLite and MySQL.
    /// </summary>
    public static class PlateauMaligoPrisonerV1
    {
        public const uint MapContextId = PlateauUstorYardV1.MapContextId;

        public const uint Prisoner = 650105, PrisonerPool = 650105, PrisonerPackage = 1307, PrisonerName = 9168, PrisonerLevel = 32;
        public const uint PrisonerHealth = EscortNpcStats.Health, PrisonerArmor = EscortNpcStats.Armor;
        public const uint OutfitOf = PlateauMaligoBaseV1.DownedPrisoner;
        public const uint Patrol = 650106, PatrolLevel = PlateauUstorYardV1.SecurityLevel,
            PatrolHitPoints = PlateauUstorYardV1.SecurityHitPoints, PatrolArmor = PlateauUstorYardV1.SecurityArmor;
        public const uint PatrolName = 9224;            // "Thrax Soldier"

        public static readonly (double X, double Y, double Z) PrisonerAt = (19, 178.15, 121);

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Prisoner}, 'Maligo Base Prisoner - Ustor Yard', {PlateauUstorYardV1.FemaleClass}, 1, {PrisonerLevel}, " +
                         $"{PrisonerHealth}, {PrisonerName}, 7, 2, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Prisoner}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return Stat(Prisoner, PrisonerHealth, PrisonerArmor);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Prisoner}, {PrisonerPackage}, 'Maligo Base Prisoner');";
            yield return Pool(PrisonerPool, PrisonerAt.X, PrisonerAt.Y, PrisonerAt.Z, MapContextId, Prisoner, 1, 1, 20, 0);

            yield return Creature(Patrol, "Thrax Patrol - Ustor Yard (Out of Thin Air scene)", PlateauUstorYardV1.ThraxClass, PatrolLevel,
                PatrolHitPoints, PatrolName, PlateauUstorYardV1.ThraxKick30, PlateauUstorYardV1.ThraxWeapon30);
            yield return Stat(Patrol, PatrolHitPoints, PatrolArmor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {PrisonerPool};";
            yield return $"DELETE FROM npc_package WHERE id = {Prisoner};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Prisoner}, {Patrol});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Prisoner};";
            yield return $"DELETE FROM creature WHERE id IN ({Prisoner}, {Patrol});";
        }
    }
}

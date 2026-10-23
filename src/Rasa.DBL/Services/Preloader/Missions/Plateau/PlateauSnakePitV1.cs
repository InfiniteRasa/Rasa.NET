using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Colonel "Snake" Washington at the Snake Pit, Valverde Pools (map 1304), for South Of The Border (1068). The
    /// server had the Snake Pit's vendors, waypoint and hospital, but not him.
    ///
    /// Sourced: client name 4372 "Colonel 'Snake' Washington" (creaturenamelanguage); package 331
    /// (objectiveconversation 1068,1 and 1326,1). TaRapedia South Of The Border: reward giver Colonel 'Snake'
    /// Washington at The Snakepit. World DB: the Snake Pit region 20022 (-954.5, 915.7, 412.6), its vendors at
    /// (-976..-963, 919, 359..461), waypoint 48 (-941.7, 919.7, 367.4).
    ///
    /// Reconstructed (ESTIMATE, the user's choice of 2026-10-23): his place by the vendors, on the navmesh and
    /// walked to from the waypoint; Peterson's officer uniform (as PlateauPerduNpcsV1's men); level 30. To be
    /// corrected in the playtest.
    ///
    /// Ids: creature and pool 650095, free before. Plain SQL, valid on SQLite and MySQL; down takes out only these rows.
    /// </summary>
    public static class PlateauSnakePitV1
    {
        public const uint MapContextId = 1304;          // adv_foreas_valverde_pools
        public const uint Snake = 650095, SnakeName = 4372, SnakePackage = 331, SnakeLevel = 30;
        public const uint MaleClass = PlateauMaligoBaseV1.MaleClass;

        public static readonly (double X, double Y, double Z) SnakeAt = (-968, 919.22, 363);

        /// <summary>Peterson's officer uniform (slot, class, color), as PlateauPerduNpcsV1's men wear.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] Outfit =
        {
            (2, 4021, 4294934528),      // NPC_Clothing_Officer_1_Boots
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (14, 9781, 4278655809),     // AvatarSwap hair
            (15, 4023, 4294934528),     // NPC_Clothing_Officer_1_Torso
            (16, 4022, 4294934528),     // NPC_Clothing_Officer_1_Legs
            (17, 24008, 4286690539),    // AvatarSwap face, Caucasian
        };

        public static IEnumerable<string> Up()
        {
            const string comment = "Colonel Snake Washington - The Snake Pit";
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Snake}, '{comment}', {MaleClass}, 1, {SnakeLevel}, 1000, {SnakeName}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Snake}, {slot}, {cls}, {color});";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Snake}, {SnakePackage}, '{comment}');";
            yield return Pool(Snake, SnakeAt.X, SnakeAt.Y, SnakeAt.Z, MapContextId, Snake, 1, 1, 20, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {Snake};";
            yield return $"DELETE FROM npc_package WHERE id = {Snake};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Snake};";
            yield return $"DELETE FROM creature WHERE id = {Snake};";
        }
    }
}

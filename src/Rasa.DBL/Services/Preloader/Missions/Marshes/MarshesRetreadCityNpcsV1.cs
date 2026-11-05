using System.Collections.Generic;
using Rasa.Missions.Content;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 10 (Retread City) NPCs, Valverde Marshes (map 1454): Commander Figgins at Falcon Hold (914's rat-out
    /// talk), a Marshes Lieutenant Holloway in Retread City (1033's giver), 1033's AFS mech squadmate, and 1052's Retread
    /// Samuel stand-in and his Retreads (scene creatures, no pool).
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760339-760399 free): 760339 Figgins (creature, creature_appearance, npc_package, pool), 760340 Holloway (the same),
    /// 760341 the mech (creature, creature_stat), 760342 the Samuel stand-in (creature, creature_appearance, creature_stat,
    /// npc_package 1120), 760343 the Retread (creature, creature_appearance, creature_stat).
    ///
    /// Sourced: client names 8623 "Commander Figgins", 8594 "Lieutenant Holloway", 8855 "Retread Samuel", 10736 "AFS Mech
    /// Defender" (creaturenamelanguage); packages 1011 (objectiveconversation 914,2, the "Damn Retreads!" officer), 1120
    /// (objectiveconversation 1052,1, Samuel's "We got turned around"). TaRapedia: Figgins Falcon Hold (-186, 219, 628), Ten
    /// Ton Hammer (-184, 219, 627); Holloway Retread City (-285, 216, -753). World DB: Lieutenant Holloway 510199 (on the
    /// Pools, name 8594, no package), Retread Samuel 510167 (Bane Refueling Station 196.4, 219.4, -669.1, package 120 of
    /// mission 425), Lieutenant Morrison 510162 (Figgins' look), the Landing Zone's AFS Mech Defender 680001 (class
    /// 10235, name 10736, its minigun and missiles; LandingZoneGarrisonContent).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Figgins a copy of Morrison's row and look with his name and the client's package 1011 (the triage's Q4), at Ten
    ///    Ton Hammer's place; Holloway a copy of the Pools Holloway's row and look with a server-only package (his id: the
    ///    client has no talk of his), at TaRapedia's (the triage's Q6). Both walked to from their waypoints (probe of
    ///    2026-11-02).
    ///  - The mech a copy of 680001's row (class, name, speeds, attacks) at level 36 with 38700 HP, 30 body, mind and spirit
    ///    and 1063 armour (the Pools control point Mech Defender's numbers, ESTIMATE); the stand-in and the Retreads copies of Samuel's row and look
    ///    with the escort stats (4000 HP, 300 armour; the stand-in three times the HP, to-fix 29), levels 38 (1052's) and 36.
    ///    The Retreads' name "Retread" is client name 9005 (creaturenamelanguage; 9315 has the same text; neither is on a
    ///    World DB creature). Without a name the client showed "Human" (to-fix 29).
    /// </summary>
    public static class MarshesRetreadCityNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Morrison = 510162, PoolsHolloway = 510199, Samuel = 510167, MechDefender = 680001;   // Landing Zone AFS Mech Defender (LandingZoneGarrisonContent)
        public const uint MechLevel = 36, MechHitPoints = 38700, MechAttribute = 30, MechArmor = 1063;
        public const uint EscortHealth = (uint)EscortNpcStats.Health, EscortArmor = (uint)EscortNpcStats.Armor;

        public const uint Figgins = 760339, FigginsName = 8623, FigginsPackage = 1011;
        public const uint Holloway = 760340, HollowayName = 8594, HollowayPackage = Holloway;
        public const uint Mech = 760341;
        public const uint SamuelStandIn = 760342, SamuelName = 8855, SamuelPackage = 1120, SamuelLevel = 38;
        public const uint Retread = 760343, RetreadName = 9005, RetreadLevel = 36;

        /// <summary>
        /// The stand-in's hit points: three times the escort stats (to-fix 29, ESTIMATE). Samuel stands through all three
        /// waves and nothing heals him; with the escort's 4000 he fell to wave 3 and the station's Thrax in both real-client
        /// runs of 2026-11-03. The Retreads keep the escort stats.
        /// </summary>
        public const uint SamuelHealth = 3 * EscortHealth;

        private static uint Health(uint id) => id == SamuelStandIn ? SamuelHealth : EscortHealth;

        public static readonly (double X, double Y, double Z) FigginsAt = (-184, 219.62, 627);
        public static readonly (double X, double Y, double Z) HollowayAt = (-285, 217.09, -753);

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, row, name, package, at) in new[]
            {
                (Figgins, "Commander Figgins - Marshes, Falcon Hold (914)", Morrison, FigginsName, FigginsPackage, FigginsAt),
                (Holloway, "Lieutenant Holloway - Marshes, Retread City (1033)", PoolsHolloway, HollowayName, HollowayPackage, HollowayAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            // The mech: the Landing Zone Mech Defender's row at the mech's level and health, the squadmate's own row.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Mech}, 'AFS Mech squadmate - Marshes (Production Destruction, Holloway)', class_id, 1, {MechLevel}, {MechHitPoints}, " +
                         $"name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {MechDefender};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"VALUES ({Mech}, {MechAttribute}, {MechAttribute}, {MechAttribute}, {MechHitPoints}, {MechArmor});";

            // Samuel's stand-in and his Retreads: friendly, the escort stats.
            foreach (var (id, comment, name, level) in new (uint, string, string, uint)[]
            {
                (SamuelStandIn, "Retread Samuel - Marshes (Trapped! stand-in)", SamuelName.ToString(), SamuelLevel),
                (Retread, "Retread - Marshes (Trapped! group)", RetreadName.ToString(), RetreadLevel)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, 1, {level}, {Health(id)}, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Samuel};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Samuel};";
                yield return Stat(id, Health(id), EscortArmor);
            }
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({SamuelStandIn}, {SamuelPackage}, 'Retread Samuel - Marshes (Trapped!)');";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Figgins}, {Holloway});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Figgins}, {Holloway}, {SamuelStandIn});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Mech}, {SamuelStandIn}, {Retread});";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Figgins} AND {Retread};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Figgins} AND {Retread};";
        }
    }
}

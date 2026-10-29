using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Turpis Refinery (squad instance 1429, the client's "Marshes (Turpis Refinery)") for its five missions, Things That
    /// Blow Up (882), Hard Core (884), Chemistry 101 (885), The Bad Doctor (886) and Cost of Doing Business (1079): the
    /// five AFS givers at the landing zone and the Toxicologist; and Corporal Hash on the Pools by the Turpis door for
    /// Turpis Transport (1305). The server had only the Turpis Forward Medical Unit (hospital, vendor), ten Thrax pools
    /// and the Far Logos there.
    ///
    /// Sourced: client names 4379 "Demolitionist Niebolo", 9031 "Spymaster Terentyeva", 4373 "Field Chemist Larouche",
    /// 8549 "Commander Lindloff", 4380 "Biotech Johansson", 9187 "Corporal Hash", 10092 "The Toxicologist"
    /// (creaturenamelanguage); the client has no objective conversation for any of the six missions. TaRapedia Field
    /// Chemist Larouche: Human, AFS, female, level 39. Ten Ton Hammer Turpis Refinery guide: after the Turpis Transport
    /// "four mission icons" (Terentyeva, Johansson, Lindloff and, "Inside the tent with the medic", Larouche); "Take the
    /// path up the hill to the entrance of the cave and talk to Demolitionist Niebolo. He will give you the Things That
    /// Blow Up mission"; the Toxicologist "the level 36 Thrax engineer"; Corporal Hash "up a short ramp" from Scientist
    /// Suresh. IGN TR Vault: Corporal Hash (159.8, 671.7, -603.1), "Turpis Transport". The door sets a player down at
    /// (-211.8, 106.8, -219) (map_link 26), by the medical unit (-203.5, 107.3, -242.6).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The givers: AFS, level 36 (Larouche 39, TaRapedia's), the Snake Pit's officer uniform (men, a copy of Riggins's
    ///    look) or the Ustor Yard women's; Larouche and Terentyeva women (Terentyeva the Russian name's female form), the
    ///    others men. Packages server-only ids (their own), as Riggins's.
    ///  - Places on the navmesh: Terentyeva, Lindloff and Johansson round the landing spot, Larouche by the medical unit,
    ///    Niebolo at the top of the hill west of it, where the navmesh of the landing zone ends 8 m short of the
    ///    facility's (the cave's wall, PoolsThingsThatBlowUpV1). Hash on the Pools 12.6 m from the door.
    ///  - The Toxicologist: a copy of Thrax Technician - Marshes (the refinery's technicians) at level 36 with three times
    ///    its hit points (as the Lamna Armory's key bosses), personal (The Bad Doctor's scene), no pool.
    ///
    /// Ids: creatures and pools 760239-760244, creature 760245, free before. Plain SQL, valid on SQLite and MySQL; down
    /// takes out only these rows.
    /// </summary>
    public static class PoolsTurpisRefineryNpcsV1
    {
        public const uint MapContextId = 1429;          // adv_foreas_valverde_marshes_wetlandrefinery
        public const uint PoolsMapContextId = PoolsMissionDataV1.MapContextId;
        public const uint MaleClass = Plateau.PlateauMaligoBaseV1.MaleClass, FemaleClass = Plateau.PlateauMaligoBaseV1.FemaleClass;
        public const uint MenOutfitOf = PoolsSnakePitNpcsV1.Riggins, WomenOutfitOf = Plateau.PlateauMaligoBaseV1.WomenOutfitOf;
        public const uint GiverLevel = 36, LaroucheLevel = 39;

        public const uint Niebolo = 760239, NieboloName = 4379, NieboloPackage = 760239;
        public const uint Terentyeva = 760240, TerentyevaName = 9031, TerentyevaPackage = 760240;
        public const uint Larouche = 760241, LaroucheName = 4373, LarouchePackage = 760241;
        public const uint Lindloff = 760242, LindloffName = 8549, LindloffPackage = 760242;
        public const uint Johansson = 760243, JohanssonName = 4380, JohanssonPackage = 760243;
        public const uint Hash = 760244, HashName = 9187, HashPackage = 760244;

        public const uint Toxicologist = 760245, ToxicologistName = 10092, ToxicologistLevel = 36;
        public const uint ToxicologistHitPoints = 5000 * 3, ToxicologistArmor = 360;
        public const uint TechnicianClass = 7043;       // Thrax Technician - Marshes (531047)

        public static readonly (double X, double Y, double Z) DoorArrival = (-211.8138, 106.7688, -219.0292);
        public static readonly (double X, double Y, double Z) NieboloAt = (-264, 123.11, -200);
        public static readonly (double X, double Y, double Z) TerentyevaAt = (-224, 106.89, -216);
        public static readonly (double X, double Y, double Z) LaroucheAt = (-200.4, 107.51, -240);
        public static readonly (double X, double Y, double Z) LindloffAt = (-224, 106.91, -224);
        public static readonly (double X, double Y, double Z) JohanssonAt = (-200, 106.8, -224);
        public static readonly (double X, double Y, double Z) HashAt = (159.8, 671.7, -603.1);

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Giver(Niebolo, "Demolitionist Niebolo - Turpis Refinery", MaleClass, NieboloName, GiverLevel,
                         NieboloPackage, MapContextId, NieboloAt))
                yield return sql;
            foreach (var sql in Giver(Terentyeva, "Spymaster Terentyeva - Turpis Refinery", FemaleClass, TerentyevaName, GiverLevel,
                         TerentyevaPackage, MapContextId, TerentyevaAt))
                yield return sql;
            foreach (var sql in Giver(Larouche, "Field Chemist Larouche - Turpis Refinery", FemaleClass, LaroucheName, LaroucheLevel,
                         LarouchePackage, MapContextId, LaroucheAt))
                yield return sql;
            foreach (var sql in Giver(Lindloff, "Commander Lindloff - Turpis Refinery", MaleClass, LindloffName, GiverLevel,
                         LindloffPackage, MapContextId, LindloffAt))
                yield return sql;
            foreach (var sql in Giver(Johansson, "Biotech Johansson - Turpis Refinery", MaleClass, JohanssonName, GiverLevel,
                         JohanssonPackage, MapContextId, JohanssonAt))
                yield return sql;
            foreach (var sql in Giver(Hash, "Corporal Hash - Pools (Turpis door)", MaleClass, HashName, GiverLevel,
                         HashPackage, PoolsMapContextId, HashAt))
                yield return sql;

            // The Toxicologist: the refinery technician's four attacks.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Toxicologist}, 'The Toxicologist - Turpis Refinery (The Bad Doctor)', {TechnicianClass}, 0, " +
                         $"{ToxicologistLevel}, {ToxicologistHitPoints}, {ToxicologistName}, 9, 5, 70024, 62043, 65012, 68077, 0, 0, 0, 0);";
            yield return Stat(Toxicologist, ToxicologistHitPoints, ToxicologistArmor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Toxicologist};";
            yield return $"DELETE FROM creature WHERE id = {Toxicologist};";
            foreach (var id in new[] { Hash, Johansson, Lindloff, Larouche, Terentyeva, Niebolo })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        /// <summary>A friendly AFS giver in the uniform of its class's precedent, on map <paramref name="map"/>.</summary>
        private static IEnumerable<string> Giver(uint id, string comment, uint classId, uint name, uint level, uint package, uint map,
            (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {classId}, 1, {level}, 1000, {name}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            var outfit = classId == FemaleClass ? WomenOutfitOf : MenOutfitOf;
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {outfit};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            yield return Pool(id, at.X, at.Y, at.Z, map, id, 1, 1, 20, 0);
        }
    }
}

using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 14 (P'reo Das) NPCs and creatures, in the Village Ruins (squad instance 1743): Forean Elder Qunle at P'reo
    /// Das, the giver of Dirty Secrets (1077); the Forean Elder of the hidden encampment at He'kal Das, Needle in a Bane Camp's
    /// (893) contact; Operation Master Myrquar, The Choices We Make's (1078) boss.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-03 against the World DB and the code of all worktrees:
    /// 760373-760399 free): 760373 Qunle (creature, creature_appearance, npc_package, pool), 760374 the encampment's elder
    /// (creature, creature_appearance, npc_package, pool), 760375 Myrquar (creature, creature_stat). No teleporter.
    ///
    /// Sourced: client names 8558 "Forean Elder Qunle", 10226 "Operation Master Myrquar", 9626 "Forean Elder"
    /// (creaturenamelanguage); package 958 (objectiveconversation 893,1, text 4697 "We knew we could not stay hidden for much
    /// longer"). The Village Ruins map (client adv_foreas_valverde_marshes_villageruins; World DB): map_region P'reo Das (100.8,
    /// -236.8, radius 62) and He'kal Das (135, 65.5, 3), the He'kal shrine (133.7, 65.6, -27), the landing pads (54.5 and -8.8,
    /// 59.3, 291). TaRapedia: Qunle at P'reo Das (Dirty Secrets, cache); Myrquar "shipping out with the next batch of Forean
    /// prisoners" (1078's offer 6079). World DB: Elder Toida 510160 (the Forean elder row and look), the Marshes Thrax
    /// Commander 531049, the Plains level 38 Commander's 12375 hit points (MarshesLogosResearchNpcsV1).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's choices of 2026-11-02):
    ///  - Qunle and the elder copies of Toida's row and look at levels 35 (MMO DB's 1077 level) and 34, public in the copy:
    ///    Qunle on the walked-to ground at the west side of P'reo Das (95, -230), the elder by the He'kal shrine (130, -18).
    ///    Qunle's package a server-only one (his id: the client has no talk on him). The elder's name the client's plain
    ///    "Forean Elder" (package 958 names no one).
    ///  - Myrquar a copy of the Marshes Commander at level 38 with the Plains one's hit points; a scene actor only, by the
    ///    landing pads (the pads are an island on the navmesh: he stands on the walked-to ground south of them).
    /// </summary>
    public static class MarshesPreoDasNpcsV1
    {
        public const uint MapContextId = 1743;          // adv_foreas_valverde_marshes_villageruins, a squad instance
        public const uint Toida = 510160, ThraxCommander = MarshesLogosResearchNpcsV1.ThraxCommander;

        public const uint Qunle = 760373, QunleName = 8558, QunlePackage = Qunle, QunleLevel = 35;
        public const uint Elder = 760374, ElderName = 9626, ElderPackage = 958, ElderLevel = 34;
        public const uint Myrquar = 760375, MyrquarName = 10226, MyrquarLevel = 38;
        public const uint MyrquarHitPoints = MarshesLogosResearchNpcsV1.CommanderHitPoints, MyrquarArmor = MarshesLogosResearchNpcsV1.CommanderArmor;

        public static readonly (double X, double Y, double Z) QunleAt = (95, 58.12, -230);
        public static readonly (double X, double Y, double Z) ElderAt = (130, 65.97, -18);

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, level, package, at) in new[]
            {
                (Qunle, "Forean Elder Qunle - Village Ruins, P'reo Das (1077)", QunleName, QunleLevel, QunlePackage, QunleAt),
                (Elder, "Forean Elder - Village Ruins, He'kal Das, the hidden encampment (893)", ElderName, ElderLevel, ElderPackage, ElderAt)
            })
            {
                yield return Copy(id, comment, Toida, name, level, "max_hp", "1");
                yield return Looks(id, Toida);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Village Ruins');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            yield return Copy(Myrquar, "Operation Master Myrquar - Village Ruins, the landing pads (1078)", ThraxCommander, MyrquarName,
                MyrquarLevel, MyrquarHitPoints.ToString(), "faction");
            yield return Stat(Myrquar, MyrquarHitPoints, MyrquarArmor);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Qunle}, {Elder});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Qunle}, {Elder});";
            yield return $"DELETE FROM creature_stat WHERE id = {Myrquar};";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Qunle}, {Elder});";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Qunle} AND {Myrquar};";
        }

        /// <summary>A copy of <paramref name="row"/> with its own name, level, hit points and faction, its attacks kept.</summary>
        private static string Copy(uint id, string comment, uint row, uint name, uint level, string health, string faction) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, '{comment.Replace("'", "''")}', class_id, {faction}, {level}, {health}, {name}, run_speed, walk_speed, " +
            $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";

        private static string Looks(uint id, uint row) =>
            "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
            $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
    }
}

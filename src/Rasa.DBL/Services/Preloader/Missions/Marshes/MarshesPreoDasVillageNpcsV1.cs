using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 15 (P'reo Das, part 2) NPCs and creatures, in the Village Ruins (squad instance 1743): Forean Warrior
    /// Tola by the arrival, the giver of Never Surrender (891) and the receiver of Cleaning House (894); Forean Elder Kirii at
    /// P'reo Das, 891's receiver and 894's giver; Forean Shaman Rakla at P'reo Das, Picking Up the Pieces' (892) giver and
    /// receiver; the Forean Prisoners of 894's three camps.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; MarshesPreoDasNpcsV1 ends at 760375; checked 2026-11-03 against the
    /// World DB copy and the code of all worktrees: 760376-760399 free): 760376 Tola, 760377 Kirii, 760378 Rakla (creature,
    /// creature_appearance, npc_package, pool each); 760379-760381 the prisoners of Ra'a Das, E'bae Das and He'kal Das
    /// (creature, npc_package; scene actors only, no pool). No teleporter.
    ///
    /// Sourced: client names 8560 "Forean Warrior Tola", 8557 "Forean Elder Kirii", 8559 "Forean Shaman Rakla", 5289 "Forean
    /// Prisoner" (creaturenamelanguage); the prisoners' packages from objectiveconversation (894,2 and 14) 1158, (894,3 and
    /// 15) 1159, (894,13 and 16) 1160 ("I will come with you"). TaRapedia Never Surrender, Picking up the Pieces, Cleaning
    /// House: Tola, Kirii and Rakla in P'reo Das; MMO DB: all three "Quest NPC", zone Marshes. Patch notes 2007-10-18 and
    /// 2007-12-11: "Forean prisoners in P'reo Das with no weapons", "no longer have healing pads or field repair tools". The
    /// Village Ruins map (World DB map_region): P'reo Das (100.8, 57.8, -236.8, radius 62), Ra'a Das (239.2, 56, -2.2), He'kal
    /// Das (135, 65.5, 3); E'bae Das (no region row) the hut and platforms round (60, -120). World DB: Elder Toida 510160,
    /// Shaman Siva 760301 and Warrior Veska 760302 (MarshesPaludosNpcsV1), the Torcastra Forean Prisoner 660022 (class 7034,
    /// no weapon).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Tola a copy of Veska's row and look (the spear), Kirii of Toida's (the elder's staff), Rakla of Siva's (the
    ///    shaman's staff); level 35 (MMO DB's level of their missions), friendly; server-only packages (their ids: the client
    ///    has no talk on them). Tola on the walked-to ground by the arrival (the arrival itself is a navmesh island), Kirii and
    ///    Rakla at P'reo Das beside Qunle (navmesh probe of 2026-11-03).
    ///  - The prisoners copies of the Torcastra prisoner's row (no weapon, no attack) at level 35, neutral (faction 3, as
    ///    MarshesEastAndNorthNpcsV1's prisoner leader: the Bane guards and waves leave them be), one row per camp and package; each camp's two prisoners (894's rescue and its optional survivor) are actors of the same row.
    /// </summary>
    public static class MarshesPreoDasVillageNpcsV1
    {
        public const uint MapContextId = MarshesPreoDasNpcsV1.MapContextId;
        public const uint Toida = MarshesPreoDasNpcsV1.Toida, Siva = MarshesPaludosNpcsV1.Siva, Veska = MarshesPaludosNpcsV1.Veska;
        public const uint TorcastraPrisoner = 660022, Friendly = 1, Neutral = 3, Level = 35;

        public const uint Tola = 760376, TolaName = 8560, TolaPackage = Tola;
        public const uint Kirii = 760377, KiriiName = 8557, KiriiPackage = Kirii;
        public const uint Rakla = 760378, RaklaName = 8559, RaklaPackage = Rakla;
        public const uint RaaPrisoner = 760379, EbaePrisoner = 760380, HekalPrisoner = 760381, PrisonerName = 5289;
        public const uint RaaPackage = 1158, EbaePackage = 1159, HekalPackage = 1160;

        public static readonly (double X, double Y, double Z) TolaAt = (-115.2, 61.07, -352);
        public static readonly (double X, double Y, double Z) KiriiAt = (106, 58.85, -242);
        public static readonly (double X, double Y, double Z) RaklaAt = (90, 57.19, -244);

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, row, name, package, at) in new[]
            {
                (Tola, "Forean Warrior Tola - Village Ruins, by the arrival (891, 894)", Veska, TolaName, TolaPackage, TolaAt),
                (Kirii, "Forean Elder Kirii - Village Ruins, P'reo Das (891, 894)", Toida, KiriiName, KiriiPackage, KiriiAt),
                (Rakla, "Forean Shaman Rakla - Village Ruins, P'reo Das (892)", Siva, RaklaName, RaklaPackage, RaklaAt)
            })
            {
                yield return Copy(id, comment, row, name, Friendly, "action1, action2, action3, action4, action5, action6, action7, action8");
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")].Replace("'", "''")} - Village Ruins');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            foreach (var (id, camp, package) in new[]
            {
                (RaaPrisoner, "Ra'a Das", RaaPackage), (EbaePrisoner, "E'bae Das", EbaePackage), (HekalPrisoner, "He'kal Das", HekalPackage)
            })
            {
                yield return Copy(id, $"Forean Prisoner - Village Ruins, {camp} (894)", TorcastraPrisoner, PrisonerName, Neutral, "0, 0, 0, 0, 0, 0, 0, 0");
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, 'Forean Prisoner - Village Ruins, {camp.Replace("'", "''")}');";
            }
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Tola}, {Kirii}, {Rakla});";
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {Tola} AND {HekalPrisoner};";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Tola}, {Kirii}, {Rakla});";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Tola} AND {HekalPrisoner};";
        }

        /// <summary>A copy of <paramref name="row"/> with its own name, level 35, <paramref name="faction"/> and the attacks given.</summary>
        private static string Copy(uint id, string comment, uint row, uint name, uint faction, string actions) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, '{comment.Replace("'", "''")}', class_id, {faction}, {Level}, max_hp, {name}, run_speed, walk_speed, " +
            $"{actions} FROM creature WHERE id = {row};";
    }
}

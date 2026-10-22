using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The three corrupted leaders of the Order of Velon in Velon Hollow (squad instance 2136), for Deadly
    /// Silence (1974): the server had none.
    ///
    /// Sourced: client names (creaturenamelanguage) 10196 "Corrupted Seer Kadali", 10197 "Decayed Guardian
    /// Minash", 10198 "Taosari the Mad"; 1974's objectives "Find Guardian Minash", "Find Shaman Taosari",
    /// "Find Seer Kadali" (missionobjective) and its turn-in "Even our greatest spiritual leaders were
    /// corrupted by this infection" (missionconversation 20801). Velon Hollow's named sites (map_region
    /// 2136): Mausoleum of the Beholden (-115.2, 60.3, -488.8), Shrine of the Penitent (-119.3, 81.8,
    /// -279.5), Sanctuary of Renewal (631.6, 49.8, -343.2), each behind an ArchForeanElohCavernDoorJambV01
    /// (client map). Gamecontext 2136: levels 30-32.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - Each is a named hostile boss: class 25607 Redshirt_Forean_Gunner_Infected, the Infected Forean's
    ///    (the client's ..._Infected_Boss 25618 carries the NPC augmentation 52, so the server makes it an
    ///    NPC, not a foe; the mesh 35823 is the same), its name from the client name id, level 32, three
    ///    times the hit points of a normal level 32 hostile (median 3700 in the World DB: 11100), its
    ///    armor (median 360), no adds.
    ///  - The attack is the Infected Forean's (PlateauInfectedForeanAttackV1: Forean lightning 203/1,
    ///    147-221): the skeleton has no other animation.
    ///  - One per site, which one where is a guess: Minash (decayed) in the Mausoleum, Kadali in the
    ///    Shrine, Taosari in the Sanctuary; at the site's navmesh ground. Respawn 600 s; a squad copy keeps
    ///    a dead one dead for its RespawnMinutes.
    ///  - The way from the door to the sites is not on one navmesh region (the outpost by the door is an
    ///    island): not checked for a player (playtest).
    ///
    /// Ids: creatures 650053-650055, pools 650058-650060, free before. Down takes out only these rows.
    /// </summary>
    public static class PlateauVelonElderBossesV1
    {
        public const uint MapContextId = 2136;
        public const uint BossClass = PlateauInfectedForeansV1.InfectedClass;   // 25607 Redshirt_Forean_Gunner_Infected
        public const uint Level = 32, HitPoints = 11100, Armor = 360;
        public const uint Attack = PlateauInfectedForeansV1.Weapon;
        public const uint RespawnSeconds = 600;

        public sealed record Boss(uint Id, uint Pool, uint NameId, string Name, string Site, double X, double Y, double Z);

        public static readonly Boss Minash = new(650053, 650058, 10197, "Decayed Guardian Minash", "Mausoleum of the Beholden", -115.2, 60.59, -488.8);
        public static readonly Boss Kadali = new(650054, 650059, 10196, "Corrupted Seer Kadali", "Shrine of the Penitent", -119.3, 82.1, -279.5);
        public static readonly Boss Taosari = new(650055, 650060, 10198, "Taosari the Mad", "Sanctuary of Renewal", 631.6, 49.9, -343.2);

        public static readonly Boss[] All = { Minash, Kadali, Taosari };

        public static IEnumerable<string> Up()
        {
            foreach (var boss in All)
            {
                yield return Creature(boss.Id, $"{boss.Name} - Velon Hollow, {boss.Site}", BossClass, Level, HitPoints, boss.NameId, Attack);
                yield return Stat(boss.Id, HitPoints, Armor);
                yield return Pool(boss.Pool, boss.X, boss.Y, boss.Z, MapContextId, boss.Id, 1, 1, RespawnSeconds, 0);
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var boss in All)
            {
                yield return $"DELETE FROM spawnpool WHERE id = {boss.Pool};";
                yield return $"DELETE FROM creature_stat WHERE id = {boss.Id};";
                yield return $"DELETE FROM creature WHERE id = {boss.Id};";
            }
        }
    }
}

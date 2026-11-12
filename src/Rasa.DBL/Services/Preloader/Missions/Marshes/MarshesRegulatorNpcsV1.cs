using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 4 (the Regulator) NPCs and pool, Valverde Marshes (map 1454): Retread Wilson, who sends the player
    /// for the regulator's parts (925 receiver, 1028 giver, 1029 receiver, 1030 giver), Conscript Kaav, the Bane Traitor
    /// outside the Bane Supply Depot (1028 receiver, 1029 giver), and a pool of one Bane Strider for 1029's power core. The
    /// server had none of them.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760313-760399 free): 760313 Wilson (creature, creature_appearance, npc_package, spawn pool); 760314 Kaav (creature,
    /// creature_stat, npc_package, spawn pool); 760315 the Strider (creature, creature_stat, spawn pool).
    ///
    /// Sourced: client names 8579 "Retread Wilson", 8580 "Conscript Kaav", 9099 "Thrax Cavalryman" (creaturenamelanguage);
    /// Wilson's package 969 (objectiveconversation 925,1, "Scrap? As much fighting as goes on here, there's always plenty of
    /// scrap. Here - take this."), the Bane Traitor's 970 (objectiveconversation 1028,1, "Quickly! Take this and go before
    /// they see you!"). Client logs: Wilson "outside Academy Point" (925), "at the Logos Research Base Camp" (1029); the Bane
    /// Traitor "outside the Bane Supply Depot" (1028). TaRapedia The Regulator: Part 2 / Part 3: the Traitor is Conscript Kaav,
    /// "around the Supply Depot". Ten Ton Hammer Retread City: Wilson "to the northeast around -138, -468 and frequently
    /// engaged in battle"; the Traitor "at the refueling station around -20, -638" (two numbers, 0.3; the client's "outside
    /// the Bane Supply Depot" wins, the triage's conflict 13). World DB: the Bane Supply Depot door (map_link 41) at (451.6,
    /// 223.6, -581.9); no Strider but the boss Daddy Long-Legs 520031 on the Marshes; the Foreas Base past's Thrax Cavalryman 750100
    /// (class 9804 Vehicle_Bane_Strider_V1, PlateauForeasBasePastNpcsV1).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Wilson is a copy of Retread McCormick's row and look (510201, the Retread who gives 925: class 3846, level 10, 1000
    ///    hit points, his six appearance slots), with Wilson's name and the client's package. He stands 4 m from Ten Ton
    ///    Hammer's point, at the nearest navmesh point walked to from Retread City (probe of 2026-11-02; the point itself is
    ///    not reached). No hostile Thrax by him (Ten Ton Hammer's "engaged in battle"): no objective needs them.
    ///  - Kaav: a Thrax a player can talk to, as the Pools' Nakrinx (PoolsSupplyNpcsV1: class 11320 NPC_Bane_Thrax_Technician,
    ///    NEUTRAL, Nakrinx's hit points and armour, no attack), level 37 (the mission's), the client's package; 13 m west of the
    ///    Depot door on the navmesh, walked to from Retread City and from the door (probe).
    ///  - The Strider: the Pools' Strider row (class 9804, its two attacks, name 9099) at level 36 with the hit points and
    ///    armour of the Marshes' level-36 rows (Hunter Lieutenant 531054: 10406, 340), the triage's Q18. One pool of one, 120 s
    ///    respawn (the Marshes rows' 1200 tenths), between Kaav and Retread City (the triage), on the navmesh (probe).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); down takes out only these rows.
    /// </summary>
    public static class MarshesRegulatorNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;

        public const uint Wilson = 760313, WilsonName = 8579, WilsonPackage = 969;
        public const uint McCormick = 510201;           // Retread McCormick, Retread City: Wilson's row and look
        public static readonly (double X, double Y, double Z) WilsonAt = (-138, 217.74, -472);

        public const uint Kaav = 760314, KaavName = 8580, KaavPackage = 970;
        public const uint KaavClass = 11320, KaavLevel = 37, KaavHitPoints = 8024, KaavArmor = 310;
        public const uint Neutral = 3;
        public static readonly (double X, double Y, double Z) KaavAt = (440, 223.68, -590);

        public const uint Strider = 760315, StriderPool = 760315;
        public const uint SourceStrider = Plateau.PlateauForeasBasePastNpcsV1.Cavalryman;  // Thrax Cavalryman 750100, the Strider
        public const uint StriderLevel = 36, StriderHitPoints = 10406, StriderArmor = 340;
        public const uint StriderRespawnTenths = 1200;
        public static readonly (double X, double Y, double Z) StriderAt = (300, 222, -650);

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Wilson}, 'Retread Wilson - Marshes (925, 1028, 1029, 1030)', class_id, faction, level, max_hp, " +
                         $"{WilsonName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {McCormick};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Wilson}, slot_id, Class_id, color FROM creature_appearance WHERE id = {McCormick};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Wilson}, {WilsonPackage}, 'Retread Wilson - Marshes');";
            yield return Pool(Wilson, WilsonAt.X, WilsonAt.Y, WilsonAt.Z, MapContextId, Wilson, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Kaav}, 'Conscript Kaav - Marshes, the Bane Traitor (1028, 1029)', {KaavClass}, {Neutral}, {KaavLevel}, " +
                         $"{KaavHitPoints}, {KaavName}, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return Stat(Kaav, KaavHitPoints, KaavArmor);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Kaav}, {KaavPackage}, 'Conscript Kaav - Marshes');";
            yield return Pool(Kaav, KaavAt.X, KaavAt.Y, KaavAt.Z, MapContextId, Kaav, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Strider}, 'Thrax Cavalryman - Marshes (Strider, 1029)', class_id, faction, {StriderLevel}, " +
                         $"{StriderHitPoints}, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {SourceStrider};";
            yield return Stat(Strider, StriderHitPoints, StriderArmor);
            yield return Pool(StriderPool, StriderAt.X, StriderAt.Y, StriderAt.Z, MapContextId, Strider, 1, 1, StriderRespawnTenths, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Wilson}, {Kaav}, {StriderPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Wilson}, {Kaav});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Wilson};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Kaav}, {Strider});";
            yield return $"DELETE FROM creature WHERE id IN ({Wilson}, {Kaav}, {Strider});";
        }
    }
}

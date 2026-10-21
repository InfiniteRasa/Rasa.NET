using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Special Agent Perdu's missions' NPCs (map 1497): Clean Slate (1983), Mind Bender (905), Photo Ops
    /// (929), The Results Are In (936), Mind Slayer (719).
    ///
    /// New NPCs; the client has each name (creaturenamelanguage), the server had no creature row:
    ///  - Corporal Wainwright (6765), Clean Slate's giver. TaRapedia NPC page: AFS, male, level 32, New
    ///    Velon Village, /loc 194, 354, 519; at the floor there, 354.36 on the client's terrain.
    ///  - Outpost Cmdr. Russ (4677), Photo Ops' giver; conversation package 398 (objectiveconversation
    ///    931,8). TaRapedia NPC page: AFS, male, level 32, Wedge Rock Outpost, /loc 449, 378, 367 - the
    ///    outpost's headquarters workstation (ArchHumGenObjWorkstation2m2mV01, 449.5, 378.16, 366). He stands
    ///    beside it, where the navmesh is (452.4, 378.12, 368.5).
    ///  - Lieutenant Jackson (6764), Major Hoyle (6768), Private Tillman (6767), Sergeant Harris (6766), Mind
    ///    Bender's four soldiers, packages 633, 635, 634, 637 (objectiveconversation 905). Client text 4805:
    ///    "wandering the battlefield between Fort Defiance and Trinity Bridge". No source gives a place:
    ///    ESTIMATE, four flat places on that line, on the navmesh reached from Trinity Bridge, 100 m and more
    ///    from every hostile pool. Level 32 (ESTIMATE).
    ///  - Clean Slate's soldier, a template the mission's scene spawns: the client's Light Soldier
    ///    (29423 Redshirt_Human_Soldier_Light_Male) with the AFS pistol, as the Bootcamp escorts (510213); the
    ///    client has no name for him ("amnesia-boy", client 20894), so his name is the server's (ESTIMATE).
    ///    His berserk copy is the same model, hostile, level 30, with the Plateau Thrax Soldier's hit points
    ///    and pistol damage a little raised (ESTIMATE).
    /// Look of the new humans (ESTIMATE): the officer's uniform every generated Plateau NPC wears (Peterson's).
    ///
    /// Two NPCs move; MissionNpcSpawnpoolPreloader put each at its area's marker:
    ///  - Special Agent Perdu (510193), at the Fort Defiance dropship marker. Client 20894: "a Penumbra agent
    ///    near the Fort Defiance entrance"; 5038: "outside the Fort Defiance gates"; TaRapedia /loc -129, 398,
    ///    719. To (-129, 398.87, 719), the floor there.
    ///  - Science Officer Clark (510191), on the Wedge Rock (482.3, 424.08, 421). Client 4980 (Russ): "You'll
    ///    find him in the research tent next door"; TaRapedia /loc 464, 378, 366, by the outpost's lab counter
    ///    and microscope (ArchCormanGenObjLabcounterV02, 466, 378.16, 370.6). To (464, 378.12, 366).
    /// Down puts both back.
    ///
    /// Ids: creatures 650029-650036, action 650028, pools 650041-650046, free before. Plain SQL.
    /// </summary>
    public static class PlateauPerduNpcsV1
    {
        public const uint MapContextId = 1497;
        public const uint MaleClass = 3846;             // NPC_Human_Swapset_Male
        public const uint SoldierClass = 29423;         // Redshirt_Human_Soldier_Light_Male "Light Soldier"

        public const uint Wainwright = 650029, WainwrightPool = 650041;
        public const uint Russ = 650030, RussPool = 650042, RussPackage = 398;
        public const uint Perdu = 510193, PerduPackage = 639;
        public const uint Clark = 510191, ClarkPackage = 407;
        public const uint Soldier = 650035, Berserk = 650036, BerserkPistol = 650028;
        public const string SoldierName = "Amnesiac Soldier";

        public static readonly (double X, double Y, double Z) WainwrightAt = (194, 354.36, 519);
        public static readonly (double X, double Y, double Z) RussAt = (452.4, 378.12, 368.5);

        /// <summary>Mind Bender's soldiers: (creature and objective order, pool, name id, package, comment, place).</summary>
        public static readonly (uint Id, uint Pool, uint Name, uint Package, string Comment, (double X, double Y, double Z) At)[] Implanted =
        {
            (650031, 650043, 6764, 633, "Lieutenant Jackson", (-120, 370.62, 650)),
            (650032, 650044, 6768, 635, "Major Hoyle", (-140, 373.37, 570)),
            (650033, 650045, 6767, 634, "Private Tillman", (-90, 376.37, 460)),
            (650034, 650046, 6766, 637, "Sergeant Harris", (-150, 374.37, 360)),
        };

        /// <summary>(pool, where it was, where it goes).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Moves =
        {
            (Perdu, (-182.5, 419.28, 865.7), (-129, 398.87, 719)),
            (Clark, (482.3, 424.08, 421), (464, 378.12, 366)),
        };

        /// <summary>The generated Plateau NPCs' officer uniform (slot, class, color), as Peterson's.</summary>
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
            foreach (var sql in Npc(Wainwright, WainwrightPool, 6765, null, "Corporal Wainwright - New Velon Village", 32, WainwrightAt))
                yield return sql;
            foreach (var sql in Npc(Russ, RussPool, 4677, RussPackage, "Outpost Cmdr. Russ - Wedge Rock Outpost", 32, RussAt))
                yield return sql;
            foreach (var soldier in Implanted)
                foreach (var sql in Npc(soldier.Id, soldier.Pool, soldier.Name, soldier.Package, soldier.Comment + " - Plateau", 32, soldier.At))
                    yield return sql;

            // Clean Slate's soldier and his berserk copy: templates, no pool.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Soldier}, 'Clean Slate amnesiac soldier', {SoldierClass}, 1, 30, 1000, 0, 7, 2, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Soldier}, 13, 6271, 1);";
            yield return $"INSERT INTO creature_actor_name (id, actor_name) VALUES ({Soldier}, '{SoldierName}');";
            yield return Action(BerserkPistol, "Clean Slate berserk soldier pistol 1/133", 1, 133, 0.5, 24, 1300, 0, 95, 140, 1);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Berserk}, 'Clean Slate berserk soldier', {SoldierClass}, 0, 30, 2600, 0, 9, 5, {BerserkPistol}, 0, 0, 0, 0, 0, 0, 0);";
            yield return Stat(Berserk, 2600, 150);
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Berserk}, 13, 6271, 1);";
            yield return $"INSERT INTO creature_actor_name (id, actor_name) VALUES ({Berserk}, '{SoldierName}');";

            foreach (var move in Moves)
                yield return PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.At);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var move in Moves)
                yield return PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.Before);
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {WainwrightPool} AND {Implanted.Last().Pool};";
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {Wainwright} AND {Berserk};";
            yield return $"DELETE FROM creature_actor_name WHERE id BETWEEN {Wainwright} AND {Berserk};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Wainwright} AND {Berserk};";
            yield return $"DELETE FROM creature_stat WHERE id = {Berserk};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Wainwright} AND {Berserk};";
            yield return $"DELETE FROM creature_action WHERE id = {BerserkPistol};";
        }

        private static IEnumerable<string> Npc(uint id, uint pool, uint nameId, uint? package, string comment, uint level,
            (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({id}, '{comment}', {MaleClass}, 1, {level}, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, {slot}, {cls}, {color});";
            if (package.HasValue)
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
        }
    }
}

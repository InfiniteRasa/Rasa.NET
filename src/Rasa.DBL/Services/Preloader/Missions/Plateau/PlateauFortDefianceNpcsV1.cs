using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Fort Defiance's mission NPCs (map 1497) for The Defiant Ones (887) and the missions after it.
    ///
    /// Transportation Officer Mamede, The Defiant Ones' giver, had no creature row; the client has the
    /// name (creaturenamelanguage 8554). TaRapedia's NPC page: AFS, male, level 33, Fort Defiance,
    /// /loc -266, 421, 857 (Ten Ton Hammer: "around -265, 420, 859", and calls Mamede "she"; the
    /// TaRapedia infobox is followed). Mamede stands at the floor there, measured on the client's
    /// terrain and entity meshes (420.87), by the Fort Defiance wormhole (map marker 54, -286, 422, 843).
    /// Look (ESTIMATE): the officer's uniform the generated Fort Defiance NPCs wear (Peterson's, 510182).
    ///
    /// Three NPCs move: MissionNpcSpawnpoolPreloader put every "Fort Defiance" NPC at the dropship's map
    /// marker (Dropship Transport: Fort Defiance, 675, -192, 423, 869). The client's own texts place these
    /// three elsewhere, and two sources give each one's /loc:
    ///  - Field Lt. Peterson (510182): "Report to Field Lt. Peterson at Fort Defiance High Command"
    ///    (client 6969); the map's AFS High Command marker 71 (-68, 401, 933), the client's
    ///    ArchHumanBaseHeadquartersForeas (-68, 399.35, 930); TaRapedia /loc -66, 401, 931, Ten Ton Hammer
    ///    -66, 401, 928. To (-66, 401.12, 931), the headquarters' floor.
    ///  - Senior Engineer Mauer (510192): "Deliver the crystals [to] Senior Engineer Mauer at Fort Defiance
    ///    High Command" (client 5183); TaRapedia /loc -61, 402, 940. To (-61, 402.49, 940), the raised
    ///    floor of the headquarters' workstation (ArchHumGenObjWorkstation1m2mV01, -62.75, 402.42, 939.75).
    ///  - Bartender McLaughlin (510173): "speak to Bartender McLaughlin at the Fort Defiance Tavern"
    ///    (client 6294); TaRapedia /loc 19, 399, 925, the client's ArchHumanBaseShopTavernSmall with its
    ///    beer kegs; Ten Ton Hammer: the tavern "located east" of Peterson. To (19, 399.59, 925).
    /// Each place is on the navmesh, reached from Trinity Bridge. Down puts them back where they were.
    ///
    /// Ids: creature 750025, pool 750034, free before. Plain SQL, valid on SQLite and MySQL.
    /// </summary>
    public static class PlateauFortDefianceNpcsV1
    {
        public const uint MapContextId = 1497;
        public const uint Mamede = 750025, MamedePool = 750034, MamedeName = 8554, MamedeLevel = 33;
        public const uint MaleClass = 3846;             // NPC_Human_Swapset_Male
        public const uint Peterson = 510182, PetersonPackage = 385;
        public const uint Mauer = 510192, MauerPackage = 1094;
        public const uint McLaughlin = 510173, McLaughlinPackage = 629;

        public static readonly (double X, double Y, double Z) MamedeAt = (-266, 420.87, 857);

        /// <summary>(pool, where it was, where it goes).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Moves =
        {
            (Peterson, (-190.1, 423.45, 873.3), (-66, 401.12, 931)),
            (Mauer, (-186.3, 423.25, 864.1), (-61, 402.49, 940)),
            (McLaughlin, (-186.3, 423.45, 869.5), (19, 399.59, 925)),
        };

        /// <summary>The Fort Defiance NPCs' officer uniform (slot, class, color), as Peterson's.</summary>
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
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Mamede}, 'Transportation Officer Mamede - Fort Defiance', {MaleClass}, 1, {MamedeLevel}, 1000, " +
                $"{MamedeName}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Mamede}, {slot}, {cls}, {color});";
            yield return Pool(MamedePool, MamedeAt.X, MamedeAt.Y, MamedeAt.Z, MapContextId, Mamede, 1, 1, 20, 0);
            foreach (var move in Moves)
                yield return MoveSql(move.Pool, move.At);
        }

        public static IEnumerable<string> Down() =>
            Moves.Select(move => MoveSql(move.Pool, move.Before)).Concat(new[]
            {
                $"DELETE FROM spawnpool WHERE id = {MamedePool};",
                $"DELETE FROM creature_appearance WHERE id = {Mamede};",
                $"DELETE FROM creature WHERE id = {Mamede};"
            });

        /// <summary>The NPC's own pool, the only one it is in, to <paramref name="at"/>.</summary>
        public static string MoveSql(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} WHERE id = {pool};";
    }
}

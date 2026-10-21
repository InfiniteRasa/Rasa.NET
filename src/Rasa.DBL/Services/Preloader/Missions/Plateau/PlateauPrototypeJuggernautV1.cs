using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Prototype Juggernaut north of Trinity Bridge (map 1497) for A Bridge Too Far (713): the server
    /// had no Juggernaut at all.
    ///
    /// Sourced: "recover the CPU from the latest model of Bane Juggernauts" (client 3366); name "Advanced
    /// Juggernaut Prototype" 8772 (creaturenamelanguage), the server's boss title "Prototype Juggernaut"
    /// (BossTitles); class 24555 Vehicle_Bane_Juggernaut_Boss (client entityclass, the client's boss
    /// Juggernaut, flags with SPECIES_JUGGERNAUT 94); weapon 1/180 Weapon_Creature_Juggernaut, physical
    /// (CreatureWeaponDamage). Place: "usually on the north side of Trinity Bridge, right in the middle"
    /// (TaRapedia A Bridge Too Far, Prototype Juggernaut); "patrols the northern end of the Eloh Bridge
    /// around -170, 374, 322" (Ten Ton Hammer Fort Defiance part 1); "North of the central Trinity Bridge"
    /// (Ellatha). "It's no more difficult than the random Juggernauts"; Peterson "recommends grouping"
    /// (client 3366).
    ///
    /// Reconstructed (ESTIMATE): level 32; 9000 hit points, armor 300 - twice the Plateau Stalker
    /// (PlateauBaneStalkersV1) for a vehicle that size; its guns 1/180 at 260-390 a shot, range 60,
    /// every 1.5 s. None of the client's Juggernaut abilities (eyebeam, mortars, shockwave) is on the
    /// server. One creature at Ten Ton Hammer's place, on the navmesh's ground; respawn 600 s, radius 10.
    ///
    /// Ids: creature 650026, action 650027, pool 650035, free before. Down takes out only these rows.
    /// </summary>
    public static class PlateauPrototypeJuggernautV1
    {
        public const uint MapContextId = 1497;
        public const uint Juggernaut = 650026, JuggernautClass = 24555, JuggernautName = 8772, Level = 32;
        public const uint HitPoints = 9000, Armor = 300;
        public const uint Guns = 650027;
        public const uint Pool = 650035, RespawnSeconds = 600, Radius = 10;
        public static readonly (double X, double Y, double Z) At = (-170, 374.86, 322);

        public static IEnumerable<string> Up()
        {
            yield return Action(Guns, "Prototype Juggernaut - Plateau weapon 1/180", 1, 180, 1.0, 60.0, 1500, 0, 260, 390, 1);
            yield return Creature(Juggernaut, "Prototype Juggernaut - Plateau", JuggernautClass, Level, HitPoints, JuggernautName, Guns);
            yield return Stat(Juggernaut, HitPoints, Armor);
            yield return PlateauCreatureSqlV1.Pool(Pool, At.X, At.Y, At.Z, MapContextId, Juggernaut, 1, 1, RespawnSeconds, Radius);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {Pool};";
            yield return $"DELETE FROM creature_stat WHERE id = {Juggernaut};";
            yield return $"DELETE FROM creature WHERE id = {Juggernaut};";
            yield return $"DELETE FROM creature_action WHERE id = {Guns};";
        }
    }
}

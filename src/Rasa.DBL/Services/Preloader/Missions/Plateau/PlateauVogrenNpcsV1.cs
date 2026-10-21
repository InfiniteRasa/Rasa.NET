using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Mount Reverence's mission NPCs (map 1497) for A Voice From The Past (934) and Trinity Bridge Logos:
    /// Past (1044). Neither had a creature row, and the client has no creature name for either.
    ///
    /// Caretaker Tendahi, A Voice From The Past's giver ("The Caretaker of Mount Reverence", client 5027).
    /// TaRapedia's NPC page: Forean, AFS, male, level 34, "the Forean Elder", "just south of the Mount
    /// Reverence waypoint in a Forean hut", /loc -781, 442.6, 431.7. Tendahi stands at the floor there,
    /// measured on the client's terrain and entity meshes (442.54). Class 7035 NPC_Forean_Shaman, which the
    /// client shows as "Forean Elder"; the look as the server's Forean Council Elders (creature 38).
    ///
    /// Vogren's hologram, A Voice From The Past's receiver and Trinity Bridge Logos: Past's giver and
    /// receiver. TaRapedia's Vogren's Hologram page: Forean, at the tomb, -673, 466, 539, "In-game it is
    /// referred to as Holographic Projection Device"; the client's map has Vogren's Tomb (7649, -672, 465.45,
    /// 540) and its door (23071, -684.83, 466.59, 540), no hologram. The client's own hologram, class 21715
    /// UsableNPCVogrenHologram, is an object, and the server takes mission offers and turn-ins from
    /// creatures only. Reconstructed (ESTIMATE): a stand-in creature of class 24591 NPC_Holographic_Eloh at
    /// the tomb's floor (466.61), with Vogren's conversation package 1014 (objectiveconversation 934,1).
    ///
    /// The hologram is safe ground (SpawnPoolManager.ValidatePools): the Tomb Miasmas' pool west of the tomb
    /// (PlateauTombMiasmasV1, 650025 at -700, 540) ranged 15 m, to the tomb's door, 12 m from the hologram.
    /// Its radius narrows to 11 (ESTIMATE), 16 m off; Down puts 15 back.
    ///
    /// Both show their names through creature_actor_name (name id 0). Ids: creatures 650027-650028,
    /// pools 650039-650040, free before. Plain SQL, valid on SQLite and MySQL.
    /// </summary>
    public static class PlateauVogrenNpcsV1
    {
        public const uint MapContextId = 1497;
        public const uint Tendahi = 650027, TendahiPool = 650039, TendahiLevel = 34;
        public const string TendahiName = "Caretaker Tendahi";
        public const uint ElderClass = 7035;            // NPC_Forean_Shaman "Forean Elder"
        public const uint Hologram = 650028, HologramPool = 650040, HologramLevel = 34, HologramPackage = 1014;
        public const string HologramName = "Holographic Projection Device";
        public const uint HologramClass = 24591;        // NPC_Holographic_Eloh

        public const uint MiasmaPool = 650025, MiasmaRadius = 11;

        public static readonly (double X, double Y, double Z) TendahiAt = (-781, 442.54, 431.7);
        public static readonly (double X, double Y, double Z) HologramAt = (-673, 466.61, 539);

        /// <summary>The Forean Council Elders' staff (creature 38's appearance).</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] ElderOutfit = { (13, 10533, 1) };

        public static IEnumerable<string> Up()
        {
            yield return Npc(Tendahi, "Caretaker Tendahi - Mount Reverence", ElderClass, TendahiLevel);
            foreach (var (slot, cls, color) in ElderOutfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Tendahi}, {slot}, {cls}, {color});";
            yield return $"INSERT INTO creature_actor_name (id, actor_name) VALUES ({Tendahi}, '{TendahiName}');";
            yield return Pool(TendahiPool, TendahiAt.X, TendahiAt.Y, TendahiAt.Z, MapContextId, Tendahi, 1, 1, 20, 0);

            yield return Npc(Hologram, "Holographic Projection Device - Vogren's Tomb", HologramClass, HologramLevel);
            yield return $"INSERT INTO creature_actor_name (id, actor_name) VALUES ({Hologram}, '{HologramName}');";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Hologram}, {HologramPackage}, 'Vogren hologram');";
            yield return Pool(HologramPool, HologramAt.X, HologramAt.Y, HologramAt.Z, MapContextId, Hologram, 1, 1, 20, 0);
            yield return $"UPDATE spawnpool SET radius = {MiasmaRadius} WHERE id = {MiasmaPool};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE spawnpool SET radius = {PlateauTombMiasmasV1.Radius} WHERE id = {MiasmaPool};";
            yield return $"DELETE FROM spawnpool WHERE id IN ({TendahiPool}, {HologramPool});";
            yield return $"DELETE FROM npc_package WHERE id = {Hologram};";
            yield return $"DELETE FROM creature_actor_name WHERE id IN ({Tendahi}, {Hologram});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Tendahi};";
            yield return $"DELETE FROM creature WHERE id IN ({Tendahi}, {Hologram});";
        }

        private static string Npc(uint id, string comment, uint classId, uint level) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment.Replace("'", "''")}', {classId}, 1, {level}, 1000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
    }
}

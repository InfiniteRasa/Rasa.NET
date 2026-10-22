using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Temporal Chamber's mission NPCs and its teleporter to Alia Das (map 2029, a squad instance
    /// behind the portal at the centre of Trinity Bridge, map_link 49), for Array Of Hope (935), Future
    /// Sacrifice (1171) and The Final Assault (1167). The client's map places no NPC and no teleporter
    /// there, and no creature had their conversation packages.
    ///
    /// The map (client map and navmesh): the Eloh chamber at the origin (an arrival corridor, round room 1
    /// round (0, -24) with the hospital, a door at z -48, round room 2 round (0, -88)); Alia Das in a
    /// future the Bane hold, a Bane outpost round (630, -580) with two crashed dropships (621.5, 292.5,
    /// -604.6 and 567.6, 290.4, -703.3) and an underground facility (control room 684, 292, -368); the
    /// Foreas Base of the past round (-474, 430) (1178, not here).
    ///
    /// Veryin, the Eloh hologram (package 1199: 935 objective 1 and 1178 objective 7; creaturenamelanguage
    /// 9006 "Veryin"; TaRapedia: Array Of Hope's receiver, Future Sacrifice's giver, The Final Assault's
    /// receiver). The client's Mis_Temporal_Eloh_Veryin (24822) is an object, and the server takes offers
    /// and turn-ins from creatures only: a stand-in creature of class 24591 NPC_Holographic_Eloh, as
    /// Vogren's hologram (PlateauVogrenNpcsV1), at the far side of room 1 facing the door (ESTIMATE).
    ///
    /// Colonel Hudson (package 1246: 1171 objective 1; name 9113; TaRapedia: Future Sacrifice's receiver,
    /// The Final Assault's giver, "Step into the teleporter to my right and seek out Colonel Hudson"):
    /// class 3846 NPC_Human_Swapset_Male in Outpost Cmdr. Russ's outfit (creature 650030), by the
    /// crashed dropship in the outpost (ESTIMATE), 17 m outside Thrax pool 531644's reach.
    ///
    /// The teleporters (ESTIMATE, the client has none here): a local teleporter pair (local_teleporter_link)
    /// of the human pad 29648, as the server's others: one on Veryin's right in room 1 (10, -34), one by
    /// Hudson (604, -588). A squad copy clones them (DynamicObjectManager.CloneTemplateMap); the link is
    /// by teleporter id.
    ///
    /// Ids: creatures 650051-650052, pools 650056-650057, teleporters 997-998 (after the local pads 990-996), free before.
    /// Plain SQL, valid on SQLite and MySQL.
    /// </summary>
    public static class PlateauTemporalChamberNpcsV1
    {
        public const uint MapContextId = 2029;
        public const uint Level = 30;

        public const uint Veryin = 650051, VeryinPool = 650056, VeryinPackage = 1199, VeryinName = 9006;
        public const uint HologramClass = 24591;        // NPC_Holographic_Eloh
        public const uint Hudson = 650052, HudsonPool = 650057, HudsonPackage = 1246, HudsonName = 9113;
        public const uint HumanClass = 3846;            // NPC_Human_Swapset_Male
        public const uint OutfitOf = 650030;            // Outpost Cmdr. Russ

        public const uint ChamberPad = 997, AliaDasPad = 998;
        public const uint PadClass = 29648;             // ArchHumTeleporterV02
        public const uint LocalTeleporterType = 1;

        public static readonly (double X, double Y, double Z) VeryinAt = (0, 248.05, -40);
        public static readonly (double X, double Y, double Z) HudsonAt = (612, 294.29, -596);
        public static readonly (double X, double Y, double Z) ChamberPadAt = (10, 248.14, -34);
        public static readonly (double X, double Y, double Z) AliaDasPadAt = (604, 294.14, -588);

        public static IEnumerable<string> Up()
        {
            yield return Npc(Veryin, "Veryin - Temporal Chamber", HologramClass, VeryinName);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Veryin}, {VeryinPackage}, 'Veryin');";
            yield return Pool(VeryinPool, VeryinAt.X, VeryinAt.Y, VeryinAt.Z, MapContextId, Veryin, 1, 1, 20, 0);

            yield return Npc(Hudson, "Colonel Hudson - Temporal Chamber, Alia Das", HumanClass, HudsonName);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Hudson}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Hudson}, {HudsonPackage}, 'Colonel Hudson');";
            yield return Pool(HudsonPool, HudsonAt.X, HudsonAt.Y, HudsonAt.Z, MapContextId, Hudson, 1, 1, 20, 0);

            yield return Pad(ChamberPad, "Local Teleporter: Temporal Chamber to Alia Das", ChamberPadAt);
            yield return Pad(AliaDasPad, "Local Teleporter: Alia Das to Temporal Chamber", AliaDasPadAt);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({ChamberPad}, {AliaDasPad});";
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({AliaDasPad}, {ChamberPad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({ChamberPad}, {AliaDasPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({ChamberPad}, {AliaDasPad});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({VeryinPool}, {HudsonPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Veryin}, {Hudson});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Hudson};";
            yield return $"DELETE FROM creature WHERE id IN ({Veryin}, {Hudson});";
        }

        private static string Npc(uint id, string comment, uint classId, uint nameId) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment}', {classId}, 1, {Level}, 1000, {nameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporterType}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, {MapContextId});";
    }
}

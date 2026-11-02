using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using K = KardashAttaColonyContent;

    /// <summary>
    /// What the Kardash Atta Colony was still missing against the walkthrough
    /// (Kardash_waypoints_mysteries):
    ///
    /// - The five research outposts' waypoints. The client names them (waypointlanguage 331-335,
    ///   "Waypoint: Research Outpost Alpha" to "Epsilon") and has a static map marker at each
    ///   outpost (uimapmarker 802-806), but their teleporter rows were the zero-coordinate
    ///   placeholders: no map, no class, no position, so no pad stood in the colony and the
    ///   network the guide sends players across did not exist. Each becomes a waypoint pad
    ///   (25651, type 2) at its outpost: Beta and Gamma at the client's marker, Alpha, Delta and
    ///   Epsilon three to four and a half metres from it, where the marker stands against a wall
    ///   or, Alpha's, off the walkable floor.
    /// - Underground Mysteries (1186): Captain Reyko's Eloh artifact in the Eloh Cache, the room
    ///   the client marks Eloh Retreat (uimapmarker 889) where Champion Boritak circles. The
    ///   artifact's conversation (package 1300) is the one objective; Reyko takes the report. The
    ///   artifact is the large Eloh hologram (UsableNPCElohHologramLarge), as Timora Mines' Eloh
    ///   data is, and its scene is bound to the colony: Reyko stands on the Plains, and an unbound
    ///   scene runs on its giver's map.
    /// - Atta Control offered the moment Into the Hive is turned in at the console, as the guide
    ///   has the console begin it, by a mission-rewarded source beside the colony's own; the
    ///   colony's radio offer (KardashAttaColony) stays, for a player who turns the first down.
    ///
    /// Underground Mysteries is under the colony's revision, so the colony's mission lists take it
    /// in. Plain SQL, valid on SQLite and MySQL alike. Each change is guarded on the row as it
    /// was, and Down puts back only what is still as Up left it.
    /// </summary>
    public static class KardashWaypointsAndMysteries
    {
        public const uint WaypointPad = 25651, WaypointType = 2;

        /// <summary>A research outpost's waypoint: its id, name, where its pad stands and the client's marker for it.</summary>
        public sealed record Waypoint(uint Id, string Name, Vector3 At, Vector3 Marker);

        public static readonly Waypoint[] Waypoints =
        {
            new(331, "Waypoint: Research Outpost Alpha", new Vector3(5.0f, 272.63f, -196.9f), new Vector3(8.0f, 273.4f, -199.9f)),
            new(332, "Waypoint: Research Outpost Beta", new Vector3(388.01f, 196.82f, 281.98f), new Vector3(388.01f, 196.5f, 281.98f)),
            new(333, "Waypoint: Research Outpost Gamma", new Vector3(-171.89f, 224.83f, 163.82f), new Vector3(-171.89f, 224.5f, 163.82f)),
            new(334, "Waypoint: Research Outpost Delta", new Vector3(369.07f, 100.63f, -437.5f), new Vector3(372.07f, 100.5f, -436.0f)),
            new(335, "Waypoint: Research Outpost Epsilon", new Vector3(-224.37f, 110.23f, -48.82f), new Vector3(-224.37f, 110.04f, -51.82f))
        };

        // Underground Mysteries: the client's mission, objective and conversation.
        public const uint UndergroundMysteries = K.UndergroundMysteries;
        public const uint MysteriesName = 9098, FindArtifactsName = 9103, FindArtifactsBody = 9104;
        public const uint ArtifactPackage = 1300;

        /// <summary>UsableNPCElohHologramLarge: the Eloh artifact.</summary>
        public const uint ArtifactClass = 24732;

        /// <summary>The Eloh Cache's artifact, at the client's Eloh Retreat marker, on the floor.</summary>
        public static readonly Vector3 ArtifactAt = new(-111.49f, 192.03f, 322.62f);
        public const double ArtifactYaw = 0;

        /// <summary>missionobjectiveindicatorlanguage: Eloh Chamber.</summary>
        public const uint ElohChamberMarker = 384;

        public const uint MysteriesXp = 12000, MysteriesCredits = 1800, MysteriesLevel = 20;

        /// <summary>Atta Control's radio sources as the colony put them in, and with the turn-in at the console beside it.</summary>
        public static string AttaControlSourcesBefore() => JsonSerializer.Serialize(new[]
        {
            new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, K.AttaControlOfferKey, K.Colony)
        });

        public static string AttaControlSourcesAfter() => JsonSerializer.Serialize(new[]
        {
            new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, K.AttaControlOfferKey, K.Colony),
            MissionOfferSourceDefinition.MissionRewarded(K.IntoTheHive, K.Colony)
        });

        public static IEnumerable<string> Up()
        {
            foreach (var w in Waypoints)
                yield return $"UPDATE teleporter SET class_id = {WaypointPad}, type = {WaypointType}, pos_x = {N(w.At.X)}, pos_y = {N(w.At.Y)}, " +
                    $"pos_z = {N(w.At.Z)}, rotation = 0, map_context_id = {K.Colony} " +
                    $"WHERE id = {w.Id} AND map_context_id = 0 AND class_id = 0 AND type = 0;";

            yield return $"UPDATE mission_channel_policy SET radio_sources = {S(AttaControlSourcesAfter())} " +
                $"WHERE mission_id = {K.AttaControl} AND content_revision = {S(K.Revision)} AND radio_sources = {S(AttaControlSourcesBefore())};";

            var sql = new List<string>();
            UndergroundMysteriesRows(sql);
            foreach (var statement in sql)
                yield return statement;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var statement in InstanceMissionSql.Remove(K.Revision, UndergroundMysteries))
                yield return statement;

            yield return $"UPDATE mission_channel_policy SET radio_sources = {S(AttaControlSourcesBefore())} " +
                $"WHERE mission_id = {K.AttaControl} AND content_revision = {S(K.Revision)} AND radio_sources = {S(AttaControlSourcesAfter())};";

            foreach (var w in Waypoints)
                yield return "UPDATE teleporter SET class_id = 0, type = 0, pos_x = 0, pos_y = 0, pos_z = 0, rotation = 0, map_context_id = 0 " +
                    $"WHERE id = {w.Id} AND map_context_id = {K.Colony} AND class_id = {WaypointPad} AND type = {WaypointType} " +
                    $"AND ABS(pos_x - {N(w.At.X)}) < 0.01 AND ABS(pos_y - {N(w.At.Y)}) < 0.01 AND ABS(pos_z - {N(w.At.Z)}) < 0.01;";
        }

        /// <summary>Reyko's (package 1200): the Eloh artifact in the Eloh Cache, then back to him.</summary>
        private static void UndergroundMysteriesRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, K.Revision, UndergroundMysteries, "Underground Mysteries", MysteriesName,
                K.Reyko, K.Reyko, MysteriesLevel, K.Category);
            mission.Reward(MysteriesXp, MysteriesCredits);
            mission.Objective(1, FindArtifactsName, FindArtifactsBody, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, ArtifactPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, ElohChamberMarker, ArtifactAt.X, ArtifactAt.Y, ArtifactAt.Z, 8);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5: Underground Mysteries1186, Captain Reyko1200; objective1 Find Eloh Artifacts, the artifact's " +
                "conversation1300 ('As you analyze the Eloh artifact, the blue light fills your senses').");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://www.tentonhammer.com/guides/kardash-atta-colony-guide",
                "Captain Reyko in front of the tower gives it; the Eloh artifact in the Eloh Cache, where Champion Boritak circles " +
                "(-106, 192, 324); back to Reyko.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "KardashWaypointsAndMysteries",
                "Ours: the large Eloh hologram at the client's Eloh Retreat marker, the scene bound to the colony, " +
                $"{MysteriesXp} XP / {MysteriesCredits} credits.");

            var scene = new MissionSceneDefinition
            {
                Requirement = new CustomRequirement("character.starting-experience-completed"),
                Script = "data.sequence",
                BoundMapContextId = K.Colony,
                Dialogue = new() { new(1, ArtifactPackage, 1, transitionId: 1) },
                IndicatorMaps = new Dictionary<uint, Dictionary<uint, uint>> { [1] = new() { [ElohChamberMarker] = K.Colony } }
            };
            scene.Actors["eloh-artifact"] = new SceneActorDefinition("eloh-artifact", SceneActorKind.Object, ArtifactClass,
                new ScenePosition(ArtifactAt.X, ArtifactAt.Y, ArtifactAt.Z), ArtifactYaw,
                Conversation: new SceneObjectConversation(UndergroundMysteries, 1, ArtifactPackage, 1));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = new List<WorldIntent> { new EnsureActorIntent("ensure-eloh-artifact", "eloh-artifact") }
            };
            mission.Enable(scene);
        }

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}

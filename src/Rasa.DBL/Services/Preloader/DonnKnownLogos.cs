using System.Collections.Generic;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Reason Why (Caves of Donn) counts the Logos a player already knows (Donn_known_logos).
    ///
    /// Its objectives 11 to 13 are the three tests' Logos - Trap, Feeling and Heal - and complete
    /// when one is acquired (LogosAcquired). A player who learned them before the Caves of Donn
    /// missions came in could not acquire them again: the shrine teaches nothing new, so the
    /// objectives stayed open and the mission with them. Standley's Logos: Feeling, Trap, Heal
    /// already counts a known Logos (its scene's ExistingFactObjectiveIds); The Reason Why's scene
    /// now does too, for 11, 12 and 13. A known Logos completes its objective as the mission is
    /// taken, and on an assignment already open, when its player next comes into the world
    /// (MissionApplication.ReconcileExistingFacts) - the content revision is the same, so the open
    /// assignments keep it.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: the scene binding changes only while it is the
    /// one CavesOfDonnContent put in, and Down puts that back.
    /// </summary>
    public static class DonnKnownLogos
    {
        public const uint MissionId = CavesOfDonnContent.TheReasonWhy;
        public const string Revision = CavesOfDonnContent.Revision;

        /// <summary>The objectives that are a Logos each: Trap, Feeling, Heal.</summary>
        public static readonly uint[] LogosObjectives = { 11, 12, 13 };

        /// <summary>The scene as CavesOfDonnContent bound it (its Scene(events: true)).</summary>
        public static MissionSceneDefinition Before() => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = "data.sequence"
        };

        /// <summary>The same, with a known Logos counting.</summary>
        public static MissionSceneDefinition After()
        {
            var scene = Before();
            scene.ExistingFactObjectiveIds = new List<uint>(LogosObjectives);
            return scene;
        }

        public static IEnumerable<string> Up()
        {
            yield return Swap(Before(), After());
        }

        public static IEnumerable<string> Down()
        {
            yield return Swap(After(), Before());
        }

        private static string Swap(MissionSceneDefinition from, MissionSceneDefinition to) =>
            $"UPDATE mission_scene_binding SET bindings = {InstanceMissionSql.S(Json(to))} " +
            $"WHERE mission_id = {MissionId} AND content_revision = {InstanceMissionSql.S(Revision)} AND bindings = {InstanceMissionSql.S(Json(from))};";

        private static string Json(MissionSceneDefinition scene) => JsonSerializer.Serialize(scene, MissionContentCodec.Options);
    }
}

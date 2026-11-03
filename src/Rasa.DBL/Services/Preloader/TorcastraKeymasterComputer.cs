using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Torcastra;

namespace Rasa.Services.Preloader
{
    using Prison = TorcastraPrisonContent;

    /// <summary>
    /// Keymaster of Torcastra's computer banks where a player can use them (Torcastra_keymaster_computer).
    ///
    /// Torcastra_prison put the computer banks - the object of objective 4, the Research Ward
    /// terminal's conversation - on the map's own Bane console (-129.94, 100.2, -455.97), and its
    /// objective marker with them. That console stands behind a pipe at the back of the alcove in
    /// the service room's west wall (x -127, from the floor to over head height): nothing reaches
    /// it from where a player can stand. The client picks what it uses with a ray from the camera,
    /// which the pipe takes, and a player in the alcove is 3 m from the banks with the pipe between.
    ///
    /// The banks move out of the pipe, to the alcove's floor in front of it (-126, 101.3, -456): a
    /// metre over the floor, a metre from the pipe, in sight from the alcove and the room. Their
    /// marker moves with them. Facing as they were.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: the scene binding's actor and the marker move
    /// only while they are where Torcastra_prison put them, and Down moves them back only while
    /// they are where Up put them.
    /// </summary>
    public static class TorcastraKeymasterComputer
    {
        public const uint MissionId = TorcastraMissionsV1.Keymaster;
        public const uint Objective = 4;
        public const string Role = "computer-banks";

        public static readonly ScenePosition Was = TorcastraMissionsV1.ComputerBanks;
        public static readonly ScenePosition Now = new(-126.0f, 101.3f, -456.0f);

        /// <summary>The computer banks actor of the binding at a position.</summary>
        public static SceneActorDefinition Actor(ScenePosition at) =>
            new(Role, SceneActorKind.Object, Prison.CommLinkClass, at, TorcastraMissionsV1.ComputerBanksYaw,
                Conversation: new SceneObjectConversation(MissionId, Objective, Prison.TerminalPackage, Objective));

        public static IEnumerable<string> Up()
        {
            yield return MoveActor(Was, Now);
            yield return MoveMarker(Was, Now);
        }

        public static IEnumerable<string> Down()
        {
            yield return MoveActor(Now, Was);
            yield return MoveMarker(Now, Was);
        }

        private static string MoveActor(ScenePosition from, ScenePosition to)
        {
            var was = InstanceMissionSql.S(Json(Actor(from)));

            return $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, {was}, {InstanceMissionSql.S(Json(Actor(to)))}) " +
                $"WHERE mission_id = {MissionId} AND content_revision = {InstanceMissionSql.S(TorcastraMissionsV1.Revision)} AND INSTR(bindings, {was}) > 0;";
        }

        private static string MoveMarker(ScenePosition from, ScenePosition to) =>
            $"UPDATE mission_indicator SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE mission_id = {MissionId} AND content_revision = {InstanceMissionSql.S(TorcastraMissionsV1.Revision)} " +
            $"AND objective_id = {Objective} AND indicator_id = {TorcastraMissionsV1.Unnamed(Objective)} " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_y - {N(from.Y)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01;";

        /// <summary>The computer banks as the binding holds them at a position: their role's entry in the actors.</summary>
        public static string ActorJson(ScenePosition at) => Json(Actor(at));

        private static string Json(SceneActorDefinition actor) =>
            JsonSerializer.Serialize(Role, MissionContentCodec.Options) + ":" + JsonSerializer.Serialize(actor, MissionContentCodec.Options);

        private static string N(float value) => System.Math.Round((double)value, 3).ToString("R", CultureInfo.InvariantCulture);
    }
}

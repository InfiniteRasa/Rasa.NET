using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Navigation;
    using Structures;

    /// <summary>
    /// The server's half of the client's line of sight check.
    ///
    /// client/communicator.py's DoTargetLOS, DoUsableLOS, DoMouseLOS and DoEntityLOS all end in
    /// ReportLOS(targetId): it prints the client's own view between "-- begin LOS check (client)"
    /// and "-- end LOS check (client)" - clear, "Is blocked by Terrain" or the entity in the way,
    /// from _world.GetEntityIdInLOS(manifestation.body, target.body) - and then calls
    /// RequestLOSReport((targetId,)) for the server's view. The slash commands that reach those
    /// functions were bound in client_nca_internal.developercommands, which the retail client does
    /// not ship, so a retail client never sends it; the .los command asks for the same report.
    ///
    /// The server's view is the cover mesh's (Navigation.CoverMesh, the map's collision triangles
    /// and terrain grid): a ray from the player's eyes to the target's middle, called clear,
    /// terrain or geometry the way the client calls its own, then the points cover is judged by
    /// (Cover.SamplePoints), and for an actor the cover modifier each way and whether a creature
    /// sees the player (BehaviorManager.Sees, what decides whether it shoots). The server does not
    /// know which entity a collision triangle belongs to, so "geometry" is as close as it gets to
    /// the client's "Is blocked by [entity]".
    ///
    /// Only accounts of <see cref="GmLevel.Observer"/> and up get an answer, the level .cover
    /// and the other read-only commands need.
    /// </summary>
    public static class LosReport
    {
        public const string Rule = "------------";

        /// <summary>Ours: how far above an object's origin its ray aims, for an object has no height we know.</summary>
        public const float ObjectAimHeight = 1.0f;

        /// <summary>Ours: how far short of an object its ray stops, so the object's own collision does not block it.</summary>
        public const float ObjectStopShort = 0.5f;

        /// <summary>RequestLOSReport from a client: the report, for a GM; nothing for anyone else.</summary>
        public static void Answer(Client client, ulong targetId)
        {
            if (client?.Player == null)
                return;

            if (!ChatCommandsManager.HasLevel(client, GmLevel.Observer))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry?.Id} (level {client.AccountEntry?.Level}) sent RequestLOSReport, which needs {(byte)GmLevel.Observer}");
                return;
            }

            Send(client, targetId);
        }

        /// <summary>The report, as system messages to the client.</summary>
        public static void Send(Client client, ulong targetId)
        {
            foreach (var line in Lines(client.Player, targetId))
                CommunicatorManager.Instance.SystemMessage(client, line);
        }

        /// <summary>The report on the line of sight from the player to the entity, as the lines it is sent as.</summary>
        public static List<string> Lines(Manifestation player, ulong targetId)
        {
            var lines = new List<string> { Rule, "-- begin LOS check (server)" };

            Body(lines, player, targetId);

            lines.Add("-- end LOS check (server)");
            lines.Add(Rule);

            return lines;
        }

        private static void Body(List<string> lines, Manifestation player, ulong targetId)
        {
            var entities = EntityManager.Instance;
            var type = targetId == 0 ? EntityType.System : entities.GetEntityType(targetId);
            Actor actor = type switch
            {
                EntityType.Creature => entities.GetCreature(targetId),
                EntityType.Character => entities.GetPlayer(targetId),
                _ => null
            };
            DynamicObject dynamicObject = null;

            if (actor == null && type == EntityType.Object)
                entities.TryGetObject(targetId, out dynamicObject);

            if (actor == null && dynamicObject == null)
            {
                lines.Add(targetId == 0 ? "- No target on server" : $"- No creature, player or object {targetId} on server ({type})");
                return;
            }

            var targetMap = actor?.MapContextId ?? dynamicObject.MapContextId;
            var targetPosition = actor?.Position ?? dynamicObject.Position;

            lines.Add($"- From [{Label(player)}] at {Format(player.Position)}");
            lines.Add($"- To [{(actor != null ? Label(actor) : Label(dynamicObject))}] at {Format(targetPosition)}, {Vector3.Distance(player.Position, targetPosition):0.0} m");

            if (targetMap != player.MapContextId)
            {
                lines.Add($"- Is on map {targetMap}, not yours ({player.MapContextId})");
                return;
            }

            var cover = player.MapChannel?.Cover;

            if (cover == null)
            {
                lines.Add("- No cover file for this map: the server treats everything as in the open");
                return;
            }

            var eye = Cover.EyeOf(player);

            if (actor == null)
            {
                lines.AddRange(Describe(cover, eye, ObjectAim(eye, dynamicObject.Position), null));
                return;
            }

            lines.AddRange(Describe(cover, eye, Middle(actor), Cover.SamplePoints(actor, eye)));

            var mapChannel = player.MapChannel;

            lines.Add($"- Your shots at it: damage x{Cover.Modifier(mapChannel, player, actor):0.00}{(actor.IsCrouching ? " (it is crouched)" : "")}");
            lines.Add($"- Its shots at you: damage x{Cover.Modifier(mapChannel, actor, player):0.00}{(player.IsCrouching ? " (you are crouched)" : "")}");

            if (actor is Creature creature)
                lines.Add($"- It {(BehaviorManager.Sees(cover, creature, player) ? "sees you and will shoot" : "does not see you and will not shoot")}");
        }

        /// <summary>
        /// The ray from the eye to the aim point, in the client's words, and when there are sample
        /// points, how many are in the clear and what blocks the rest. Terrain is checked first, as
        /// CoverMesh.Blocked does.
        /// </summary>
        public static List<string> Describe(CoverMesh cover, Vector3 eye, Vector3 aim, Vector3[] points)
        {
            var lines = new List<string>();

            if (cover == null)
            {
                lines.Add("- No cover file for this map: the server treats everything as in the open");
                return lines;
            }

            lines.Add(cover.TerrainBlocks(eye, aim) ? "- Is blocked by Terrain"
                : cover.GeometryBlocks(eye, aim) ? "- Is blocked by collision geometry"
                : "- Has clear LOS on server");

            if (points == null || points.Length == 0)
                return lines;

            int clear = 0, terrain = 0, geometry = 0;

            foreach (var point in points)
            {
                if (cover.TerrainBlocks(eye, point))
                    terrain++;
                else if (cover.GeometryBlocks(eye, point))
                    geometry++;
                else
                    clear++;
            }

            lines.Add($"- Body points: {clear}/{points.Length} clear, {terrain} behind terrain, {geometry} behind geometry");

            return lines;
        }

        /// <summary>An actor's middle: half its height, crouched or standing, above where it stands.</summary>
        public static Vector3 Middle(Actor actor) => actor.Position + new Vector3(0f, Cover.HeightOf(actor) * 0.5f, 0f);

        /// <summary>
        /// Where the ray to an object ends: <see cref="ObjectAimHeight"/> above its origin, pulled
        /// <see cref="ObjectStopShort"/> back towards the eye (or to the eye itself when nearer).
        /// </summary>
        public static Vector3 ObjectAim(Vector3 eye, Vector3 position)
        {
            var aim = position + new Vector3(0f, ObjectAimHeight, 0f);
            var back = eye - aim;
            var length = back.Length();

            if (length <= ObjectStopShort)
                return eye;

            return aim + back / length * ObjectStopShort;
        }

        private static string Label(Actor actor)
        {
            var kind = actor switch
            {
                Creature c => $"creature {c.DbId}",
                Manifestation => "player",
                _ => "actor"
            };
            var name = actor is Creature cr && !string.IsNullOrEmpty(cr.ActorName) ? cr.ActorName : actor.Name;

            return string.IsNullOrEmpty(name) ? $"{kind} #{actor.EntityId}" : $"{kind} #{actor.EntityId} {name}";
        }

        private static string Label(DynamicObject dynamicObject) => $"object #{dynamicObject.EntityId} {dynamicObject.EntityClassId}";

        private static string Format(Vector3 v) => FormattableString.Invariant($"({v.X:0.0}, {v.Y:0.0}, {v.Z:0.0})");
    }
}

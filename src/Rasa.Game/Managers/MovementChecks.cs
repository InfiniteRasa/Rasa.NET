using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Game;
    using Navigation;
    using Structures;

    /// <summary>
    /// Whether a step the client took could have been taken in the world: the speed check
    /// (ManifestationManager.JudgeMove) says how far a character can go in the time, and nothing
    /// said where. A client that lies about its position within the speed budget walked through
    /// walls and through Bane force fields, climbed with nothing under it, and stood in the air.
    ///
    /// Three checks, on every accepted Move, after the hidden teleports (SecretPassages) and
    /// before the floor (SafetyFloor), each on its own setting (<see cref="MovementChecksConfig"/>):
    ///
    ///  - <b>Force fields.</b> The step crosses the sheet of an intact field whose side stops
    ///    players (<see cref="ForceFields.Crosses"/>, the test creatures already get). The field
    ///    is the one the client draws and stops at, so no honest step crosses it.
    ///  - <b>Geometry.</b> A level step - within <see cref="LevelStep"/> up or down - whose ray at
    ///    <see cref="ChestHeight"/> crosses one of the map's collision triangles
    ///    (<see cref="CoverMesh.GeometryBlocks"/>). Only level steps: a jump onto a crate and a
    ///    fall past a ledge both cross faces on the way, and a wall is walked through on the level.
    ///    The terrain is not in it, so a hill is never a wall.
    ///  - <b>Hover.</b> Nothing within <see cref="SupportDepth"/> underneath - no walkable navmesh,
    ///    no terrain, no collision triangle, no water - and not descending, for longer than
    ///    <see cref="HoverGraceMs"/>. A jump is a rise and then a descent, so it never gets there;
    ///    standing on a roof or a crate has geometry under it; a lift has its platform. Refused,
    ///    only the rises and the level steps are: a hovering player can always come down.
    ///
    /// The geometry and hover checks need the map's cover mesh (&lt;map&gt;.cover, built by
    /// Rasa.NavMesh --cover-only, the same file cover and line of sight read); a map without one
    /// gets the force field check alone.
    ///
    /// A refusal is the speed refusal's: the Move is dropped and the client put back where the
    /// server has the player. Log does the same line and lets the Move through. Either way the
    /// line is rate-limited per player, as the speed refusal's is.
    /// </summary>
    public static class MovementChecks
    {
        /// <summary>How high above the feet the wall ray is cast: above a kerb or a stair, below a wall's top.</summary>
        public const float ChestHeight = 1.2f;

        /// <summary>Up to this much rise or drop a step is level, and gets the wall ray.</summary>
        public const float LevelStep = 0.5f;

        /// <summary>A step shorter than this is not looked at: standing still, turning.</summary>
        public const float MinStep = 0.05f;

        /// <summary>How far below the feet something has to be for the player to be standing on it.</summary>
        public const float SupportDepth = 6f;

        /// <summary>How long a player may be unsupported and not descending before they are in the air: longer than any jump.</summary>
        public const long HoverGraceMs = 3000;

        /// <summary>Quiet time between one player's lines, per check.</summary>
        public const long QuietMs = 5000;

        public static MovementChecksConfig Config { get; set; } = new MovementChecksConfig();

        public enum Check
        {
            ForceField,
            Geometry,
            Hover
        }

        /// <summary>What a check found, and whether the Move is to be refused for it.</summary>
        public readonly struct Finding
        {
            public Finding(Check check, string what, bool refuse)
            {
                Check = check;
                What = what;
                Refuse = refuse;
            }

            public Check Check { get; }
            public string What { get; }
            public bool Refuse { get; }
        }

        /// <summary>
        /// Looks at the step from where the server has the player to where the client says they
        /// are. Returns the first finding, with whether to refuse for it; null when the step is
        /// fine or every check is off. Logging is the caller's (<see cref="Report"/>).
        /// </summary>
        public static Finding? Judge(Manifestation player, Vector3 from, Vector3 to, long now)
        {
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return null;

            var config = Config ?? new MovementChecksConfig();

            if (Mode(config.ForceFields) != MovementChecksConfig.Off)
            {
                var field = CrossedField(ForceFields.OnMap(mapChannel), from, to);

                if (field != null)
                    return new Finding(Check.ForceField, $"through force field {field.Id} ({field.Class.Key}) at {field.Position}",
                        Mode(config.ForceFields) == MovementChecksConfig.Refuse);
            }

            if (Mode(config.Geometry) != MovementChecksConfig.Off && ThroughGeometry(mapChannel.Cover, from, to))
                return new Finding(Check.Geometry, "through the map's collision", Mode(config.Geometry) == MovementChecksConfig.Refuse);

            if (Mode(config.Hover) != MovementChecksConfig.Off)
            {
                var descending = player.Fall.Descending || to.Y < from.Y - FallDamage.LevelTolerance;

                if (descending || IsSupported(mapChannel, to))
                {
                    player.UnsupportedSinceTick = 0;
                }
                else
                {
                    if (player.UnsupportedSinceTick == 0)
                        player.UnsupportedSinceTick = now;
                    else if (now - player.UnsupportedSinceTick > HoverGraceMs)
                        return new Finding(Check.Hover, $"in the air for {(now - player.UnsupportedSinceTick) / 1000} s",
                            Mode(config.Hover) == MovementChecksConfig.Refuse);
                }
            }

            return null;
        }

        /// <summary>The intact, player-stopping field whose sheet the step crosses, or null.</summary>
        public static ForceFields.Field CrossedField(IEnumerable<ForceFields.Field> fields, Vector3 from, Vector3 to)
        {
            if (fields == null || Vector3.DistanceSquared(from, to) < MinStep * MinStep)
                return null;

            foreach (var field in fields)
                if (ForceFields.StopsPlayer(field) && ForceFields.Crosses(field.Class, field.Position, field.Yaw, from, to))
                    return field;

            return null;
        }

        /// <summary>Whether a level step's chest-height ray crosses a collision triangle. False with no cover mesh.</summary>
        public static bool ThroughGeometry(CoverMesh cover, Vector3 from, Vector3 to)
        {
            if (cover == null)
                return false;

            if (Math.Abs(to.Y - from.Y) > LevelStep)
                return false;

            var horizontal = new Vector2(to.X - from.X, to.Z - from.Z);

            if (horizontal.LengthSquared() < MinStep * MinStep)
                return false;

            var lift = new Vector3(0f, ChestHeight, 0f);

            return cover.GeometryBlocks(from + lift, to + lift);
        }

        /// <summary>
        /// Whether something is under the position within <see cref="SupportDepth"/>: walkable
        /// navmesh, terrain, a collision triangle, or water. A map with no cover mesh supports
        /// everyone: without the collision triangles a roof, a crate or a lift platform is nothing
        /// the server can see under a player, and the navmesh alone would call standing on any of
        /// them hovering. The cover files are built per server (Rasa.NavMesh --cover-only) and
        /// not every map has one.
        /// </summary>
        public static bool IsSupported(MapChannel mapChannel, Vector3 position)
        {
            var navMesh = mapChannel.NavMesh;
            var cover = mapChannel.Cover;

            if (cover == null)
                return true;

            if (FallDamage.InWater(mapChannel.MapInfo?.MapName, position))
                return true;

            if (navMesh != null)
            {
                // The nearest walkable point in the band from SupportDepth below the feet up to
                // the feet: searched from the band's middle so a surface above the player, which
                // a column search centred on them could answer with, is not what comes back.
                var band = new Vector3(position.X, position.Y - SupportDepth / 2f, position.Z);
                var walkable = navMesh.NearestInColumn(band, SupportDepth / 2f + SafetyFloor.GroundTolerance);

                if (walkable.HasValue && walkable.Value.Y <= position.Y + SafetyFloor.GroundTolerance)
                    return true;
            }

            var terrain = cover.TerrainHeight(position.X, position.Z);

            // Terrain just under them, or above them: a cave is under the terrain and has the
            // navmesh's say; what this is for is a player far above it. Off the edge of a map
            // that has terrain, nothing is known and nothing is said.
            if (terrain.HasValue ? terrain.Value >= position.Y - SupportDepth : cover.HasTerrain)
                return true;

            var top = position + new Vector3(0f, 0.3f, 0f);
            var bottom = position - new Vector3(0f, SupportDepth, 0f);

            return cover.GeometryBlocks(top, bottom);
        }

        /// <summary>
        /// The finding's Security line, rate-limited per player: one line per <see cref="QuietMs"/>,
        /// carrying how many went unreported in between.
        /// </summary>
        public static void Report(Client client, Finding finding, Vector3 from, Vector3 to, long now)
        {
            var player = client?.Player;

            if (player == null)
                return;

            player.MovementCheckHits++;

            if (player.MovementCheckLogTick != 0 && now < player.MovementCheckLogTick + QuietMs)
                return;

            var repeat = player.MovementCheckHits > 1 ? $" ({player.MovementCheckHits} since the last of these)" : "";
            var action = finding.Refuse ? "put back" : "let through (log mode)";

            Logger.WriteLog(LogType.Security,
                $"{player.FamilyName} moved {finding.What} on map {player.MapContextId}, from {from} to {to}{repeat}; {action}.");

            player.MovementCheckLogTick = now;
            player.MovementCheckHits = 0;
        }

        private static string Mode(string value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                MovementChecksConfig.Refuse => MovementChecksConfig.Refuse,
                MovementChecksConfig.Off => MovementChecksConfig.Off,
                _ => MovementChecksConfig.Log
            };
        }
    }
}

using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Structures;

    /// <summary>
    /// A floor under every map: a player who falls below it has fallen out of the world - off an
    /// edge, through a gap in the terrain or the collision - and is put back where they last stood.
    ///
    /// Nothing in the client does this. A player who falls out keeps falling for as long as the
    /// game runs; once they pass 32 768 m down the position packets' 24-bit coordinates wrap to the
    /// top of the world, the server refuses the jump and puts them back, and they are stuck
    /// falling and being put back until they relog - into the same fall.
    ///
    /// Where the floor is: <see cref="Margin"/> below the lowest thing on the map, as far as its
    /// navmesh knows - the lowest walkable surface or the lowest geometry the navmesh was built
    /// from, whichever is lower (MapChannel.SafetyFloorY, set by NavMeshManager). Nothing can be
    /// stood on beneath that, so nobody below it got there on purpose. A map with no navmesh has
    /// no floor.
    ///
    /// Where they are put back, in order:
    ///   1. the last place they stood on this map: the last Move that was not part of a descent and
    ///      was on walkable ground (on the navmesh, or in one of the map's water planes);
    ///   2. where they last arrived on this map (a teleport, a map change, login), moved to the
    ///      nearest walkable point;
    ///   3. the highest walkable surface above the point they fell through.
    /// Putting them there ends the fall (Manifestation.PlaceAt), so it costs no falling damage.
    ///
    /// Checked on every accepted Move after the secret passages (SecretPassages), so a pane that
    /// catches a jumper on purpose - the Torden Abyss leap - always wins over the floor, which is
    /// hundreds of metres below it; and on entering the world, for a character saved below it.
    /// </summary>
    public static class SafetyFloor
    {
        /// <summary>How far below the lowest thing on the map the floor is.</summary>
        public const float Margin = 50f;

        /// <summary>How near the navmesh a position has to be, up or down, to count as standing on it.</summary>
        public const float GroundTolerance = 2.5f;

        /// <summary>How often a player's last safe position is refreshed. A player walking sends ten Moves a second.</summary>
        public const long NoteIntervalMs = 250;

        /// <summary>The floor under a map with this navmesh height range.</summary>
        public static float FloorFor(float lowest) => lowest - Margin;

        /// <summary>The floor of the map, or null when it has none (no navmesh).</summary>
        public static float? Floor(MapChannel mapChannel) => mapChannel?.SafetyFloorY;

        /// <summary>Whether a position is below the map's floor.</summary>
        public static bool IsBelow(MapChannel mapChannel, Vector3 position)
        {
            var floor = Floor(mapChannel);

            return floor != null && position.Y < floor.Value;
        }

        /// <summary>
        /// One accepted Move. Returns true when it went below the floor and the player has been put
        /// back: the Move is then spent. Otherwise notes the position if it is a safe one.
        /// </summary>
        public static bool OnMove(Client client, Vector3 to, long now)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return false;

            if (IsBelow(mapChannel, to))
                return Rescue(client, "fell below the floor");

            return false;
        }

        /// <summary>
        /// After the fall tracker has seen the Move: if the player is standing somewhere, that is
        /// where they go back to if they later fall out of the world.
        /// </summary>
        public static void NoteStanding(Client client, long now)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null || player.Fall.Descending || now - player.LastSafeTick < NoteIntervalMs)
                return;

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
                return;

            var position = player.Position;

            if (IsBelow(mapChannel, position) || !IsStanding(mapChannel, position))
                return;

            player.LastSafePosition = position;
            player.LastSafeMapContextId = player.MapContextId;
            player.LastSafeTick = now;
        }

        /// <summary>
        /// On walkable ground: the navmesh surface is within GroundTolerance of the position, or it
        /// is in one of the map's water planes. With no navmesh, anywhere not falling will do.
        /// </summary>
        public static bool IsStanding(MapChannel mapChannel, Vector3 position)
        {
            var navMesh = mapChannel.NavMesh;

            if (navMesh == null)
                return true;

            var ground = navMesh.GroundHeight(position);

            if (ground != null && Math.Abs(ground.Value - position.Y) <= GroundTolerance)
                return true;

            return FallDamage.InWater(mapChannel.MapInfo?.MapName, position);
        }

        /// <summary>
        /// A character entering the world: one saved below the floor - who logged out, or dropped,
        /// mid-fall - is put back before they fall any further.
        /// </summary>
        public static void OnEnteredWorld(Client client)
        {
            var player = client?.Player;

            if (player?.MapChannel == null)
                return;

            if (IsBelow(player.MapChannel, player.Position))
            {
                Rescue(client, "entered the world below the floor");
                return;
            }

            if (player.ArrivalMapContextId != player.MapContextId || player.ArrivalPosition == null)
            {
                player.ArrivalPosition = player.Position;
                player.ArrivalMapContextId = player.MapContextId;
            }
        }

        /// <summary>Where a player who fell out at their current position goes back to, and why there; null when nowhere.</summary>
        public static (Vector3 Position, string Where)? Destination(Manifestation player)
        {
            var mapChannel = player.MapChannel;
            var floor = Floor(mapChannel) ?? float.MinValue;

            if (player.LastSafePosition is Vector3 safe && player.LastSafeMapContextId == player.MapContextId && safe.Y > floor)
                return (safe, "where they last stood");

            if (player.ArrivalPosition is Vector3 arrival && player.ArrivalMapContextId == player.MapContextId && arrival.Y > floor)
            {
                // Snapped to the navmesh: an arrival can be in mid-air (a GM teleport), and putting
                // them back there would only start the same fall again.
                var near = NavMeshManager.NearestWalkable(mapChannel, arrival) ?? TopOfColumn(mapChannel, arrival);

                if (near != null)
                    return (near.Value, "where they arrived");
            }

            var surface = TopOfColumn(mapChannel, player.Position);

            return surface != null ? (surface.Value, "the ground above where they fell") : null;
        }

        /// <summary>The highest walkable surface over (x, z): the ground, not a cave under it. Null without a navmesh or with nothing there.</summary>
        public static Vector3? TopOfColumn(MapChannel mapChannel, Vector3 position)
        {
            var top = mapChannel?.TopWalkableY;
            var floor = Floor(mapChannel);

            if (top == null || floor == null)
                return null;

            var column = new Vector3(position.X, top.Value + 2f, position.Z);

            return NavMeshManager.NearestInColumn(mapChannel, column, top.Value - floor.Value + 2f);
        }

        /// <summary>Puts a player who has fallen out of the world back. False when there is nowhere to put them.</summary>
        public static bool Rescue(Client client, string what)
        {
            var player = client.Player;
            var from = player.Position;
            var destination = Destination(player);

            if (destination == null)
            {
                Logger.WriteLog(LogType.Error, $"{player.FamilyName} {what} of map {player.MapContextId} at {from}, and there is nowhere to put them back.");
                return false;
            }

            var (to, where) = destination.Value;
            var view = client.Movement?.ViewDirection ?? new Vector2((float)player.Rotation, 0f);
            var movement = new Movement(to, view);

            Logger.WriteLog(LogType.Network, $"{player.FamilyName} {what} of map {player.MapContextId} at {from}; put back {where}, {to}.");

            player.PlaceAt(to);
            client.Movement = movement;

            client.MoveObject(player.EntityId, movement);

            if (player.MapChannel != null)
                client.CellMoveObject(client, new Packets.Protocol.MoveObjectMessage(player.EntityId, movement), true);

            return true;
        }
    }
}

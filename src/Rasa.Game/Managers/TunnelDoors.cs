using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The doors of the Bane industrial tunnel mouths: put in by the server, opening as a player
    /// comes up to one and closing behind them.
    ///
    /// The mouth the .maps place, ArchBaneIndustrialTunnelEntrance32mV01 (6102), is a door by its
    /// class (augmentation 10, Door) and has a door's states and animations in usabledata, but its
    /// mesh (arch_bane_industrial_tunnel_entrance_32m_v01.geo) has no door in it: one bone, the
    /// structure, and a collision surface that leaves the way in open. The animations
    /// (arch_bane_industrial_tunnel_entrance_32m_v01_closed_to_open.anm and _open_to_closed.anm)
    /// move bones that mesh does not have - Bip01 L_Low_Wing, L_Mid_Wing, L_Top_Wing and the right
    /// ones, the sliders, Door_Main - and those are the bones of the door's own mesh,
    /// arch_bane_industrial_tunnel_entrance_door_v01.geo: UsableDoorBaneIndustrialTunnelEntranceV01
    /// (26128), the same door states and animations, its open and close sounds
    /// (usabledata.specialFX: arch_bane_door_industrial_open.pkg, _close.pkg), and a collision box
    /// across the mouth, 23.5 m wide and 18.6 m high, z -19 to -12 in the tunnel's frame. No .map
    /// places that class. So the server puts one at each of the twenty-two tunnel mouths, at the
    /// mouth's own origin and turned as it is (all of them stand level and unscaled).
    ///
    /// Each is server Scenery (DynamicObjectType.Scenery): not targetable, out of service, so the
    /// client offers no Use, and given its state on creation (UsableInfo), which is what the
    /// client's Door takes its collision from - the box is in while it is closed, out while it is
    /// open (door.py _UpdateCollision). It stands closed. A living player within
    /// <see cref="OpenRadius"/> of the middle of the door, level, and <see cref="ReachHeight"/> of
    /// it upright opens it: Use to OPEN to everyone in range (the opening animation, about 1.9 s,
    /// and the open sound). Once nobody has been within <see cref="OpenRadius"/> for
    /// <see cref="CloseAfterMs"/> it closes the same way (about 1 s). Its state follows, so a
    /// client that comes into range is shown it as it is. Creatures do not open them.
    ///
    /// Five of the mouths have an instance's way in a few metres from them, in the tunnel
    /// (Bane Conscript Facility, Bane Fluxite Mines, Torcastra Prison, Timora Mines, the Test
    /// Weapons Center); the door is open by the time anyone gets there.
    /// </summary>
    public static class TunnelDoors
    {
        public sealed class Tunnel
        {
            public Tunnel(uint mapContextId, ulong mouthEntityId, Vector3 position, float yaw, string mapName)
            {
                MapContextId = mapContextId;
                MouthEntityId = mouthEntityId;
                Position = position;
                Yaw = yaw;
                MapName = mapName;
            }

            public uint MapContextId { get; }

            /// <summary>The .map's id for the tunnel mouth (6102) the door is put in.</summary>
            public ulong MouthEntityId { get; }

            /// <summary>The mouth's origin, which is the door's.</summary>
            public Vector3 Position { get; }

            /// <summary>The mouth's turn about the vertical, as a DynamicObject's Rotation.</summary>
            public float Yaw { get; }

            public string MapName { get; }

            /// <summary>The middle of the door: its box's, in the tunnel's frame, turned and moved as the tunnel is.</summary>
            public Vector3 DoorMiddle => Position + Vector3.Transform(DoorMiddleInTunnel, Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));

            public override string ToString() => $"tunnel door at {MapName} ({Position.X:F0}, {Position.Y:F0}, {Position.Z:F0})";
        }

        public const EntityClasses DoorClass = (EntityClasses)26128;   // UsableDoorBaneIndustrialTunnelEntranceV01

        /// <summary>The middle of the door's collision box in the tunnel's frame (x -11.7..11.8, y -0.3..18.6, z -19.0..-12.1).</summary>
        public static readonly Vector3 DoorMiddleInTunnel = new Vector3(0f, 9.1f, -15.5f);

        /// <summary>How near the middle of the door, level, a player opens it from.</summary>
        public const float OpenRadius = 14f;

        /// <summary>And upright, either way: the door is 18.6 m high, and the ground outside a mouth falls away from it.</summary>
        public const float ReachHeight = 16f;

        /// <summary>How long nobody has to have been near before it closes.</summary>
        public const long CloseAfterMs = 4000;

        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static readonly IReadOnlyList<Tunnel> All = new[]
        {
            new Tunnel(1734, 134419591463296, new Vector3(428.0f, 304.0f, 204.0f), 0f, "adv_arieki_ligo_ashendesert"),
            new Tunnel(1988, 134419591467840, new Vector3(6.5f, 72.0f, -281.25f), -MathF.PI, "adv_arieki_ligo_ashendesert_baneconscriptfacility"),
            new Tunnel(1988, 134419591467841, new Vector3(6.5f, 72.0f, -65.25f), 0f, "adv_arieki_ligo_ashendesert_baneconscriptfacility"),
            new Tunnel(2055, 134419591463640, new Vector3(-292.0f, 176.0f, -288.0f), MathF.PI / 2, "adv_arieki_ligo_ashendesert_indracaverns"),
            new Tunnel(2028, 134419591466318, new Vector3(286.0f, 544.0f, 154.0f), 0f, "adv_arieki_torden_abyss"),
            new Tunnel(1759, 134419591463243, new Vector3(-415.0f, 241.0f, 952.0f), 0f, "adv_arieki_torden_mires"),
            new Tunnel(1148, 132770324747669, new Vector3(184.0f, 204.0f, -1028.0f), MathF.PI / 2, "adv_foreas_concordia_divide"),
            new Tunnel(1148, 132770324748883, new Vector3(-574.0f, 187.0f, -1133.0f), MathF.PI, "adv_foreas_concordia_divide"),
            new Tunnel(1348, 133625022543740, new Vector3(-152.0f, 204.0f, 484.0f), 0f, "adv_foreas_concordia_divide_timoramines"),
            new Tunnel(1348, 133625022544880, new Vector3(432.0f, 232.0f, -200.0f), MathF.PI / 2, "adv_foreas_concordia_divide_timoramines"),
            new Tunnel(1349, 133629317497591, new Vector3(-272.0f, 12.0f, 312.0f), -MathF.PI / 2, "adv_foreas_concordia_divide_torcastraprison"),
            new Tunnel(1244, 133182640958055, new Vector3(572.0f, 132.0f, -712.0f), -MathF.PI, "adv_foreas_concordia_palisades"),
            new Tunnel(2047, 134419591464678, new Vector3(177.5f, 240.5f, 37.0f), MathF.PI / 2, "adv_foreas_valverde_descent"),
            new Tunnel(2047, 134419591464679, new Vector3(333.25f, 260.0f, -54.0f), MathF.PI / 2, "adv_foreas_valverde_descent"),
            new Tunnel(1454, 134084584022817, new Vector3(725.4296f, 224.0f, 455.7164f), MathF.PI / 2, "adv_foreas_valverde_marshes"),
            new Tunnel(1454, 134084584050081, new Vector3(-134.9882f, 222.3501f, -612.6696f), -1.1846f, "adv_foreas_valverde_marshes"),
            new Tunnel(1454, 134084584050094, new Vector3(-223.9171f, 286.3501f, -576.5098f), 1.957f, "adv_foreas_valverde_marshes"),
            new Tunnel(1743, 134419591469447, new Vector3(25.0f, 64.0f, 349.0f), 0f, "adv_foreas_valverde_marshes_villageruins"),
            new Tunnel(1497, 134269267625733, new Vector3(232.0f, 312.0f, -240.0f), -MathF.PI / 2, "adv_foreas_valverde_plateau"),
            new Tunnel(1830, 134419591464496, new Vector3(-124.0f, 44.0f, 140.0f), -MathF.PI, "adv_foreas_valverde_plateau_maligobasev3"),
            new Tunnel(1304, 133436043986226, new Vector3(466.0f, 684.0f, 584.0f), -MathF.PI / 2, "adv_foreas_valverde_pools"),
            new Tunnel(1304, 133436043987758, new Vector3(-228.0f, 684.0f, -288.0f), -MathF.PI / 2, "adv_foreas_valverde_pools"),
        };

        private sealed class Door
        {
            public Door(Tunnel tunnel, DynamicObject obj)
            {
                Tunnel = tunnel;
                Object = obj;
            }

            public Tunnel Tunnel { get; }
            public DynamicObject Object { get; }

            /// <summary>When a player was last near it, while it is open.</summary>
            public long LastNearAt { get; set; }

            /// <summary>The last player near it: whom the close is played for.</summary>
            public ulong LastNearBy { get; set; }
        }

        private static readonly ConditionalWeakTable<MapChannel, List<Door>> Doors = new();

        /// <summary>The map's tunnels.</summary>
        public static IEnumerable<Tunnel> OnMap(uint mapContextId) => All.Where(tunnel => tunnel.MapContextId == mapContextId);

        /// <summary>Puts the doors on every loaded map that has tunnels. Runs after MapChannelInit.</summary>
        public static void Init()
        {
            var placed = 0;

            foreach (var mapContextId in All.Select(tunnel => tunnel.MapContextId).Distinct())
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(mapContextId);

                if (mapChannel != null)
                    placed += Place(mapChannel);
            }

            Logger.WriteLog(LogType.Initialize, $"Placed {placed} of {All.Count} Bane tunnel doors");
        }

        /// <summary>
        /// Puts this map's tunnel doors on one channel of it - the open world's or a copy - closed,
        /// and tells whoever is in range. Returns how many were put in.
        /// </summary>
        public static int Place(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null)
                return 0;

            if (EntityClassManager.Instance.GetClassInfo(DoorClass) == null)
            {
                Logger.WriteLog(LogType.Error, $"Entity class {(uint)DoorClass} is not loaded; the Bane tunnel doors are left out.");
                return 0;
            }

            var doors = Doors.GetOrCreateValue(mapChannel);
            var placed = 0;

            foreach (var tunnel in OnMap(mapChannel.MapInfo.MapContextId))
            {
                if (doors.Any(door => door.Tunnel == tunnel))
                    continue;

                var obj = new DynamicObject
                {
                    EntityClassId = DoorClass,
                    DynamicObjectType = DynamicObjectType.Scenery,
                    StateId = UseObjectState.DoorStateClosed,
                    ObjectData = tunnel,
                    Position = tunnel.Position,
                    Rotation = tunnel.Yaw,
                    MapContextId = tunnel.MapContextId,
                    TargetCategory = TargetCategory.Object,
                    Comment = tunnel.ToString(),
                    IsInWorld = true
                };

                CellManager.Instance.AddToWorld(mapChannel, obj);
                doors.Add(new Door(tunnel, obj));
                placed++;
            }

            return placed;
        }

        /// <summary>The door put in a tunnel on a map channel, or null.</summary>
        public static DynamicObject DoorOf(MapChannel mapChannel, Tunnel tunnel)
        {
            if (mapChannel == null || !Doors.TryGetValue(mapChannel, out var doors))
                return null;

            return doors.FirstOrDefault(door => door.Tunnel == tunnel)?.Object;
        }

        /// <summary>Whether a player is near enough a tunnel's door to open it.</summary>
        public static bool IsNear(Tunnel tunnel, Vector3 position)
        {
            var middle = tunnel.DoorMiddle;
            var level = new Vector2(position.X - middle.X, position.Z - middle.Z);

            return level.Length() <= OpenRadius && MathF.Abs(position.Y - middle.Y) <= ReachHeight;
        }

        /// <summary>Opens the doors a player has come up to, and closes those nobody has been near for long enough.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel == null || !Doors.TryGetValue(mapChannel, out var doors) || doors.Count == 0)
                return;

            var now = Now();
            var players = mapChannel.ClientList
                .Where(client => client?.Player != null && client.State == ClientState.Ingame && client.Player.State != CharacterState.Dead)
                .Select(client => client.Player)
                .ToList();

            foreach (var door in doors)
            {
                var obj = door.Object;

                if (!obj.IsInWorld)
                    continue;

                var near = players.FirstOrDefault(player => IsNear(door.Tunnel, player.Position));

                if (near != null)
                {
                    door.LastNearAt = now;
                    door.LastNearBy = near.EntityId;

                    if (obj.StateId == UseObjectState.DoorStateClosed)
                        Set(mapChannel, obj, UseObjectState.DoorStateOpen, near.EntityId);
                }
                else if (obj.StateId == UseObjectState.DoorStateOpen && now - door.LastNearAt >= CloseAfterMs)
                    Set(mapChannel, obj, UseObjectState.DoorStateClosed, door.LastNearBy);
            }
        }

        /// <summary>Takes the doors off a map channel's books (not the map): for tests and a channel being torn down.</summary>
        public static void Forget(MapChannel mapChannel)
        {
            if (mapChannel != null)
                Doors.Remove(mapChannel);
        }

        private static void Set(MapChannel mapChannel, DynamicObject obj, UseObjectState state, ulong actorId)
        {
            obj.StateId = state;
            CellManager.Instance.CellCallMethod(mapChannel, obj, new UsePacket(actorId, state, 0));
        }
    }
}

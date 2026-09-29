using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Force fields: the shields that closed the gates of outposts and bases, placed by a GM with
    /// .placefield for now.
    ///
    /// From the client:
    ///  - three augmentations: FORCEFIELD (62, "blocks enemy forces, allows allies to pass"),
    ///    CLANFORCEFIELD (79) and OWNABLEFORCEFIELD (84), all ForceFieldBase, a Usable;
    ///  - ForceFieldBase blocks its avatar until told otherwise: Recv_BlockInfo(actorId, doesBlock)
    ///    for the client's own manifestation sets SetNoBlockCollision, SetPickable and
    ///    SetIsTargetable on the body, and a destroyed field never blocks. So a field that lets
    ///    a player through cannot be targeted by them, and one that stops them can;
    ///  - FORCEFIELD's states are USE_FF_STATE_FACTION_A_/B_ INTACT, 75P_HEALTH, 25P_HEALTH and
    ///    DESTROYED (191-194, 196-199); CLANFORCEFIELD's USE_CFF_STATE_AFS_OWNS / BANE_OWNS /
    ///    CLAN_OWNS (211-213), its damage animations picked from the hit points, and
    ///    USE_STATE_DESTROYED; OWNABLEFORCEFIELD takes a target category (Recv_TargetCategory) and
    ///    a hue (Recv_SetHue, 0-255 each) and is destroyed at USE_STATE_DESTROYED;
    ///  - hit points through Recv_DamageInfo and Recv_UpdateHitPoints, as any destroyable usable.
    ///
    /// The maps build the gates - human outpost fences, fort walls, Bane outpost walls - with
    /// their openings empty; no map places a field in one. Each field class's mesh is its gate's
    /// with "_forcefield" on the name, so the fields were the server's to put there. Their
    /// extents (<see cref="FieldClass"/>) are the meshes' bounding boxes from the client's
    /// mesh02/08/09.glm: a sheet in the field's local X and Y, a few centimetres thick in Z.
    ///
    /// Ours, with nothing in the data to say otherwise:
    ///  - faction A is the AFS and faction B the Bane. A field of a side lets that side through:
    ///    an A field lets players and friendly creatures pass and stops hostile ones; a B field
    ///    stops players and friendly creatures and lets the Bane pass;
    ///  - hit points (<see cref="DefaultHealth"/> unless the GM gives some), the states at more
    ///    than 75%, more than 25% and above none, and no repair but a GM's;
    ///  - who can hurt one: whoever it stops, which for now is a player shooting a B field;
    ///  - a creature it stops just stops, where the field is, and goes no further until it is down.
    /// </summary>
    public static class ForceFields
    {
        public const int DefaultHealth = 5000;

        /// <summary>How far outside the field's sheet a creature is still held back, around its own middle.</summary>
        public const float CreatureRadius = 0.5f;

        /// <summary>How far below the sheet's lowest point a creature's feet can be and still meet it.</summary>
        public const float BelowSheet = 2.0f;

        public enum Kind
        {
            Faction,    // FORCEFIELD 62
            Clan,       // CLANFORCEFIELD 79
            Ownable     // OWNABLEFORCEFIELD 84
        }

        public enum Side
        {
            A,          // AFS: ours
            B           // Bane: ours
        }

        public sealed class FieldClass
        {
            public FieldClass(string key, uint classId, Kind kind, Vector3 min, Vector3 max, string gate)
            {
                Key = key;
                ClassId = classId;
                Kind = kind;
                Min = min;
                Max = max;
                Gate = gate;
            }

            public string Key { get; }
            public uint ClassId { get; }
            public Kind Kind { get; }
            public Vector3 Min { get; }
            public Vector3 Max { get; }
            public string Gate { get; }
        }

        /// <summary>The field classes and their meshes' bounds (local, metres).</summary>
        public static readonly FieldClass[] Classes =
        {
            new FieldClass("humgate", 20000001, Kind.Faction, new Vector3(-6.48f, 1.12f, -0.17f), new Vector3(6.48f, 9.36f, 0.17f), "human outpost fence gate (ArchHumOutpostFenceGateV01/V02)"),
            new FieldClass("humbase", 9571, Kind.Faction, new Vector3(-9.17f, 4.1f, -0.17f), new Vector3(9.17f, 12.34f, 0.17f), "human base gate, 32 m fort wall entrance"),
            new FieldClass("banemajor", 9572, Kind.Faction, new Vector3(-16.48f, 0.21f, -0.29f), new Vector3(16.48f, 20.38f, 0.31f), "Bane outpost wall, major entrance"),
            new FieldClass("baneminor", 20000003, Kind.Faction, new Vector3(-10.85f, -0.16f, -0.13f), new Vector3(2.85f, 5.16f, 0.13f), "Bane outpost wall, minor entrance"),
            new FieldClass("eloh", 21994, Kind.Faction, new Vector3(-3.15f, -0.11f, -0.21f), new Vector3(3.15f, 17.55f, 1.4f), "Elohvale front gate door"),
            new FieldClass("catwalkgate", 29361, Kind.Faction, new Vector3(0f, 8.03f, 3.51f), new Vector3(16.03f, 14.83f, 4.22f), "PvP outpost catwalk gate"),
            new FieldClass("catwalk28", 29362, Kind.Faction, new Vector3(-28.09f, -0.38f, -0.35f), new Vector3(0.36f, 4.85f, 0.36f), "PvP outpost catwalk, 28 m"),
            new FieldClass("catwalk8", 29363, Kind.Faction, new Vector3(-12f, -0.38f, -0.35f), new Vector3(0.36f, 4.85f, 0.36f), "PvP outpost catwalk, 8 m"),
            new FieldClass("clan", 29342, Kind.Clan, new Vector3(-6.48f, 1.12f, -0.17f), new Vector3(6.48f, 9.36f, 0.17f), "TEST_ClanForceField, on the PvP outpost gate mesh"),
            new FieldClass("ownable", 10000083, Kind.Ownable, new Vector3(-6.48f, 1.12f, -0.17f), new Vector3(6.48f, 9.36f, 0.17f), "human outpost fence gate, hued")
        };

        /// <summary>The ownable field's hue for each side, 0-255 red, green, blue, alpha: ours.</summary>
        public static (byte R, byte G, byte B, byte A) HueOf(Side side) => side == Side.A ? ((byte)64, (byte)128, (byte)255, (byte)96) : ((byte)255, (byte)64, (byte)48, (byte)96);

        public sealed class Field
        {
            public int Id { get; set; }
            public FieldClass Class { get; set; }
            public Side Side { get; set; }
            public MapChannel MapChannel { get; set; }
            public DynamicObject Object { get; set; }
            public int MaxHealth { get; set; }
            public int Health { get; set; }

            public bool IsDestroyed => Health <= 0;
            public Vector3 Position => Object.Position;
            public float Yaw => (float)Object.Rotation;
        }

        private static readonly List<Field> Fields = new List<Field>();
        private static readonly object FieldsLock = new object();
        private static int _nextId;

        public static FieldClass ClassOf(string keyOrId)
        {
            if (string.IsNullOrWhiteSpace(keyOrId))
                return null;

            if (uint.TryParse(keyOrId, out var classId))
                return Classes.FirstOrDefault(c => c.ClassId == classId);

            return Classes.FirstOrDefault(c => string.Equals(c.Key, keyOrId, StringComparison.OrdinalIgnoreCase));
        }

        public static Field Find(ulong entityId)
        {
            lock (FieldsLock)
                return Fields.FirstOrDefault(f => f.Object.EntityId == entityId);
        }

        public static Field FindById(int id)
        {
            lock (FieldsLock)
                return Fields.FirstOrDefault(f => f.Id == id);
        }

        public static List<Field> OnMap(MapChannel mapChannel)
        {
            lock (FieldsLock)
                return Fields.Where(f => f.MapChannel == mapChannel).ToList();
        }

        #region States

        /// <summary>The usable state a field of this kind, side and health is in.</summary>
        public static UseObjectState StateOf(Kind kind, Side side, int health, int maxHealth)
        {
            if (kind == Kind.Clan)
                return health <= 0 ? UseObjectState.StateDestroyed : side == Side.A ? UseObjectState.CffStateAfsOwns : UseObjectState.CffStateBaneOwns;

            if (kind == Kind.Ownable)
                return health <= 0 ? UseObjectState.StateDestroyed : UseObjectState.StateNull;

            var a = side == Side.A;

            if (health <= 0)
                return a ? UseObjectState.FfStateFactionADestroyed : UseObjectState.FfStateFactionBDestroyed;

            var percent = maxHealth <= 0 ? 100 : health * 100.0 / maxHealth;

            if (percent > 75)
                return a ? UseObjectState.FfStateFactionAIntact : UseObjectState.FfStateFactionBIntact;

            if (percent > 25)
                return a ? UseObjectState.FfStateFactionA75pHealth : UseObjectState.FfStateFactionB75pHealth;

            return a ? UseObjectState.FfStateFactionA25pHealth : UseObjectState.FfStateFactionB25pHealth;
        }

        public static UseObjectState StateOf(Field field) => StateOf(field.Class.Kind, field.Side, field.Health, field.MaxHealth);

        /// <summary>Whether an intact field of this side stops players (the AFS): only a Bane one does.</summary>
        public static bool StopsPlayers(Side side) => side == Side.B;

        /// <summary>
        /// Whether an intact field of this side stops a creature of this category: an AFS field
        /// stops the hostile, a Bane field the friendly; neutral and the rest go where they like.
        /// </summary>
        public static bool StopsCategory(Side side, TargetCategory category) =>
            side == Side.A ? category == TargetCategory.Hostile : category == TargetCategory.Friendly;

        public static bool StopsPlayer(Field field) => !field.IsDestroyed && StopsPlayers(field.Side);

        #endregion

        #region Placing

        /// <summary>A field of the class, on the map, standing at the position and facing the yaw.</summary>
        public static Field Place(MapChannel mapChannel, FieldClass fieldClass, Side side, Vector3 position, float yaw, int maxHealth)
        {
            var field = new Field
            {
                Class = fieldClass,
                Side = side,
                MapChannel = mapChannel,
                MaxHealth = Math.Max(1, maxHealth),
                Health = Math.Max(1, maxHealth)
            };

            lock (FieldsLock)
            {
                field.Id = ++_nextId;
                Fields.Add(field);
            }

            Build(field, position, yaw);

            return field;
        }

        /// <summary>Puts the field's object into the world, where and facing how it is to be.</summary>
        private static void Build(Field field, Vector3 position, float yaw)
        {
            field.Object = new DynamicObject
            {
                EntityClassId = (EntityClasses)field.Class.ClassId,
                DynamicObjectType = DynamicObjectType.ForceField,
                Position = position,
                Rotation = yaw,
                MapContextId = field.MapChannel.MapInfo.MapContextId,
                TargetCategory = field.Side == Side.A ? TargetCategory.Friendly : TargetCategory.Hostile,
                StateId = StateOf(field),
                IsEnabled = true,
                IsInWorld = true,
                ObjectData = field,
                Comment = $"Force field {field.Id}"
            };

            CellManager.Instance.AddToWorld(field.MapChannel, field.Object);
        }

        /// <summary>The field somewhere else or facing another way: taken out of the world and put back.</summary>
        public static void Move(Field field, Vector3 position, float yaw)
        {
            CellManager.Instance.RemoveFromWorld(field.MapChannel, field.Object);
            Build(field, position, yaw);
        }

        public static void Remove(Field field)
        {
            lock (FieldsLock)
                if (!Fields.Remove(field))
                    return;

            CellManager.Instance.RemoveFromWorld(field.MapChannel, field.Object);
        }

        /// <summary>
        /// What a client meeting the field is told besides its creation: its hit points, whether
        /// it blocks that client's own manifestation, and for an ownable one its category and hue.
        /// Called from DynamicObjectManager.CreateDynamicObjectOnClient.
        /// </summary>
        internal static void ShowTo(Client client, DynamicObject obj)
        {
            if (!(obj.ObjectData is Field field) || client?.Player == null)
                return;

            client.CallMethod(obj.EntityId, new UsableDamageInfoPacket(true, false, field.MaxHealth, field.Health));
            client.CallMethod(obj.EntityId, new BlockInfoPacket(client.Player.EntityId, StopsPlayer(field)));

            if (field.Class.Kind == Kind.Ownable)
            {
                var (r, g, b, a) = HueOf(field.Side);

                client.CallMethod(obj.EntityId, new TargetCategoryPacket(field.Object.TargetCategory));
                client.CallMethod(obj.EntityId, new SetHuePacket(r, g, b, a));
            }
        }

        /// <summary>Everyone who can see the field, each told whether it blocks them now.</summary>
        private static void SendBlockInfo(Field field)
        {
            foreach (var client in ClientsAround(field))
                if (client.Player != null)
                    client.CallMethod(field.Object.EntityId, new BlockInfoPacket(client.Player.EntityId, StopsPlayer(field)));
        }

        private static IEnumerable<Client> ClientsAround(Field field)
        {
            var obj = field.Object;
            var cellX = (uint)(obj.Position.X / CellManager.CellSize + CellManager.CellBias);
            var cellZ = (uint)(obj.Position.Z / CellManager.CellSize + CellManager.CellBias);
            var matrix = CellManager.Instance.CreateCellMatrix(field.MapChannel, cellX, cellZ);

            return CellManager.CellsIn(field.MapChannel, matrix).SelectMany(cell => cell.ClientList).ToList();
        }

        /// <summary>The field changes hands: its state, category, hue and who it blocks, all anew.</summary>
        public static void SetSide(Field field, Side side)
        {
            if (field.Side == side)
                return;

            field.Side = side;

            // Its category and hue are part of its introduction: put it back so everyone meets it again.
            Move(field, field.Position, field.Yaw);
        }

        #endregion

        #region Damage

        /// <summary>
        /// The field takes a hit: its hit points, then its state when that changes, and at none it
        /// is down and blocks nobody. What it took.
        /// </summary>
        public static int Damage(Field field, int amount, ulong byEntityId)
        {
            if (field == null || field.IsDestroyed || amount <= 0)
                return 0;

            var taken = Math.Min(amount, field.Health);
            var before = StateOf(field);

            field.Health -= taken;

            CellManager.Instance.CellCallMethod(field.Object, new UpdateHitPointsPacket(field.Health));
            UpdateState(field, before, byEntityId);

            return taken;
        }

        /// <summary>Back to full health and its intact state.</summary>
        public static void Repair(Field field, ulong byEntityId)
        {
            var before = StateOf(field);

            field.Health = field.MaxHealth;

            CellManager.Instance.CellCallMethod(field.Object, new UpdateHitPointsPacket(field.Health));
            UpdateState(field, before, byEntityId);
        }

        /// <summary>The state it is in now, if that changed, from whoever changed it (Recv_Use's actor).</summary>
        private static void UpdateState(Field field, UseObjectState before, ulong byEntityId)
        {
            var now = StateOf(field);

            if (now == before)
                return;

            field.Object.StateId = now;
            CellManager.Instance.CellCallMethod(field.Object, new UsePacket(byEntityId, now, 0));

            // Down, or up again: whoever it stopped is told.
            SendBlockInfo(field);
        }

        /// <summary>Whether this actor may shoot the field: it has to be one that stops them.</summary>
        public static bool MayShoot(Field field, Actor shooter)
        {
            if (field == null || shooter == null || field.IsDestroyed)
                return false;

            return shooter switch
            {
                Manifestation => StopsPlayers(field.Side),
                Creature creature => StopsCategory(field.Side, creature.TargetCategory),
                _ => false
            };
        }

        /// <summary>
        /// A missile at a field landing (MissileManager.MissileTrigger): its damage goes on the
        /// field as it is, no crit, cover or resistance, and what it took is what the hit shows.
        /// </summary>
        internal static void TakeHit(Missile missile)
        {
            var field = Find(missile.TargetEntityId);

            missile.DamageA = field != null && MayShoot(field, missile.Source) ? Damage(field, missile.DamageA, missile.Source.EntityId) : 0;
        }

        #endregion

        #region Blocking creatures

        /// <summary>Whether a field stops this creature walking from one point to the next.</summary>
        public static bool Stops(MapChannel mapChannel, Creature creature, Vector3 from, Vector3 to)
        {
            if (creature == null || Emplacements.Is(creature))
                return false;

            foreach (var field in OnMap(mapChannel))
                if (!field.IsDestroyed && StopsCategory(field.Side, creature.TargetCategory)
                    && Crosses(field.Class, field.Position, field.Yaw, from, to))
                    return true;

            return false;
        }

        /// <summary>
        /// Whether the step from one point to the next passes through the field's sheet: the
        /// segment, taken into the field's own frame, against its box grown by
        /// <see cref="CreatureRadius"/> across and <see cref="BelowSheet"/> under it. A step that
        /// starts inside it already is let out.
        /// </summary>
        public static bool Crosses(FieldClass fieldClass, Vector3 position, float yaw, Vector3 from, Vector3 to)
        {
            var inverse = Quaternion.Inverse(Quaternion.CreateFromYawPitchRoll(yaw, 0f, 0f));
            var a = Vector3.Transform(from - position, inverse);
            var b = Vector3.Transform(to - position, inverse);

            var min = fieldClass.Min - new Vector3(CreatureRadius, BelowSheet, CreatureRadius);
            var max = fieldClass.Max + new Vector3(CreatureRadius, 0f, CreatureRadius);

            if (Inside(a, min, max))
                return false;

            return SegmentHitsBox(a, b, min, max);
        }

        private static bool Inside(Vector3 p, Vector3 min, Vector3 max) =>
            p.X >= min.X && p.X <= max.X && p.Y >= min.Y && p.Y <= max.Y && p.Z >= min.Z && p.Z <= max.Z;

        private static bool SegmentHitsBox(Vector3 a, Vector3 b, Vector3 min, Vector3 max)
        {
            var d = b - a;
            float t0 = 0f, t1 = 1f;

            for (var axis = 0; axis < 3; axis++)
            {
                var start = axis == 0 ? a.X : axis == 1 ? a.Y : a.Z;
                var dir = axis == 0 ? d.X : axis == 1 ? d.Y : d.Z;
                var lo = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
                var hi = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;

                if (Math.Abs(dir) < 1e-6f)
                {
                    if (start < lo || start > hi)
                        return false;

                    continue;
                }

                var near = (lo - start) / dir;
                var far = (hi - start) / dir;

                if (near > far)
                    (near, far) = (far, near);

                t0 = Math.Max(t0, near);
                t1 = Math.Min(t1, far);

                if (t0 > t1)
                    return false;
            }

            return true;
        }

        #endregion
    }
}

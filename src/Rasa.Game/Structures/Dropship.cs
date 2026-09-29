using System;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Managers;

    public class Dropship : DynamicObject
    {
        public long PhaseTimeleft { get; set; }
        public byte Phase { get; set; }
        public SpawnPool SpawnPool { get; set; }
        public DropshipType DropshipType { get; set; }

        /// <summary>The pad or bay a spawner dropship comes down on, when its pool has one; its creatures step off there.</summary>
        public World.SpawnPoolArrivalEntry Arrival { get; set; }

        /// <summary>Departure or arrival; meaningful for a teleporter dropship, see <see cref="DropshipRole"/>.</summary>
        public DropshipRole Role { get; set; }

        internal Client Client { get; set; }
        internal Vector3 Destination { get; set; }
        internal uint DestinationMapId { get; set; }
        internal double DestinationRotation { get; set; }

        /// <summary>A departure whose destination pad is on the map it leaves from: no map change, a flight and a move.</summary>
        internal bool StaysOnMap => Role == DropshipRole.Departure && DestinationMapId == MapContextId;

        /// <summary>
        /// When an arrival that flies in beams its passenger down, in ms from when it is built:
        /// the fly-in (phase 0, 5 s) and two seconds of the landing and its transporter beam
        /// (phase 2) - the same point in the flight at which a departure beams its passenger up.
        /// A ride that stays on its map lands this way.
        /// </summary>
        public const uint BeamDownMs = 7000;

        /// <summary>
        /// How long an arrival on another map beams before it lifts off: it is built already over
        /// the pad (<see cref="ArriveOverhead"/>). The passenger's own client plays their
        /// arrival when it leaves the loading screen - wonkavator.py OnExitState calls
        /// Recv_TeleportArrival(500, ...) on its avatar, 1.8 s after the avatar is there and
        /// 0.5 s delayed - about 2.3 s after MapLoaded, whatever the server does; this is when
        /// everyone else is shown it.
        /// </summary>
        public const long OverheadBeamMs = 2500;

        /// <summary>
        /// Built hovering over the pad with its transporter beam on (CsStateSpawn, the landing)
        /// rather than flying in: the passenger's client beams them down on its own clock as it
        /// comes out of the loading screen, which a five second fly-in could never be ahead of.
        /// </summary>
        internal void ArriveOverhead()
        {
            StateId = UseObjectState.CsStateSpawn;
            Phase = 2;
            PhaseTimeleft = OverheadBeamMs;
        }

        /// <param name="side">Whose ship: FRIENDLY for the AFS (human) dropship, HOSTILE for the Bane one.</param>
        public Dropship(TargetCategory side, DropshipType dropshipType, SpawnPool spawnPool = null)
        {
            // EntityId is the one DynamicObject's constructor has already taken. This took a
            // second over it, and the first was never freed: an id lost per dropship built.
            EntityClassId = side == TargetCategory.Friendly ? Data.EntityClasses.UsableCrSpawnerHumDropshipV01 : Data.EntityClasses.UsableCrSpawnerBaneDropshipV01;
            TargetCategory = side;
            StateId = UseObjectState.CsStateBegin;
            PhaseTimeleft = 5000;
            Phase = 0;
            DropshipType = dropshipType;
            SpawnPool = spawnPool;
            MapContextId = spawnPool.MapContextId;
            Position = new Vector3(
                spawnPool.Position.X + (2.0f - (Random.Shared.Next() % 100) * 0.04f),
                spawnPool.Position.Y,
                spawnPool.Position.Z + (2.0f - (Random.Shared.Next() % 100) * 0.04f)
                );
            Rotation = (Random.Shared.Next() % 640) * 0.01f;
        }
        
        /// <summary>
        /// A teleporter dropship for one player. A departure is built where the player stands,
        /// with the pad they chose as its destination; an arrival is built where they have just
        /// been put down, with no destination.
        /// </summary>
        public Dropship(TargetCategory side, DropshipType dropshipType, Client client, DropshipRole role, Vector3 destination = new Vector3(), uint destinationMapId = 0)
        {
            Role = role;
            // EntityId is the one DynamicObject's constructor has already taken. This took a
            // second over it, and the first was never freed: an id lost per dropship built.
            EntityClassId = side == TargetCategory.Friendly ? Data.EntityClasses.UsableCrSpawnerHumDropshipV01 : Data.EntityClasses.UsableCrSpawnerBaneDropshipV01;
            TargetCategory = side;
            StateId = UseObjectState.CsStateBegin;
            PhaseTimeleft = 5000;
            Phase = 0;
            DropshipType = dropshipType;
            Client = client;

            if (client.State == ClientState.Teleporting)
                MapContextId = client.LoadingMap;
            else
                MapContextId = client.Player.MapContextId;

            Position = client.Player.Position;
            Rotation = (Random.Shared.Next() % 640) * 0.01f;
            DynamicObjectType = DynamicObjectType.DropshipTeleporter;
            Destination = destination;
            DestinationMapId = destinationMapId;
        }
    }
}

using System;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Managers;

    public class DropshipSpawner : DynamicObject
    {
        public long PhaseTimeleft { get; set; }
        public byte Phase { get; set; }
        public SpawnPool SpawnPool { get; set; }

        public DropshipSpawner(SpawnPool spawnPool, Factions faction)
        {
            EntityId = EntityManager.Instance.GetEntityId;
            EntityClassId = faction == Factions.AFS ? EntityClassId.UsableCrSpawnerHumDropshipV01 : EntityClassId.UsableCrSpawnerBaneDropshipV01;
            Faction = faction;
            StateId = UseObjectState.CsStateBegin;
            PhaseTimeleft = 5000;
            Phase = 0;
            SpawnPool = spawnPool;
            MapContextId = spawnPool.MapContextId;
            Position = new Vector3(
                spawnPool.HomePosition.X + (2.0f - (new Random().Next() % 100) * 0.04f),
                spawnPool.HomePosition.Y,
                spawnPool.HomePosition.Z + (2.0f - (new Random().Next() % 100) * 0.04f)
                );
            Orientation = (new Random().Next() % 640) * 0.01f;
        }
    }
}

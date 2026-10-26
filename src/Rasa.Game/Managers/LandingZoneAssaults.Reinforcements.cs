using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    internal static partial class LandingZoneAssaults
    {
        private static IEnumerable<Creature> Reinforcements(MapChannel map, Assault state) =>
            EntityManager.Instance.Creatures.Values.Where(c => state.Reinforcements.Contains(c.SpawnPool) &&
                CreatureManager.IsLivingOnMap(map, c));

        private static void Reinforce(MapChannel map, Assault state)
        {
            // Once at assault start, never again for waves two/three or when a squad dies.
            foreach (var post in LandingZoneReinforcementContent.Posts)
            {
                var pool = new SpawnPool
                {
                    MapContextId = LandingZoneGarrisonContent.Map, RuntimeMapChannel = map,
                    Position = post, Rotation = LandingZoneGarrisonContent.GunshipRotation,
                    AnimType = 2, Mode = SpawnPoolManager.ModeScripted,
                    SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                    IsGarrison = true, IsControlPointReinforcement = true, HasSpawned = true,
                    SpawnSlot = new List<SpawnPoolSlot>
                    {
                        new(LandingZoneGarrisonContent.Rifleman, 3, 3),
                        new(LandingZoneReinforcementContent.Pistol, 1, 1),
                        new(Random.Shared.Next(2) == 0 ? LandingZoneReinforcementContent.Shotgun : LandingZoneReinforcementContent.MachineGun, 1, 1)
                    }
                };
                pool.Delivered = creatures =>
                {
                    for (var i = 0; i < creatures.Count; i++)
                    {
                        var creature = creatures[i];
                        var raw = post + new Vector3((i - 2) * 2.5f, 0, 0);
                        var destination = NavMeshManager.NearestWalkable(map, raw) ?? post;
                        creature.AggroRange = 35;
                        // WalkIn uses WalkSpeed for arrival; these infantry run at their normal run speed.
                        creature.WalkSpeed = creature.RunSpeed;
                        BehaviorManager.Instance.WalkIn(creature, destination);
                    }
                };
                state.Reinforcements.Add(pool);
                map.SpawnPools.Add(pool);
                LandingZoneGarrison.DispatchReinforcements(map, pool, SpawnPoolManager.Instance.CreateListOfCreatures(pool));
            }
        }
    }
}

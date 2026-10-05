using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A destroyed turret or vehicle left standing as its wreck, and put back as it was: the
    /// client's ALTERNATE_MESH game effect (170, gameeffects/altmesh.py).
    ///
    /// The effect goes on with (origMeshId, altMeshId, collisionRole, alreadySwapped) for its
    /// OnAttach - both models as string table ids, as an entity class names its own - and does
    /// nothing until one of its two methods is called through CallGameEffectMethod:
    ///  - SwapMeshDeath: the body becomes the alternate model and the effect announces itself,
    ///    which at level 1 plays OBJECT_ABILITY_EXPLOSION_LARGE (specialFX (170, 1), the only
    ///    level with any): the model changes inside an explosion;
    ///  - SwapMeshRevive: the body goes back to its own model.
    /// A client given the creature when it is already a wreck is told so (alreadySwapped) and
    /// shows the wreck from the start, with no explosion.
    ///
    /// It has to be attached unannounced, and by a source the client holds - the creature
    /// itself. AltMesh draws nothing when it is announced on attaching, but
    /// BaseGameEffect.AnnounceAttach marks it announced all the same, and the announce
    /// SwapMeshDeath makes - the one that plays the explosion - then returns at once. A source
    /// the client does not hold forces that announce whatever the attach asks for
    /// (Recv_GameEffectAttached).
    ///
    /// Which creatures have one was the server's to know. The client's creature flags name
    /// ALT_MESH (43) and ALT_MESH_DELAYED_3500 (103) and it reads neither, and nothing in its
    /// data pairs a class with a second model. The flags are in creature_class_flag; the models
    /// are <see cref="Wrecks"/>, each the class's own model with "_destroyed" on its name -
    /// seven creature classes have such a twin, none of the twins with a skeleton. The two that
    /// have a death animation, the walkers, carry the delayed flag: they fall, and are their
    /// wreck 3.5 s later. The turrets have none, and are their wreck at once.
    ///
    /// Kept on the creature rather than among its GameEffects: those are cleared when it dies
    /// (CreatureManager.HandleCreatureKill, GameEffectManager.DoWork), and this one is for
    /// when it is dead.
    ///
    /// An emplacement's wreck stays on its mount. A turret a spawn pool put there does not go
    /// when its corpse's time is up, and when the pool's respawn comes round the pool does not
    /// set a second one down in the wreckage: the wreck is put back in service where it
    /// stands, at full health and armour (<see cref="ReviveWrecks"/>). A Technician's Jumpstart
    /// does the same sooner (CreatureSupport.Revive). A walker's wreck is a corpse like any
    /// other and goes when its loot is done with.
    /// </summary>
    public static class AlternateMesh
    {
        /// <summary>gameeffectdata.ALTERNATE_MESH.</summary>
        public const int EffectTypeId = 170;

        /// <summary>The one level the client has FX for: the explosion the swap happens in.</summary>
        public const uint EffectLevel = 1;

        /// <summary>ALT_MESH_DELAYED_3500: how long after its death a walker becomes its wreck.</summary>
        public const long DelayMs = 3500;

        /// <summary>The wreck of each class that has one: its "_destroyed" model's string table id.</summary>
        public static readonly IReadOnlyDictionary<EntityClasses, int> Wrecks = new Dictionary<EntityClasses, int>
        {
            { (EntityClasses)4064, 14255 },     // Emplacement_AFS_Turret_Standard: prop_hum_turret_auto_v01_destroyed
            { (EntityClasses)11302, 33452 },    // Emplacement_AFS_Turret_Mini: prop_hum_turret_auto_mini_destroyed
            { (EntityClasses)23902, 42134 },    // Emplacement_AFS_Turret_Brann: prop_brann_turret_v01_destroyed
            { (EntityClasses)7482, 19367 },     // Emplacement_Bane_Turret_Standard: prop_bane_mortarlauncher_v01_destroyed
            { (EntityClasses)10509, 19367 },    // Emplacement_Bane_Turret_Mini: the same model, the same wreck
            { (EntityClasses)3902, 15296 },     // Vehicle_Bane_Predator: creature_thrax_predator_destroyed
            { (EntityClasses)30080, 45229 }     // Vehicle_Bane_Ravager: creature_thrax_stalker_mini_destroyed_v01
        };

        /// <summary>
        /// Whether this creature has a wreck, and how long after its death it becomes it: its
        /// class has a model in <see cref="Wrecks"/> and carries one of the two flags.
        /// </summary>
        public static bool TryGet(Creature creature, out int wreckMeshId, out long delayMs)
        {
            wreckMeshId = 0;
            delayMs = 0;

            if (creature == null || !Wrecks.TryGetValue(creature.EntityClass, out wreckMeshId))
                return false;

            var flags = CreatureManager.CreatureFlagsOf(creature);

            if (flags.Contains((int)CreatureFlag.AltMeshDelayed3500))
            {
                delayMs = DelayMs;
                return true;
            }

            return flags.Contains((int)CreatureFlag.AltMesh);
        }

        /// <summary>
        /// The effect's attach, for a client that has just been given the creature - every
        /// client is, here (CreatureManager.CreateCreatureOnClient) - or null for a creature with
        /// no wreck. Unannounced and from the creature itself, as SwapMeshDeath needs; already
        /// swapped when it is a wreck now.
        /// </summary>
        public static GameEffectAttachedPacket AttachedPacket(Creature creature)
        {
            if (!TryGet(creature, out var wreckMeshId, out _))
                return null;

            if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(creature.EntityClass, out var entityClass) || entityClass == null)
                return null;

            // Its id on the map, taken the first time anyone is told of it.
            if (creature.AlternateMeshEffectId == 0)
            {
                var mapChannel = creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId);

                if (mapChannel == null)
                    return null;

                creature.AlternateMeshEffectId = GameEffectManager.Instance.NextEffectId(mapChannel);
            }

            return new GameEffectAttachedPacket
            {
                EffectTypeId = EffectTypeId,
                EffectId = creature.AlternateMeshEffectId,
                EffectLevel = EffectLevel,
                SourceId = creature.EntityId,
                Announced = false,
                IsActive = true,
                Args = new List<object> { entityClass.MeshId, wreckMeshId, (int)entityClass.ClassCollisionRole, creature.AlternateMeshSwapped }
            };
        }

        /// <summary>Gives a client the effect with the creature it has just been given.</summary>
        public static void ShowTo(Client client, Creature creature)
        {
            var attached = AttachedPacket(creature);

            if (attached != null)
                client?.CallMethod(creature.EntityId, attached);
        }

        /// <summary>
        /// The creature has died: a turret is its wreck now, a walker once it has fallen
        /// (<see cref="DeadTick"/>). An emplacement its pool will put back is kept for it.
        /// </summary>
        public static void OnDeath(MapChannel mapChannel, Creature creature)
        {
            if (!TryGet(creature, out _, out var delayMs))
                return;

            if (StaysForItsPool(creature) && !creature.SpawnPool.Wrecks.Contains(creature))
                creature.SpawnPool.Wrecks.Add(creature);

            if (delayMs > 0)
                creature.AlternateMeshSwapAt = Environment.TickCount64 + delayMs;
            else
                Swap(mapChannel, creature);
        }

        /// <summary>A dead creature's turn in the behaviour loop: a fallen walker whose time has come becomes its wreck.</summary>
        public static void DeadTick(MapChannel mapChannel, Creature creature, long now)
        {
            if (creature.AlternateMeshSwapAt != 0 && now >= creature.AlternateMeshSwapAt)
                Swap(mapChannel, creature);
        }

        private static void Swap(MapChannel mapChannel, Creature creature)
        {
            creature.AlternateMeshSwapAt = 0;

            if (creature.AlternateMeshSwapped)
                return;

            creature.AlternateMeshSwapped = true;

            // Nobody has been given it yet: whoever is, is given the wreck.
            if (creature.AlternateMeshEffectId != 0)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new GameEffectCallPacket(creature.AlternateMeshEffectId, "SwapMeshDeath"));
        }

        /// <summary>
        /// The creature is being stood up again (CreatureSupport.Revive): its own model back,
        /// before the Revived that shows its weapon on it. A walker revived before it became its
        /// wreck never does.
        /// </summary>
        public static void OnRevive(MapChannel mapChannel, Creature creature)
        {
            creature.AlternateMeshSwapAt = 0;
            creature.SpawnPool?.Wrecks.Remove(creature);

            if (!creature.AlternateMeshSwapped)
                return;

            creature.AlternateMeshSwapped = false;

            if (creature.AlternateMeshEffectId != 0)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new GameEffectCallPacket(creature.AlternateMeshEffectId, "SwapMeshRevive"));
        }

        /// <summary>The creature has left the world: its pool is not waiting to put it back.</summary>
        public static void Forget(Creature creature)
        {
            creature?.SpawnPool?.Wrecks.Remove(creature);
        }

        /// <summary>Whether this corpse stays past its time: a wreck its pool is going to put back in service.</summary>
        public static bool KeepsWreck(Creature creature) =>
            creature?.SpawnPool != null && creature.SpawnPool.Wrecks.Contains(creature);

        /// <summary>
        /// An emplacement with a wreck, set on its mount by a pool that respawns on a timer and
        /// holds nothing but such emplacements - so that the pool having nothing alive is the
        /// same as its wrecks being all it has - and not finished by a Critical Death, which
        /// destroys the body.
        /// </summary>
        private static bool StaysForItsPool(Creature creature)
        {
            var pool = creature.SpawnPool;

            if (pool == null || creature.CritKilled || !Emplacements.Is(creature))
                return false;

            if (pool.Mode != SpawnPoolManager.ModeAutomatic || pool.AnimType != 0 || pool.RespawnTime <= 0)
                return false;

            if (pool.SpawnPolicy == Structures.World.MissionSpawnGroupPolicy.ScenarioControlled || pool.ScenarioKey != null || pool.SceneRunId != null)
                return false;

            return pool.SpawnSlot != null && pool.SpawnSlot.All(slot => slot == null || slot.CreatureId == 0 || slot.CountMax == 0 || IsWreckEmplacement(slot.CreatureId));
        }

        private static bool IsWreckEmplacement(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature) && creature != null
            && Emplacements.Classes.Contains(creature.EntityClass) && Wrecks.ContainsKey(creature.EntityClass);

        /// <summary>
        /// The pool's respawn has come round (SpawnPoolManager.SpawnPoolWorker): its wrecks are
        /// put back in service where they stand - their own model, full health and armour - in
        /// place of new creatures. False when it has none in the world, and spawns as it always
        /// did.
        /// </summary>
        public static bool ReviveWrecks(MapChannel mapChannel, SpawnPool pool)
        {
            if (pool == null || pool.Wrecks.Count == 0)
                return false;

            var revived = false;

            foreach (var wreck in pool.Wrecks.ToList())
            {
                pool.Wrecks.Remove(wreck);

                if (wreck.State != CharacterState.Dead
                    || !EntityManager.Instance.Creatures.TryGetValue(wreck.EntityId, out var registered) || registered != wreck)
                    continue;

                wreck.CritKilled = false;
                CreatureSupport.Revive(mapChannel, wreck, wreck.Attributes[Attributes.Health].CurrentMax, null);

                if (wreck.Attributes.TryGetValue(Attributes.Armor, out var armor) && armor.Current < armor.CurrentMax)
                {
                    armor.Current = armor.CurrentMax;
                    CellManager.Instance.CellCallMethod(mapChannel, wreck, new UpdateArmorPacket(armor, wreck.EntityId));
                }

                revived = true;
            }

            return revived;
        }
    }
}

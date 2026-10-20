using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Maws lie under the ground until something comes near, come up at it, and go back down when
    /// the fight is over.
    ///
    /// What the client has for it:
    ///  - coming up is the Maw's CREATURE_BIRTH (155, at its class 6332, CREATURE_BIRTH_MAW): a
    ///    1.5 s recovery whose animation family, Creature Birth (622), is
    ///    creature_foreas_maw_emerge_v01 on the Maw's skeleton, with creature_birth_foreas_maw
    ///    for its FX. Seymour, the boss (22980), is the same model and is sent the same birth;
    ///    the client has no birth at 22980.
    ///  - going down is CR_DESPAWN at 1 (424/1, DespawnAbility, "Causes a creature to despawn"):
    ///    a 2.066 s recovery, Ability - Creature - Maw - Despawn - Resolve (1259), which is
    ///    creature_foreas_maw_decend_v01, with ABILITY_CREATURE_MAW_DESPAWN_RESOLVE's FX.
    /// The same two animations are its turn from non-combat to combat and back (856, 857). Its
    /// underground idle has a family, DELETEME2, that nothing plays, so a Maw that has gone down
    /// is not shown at all: it is taken off its clients and out of its cells, and nothing - no
    /// scan, no area attack, no player coming into view - finds it there.
    ///
    /// Only a Maw an automatic spawn pool put in the world lurks; one a GM put down, a scene's or
    /// a minion stands about as any creature does.
    ///
    ///  - It spawns under the ground (<see cref="Bury"/>), registered and counted by its pool,
    ///    but in no cell.
    ///  - Every tick of a map with players on it (<see cref="Worker"/>), a buried Maw looks out
    ///    with the aggro scan (BehaviorManager.FindAttackableInRange, its AggroRange) from where
    ///    it lies. Something there: it is put back in its cell and on the clients around, comes up
    ///    (the birth), and goes for it, doing nothing else until it is up.
    ///  - Out of a fight, standing, with nothing to hate and nothing found for
    ///    <see cref="LingerMs"/> since it last fought (<see cref="Sink"/>), it goes down (the
    ///    despawn), doing nothing else; then it is taken off the clients and out of its cells,
    ///    whole again - health, armour, its effects gone - and back at its spawn point, where it
    ///    next comes up. One killed on the way down stays where it fell.
    ///  - A map reset takes the buried with the rest (MapReset), and so does a map closing.
    /// </summary>
    public static class MawLurk
    {
        public const ActionId BirthAction = ActionId.CreatureBirth;
        public const uint BirthArg = 6332;
        public const ActionId DespawnAction = ActionId.CrDespawn;
        public const uint DespawnArg = 1;

        /// <summary>The recoveries' lengths when the action data has none.</summary>
        public const int EmergeMs = 1500;
        public const int SinkMs = 2066;

        /// <summary>How long after its last fight, with nothing to fight, a Maw stays up. Ours.</summary>
        public const long LingerMs = 10000;

        /// <summary>Creature_Maw and Creature_Maw_Boss.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)6332, (EntityClasses)22980
        };

        /// <summary>The time, in ms, the coming up and going down are timed by: a test sets it.</summary>
        internal static Func<long> Clock = () => Environment.TickCount64;

        private static readonly object Sync = new object();
        private static readonly Dictionary<MapChannel, HashSet<Creature>> Buried = new Dictionary<MapChannel, HashSet<Creature>>();
        private static readonly Dictionary<Creature, long> Emerging = new Dictionary<Creature, long>();
        private static readonly Dictionary<Creature, (MapChannel Map, long Until)> Sinking = new Dictionary<Creature, (MapChannel, long)>();

        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);

        /// <summary>Whether this creature lurks: a Maw of an automatic spawn pool, nobody's minion.</summary>
        public static bool Lurks(Creature creature) =>
            Is(creature) && creature.SpawnPool != null && creature.SpawnPool.Mode == SpawnPoolManager.ModeAutomatic
            && creature.MasterEntityId == 0 && !creature.IsScripted;

        public static bool IsBuried(Creature creature)
        {
            lock (Sync)
                return creature != null && Buried.Values.Any(set => set.Contains(creature));
        }

        /// <summary>The Maws lying under this map.</summary>
        public static List<Creature> BuriedOn(MapChannel mapChannel)
        {
            lock (Sync)
                return mapChannel != null && Buried.TryGetValue(mapChannel, out var set) ? set.ToList() : new List<Creature>();
        }

        /// <summary>Coming up or going down: it does nothing else (BehaviorManager).</summary>
        public static bool IsBusy(Creature creature)
        {
            if (creature == null)
                return false;

            var now = Clock();

            lock (Sync)
                return (Emerging.TryGetValue(creature, out var up) && now < up)
                       || Sinking.ContainsKey(creature);
        }

        /// <summary>
        /// A Maw just made by its pool goes into the world under the ground: registered as
        /// CellManager.AddToWorld registers it, with its cells worked out, but in none of them and
        /// on no client.
        /// </summary>
        public static void Bury(MapChannel mapChannel, Creature creature)
        {
            creature.RuntimeMapChannel = mapChannel;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            creature.Cells = CellsAt(mapChannel, creature);

            lock (Sync)
                Hide(mapChannel, creature);
        }

        /// <summary>
        /// The buried Maws that have something to come up at come up, and those that have gone
        /// all the way down are taken off. Run on every tick of a map with players on it.
        /// </summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            var now = Clock();
            List<Creature> down;

            lock (Sync)
            {
                down = Sinking.Where(s => s.Value.Map == mapChannel && now >= s.Value.Until).Select(s => s.Key).ToList();

                foreach (var creature in down)
                    Sinking.Remove(creature);

                foreach (var done in Emerging.Where(e => now >= e.Value).Select(e => e.Key).ToList())
                    Emerging.Remove(done);
            }

            foreach (var creature in down)
                Park(mapChannel, creature);

            foreach (var creature in BuriedOn(mapChannel))
            {
                if (!Alive(creature))
                    continue;

                var target = BehaviorManager.FindAttackableInRange(mapChannel, creature, creature.AggroRange);

                if (target != 0)
                    Emerge(mapChannel, creature, target);
            }
        }

        /// <summary>
        /// Up out of the ground at <paramref name="targetId"/>: back in its cell and on the clients
        /// around it, its birth played, and the fight begun.
        /// </summary>
        public static void Emerge(MapChannel mapChannel, Creature creature, ulong targetId)
        {
            lock (Sync)
            {
                if (!Buried.TryGetValue(mapChannel, out var set) || !set.Remove(creature))
                    return;

                if (set.Count == 0)
                    Buried.Remove(mapChannel);

                Emerging[creature] = Clock() + RecoveryMs(BirthAction, BirthArg, EmergeMs);
            }

            creature.Cells = CellsAt(mapChannel, creature);
            mapChannel.MapCellInfo.Cells[creature.Cells[2, 2]].CreatureList.Add(creature);
            CreatureManager.Instance.CellIntroduceCreatureToClients(mapChannel, creature,
                CellManager.Instance.GetClientsInCells(mapChannel, creature.Cells));

            CellManager.Instance.CellCallMethod(mapChannel, creature, new PerformWindupPacket(PerformType.TwoArgs, BirthAction, BirthArg));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new PerformRecoveryPacket(PerformType.TwoArgs, BirthAction, BirthArg));

            creature.LastAgression = 0;
            creature.Hate.Ensure(targetId, Threat.NoticedThreat);
            BehaviorManager.Instance.SetActionFighting(creature, targetId);

            // Refused the fight: nothing held against it, so it can go down again.
            if (creature.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                creature.Hate.Remove(targetId);
        }

        /// <summary>
        /// A lurking Maw standing about with nothing to fight, LingerMs since its last fight, goes
        /// down: its despawn is played and it waits for it. Whether it did.
        /// </summary>
        public static bool Sink(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || !Lurks(creature) || !Alive(creature) || creature.Hate.Count > 0
                || creature.LastAgression < LingerMs || IsBusy(creature))
                return false;

            BehaviorManager.Instance.StopMoving(creature);
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;

            lock (Sync)
                Sinking[creature] = (mapChannel, Clock() + RecoveryMs(DespawnAction, DespawnArg, SinkMs));

            CellManager.Instance.CellCallMethod(mapChannel, creature, new PerformWindupPacket(PerformType.TwoArgs, DespawnAction, DespawnArg));
            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new AbilityRecoveryPacket(DespawnAction, DespawnArg, AbilityRecoveryPacket.HitDataKind.None));

            return true;
        }

        /// <summary>
        /// All the way down: off its clients and out of its cells, whole, and back at its spawn
        /// point. Not one that died on the way, nor one no longer in the world.
        /// </summary>
        private static void Park(MapChannel mapChannel, Creature creature)
        {
            if (!Alive(creature) || !ReferenceEquals(creature.RuntimeMapChannel, mapChannel)
                || !EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) || registered != creature)
                return;

            GameEffectManager.Instance.ClearEffects(mapChannel, creature);

            foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, creature.Cells))
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));

            foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                cell.CreatureList.RemoveAll(candidate => candidate == creature);

            if (creature.Attributes.TryGetValue(Attributes.Health, out var health))
                health.Current = health.CurrentMax;

            if (creature.Attributes.TryGetValue(Attributes.Armor, out var armor))
                armor.Current = armor.CurrentMax;

            creature.Hate.Clear();
            creature.Target = 0;
            creature.Position = creature.HomePos.Position;
            creature.Cells = CellsAt(mapChannel, creature);
            BehaviorManager.StartWandering(creature, false);

            lock (Sync)
                Hide(mapChannel, creature);
        }

        /// <summary>It has left the world (CellManager.RemoveCreatureFromWorld): nothing kept for it.</summary>
        public static void Forget(Creature creature)
        {
            if (creature == null)
                return;

            lock (Sync)
            {
                Emerging.Remove(creature);
                Sinking.Remove(creature);

                foreach (var map in Buried.Where(b => b.Value.Remove(creature) && b.Value.Count == 0).Select(b => b.Key).ToList())
                    Buried.Remove(map);
            }
        }

        private static void Hide(MapChannel mapChannel, Creature creature)
        {
            if (!Buried.TryGetValue(mapChannel, out var set))
                Buried[mapChannel] = set = new HashSet<Creature>();

            set.Add(creature);
        }

        private static uint[,] CellsAt(MapChannel mapChannel, Creature creature)
        {
            var seed = CellManager.Instance.GetCellSeed(creature.Position);

            return CellManager.Instance.CreateCellMatrix(mapChannel, seed & 0xFFFF, seed >> 16);
        }

        private static int RecoveryMs(ActionId actionId, uint argId, int fallback) =>
            AbilityManager.Instance != null && AbilityManager.Instance.TryGetLevel(actionId, argId, out var info) && info.RecoveryMs > 0
                ? info.RecoveryMs
                : fallback;

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying
            && creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
    }
}

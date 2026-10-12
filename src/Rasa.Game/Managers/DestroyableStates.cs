using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// How an object a mission has given hit points shows its damage and its destruction, by the
    /// states and transitions its class has on the client.
    ///
    /// An InertDestroyable (augmentation 41) - and a DestroyableCreatureSpawner (68), which has
    /// the same states - goes down through its health states: USE_IDES_STATE_INTACT (110),
    /// USE_IDES_STATE_50P_HEALTH (185), USE_IDES_STATE_25P_HEALTH (186), and from there to
    /// USE_STATE_DESTROYED (2). Those are its only transitions downwards
    /// (usableaugmentationstatetransition 41: 110 to 185, 185 to 186, 186 to 2), and the explosion
    /// is the last one's effect (usabledata.specialFX (class, 186, 2): the Bane barrel's
    /// arch_bane_gen_obj_barrel_explode_v01.pkg, the gas harvester's
    /// vfx_arch_bane_gen_obj_gasharvester_explode.pkg). Sent straight from intact to destroyed,
    /// the object was given ForceState 2: its wreck and its destroyed effect, and no explosion -
    /// and a Use from 110 to 2 would have done nothing at all, as the client has no such
    /// transition (usable.py _Transition). So it is taken down a state at a time, by Use: to 185
    /// once its hit points are at half or less, to 186 at a quarter or less, and when it is
    /// destroyed, through whatever is left of that, then 186 to 2 - the explosion, then the wreck
    /// (UsableStateTransition: the transition's effect, then the state's mesh and effect). Some
    /// classes animate the damaged states (the articulated drill's 50% and 25%).
    ///
    /// A CreatureSpawner (61) has a transition to destroyed from each of its states, the
    /// explosion on each (the Fithik egg cluster's destroyable_fithikegg_explosion.pkg), and is
    /// sent Use to it directly. Anything else, or a destroyed state other than 2, keeps
    /// ForceState.
    /// </summary>
    public static class DestroyableStates
    {
        private static readonly UseObjectState[] HealthChain =
        {
            UseObjectState.IdesStateIntact,
            UseObjectState.IdesState50pHealth,
            UseObjectState.IdesState25pHealth
        };

        /// <summary>Whether the object's class goes down through the health states (augmentation 41 or 68).</summary>
        public static bool HasHealthStates(DynamicObject obj) =>
            Augmentations(obj).Any(aug => aug == AugmentationType.InertDestroyable || aug == AugmentationType.DestroyableCreatureSpawner);

        /// <summary>Whether the object's class has a transition to destroyed from each of its states (augmentation 61).</summary>
        public static bool GoesStraightToDestroyed(DynamicObject obj) =>
            Augmentations(obj).Contains(AugmentationType.CreatureSpawner);

        /// <summary>The health state for hit points: intact above half, 50% above a quarter, 25% below that.</summary>
        public static UseObjectState HealthStateFor(uint current, uint total)
        {
            if (total == 0 || (ulong)current * 2 > total)
                return UseObjectState.IdesStateIntact;

            return (ulong)current * 4 > total ? UseObjectState.IdesState50pHealth : UseObjectState.IdesState25pHealth;
        }

        /// <summary>
        /// The health states between <paramref name="from"/> and <paramref name="to"/>, in order,
        /// not counting <paramref name="from"/>; none if either is no health state or the way is up.
        /// </summary>
        public static IReadOnlyList<UseObjectState> Down(UseObjectState from, UseObjectState to)
        {
            var start = System.Array.IndexOf(HealthChain, from);
            var end = System.Array.IndexOf(HealthChain, to);

            if (start < 0 || end <= start)
                return System.Array.Empty<UseObjectState>();

            return HealthChain.Skip(start + 1).Take(end - start).ToList();
        }

        /// <summary>
        /// A hit that left the object standing: down to the health state its hit points are at,
        /// a state at a time, each a Use to everyone in range. Returns the states it went through.
        /// </summary>
        public static IReadOnlyList<UseObjectState> ShowDamage(MapChannel map, DynamicObject obj, uint total, ulong actorId)
        {
            if (map == null || obj == null || !HasHealthStates(obj))
                return System.Array.Empty<UseObjectState>();

            var steps = Down(obj.StateId, HealthStateFor(obj.CurrentHitPoints, total));

            foreach (var step in steps)
                Use(map, obj, step, actorId);

            return steps;
        }

        /// <summary>
        /// The object destroyed, from the state its clients were last shown: down through what is
        /// left of its health states and into <paramref name="destroyed"/> by Use, where its class
        /// has that way; ForceState otherwise. Sets its StateId. Returns the states sent.
        /// </summary>
        public static IReadOnlyList<UseObjectState> ShowDestroyed(MapChannel map, DynamicObject obj, UseObjectState shown,
            UseObjectState destroyed, ulong actorId)
        {
            if (map == null || obj == null)
                return System.Array.Empty<UseObjectState>();

            var sent = new List<UseObjectState>();

            if (destroyed == UseObjectState.StateDestroyed && HasHealthStates(obj) && System.Array.IndexOf(HealthChain, shown) >= 0)
            {
                obj.StateId = shown;
                sent.AddRange(Down(shown, UseObjectState.IdesState25pHealth));
                sent.Add(destroyed);

                foreach (var step in sent)
                    Use(map, obj, step, actorId);

                return sent;
            }

            if (destroyed == UseObjectState.StateDestroyed && GoesStraightToDestroyed(obj) && shown != destroyed)
            {
                Use(map, obj, destroyed, actorId);
                return new[] { destroyed };
            }

            obj.StateId = destroyed;
            CellManager.Instance.CellCallMethod(map, obj, new ForceStatePacket(destroyed, 0));
            return sent;
        }

        private static void Use(MapChannel map, DynamicObject obj, UseObjectState state, ulong actorId)
        {
            obj.StateId = state;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actorId, state, 0));
        }

        private static IEnumerable<AugmentationType> Augmentations(DynamicObject obj) =>
            EntityClassManager.Instance.GetClassInfo(obj.EntityClassId)?.Augmentations ?? Enumerable.Empty<AugmentationType>();
    }
}

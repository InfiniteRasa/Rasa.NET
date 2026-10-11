using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// An object's blast: OBJ_DAMAGE (102) and OBJ_KNOCKBACK (128), the object actions the client
    /// plays through PhysicalEntity.Recv_PerformObjectAbility(actionId, argId, hits, misses,
    /// hitdata, sourceId) - ObjDamageBase and ObjKnockback both play the action's FX family from
    /// the object at the hits and announce each hit's rawInfo as damage from sourceId. What they
    /// do is the argument's data, read as it stands:
    ///  - RADIUS_AROUND_SOURCE: everyone within it of the object is hit;
    ///  - DAMAGE_AMOUNT_MIN..MAX, unscaled (no argument gives a DAMAGE_SCALE_TYPE), as
    ///    DAMAGE_TYPE where one is given, physical otherwise, resisted as that type;
    ///  - KNOCKBACK_DISTANCE: thrown that far straight away from the object, as a creature's or
    ///    a player's knockback is (CrowdControl, PlayerCrowdControl).
    ///
    /// OBJ_DAMAGE's arguments: 1 small explosion (3 m, 40-60), 2 large explosion (10 m, 100-300),
    /// 3 lightning (5 m, 250-750, electrical, no knockback), 4 landmine and 5 bomb (3 m, 10-20),
    /// 6 geyser (6 m, 50-100, environmental) and 7 hornito (6 m, 50-100, fire), all but 3, 6 and 7
    /// throwing 10 m. OBJ_KNOCKBACK's: 1 the explosion column (8 m, 20-31), 2 landmine (3 m),
    /// 3 the human toroid cannon (50 m, 100-200, electrical), 4 and 5 two smaller ones, and 6-57
    /// the explosion column again with its damage rising step by step to 2500-5000; all throw
    /// 10 m.
    ///
    /// Nothing in the world performs one yet: the eruptions, bombardments, mines and cannons that
    /// would are not placed. Perform is what a placement calls, for an object the server holds or
    /// one the clients build from the .map, with the actor behind it if there is one (a mine's
    /// layer) and who it may hit. With no rule given it hits every living player and creature in
    /// its radius: an eruption takes no sides.
    /// </summary>
    public static class ObjectAbilities
    {
        public const string DamageModule = "abilities.obj_damage";
        public const string KnockbackModule = "abilities.obj_knockback";

        /// <summary>
        /// The object at its own position performs the action at the argument: see the class. The
        /// hits, each as it was dealt; none for an action that is not an object blast, or one
        /// whose argument the data has not got.
        /// </summary>
        public static List<ObjectDamageHit> Perform(MapChannel mapChannel, DynamicObject obj, ActionId actionId, uint argId,
            Actor owner = null, Func<Actor, bool> mayHit = null)
        {
            if (obj == null)
                return new List<ObjectDamageHit>();

            return Perform(mapChannel, obj.Position, obj.EntityId, actionId, argId, owner, mayHit,
                packet => CellManager.Instance.CellCallMethod(mapChannel, obj, packet));
        }

        /// <summary>
        /// The same for an object the server does not hold - one the clients build from the .map -
        /// standing at centre with the entity id they know it by.
        /// </summary>
        public static List<ObjectDamageHit> Perform(MapChannel mapChannel, Vector3 centre, ulong entityId, ActionId actionId, uint argId,
            Actor owner = null, Func<Actor, bool> mayHit = null)
        {
            return Perform(mapChannel, centre, entityId, actionId, argId, owner, mayHit,
                packet => CellManager.Instance.CellCallMethod(mapChannel, centre, entityId, packet));
        }

        private static List<ObjectDamageHit> Perform(MapChannel mapChannel, Vector3 centre, ulong entityId, ActionId actionId, uint argId,
            Actor owner, Func<Actor, bool> mayHit, Action<PerformObjectAbilityPacket> send)
        {
            var hits = new List<ObjectDamageHit>();

            if (mapChannel == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetAction(actionId, argId, out var module, out var info)
                || (module != DamageModule && module != KnockbackModule))
                return hits;

            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));
            var damageType = info.Has(AbilityProperty.DamageType) && info.Get(AbilityProperty.DamageType) > 0
                ? (DamageType)info.Get(AbilityProperty.DamageType)
                : DamageType.Physical;
            var knockback = Math.Max(0, info.Get(AbilityProperty.KnockbackDistance));

            var victims = Caught(mapChannel, centre, radius, mayHit);

            foreach (var victim in victims)
            {
                var amount = GameEffectManager.ApplyResist(victim, Random.Shared.Next(min, max + 1), out var resisted, damageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, owner, out var outcome, damageType);

                hits.Add(new ObjectDamageHit
                {
                    EntityId = victim.EntityId,
                    DamageType = damageType,
                    Amount = outcome.Delivered,
                    Resisted = resisted,
                    Absorbed = outcome.Absorbed,
                    Immune = outcome.Immune,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                });
            }

            var perform = new PerformObjectAbilityPacket(actionId, argId) { DamageHits = hits, DamageSourceId = entityId };

            foreach (var hit in hits)
                perform.Hits.Add(hit.EntityId);

            send(perform);

            // Thrown once the blast has been told: from where they stood, away from the object.
            if (knockback > 0)
                foreach (var victim in victims)
                {
                    if (victim is Manifestation player)
                        PlayerCrowdControl.Knockback(mapChannel, player, owner, centre, entityId, knockback);
                    else if (victim is Creature creature)
                        CrowdControl.Knockback(mapChannel, creature, owner, centre, entityId, knockback, CrowdControl.KnockbackTypeId, damageType);
                }

            return hits;
        }

        /// <summary>
        /// Every living player on the map and creature in the cells around centre within radius
        /// of it, that mayHit allows: the players first, then the creatures.
        /// </summary>
        public static List<Actor> Caught(MapChannel mapChannel, Vector3 centre, float radius, Func<Actor, bool> mayHit = null)
        {
            var found = new List<Actor>();

            if (mapChannel == null || !(radius > 0))
                return found;

            bool Caught(Actor actor) =>
                actor != null && actor.State != CharacterState.Dead && actor.State != CharacterState.Dying
                && actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0
                && actor.MapContextId == mapChannel.MapInfo.MapContextId
                && Vector3.DistanceSquared(centre, actor.Position) <= radius * radius
                && (mayHit == null || mayHit(actor));

            foreach (var player in mapChannel.ClientList.Select(client => client?.Player))
                if (Caught(player))
                    found.Add(player);

            var seed = CellManager.Instance.GetCellSeed(centre);

            foreach (var cell in CellManager.CellsIn(mapChannel, CellManager.Instance.CreateCellMatrix(mapChannel, seed & 0xFFFF, seed >> 16)))
                foreach (var creature in cell.CreatureList)
                    if (!found.Contains(creature) && Caught(creature))
                        found.Add(creature);

            return found;
        }
    }
}

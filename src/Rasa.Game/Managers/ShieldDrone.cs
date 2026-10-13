using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Bane Shield Drone's shield: what it does to the people shooting it, and what it does
    /// for the Bane standing under it.
    ///
    /// The strategy guide's Enemy Intel entry is the whole design, and the client's two effect
    /// classes are the same rules written as code:
    ///
    ///   "Offense: None. Defense: Projects a shield that reflects almost all damage back at
    ///    attackers; this shield also protects and heals Bane within its area of effect. Energy
    ///    and Virulent weapons do no damage to these machines."
    ///   "Race underneath their shield and use melee attacks, close-range weaponry, or Lightning
    ///    to blast the devices."
    ///
    /// SHIELD_DRONE_SHIELD_SOURCE (229) sits on the drone. Its client class carries
    /// Recv_ReflectDamage and a DAMAGE_REFLECTION_SPEED of 45, so the server names who is being
    /// reflected at and the client flies the hit there itself and floats the damage on arrival.
    /// SHIELD_DRONE_SHIELD (230) sits on the Bane inside, and its class says exactly how the
    /// counterplay works: it "reduces non-EMP damage to friendlies inside a Shield Drone's
    /// shield. Damage from enemies who are also inside the shield is ignored." Getting under the
    /// shield is what turns both halves off, which is what the guide tells the player to do.
    ///
    /// Three numbers are ours rather than the client's, because nothing carries them. The radius
    /// is the drone's own heal radius: CR_SHIELD_DRONE_HEAL states RADIUS_AROUND_SOURCE 60, and
    /// the guide describes the shield and the heal with the same phrase - "within its area of
    /// effect" - so they are one volume. The reflected share is 90%, which is "almost all"
    /// without being all. The protected share is 50%, chosen so that shooting into the bubble is
    /// bad rather than pointless; nothing states it.
    ///
    /// A drone is recognised by its entity class - Bane_Shield_Drone (7233) and the boss,
    /// Bane_Shield_Drone_Boss (24084, Cavalon) - or by carrying CR_SHIELD_DRONE_HEAL, so any other
    /// creature seeded with the heal behaves as one. Only two of the drone rows carry the heal;
    /// the zone drones and Cavalon have the strike alone, and a drone without the heal heals on
    /// the heal row's own schedule (HealIntervalMs) all the same.
    /// </summary>
    public static class ShieldDrone
    {
        /// <summary>SHIELD_DRONE_SHIELD_SOURCE: on the drone, and what reflects.</summary>
        public const int SourceTypeId = 229;

        /// <summary>SHIELD_DRONE_SHIELD: on the Bane under it.</summary>
        public const int ShieldTypeId = 230;

        /// <summary>CR_SHIELD_DRONE_HEAL: the heal, which also makes whatever carries it a drone.</summary>
        public const ActionId HealAction = (ActionId)245;

        /// <summary>CR_SHIELD_DRONE_ATTACK: the contact strike, three metres and no chasing.</summary>
        public const ActionId AttackAction = (ActionId)396;

        /// <summary>Bane_Shield_Drone and Bane_Shield_Drone_Boss.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses> { (EntityClasses)7233, (EntityClasses)24084 };

        /// <summary>
        /// How often a drone without a heal row heals: the cooldown of the one the world has
        /// ("Bane Shield Drone heal aura", 5000 ms).
        /// </summary>
        public const int HealIntervalMs = 5000;

        /// <summary>RADIUS_AROUND_SOURCE on the heal, and the guide's "area of effect" for both halves.</summary>
        public const float Radius = 60f;

        /// <summary>"Almost all damage", ours to pick. Nothing in the client states a share.</summary>
        public const int ReflectPercent = 90;

        /// <summary>What the shield takes off a non-EMP hit on the Bane inside it. Ours; see above.</summary>
        public const int MitigatePercent = 50;

        /// <summary>How often the shield is re-cast over whoever is now inside it.</summary>
        private const int RefreshMs = 1000;

        /// <summary>Whether this creature is a Shield Drone: one of the drone classes, or anything carrying the drone's heal.</summary>
        public static bool Is(Creature creature) =>
            creature != null && (Classes.Contains(creature.EntityClass)
                                 || creature.Actions != null && creature.Actions.Any(a => a.ActionId == HealAction));

        /// <summary>
        /// Whether this creature holds its ground. A drone's job is to stand over a piece of
        /// ground and cover it, and the guide gives it no offence at all - so it strikes what has
        /// closed on it and chases nothing, which is what makes "sprint directly toward them"
        /// a tactic rather than a way to get led away.
        /// </summary>
        public static bool HoldsGround(Creature creature) => Is(creature);

        /// <summary>
        /// Puts the shield on a drone as it enters the world. Announced, undetachable and without
        /// an expiry: it is what the creature is, not something done to it.
        /// </summary>
        public static void Raise(MapChannel mapChannel, Creature creature)
        {
            if (!Is(creature) || creature.ActiveEffects.Values.Any(e => e.TypeId == SourceTypeId))
                return;

            GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
            {
                TypeId = SourceTypeId,
                EffectLevel = 1,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true
            });
        }

        /// <summary>
        /// Raises the shield on any drone that has not got one, keeps it over whoever is under
        /// it now, and heals them on the drone's own schedule. Runs with the other effect workers.
        ///
        /// Raising from here rather than from the spawn path is deliberate: a drone arrives by
        /// spawn pool, by respawn and by a GM command, and one pass a second over the map's
        /// creatures covers all three without a hook in any of them.
        /// </summary>
        public static void Worker(MapChannel mapChannel)
        {
            // The drones on the map, found in one pass over its creatures. This used to gather
            // every creature on the map into a list first, deduplicated with List.Contains -
            // a linear search per creature, so quadratic in the map's population, twice a second,
            // on every map with a player on it, whether or not it had a single drone - and then
            // measured every drone against every creature on the map again. A creature is on
            // exactly one cell's list, so there is nothing to deduplicate, and a map without a
            // drone is done here.
            List<Creature> drones = null;

            foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                foreach (var creature in cell.CreatureList)
                    if (Is(creature) && Alive(creature))
                        (drones ??= new List<Creature>()).Add(creature);

            if (drones == null)
                return;

            foreach (var drone in drones)
                Raise(mapChannel, drone);

            foreach (var drone in drones)
            {
                var covered = Covered(mapChannel, drone);

                foreach (var bane in covered)
                    Cover(mapChannel, drone, bane);

                Uncover(mapChannel, drone, covered);
                Heal(mapChannel, drone, covered);
            }
        }

        private static bool Alive(Actor actor) =>
            actor != null && actor.State != CharacterState.Dead && actor.State != CharacterState.Dying;

        /// <summary>How many cells out from the drone's own the radius can reach: 60 m over 25.6 m cells.</summary>
        private static readonly int ReachCells = (int)Math.Ceiling(Radius / CellManager.CellSize);

        /// <summary>
        /// The drone's own side, alive, inside the radius, and not the drone itself. Looked for in
        /// the cells the radius can reach, not across the whole map. Cells the map has not got are
        /// passed over rather than created: nothing can be standing in one.
        /// </summary>
        private static List<Creature> Covered(MapChannel mapChannel, Creature drone)
        {
            var found = new List<Creature>();
            var cells = mapChannel.MapCellInfo.Cells;

            // As CellManager.GetCell and GetCellSeed number them.
            var centreX = (uint)(drone.Position.X / CellManager.CellSize + CellManager.CellBias);
            var centreZ = (uint)(drone.Position.Z / CellManager.CellSize + CellManager.CellBias);

            for (var dx = -ReachCells; dx <= ReachCells; dx++)
                for (var dz = -ReachCells; dz <= ReachCells; dz++)
                {
                    var cellX = (uint)(centreX + dx);
                    var cellZ = (uint)(centreZ + dz);

                    if (!cells.TryGetValue((cellX & 0xFFFF) | (cellZ << 16), out var cell))
                        continue;

                    foreach (var creature in cell.CreatureList)
                    {
                        if (creature == drone || !Alive(creature) || creature.TargetCategory != drone.TargetCategory)
                            continue;

                        if (Vector3.Distance(creature.Position, drone.Position) <= Radius)
                            found.Add(creature);
                    }
                }

            return found;
        }

        private static void Cover(MapChannel mapChannel, Creature drone, Creature bane)
        {
            var existing = bane.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == ShieldTypeId);

            if (existing != null)
            {
                // Still inside: push the expiry out rather than re-attaching, which would make
                // the client replay the attach FX on every pass.
                existing.ExpiresTick = System.Environment.TickCount64 + RefreshMs * 3;
                existing.SourceId = drone.EntityId;
                return;
            }

            GameEffectManager.Instance.Attach(mapChannel, bane, new GameEffect
            {
                TypeId = ShieldTypeId,
                EffectLevel = 1,
                SourceId = drone.EntityId,
                Source = drone,
                SourceLevel = (int)drone.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ExpiresTick = System.Environment.TickCount64 + RefreshMs * 3,

                // Its end is pushed out every pass while the Bane stays under the drone, which
                // the client is never going to see: its icon takes the timer from the attach
                // alone. Given three seconds, every shielded Bane showed a shield running out and
                // then sitting at zero for as long as it was under the drone. It ends when the
                // Bane walks out or the drone goes, so it is shown with no timer.
                ShowsDuration = false
            });
        }

        /// <summary>
        /// Who each drone covered on its last pass. Held weakly by the drone, so a drone that
        /// leaves the world takes its entry with it; the shields it left behind run out on their
        /// own three-second expiry.
        /// </summary>
        private static readonly ConditionalWeakTable<Creature, List<Creature>> LastCovered = new ConditionalWeakTable<Creature, List<Creature>>();

        /// <summary>When a drone without a heal row heals next (Environment.TickCount64), held weakly as LastCovered is.</summary>
        private static readonly ConditionalWeakTable<Creature, StrongBox<long>> NextHeal = new ConditionalWeakTable<Creature, StrongBox<long>>();

        /// <summary>
        /// Takes the shield off anyone this drone was covering who has walked out of it. Only
        /// those it covered last time are looked at - it used to search every creature on the map
        /// for a shield from this drone, per drone, per pass.
        /// </summary>
        private static void Uncover(MapChannel mapChannel, Creature drone, List<Creature> covered)
        {
            if (LastCovered.TryGetValue(drone, out var previous))
            {
                foreach (var creature in previous)
                {
                    if (covered.Contains(creature))
                        continue;

                    var shield = creature.ActiveEffects.Values.FirstOrDefault(
                        e => e.TypeId == ShieldTypeId && e.SourceId == drone.EntityId);

                    if (shield != null)
                        GameEffectManager.Instance.DettachEffect(mapChannel, creature, shield);
                }
            }

            LastCovered.AddOrUpdate(drone, covered);
        }

        /// <summary>
        /// "Protects and heals Bane within its area of effect", on the schedule the client's own
        /// action data gives: HEAL_AMOUNT 500 every five seconds, which is the creature_action
        /// row's cooldown. A drone without the row keeps the same schedule on its own clock.
        /// </summary>
        private static void Heal(MapChannel mapChannel, Creature drone, List<Creature> covered)
        {
            var heal = drone.Actions?.FirstOrDefault(a => a.ActionId == HealAction);

            if (heal != null)
            {
                if (heal.CooldownTimer > 0)
                    return;

                heal.CooldownTimer = BehaviorManager.NextCooldown(drone, heal);
            }
            else
            {
                var now = Environment.TickCount64;
                var next = NextHeal.GetValue(drone, _ => new StrongBox<long>(0));

                if (now < next.Value)
                    return;

                next.Value = now + HealIntervalMs;
            }

            var amount = HealAmount(drone);

            if (amount <= 0)
                return;

            foreach (var bane in covered)
                ActorManager.Instance.Heal(bane, amount, drone.EntityId);

            // The drone patches itself as well: it is the thing standing in the shield.
            ActorManager.Instance.Heal(drone, amount, drone.EntityId);
        }

        /// <summary>HEAL_AMOUNT_MIN/MAX off CR_SHIELD_DRONE_HEAL, scaled to the drone's level.</summary>
        private static int HealAmount(Creature drone)
        {
            if (!AbilityManager.Instance.TryGetLevel(HealAction, 1, out var level))
                return 0;

            var min = level.Get(AbilityProperty.HealAmountMin);
            var max = level.Get(AbilityProperty.HealAmountMax);

            if (max < min)
                max = min;

            return AbilityManager.Scale((int)drone.Level, min == max ? min : min + (max - min) / 2,
                level.Get(AbilityProperty.DamageScaleType));
        }

        /// <summary>
        /// Whether an attacker is standing inside the shield that covers this victim - which is
        /// what turns both halves of it off. For the drone that is its own radius; for a Bane
        /// under the shield it is the radius of whichever drone put the shield there.
        /// </summary>
        public static bool AttackerIsInside(MapChannel mapChannel, Actor victim, Actor attacker)
        {
            if (attacker == null || victim == null)
                return false;

            var centre = ShieldCentre(mapChannel, victim);

            return centre.HasValue && Vector3.Distance(attacker.Position, centre.Value) <= Radius;
        }

        private static Vector3? ShieldCentre(MapChannel mapChannel, Actor victim)
        {
            if (victim.ActiveEffects.Values.Any(e => e.TypeId == SourceTypeId))
                return victim.Position;

            var shield = victim.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == ShieldTypeId);

            if (shield == null)
                return null;

            // The drone that raised it; if it has gone, its own position is the best centre left.
            return shield.Source != null && Alive(shield.Source) ? shield.Source.Position : (Vector3?)null;
        }

        /// <summary>
        /// What is left of a hit after the shield has taken its share. Non-EMP damage on a Bane
        /// under the shield is reduced; damage from an attacker who is also inside is not, which
        /// is the client's own rule and the guide's tactic.
        /// </summary>
        public static int Mitigate(MapChannel mapChannel, Actor victim, Actor attacker, int amount, DamageType damageType)
        {
            if (amount <= 0 || damageType == DamageType.EMP)
                return amount;

            if (!victim.ActiveEffects.Values.Any(e => e.TypeId == ShieldTypeId))
                return amount;

            if (AttackerIsInside(mapChannel, victim, attacker))
                return amount;

            return amount - amount * MitigatePercent / 100;
        }

        /// <summary>
        /// Sends what a drone was hit with back at whoever hit it, unless they are inside the
        /// shield. The attacker takes it through ActorManager.Damage, so a reflection can kill
        /// and the kill belongs to the drone; the clients that can see the drone are told through
        /// the shield effect's own ReflectDamage, and the client flies it across at its
        /// DAMAGE_REFLECTION_SPEED and floats the damage when it lands.
        /// </summary>
        public static void Reflect(MapChannel mapChannel, Creature drone, Actor attacker, int amount, DamageType damageType)
        {
            if (attacker == null || amount <= 0 || !Alive(attacker))
                return;

            var source = drone.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == SourceTypeId);

            if (source == null || AttackerIsInside(mapChannel, drone, attacker))
                return;

            var reflected = amount * ReflectPercent / 100;

            if (reflected <= 0)
                return;

            var taken = ActorManager.Instance.Damage(mapChannel, attacker, reflected, drone, out var outcome, damageType);

            CellManager.Instance.CellCallMethod(mapChannel, drone, new ShieldDroneReflectPacket(
                source.EffectId, attacker.EntityId, damageType, outcome.Delivered,
                taken > 0 && attacker.Attributes[Attributes.Health].Current <= 0, outcome.Absorbed, outcome.Immune));
        }
    }
}

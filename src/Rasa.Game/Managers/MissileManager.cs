using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Timer;
    using Structures;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;

    public class MissileManager
    {
        private static MissileManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Timer Timer = new Timer();

        public static MissileManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (InstanceLock)
                {
                    if (_instance == null)
                        _instance = new MissileManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Furthest a missile may be aimed. Cells are 25.6 units and a client is only ever told
        /// about the 5x5 cells around it, so nothing past ~64 units is even on its screen; this
        /// is twice that, which no weapon reaches and no honest client asks for.
        /// </summary>
        internal const float MaxTargetDistance = 128f;

        private MissileManager()
        {
        }

        /// <summary>
        /// Entity ids are global, but cells are per map: CellCallMethod indexes this map's cell
        /// table with the target's cell seeds, and a target on another map throws
        /// KeyNotFoundException on the main loop. A client keeps its Target across a waypoint
        /// or summon, so this is reachable by pressing fire before re-targeting.
        /// </summary>
        private static bool IsOnMap(MapChannel mapChannel, Actor actor)
            => MapInstanceScope.Contains(mapChannel, actor);

        /// <summary>
        /// Marks an actor as being in a fight, if it is a player. Creatures have their own notion
        /// of it in BehaviorManager and are not touched here.
        ///
        /// Looked up through the actor's own map rather than Server.Clients: damage is a hot path
        /// and one map's client list is a great deal shorter than every client on the server.
        /// </summary>
        private static void EnterCombat(Actor actor)
        {
            if (!(actor is Manifestation player) || player.MapChannel == null)
                return;

            foreach (var client in player.MapChannel.ClientList)
                if (client.Player == player)
                {
                    ManifestationManager.Instance.EnterCombat(client);
                    return;
                }
        }

        /// <summary>
        /// Applies the victim's resistance (from the effects on them; see
        /// GameEffectManager.ApplyResist) to a missile about to land, so the damage taken and
        /// the damage reported agree, and the hit data says what was resisted.
        /// </summary>
        private static void Resist(Actor victim, Missile missile)
        {
            // Typed: a player's weapon and a creature's both say what they deal, so a type-limited
            // resistance (Polarity Field, Hazmat Armor) counts only against its own type.
            missile.DamageA = GameEffectManager.ApplyResist(victim, missile.DamageA, out var resisted, missile.DamageType);

            if (resisted > 0)
                foreach (var hit in missile.Args.HitData)
                    if (hit.EntityId == victim.EntityId)
                    {
                        hit.Resisted = (uint)resisted;
                        hit.FinalAmt = missile.DamageA;
                    }
        }

        /// <summary>
        /// A hit the victim is immune to: nothing lands, and its hit records carry wasImmune, so
        /// the recovery that shows the shot shows "Immune" over the victim in place of a number
        /// (Actor.AnnounceDamage posts COMBAT_IMMUNE_ANNOUNCED for it).
        /// </summary>
        private static void Immune(Missile missile, Actor victim)
        {
            missile.DamageA = 0;

            foreach (var hit in missile.Args.HitData)
                if (hit.EntityId == victim.EntityId)
                {
                    hit.WasImune = 1;
                    hit.FinalAmt = 0;
                    hit.Resisted = 0;
                    hit.Absorbed = 0;
                }
        }

        /// <summary>
        /// The part of a missile's damage that meets armour. The rest - ArmorBypassPercent of it
        /// - goes past, and with whatever the armour could not stop comes off health: "Bypass
        /// Armor: 25% of damage done directly to Health".
        /// </summary>
        public static int ArmorShare(Missile missile)
        {
            // The weapon's own bypass and the target's Target Painting pierce add up.
            var bypass = Math.Min(100, missile.ArmorBypassPercent + (missile.TargetActor != null ? GameEffectManager.ArmorPiercePercentOf(missile.TargetActor) : 0));

            if (bypass <= 0)
                return missile.DamageA;

            return missile.DamageA - missile.DamageA * bypass / 100;
        }

        private void DoDamageToCreature(MapChannel mapChannel, Missile missile)
        {
            var creature = EntityManager.Instance.GetCreature(missile.TargetEntityId);

            // Dead, or held in its Critical Death window, where nothing but the finisher touches it.
            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            // A manually gated target is also excluded by MayFight below. Keep the immune hit
            // record before that refusal, as for every other form of damage immunity.
            if (DamageImmunity.IsImmune(creature, missile.DamageType))
            {
                Immune(missile, creature);
                return;
            }

            // Its target category has to allow the hit: a player may strike HOSTILE and NEUTRAL,
            // a creature anything its own category may fight (TargetCategories), as Mind Control
            // bends it (BehaviorManager.MayFight). The client does not offer the rest as targets;
            // this is the server holding to it.
            if (missile.Source != null && (missile.Source is Manifestation shooter
                                               ? !TargetCategories.PlayerMayAttack(creature.TargetCategory) && !Pvp.IsEnemySummon(shooter, creature)
                                           : missile.Source is Creature attacker ? !BehaviorManager.MayFight(attacker, creature.EntityId)
                                           : true))
            {
                missile.DamageA = 0;
                return;
            }

            // A player under an enemy's Traitor may not hurt the creatures of the caster's side (Pvp.Restrained).
            if (Pvp.Restrained(missile.Source, creature))
            {
                Immune(missile, creature);
                return;
            }

            // Shooting something is being in a fight, not only being shot at - otherwise a player
            // who opens fire and wins never enters combat at all.
            EnterCombat(missile.Source);

            var beforeResist = missile.DamageA;

            Resist(creature, missile);

            var resisted = Math.Max(0, beforeResist - missile.DamageA);

            // A Shield Drone's shield, before armour: it takes its share of any non-EMP hit on a
            // Bane standing inside it, and none at all from an attacker who is inside it too.
            missile.DamageA = ShieldDrone.Mitigate(mapChannel, creature, missile.Source, missile.DamageA, missile.DamageType);

            // decrease armor first - all of it but what bypasses armour
            // all of it to health while an EMP crit suppresses its armour
            var armorDecrease = GameEffectManager.ArmorSuppressed(creature) ? 0 : Math.Min(ArmorShare(missile), creature.Attributes[Attributes.Armor].Current);
            creature.Attributes[Attributes.Armor].Current -= armorDecrease;
            // With the rate the effects make, or a painted creature's bar would start regenerating again on the clients.
            CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateArmorPacket(GameEffectManager.WithRegen(creature, creature.Attributes[Attributes.Armor]), creature.EntityId));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(missile.DamageA - armorDecrease, creature.Attributes[Attributes.Health].Current);
            CreatureManager.RecordCombatDamage(mapChannel, creature, missile.Source, armorDecrease + healthDecrease);
            creature.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateHealthPacket(creature.Attributes[Attributes.Health], creature.EntityId));
            
            if (creature.Attributes[Attributes.Health].Current <= 0)
            {
                // fix health so it dont regenerate after death
                missile.TargetActor.Attributes[Attributes.Health].Current = 0;
                missile.TargetActor.Attributes[Attributes.Health].RefreshAmount = 0;
                missile.TargetActor.Attributes[Attributes.Health].RefreshPeriod = 0;

                // fix armor so it dont regenerate after death
                missile.TargetActor.Attributes[Attributes.Armor].Current = 0;
                missile.TargetActor.Attributes[Attributes.Armor].RefreshAmount = 0;
                missile.TargetActor.Attributes[Attributes.Armor].RefreshPeriod = 0;
                // kill craeture
                CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, missile.Source);
            }
            else if (!StunAndCheckCritDeath(mapChannel, creature, missile))
            {
                // The shooter is hated for what landed and what was resisted, and a wandering
                // creature turns on them (Threat).
                Threat.FromDamage(creature, missile.Source, armorDecrease + healthDecrease, 0, resisted);

                WeaponBonus(mapChannel, creature, missile);

                // The weapon's modules: a steal, a resist debuff, each on its own roll.
                if (missile.WeaponProcs != null && missile.DamageA > 0)
                    ItemModuleBonuses.OnWeaponHit(mapChannel, missile.Source, creature, missile.WeaponProcs);

                // Explosive Nanites go off on damage taken.
                if (missile.DamageA > 0)
                    AbilityManager.OnCreatureDamaged(mapChannel, creature);

                // Mind Control's Infectious: a slave's victim may be confused in turn.
                if (missile.DamageA > 0 && missile.Source is Creature slave)
                    AbilityManager.Instance.OnMindSlaveHit(mapChannel, slave, creature);

                // A creature's attack that carries an effect puts it on the creature it hit as
                // on a player (a Forean shaman's Decay on the Bane it fights).
                if (missile.Source is Creature striker)
                    CreatureEffectAttacks.OnCreatureHit(mapChannel, striker, creature, missile);
            }

            // A Shield Drone sends almost all of it back, unless the shot came from inside the
            // shield. Last, so what is reflected is what the drone actually took, and so a drone
            // killed by the hit still answers it - "errant shots are reflected off these
            // projected shields, oftentimes resulting in death to the attackers".
            if (armorDecrease + healthDecrease > 0)
                ShieldDrone.Reflect(mapChannel, creature, missile.Source, armorDecrease + healthDecrease, missile.DamageType);
        }

        private readonly Random _random = new Random();

        /// <summary>
        /// misstype 4: WEAPON_STAFF_PARRY on the one missed and PM_COMBAT_DEFLECT - a staff
        /// deflecting the hit (3 is the same parry with "Parry", 2 a dodge, 1 a plain miss).
        /// </summary>
        public const uint MissTypeDeflect = 4;

        /// <summary>misstype 2, a dodge: the player a creature's area ability was wound up at had left its area when it landed (CreatureWindups).</summary>
        public const uint MissTypeDodge = 2;

        /// <summary>misstype 1, a plain miss: a shot that went wide (ToHit).</summary>
        public const uint MissTypeMiss = 1;

        /// <summary>
        /// A hit that left its creature alive: the stuns, knockback, slow and freeze it carries (an
        /// Ice, Sonic or Virulent crit, Hand to Hand, a grenade, a net gun), then whether the creature is now stunned and near death, which opens
        /// its Critical Death window. Returns whether the window opened.
        /// </summary>
        private static bool StunAndCheckCritDeath(MapChannel mapChannel, Creature creature, Missile missile)
        {
            // What the hit puts on the creature is announced by the hit, when the shot is seen
            // to land: attached quietly and named in this creature's entry of the recovery
            // (HitEffects). With no entry to name them in they announce themselves, as before.
            using (HitEffects.On(creature, missile.Source, HitOf(missile, creature)?.TargetEffectIds, StrikeMsOf(missile, creature)))
                return ApplyHitEffects(mapChannel, creature, missile);
        }

        /// <summary>
        /// How long, at most, after the recovery arrives the client plays the hit on this actor:
        /// the shot's flight to it from where the shooter stands (ShotFlight). 0 for a swing or
        /// a creature's attack, which the server does not time.
        /// </summary>
        private static int StrikeMsOf(Missile missile, Actor struck)
        {
            if (missile?.FlightVelocity is not int velocity || missile.Source == null || struck == null)
                return 0;

            return ShotFlight.Ms(Vector3.Distance(missile.Source.Position, struck.Position), velocity);
        }

        /// <summary>The missile's entry for a hit on this actor in the recovery it will be sent in, or null.</summary>
        private static HitData HitOf(Missile missile, Actor struck)
        {
            var hits = missile?.Args?.HitData;

            if (hits == null || struck == null)
                return null;

            for (var i = hits.Count - 1; i >= 0; i--)
                if (hits[i] != null && hits[i].EntityId == struck.EntityId)
                    return hits[i];

            return null;
        }

        private static bool ApplyHitEffects(MapChannel mapChannel, Creature creature, Missile missile)
        {
            var damageType = missile.DamageType == 0 ? DamageType.Physical : missile.DamageType;

            if (missile.Source is Manifestation)
            {
                if (missile.IsCritical)
                    CritEffects.OnCritical(mapChannel, creature, missile.Source, damageType, missile.DamageA);

                if (creature.State != CharacterState.Dying && missile.StunMs > 0 && Stuns.Roll(missile.StunChance))
                    Stuns.Apply(mapChannel, creature, missile.Source, Stuns.StunTypeId, missile.StunMs, damageType);

                // Net guns hold what they hit where it stands.
                if (creature.State != CharacterState.Dying && missile.RootMs > 0)
                    CrowdControl.Root(mapChannel, creature, missile.Source, CrowdControl.NetGunRootTypeId, missile.RootMs);

                // Firearms' shotguns and Hand to Hand knock it back: the client's default
                // distance, since the weapons' own knockback numbers are not in anything we have.
                // A melee knockback stays down for Hand to Hand's stun duration on top.
                if (creature.State != CharacterState.Dying && missile.KnockbackChance > 0 && Stuns.Roll(missile.KnockbackChance))
                    CrowdControl.Knockback(mapChannel, creature, missile.Source, CrowdControl.DefaultKnockbackDistance, CrowdControl.KnockbackTypeId, damageType, missile.KnockbackStunMs);
            }

            if (creature.State == CharacterState.Dying)
                return true;

            return CritDeathManager.Instance.TryEnterPreDeath(mapChannel, creature, missile.Source, damageType);
        }

        /// <summary>
        /// Shredder Ammo: a weapon hit by someone carrying it does the effect's extra damage too,
        /// at most once per its interval. Taken through ActorManager.Damage, so it can kill and
        /// the kill is the shooter's, and shown through the effect's own AnnounceDamage, which
        /// floats it on the target from the shooter.
        /// </summary>
        private void WeaponBonus(MapChannel mapChannel, Actor target, Missile missile)
        {
            var shooter = missile.Source;

            if (shooter == null || target.State == CharacterState.Dead)
                return;

            var now = Environment.TickCount64;

            foreach (var effect in shooter.ActiveEffects.Values)
            {
                if (effect.WeaponBonusMax <= 0 || now < effect.WeaponBonusReadyAt)
                    continue;

                effect.WeaponBonusReadyAt = now + Math.Max(100, effect.WeaponBonusIntervalMs);

                var rolled = AbilityManager.Scale(effect.SourceLevel, _random.Next(effect.WeaponBonusMin, effect.WeaponBonusMax + 1), effect.TickScaleType);
                var amount = GameEffectManager.ApplyResist(target, rolled, out var resisted, effect.WeaponBonusType);
                var taken = ActorManager.Instance.Damage(mapChannel, target, amount, shooter, out var outcome, effect.WeaponBonusType);

                var announce = new GameEffectAnnounceDamagePacket(effect.EffectId);
                announce.Hits.Add(new TickEntry
                {
                    EntityId = target.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = effect.WeaponBonusType,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                });

                CellManager.Instance.CellCallMethod(mapChannel, shooter, announce);
                return;
            }
        }

        /// <summary>
        /// Whether one player's weapon may hurt another: when they are enemies across a wargame
        /// (Pvp.AreEnemies). PvP Safety on either of them is checked where the hit lands, which
        /// shows it as Immune.
        /// </summary>
        private static bool PlayersMayFight(Manifestation attacker, Manifestation defender) => Pvp.AreEnemies(attacker, defender);

        /// <summary>
        /// Who a player's armour and health updates name as having done it, for a missile's hit.
        ///
        /// The client leaves a bar alone when the update names an entity it can see
        /// (Actor.UpdateAttribute: "announce = not HasEntity(whoId)") and moves it when that
        /// entity's damage is announced (Actor.AnnounceDamage ends in AnnounceAttrChange of
        /// HEALTH and ARMOR) - which for a shot is when it is seen to land, its flight after the
        /// recovery arrives (ShotFlight). Naming nobody, as every update here used to, moved a
        /// player's bars the moment a creature fired, ahead of the shot and of its number.
        ///
        /// Named only where the client is known to announce the damage: a creature's weapon
        /// attack, whose class is a BaseWeaponAttack and whose DoHits announces every hit. A
        /// creature ability's class does what it likes with its hits (BaseActorAbility.DoHits
        /// is its DoAbility), and a bar that waited on one that announces nothing would not
        /// move at all. A player's shot at a player names nobody either: the server has already
        /// held that one for its flight, and the bars are due when it lands.
        /// </summary>
        internal static ulong StagedBy(Missile missile)
            => missile?.Source is Creature attacker && CreatureAttacks.IsWeaponAttack(missile) ? attacker.EntityId : 0;

        private void DoDamageToPlayer(MapChannel mapChannel, Missile missile)
        {
            var actor = EntityManager.Instance.GetActor(missile.TargetEntityId);

            // Gone since the missile was launched: logged out, or off the map.
            if (actor == null || actor.State == CharacterState.Dead)
                return;

            // The same rule where the damage lands, for any path that did not go through the
            // launch check: a player's hit on a player does nothing unless PvP allows it.
            if (missile.Source is Manifestation attackerPlayer && actor is Manifestation defenderPlayer
                && !PlayersMayFight(attackerPlayer, defenderPlayer))
            {
                missile.DamageA = 0;
                return;
            }

            // A creature's hit has to be one its target category allows on a player
            // (TargetCategories.MayFightPlayer): a FRIENDLY creature's stray shot does nothing.
            if (missile.Source is Creature attacker && actor is Manifestation hitPlayer
                && !TargetCategories.MayFightPlayer(attacker.TargetCategory, hitPlayer.CombatCategory)
                && !Pvp.SummonMayFight(attacker, hitPlayer.EntityId))
            {
                missile.DamageA = 0;
                return;
            }

            // An enemy player's: none of it while the one hit holds PvP Safety - the hit shows
            // Immune - and PVP_DAMAGE_MODIFIER of it otherwise (Pvp). Firing on an enemy player
            // ends the shooter's own Safety (Pvp.Attack).
            var pvp = Pvp.IsPvp(missile.Source, actor);

            if (pvp)
            {
                if (Pvp.Attack(mapChannel, missile.Source, actor))
                {
                    Immune(missile, actor);
                    return;
                }

                missile.DamageA = Pvp.ScaleDamage(missile.Source, actor, missile.DamageA);
                Pvp.RecordEngagement(missile.Source, actor);
                Pvp.OnHit(mapChannel, missile.Source, actor);
                Battlegrounds.Instance.Damaged(missile.Source, actor, missile.DamageA);
            }

            // Both ends: whoever was hit, and whoever hit them if that was a player too.
            EnterCombat(actor);
            EnterCombat(missile.Source);

            // Immune to it (DamageImmunity): the hit says so, and takes nothing.
            if (DamageImmunity.IsImmune(actor, missile.DamageType))
            {
                Immune(missile, actor);
                return;
            }

            // A smoke screen takes its share off a shot before anything else: "Incoming ranged
            // damage reduced by X%", and melee walks through it.
            if (!missile.IsMelee)
                missile.DamageA = GameEffectManager.ApplyIncomingRanged(actor, missile.DamageA);

            // What the effects on the victim resist comes off first (Rage, Resistance, Sacrifice,
            // Base Wave), and off the missile too, since the recovery packet reports its DamageA
            // as the amount that landed.
            Resist(actor, missile);

            // Then a shield takes its share (Shield Extender, Shield Wave).
            missile.DamageA = GameEffectManager.Instance.ApplyAbsorb(mapChannel, actor, missile.DamageA, out var absorbed);

            if (absorbed > 0)
                foreach (var hit in missile.Args.HitData)
                    if (hit.EntityId == actor.EntityId)
                    {
                        hit.Absorbed = (uint)absorbed;
                        hit.FinalAmt = missile.DamageA;
                    }

            // decrease armor first - all of it but what bypasses armour, and none while an EMP crit
            // suppresses it
            var armorDecrease = GameEffectManager.ArmorSuppressed(actor) ? 0 : Math.Min(ArmorShare(missile), actor.Attributes[Attributes.Armor].Current);

            // Whose doing it is, to the clients: a creature's shot is shown landing after its
            // flight, and the bars move with it rather than as it is fired (StagedBy).
            var stagedBy = StagedBy(missile);

            actor.Attributes[Attributes.Armor].Current -= armorDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(actor.Attributes[Attributes.Armor], stagedBy));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(missile.DamageA - armorDecrease, actor.Attributes[Attributes.Health].Current);

            actor.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(actor.Attributes[Attributes.Health], stagedBy));

            // Brought to zero: dead, or defeated in a duel - the kill counted, back on full and safe
            // for a while - or a GM who may not die back on their feet (PlayerDeath). Either way the
            // hit carries nothing more.
            var defeated = false;

            if (actor.Attributes[Attributes.Health].Current == 0 && actor is Manifestation fallen)
            {
                PlayerDeath.AtZero(mapChannel, fallen, missile.Source);
                defeated = true;
            }

            // Reflective Armor and the Guardian's Reflection send some of it back (Reflection).
            Reflection.Reflect(mapChannel, actor, missile.Source, missile.DamageA, missile.DamageType);

            // What the player says of it, at the end of the tick (PlayerVoices).
            if (!defeated && actor is Manifestation pained)
                PlayerVoices.Hurt(pained, armorDecrease + healthDecrease, missile.IsCritical);

            // Self Destruct goes off on the next damage its holder takes, and Conversion turns
            // what landed into healing for the squad.
            if (actor is Manifestation victim && armorDecrease + healthDecrease > 0)
            {
                AbilityManager.OnPlayerDamaged(mapChannel, victim, armorDecrease + healthDecrease);

                // "The more hits you endure, the more your equipment's durability decreases."
                Durability.WearArmor(mapChannel, victim);
            }

            // A creature attack that knocks back or stuns does so to a player as well, and one
            // that carries a game effect puts it on them (CreatureEffectAttacks) - before the
            // recovery goes out, which is what announces it.
            if (missile.Source is Creature striker && actor is Manifestation struck && struck.State != CharacterState.Dead)
            {
                PlayerCrowdControl.CreatureActionHit(mapChannel, striker, struck, missile.ActionId, missile.ActionArgId);
                CreatureEffectAttacks.OnHit(mapChannel, striker, struck, missile);
            }

            // An enemy player's weapon does to a player what it does to a creature, for
            // PVP_EFFECT_DURATION_MODIFIER of the time (Pvp.ScaleDuration): a crit's side effect,
            // Shredder Ammo's extra damage, a grenade's stun, a net gun's hold, a shotgun's or Hand to
            // Hand's knockback. Not on a player just defeated, who is back on their feet under PvP
            // Safety; and a creature of theirs carries none of it.
            if (pvp && !defeated && missile.DamageA > 0 && actor is Manifestation enemy && missile.Source is Manifestation)
            {
                var damageType = missile.DamageType == 0 ? DamageType.Physical : missile.DamageType;

                // Its side effect is announced by the hit, as on a creature (HitEffects).
                if (missile.IsCritical)
                    using (HitEffects.On(enemy, missile.Source, HitOf(missile, enemy)?.TargetEffectIds, StrikeMsOf(missile, enemy)))
                        CritEffects.OnCritical(mapChannel, enemy, missile.Source, damageType, missile.DamageA);

                WeaponBonus(mapChannel, enemy, missile);

                // The weapon's modules: a steal, a resist debuff, each on its own roll.
                if (missile.WeaponProcs != null)
                    ItemModuleBonuses.OnWeaponHit(mapChannel, missile.Source, enemy, missile.WeaponProcs);

                if (missile.StunMs > 0 && Stuns.Roll(missile.StunChance))
                    PlayerCrowdControl.Stun(mapChannel, enemy, missile.Source, Pvp.ScaleDuration(missile.Source, enemy, missile.StunMs));

                if (missile.RootMs > 0)
                    PlayerCrowdControl.Root(mapChannel, enemy, missile.Source, CrowdControl.NetGunRootTypeId, missile.RootMs);

                if (missile.KnockbackChance > 0 && Stuns.Roll(missile.KnockbackChance))
                    PlayerCrowdControl.Knockback(mapChannel, enemy, missile.Source, CrowdControl.DefaultKnockbackDistance,
                        Pvp.ScaleDuration(missile.Source, enemy, missile.KnockbackStunMs));
            }
        }

        public void RequestWeaponAttack(Client client, RequestWeaponAttackPacket packet)
        {
            // The alternate attack is a melee swing, not a shot: no ammunition, no heat, the
            // weapon's alt damage, and Hand to Hand's bonus rather than the weapon skill's. It
            // used to come through here as one more shot of the gun.
            if (packet.IsAltAction)
            {
                ManifestationManager.Instance.TryMeleeAttack(client, packet);
                return;
            }

            // A constant-fire weapon (leech, polarity and propellant guns, machine guns) is fired
            // by the held trigger - StartAutoFire until StopAutoFire - and nothing else stops its
            // effect. A lone request would put the effect on with no trigger to take it off, and
            // the client, charging until it comes off, would not let the weapon be put away.
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            if (weapon != null && ConstantFire.Handles(EntityClassManager.Instance.GetWeaponClassInfo(weapon)))
                return;

            ManifestationManager.Instance.PlayerTryFireWeapon(client);

                /*

                var weapon = InventoryManager.Instance.CurrentWeapon(client);

                if (weapon == null)
                {
                    Logger.WriteLog(LogType.Error, "no weapon armed but player tries to shoot");
                    return;
                }

                var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);
                var targetType = EntityManager.Instance.GetEntityType((uint)packet.TargetId);
                var target = new Actor();

                switch (targetType)
                {
                    case EntityType.Creature:
                        target = EntityManager.Instance.GetCreature((uint)packet.TargetId).Actor;
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"RequestWeaponAttack:\nUnsuported targetType = {targetType}");
                        return; ;
                }

                var distance = Vector3.Distance(client.MapClient.Player.Actor.Position, target.Position);
                var triggerTime = (int)Math.Round(distance, 0);

                var missile = new Missile
                {
                    ActionId = packet.ActionId,
                    ActionArgId = packet.ActionArgId,
                    TargetEntityId = (uint)packet.TargetId,
                    DamageA = weaponClassInfo.DamageType,
                    IsAbility = false,
                    Source = client,
                    TriggerTime = triggerTime
                };


                missile.TriggerTime = triggerTime;


                QueuedMissiles.Add(missile);
                */
        }

        /// <summary>
        /// Lands the missiles whose time has run out, and only those: a creature's ability when
        /// its windup is done (CreatureWindups), a player's shot held for its flight (ShotFlight),
        /// and everything else on the pass after it was launched.
        ///
        /// Every queued missile used to be triggered on the pass after it was launched, whatever
        /// its trigger time said - the distance to the target was measured, written on the
        /// missile and never read. It rarely showed: the world loop runs every 100 ms and the
        /// longest shot in range flies for 64, so the tick a missile was due on was almost always
        /// the tick it got. It is the second half that mattered: the queue was emptied after the
        /// loop rather than as it was walked, so a missile that threw on the way in took the
        /// emptying with it, left every queued missile where it was, and threw again on the next
        /// pass - out of the map worker, which abandons the rest of that tick and every map after
        /// it, for as long as the server ran. A missile comes off the queue before it is
        /// triggered now, and a trigger that fails costs only itself.
        /// </summary>
        public void DoWork(MapChannel mapChannel, long delta)
        {
            for (var i = mapChannel.QueuedMissiles.Count - 1; i >= 0; i--)
            {
                var missile = mapChannel.QueuedMissiles[i];

                missile.TriggerTime -= delta;

                if (missile.TriggerTime > 0)
                    continue;

                mapChannel.QueuedMissiles.RemoveAt(i);

                // A creature killed while winding an ability up does not land it, nor one stunned
                // or knocked down out of it - and one still alive has the windup called off on the
                // clients, which would otherwise hold it wound up (CreatureWindups.Interrupt).
                if (missile.AfterWindup && missile.Source is Creature winder
                    && (winder.State == CharacterState.Dead || winder.State == CharacterState.Dying
                        || !winder.Attributes.TryGetValue(Attributes.Health, out var winderHealth) || winderHealth.Current <= 0
                        || Stuns.IsStunned(winder)))
                {
                    CreatureWindups.Interrupt(mapChannel, winder, missile.ActionId, missile.ActionArgId);
                    continue;
                }

                // A shot held for its flight (ShotFlight) whose shooter has left the map since -
                // through a waypoint, or out of the game: it lands on nothing. The shooter's
                // cell is where its recovery is sent, and that is on another map now.
                if (missile.HeldForFlight && !IsOnMap(mapChannel, missile.Source))
                    continue;

                try
                {
                    MissileTrigger(mapChannel, missile);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Missile {missile.ActionId} from {missile.Source?.EntityId} at {missile.TargetEntityId} on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                }
            }
        }

        /// <param name="armorBypassPercent">Percent of the damage that skips armour: the Torqueshell and Injection Gun skills. A player's item modules add their "Armor Piercing" to it here.</param>
        /// <param name="critBonus">Crit chance in percent the attack adds to the shooter's own (Firearms on a rifle).</param>
        /// <param name="melee">A melee swing, for the crouching crit modifiers.</param>
        /// <param name="stunChance">Chance in percent the hit stuns a creature for stunMs (Hand to Hand, grenades).</param>
        /// <param name="rootMs">How long the hit holds a creature where it stands (net guns).</param>
        /// <param name="splashRadius">Metres around the target a launcher's splash reaches (Splash); 0 for none.</param>
        /// <param name="coneHalfAngle">Degrees either side of the shooter's facing a cone weapon hits (ConeWeapons); 0 for a single target.</param>
        /// <param name="knockbackStunMs">How much longer a creature the knockback lands on stays down (Hand to Hand).</param>
        /// <param name="landsInMs">A creature ability's windup and flight (CreatureWindups): when it lands, in place of the next pass a shot resolves on.</param>
        /// <param name="optimalRange">A player's weapon shot: the weapon's optimal range, past which its damage drops (RangeFalloff); 0 for no drop.</param>
        /// <param name="flightVelocity">A player's weapon shot: the weapon class's velocity, which their client flies it at (ShotFlight). Null for anything the server does not hold for a flight - a swing, a creature's attack.</param>
        /// <param name="refireMs">A player's weapon shot: how soon the weapon can be fired again; a shot is held for its flight only if it lands before that (ShotFlight).</param>
        public void MissileLaunch(MapChannel mapChannel, ActionData action, int damage, int armorBypassPercent = 0, DamageType damageType = 0, double critBonus = 0, bool melee = false, int stunChance = 0, int stunMs = 0, int rootMs = 0, int knockbackChance = 0, float splashRadius = 0, float coneHalfAngle = 0, int knockbackStunMs = 0, CreatureAction creatureAction = null, int? landsInMs = null, float optimalRange = 0, int? flightVelocity = null, long refireMs = 0)
        {
            // How long before it lands, from how far it has to go. A player's shot is held for
            // its flight where their client can still show it land (ShotFlight); anything else
            // resolves on the next pass, as the half a millisecond a metre it used to be given
            // always came to.
            int LandsIn(float distance) => flightVelocity.HasValue && !melee
                ? ShotFlight.HeldMs(distance, flightVelocity.Value, refireMs)
                : (int)(distance * 0.5f);

            var manualSource = action.Actor as Creature;
            var sourceCombatAuthorization = manualSource?.ScriptedCombatAuthorization;
            if (manualSource != null &&
                !CreatureWindups.HasCombatAuthorization(mapChannel, manualSource, sourceCombatAuthorization))
            {
                Logger.WriteLog(LogType.Debug, $"Manual combat source {manualSource.EntityId} has no current attack authority.");
                return;
            }

            // A player's ranged shot is weakened by what weakens their ranged attacks (a Laser
            // crit, Called Shot: Eye); a swing is not.
            if (!melee && action.Actor is Manifestation rangedShooter)
                damage = GameEffectManager.ApplyRangedDamage(rangedShooter, damage);

            // A player's weapon attack, shot or swing: the modules in what they wear and hold
            // pierce armor too, and the weapon's may steal or debuff where it lands
            // (ItemModuleBonuses).
            var modules = ItemModuleBonuses.Of(action.Actor);

            armorBypassPercent += modules.ArmorPierce;

            var missile = new Missile
            {
                CreatureAction = creatureAction,
                DamageA = damage,
                DamageType = damageType,
                WeaponProcs = modules.Procs.Count > 0 ? modules.Procs : null,
                ArmorBypassPercent = Math.Max(0, Math.Min(100, armorBypassPercent)),
                Source = action.Actor,
                SourceCombatAuthorization = sourceCombatAuthorization,
                IsMelee = melee,
                CritChance = CriticalHits.AttackerChance(action.Actor, melee, critBonus),
                StunChance = stunChance,
                StunMs = stunMs,
                RootMs = rootMs,
                KnockbackChance = Math.Max(0, knockbackChance),
                KnockbackStunMs = Math.Max(0, knockbackStunMs),
                // Launchers: the share each splashed creature takes is of the damage before the
                // crit roll, which only the target hit makes.
                SplashRadius = Math.Max(0, splashRadius),
                SplashDamage = splashRadius > 0 ? Splash.DamageOf(damage) : 0,
                OptimalRange = melee ? 0 : Math.Max(0, optimalRange)
            };

            // A cone weapon picks what it hits from where the shooter faces, not from a lock.
            if (coneHalfAngle > 0 && action.Actor is Manifestation coneShooter)
                AimCone(mapChannel, coneShooter, action, missile, coneHalfAngle, damage);

            // A player's weapon at a creature they may not attack - a FRIENDLY soldier, a vendor.
            // The client fires at whatever SetTargetId named, and the missile used to land on it
            // as a hit: DoDamageToCreature took the damage back, but every client was still shown
            // the creature being shot. It goes out as a shot at nothing instead.
            if (action.TargetId != 0 && action.Actor is Manifestation aiming &&
                EntityManager.Instance.GetEntityType(action.TargetId) == EntityType.Creature &&
                EntityManager.Instance.GetCreature(action.TargetId) is { } notAnEnemy &&
                !TargetCategories.PlayerMayAttack(notAnEnemy.TargetCategory) && !Pvp.IsEnemySummon(aiming, notAnEnemy))
                action.TargetId = 0;

            // get distance between actors
            Actor targetActor = null;
            var triggerTime = 0; // time between windup and recovery

            if (action.TargetId != 0)
            {
                // target on entity
                var targetType = EntityManager.Instance.GetEntityType(action.TargetId);

                // A destructible prop of the .map is in no registry (WorldDestructibles): an object.
                if (targetType == 0 && WorldDestructibles.TryGetTarget(mapChannel, action.TargetId, out _))
                    targetType = EntityType.Object;

                if (targetType == 0)
                {
                    Logger.WriteLog(LogType.Error, $"The missile target doesnt exist: {action.TargetId}");
                    // entity does not exist
                    return;
                }

                // A force field (ForceFields): only one that stops the shooter, and the missile
                // lands on the field's own hit points; there is no actor to it.
                if (targetType == EntityType.Object && ForceFields.Find(action.TargetId) is { } field)
                {
                    if (field.MapChannel != mapChannel || !ForceFields.MayShoot(field, action.Actor))
                        return;

                    var fieldDistance = Vector3.Distance(field.Position, action.Actor.Position);

                    if (fieldDistance > MaxTargetDistance)
                        return;

                    missile.TargetEntityId = action.TargetId;
                    triggerTime = LandsIn(fieldDistance);
                }
                else if (targetType == EntityType.Object && PersonalWaypoints.TryGetPosition(action.TargetId, out var waypointPosition))
                {
                    // A Personal Waypoint (PersonalWaypoints): only for an enemy of its owner's or
                    // a creature that fights players, and as with a field the missile lands on
                    // its own hit points.
                    if (!PersonalWaypoints.MayBeAttackedBy(action.Actor, action.TargetId))
                        return;

                    var waypointDistance = Vector3.Distance(waypointPosition, action.Actor.Position);

                    if (!float.IsFinite(waypointDistance) || waypointDistance > MaxTargetDistance)
                        return;

                    missile.TargetEntityId = action.TargetId;
                    triggerTime = LandsIn(waypointDistance);
                }
                else if (targetType == EntityType.Object && AbilityManager.TryGetPlantPosition(action.TargetId, out var plantPosition))
                {
                    // A Hortimonculus (AbilityManager): only for an enemy of its owner's or a
                    // creature fighting it, and the missile lands on its own hit points.
                    if (!AbilityManager.PlantMayBeAttackedBy(action.Actor, action.TargetId))
                        return;

                    var plantDistance = Vector3.Distance(plantPosition, action.Actor.Position);

                    if (!float.IsFinite(plantDistance) || plantDistance > MaxTargetDistance)
                        return;

                    missile.TargetEntityId = action.TargetId;
                    triggerTime = LandsIn(plantDistance);
                }
                else if (targetType == EntityType.Object)
                {
                    // A Bootcamp practice target (PracticeTargetManager).
                    if (!PracticeTargetManager.TryGetTarget(mapChannel, action.TargetId, out var practiceTarget) ||
                        !PracticeTargetManager.CanHit(mapChannel, action.Actor, practiceTarget))
                    {
                        Logger.WriteLog(LogType.Debug, $"MissileLaunch: invalid practice target {action.TargetId}.");
                        return;
                    }

                    var targetDistance = Vector3.Distance(practiceTarget.Position, action.Actor.Position);

                    if (!float.IsFinite(targetDistance) || targetDistance > MaxTargetDistance)
                        return;

                    missile.TargetObject = practiceTarget;
                    missile.TargetEntityId = action.TargetId;
                    triggerTime = LandsIn(targetDistance);
                }
                else
                {
                    switch (targetType)
                    {
                        case EntityType.Creature:
                            {
                                targetActor = EntityManager.Instance.GetCreature(action.TargetId);
                                missile.TargetEntityId = action.TargetId;
                            }
                            break;
                        case EntityType.Character:
                            {
                                targetActor = EntityManager.Instance.GetPlayer(action.TargetId);
                                missile.TargetEntityId = action.TargetId;

                                // A player's weapon at another player: only at an enemy across a
                                // wargame (Pvp), which the client offers once it is shown HOSTILE.
                                if (action.Actor is Manifestation shootingPlayer && targetActor is Manifestation shotPlayer
                                    && !PlayersMayFight(shootingPlayer, shotPlayer))
                                    return;
                            }
                            break;
                        default:
                            Logger.WriteLog(LogType.Error, $"Can't shoot that object");
                            return;
                    };

                    if (targetActor == null || targetActor.State == CharacterState.Dead || targetActor.State == CharacterState.Dying ||
                        !targetActor.Attributes.TryGetValue(Attributes.Health, out var targetHealth) || targetHealth.Current <= 0)
                        return; // actor is dead (or dying in its Critical Death window), cannot be shot at

                    if (!IsOnMap(mapChannel, targetActor))
                    {
                        Logger.WriteLog(LogType.Debug, $"MissileLaunch: {action.Actor.EntityId} aimed at {action.TargetId}, which is on map {targetActor.MapContextId}, not {mapChannel.MapInfo.MapContextId}");
                        return;
                    }

                    var distance = Vector3.Distance(targetActor.Position, action.Actor.Position);

                    if (!float.IsFinite(distance) || distance > MaxTargetDistance)
                    {
                        Logger.WriteLog(LogType.Debug, $"MissileLaunch: {action.Actor.EntityId} aimed at {action.TargetId} from {distance:F0} units away");
                        return;
                    }

                    triggerTime = LandsIn(distance);
                }
            }
            else
            {
                // has no target -> Shoot towards looking angle
                targetActor = null;
                triggerTime = 0;
            }

            // Weapons only: abilities resolve in AbilityManager from their own tables, and the
            // lightning special case that used to fire from here on a hand-typed damage roll
            // went with them.
            missile.TargetActor = targetActor;
            missile.TriggerTime = landsInMs ?? triggerTime;
            missile.AfterWindup = landsInMs.HasValue;
            missile.HeldForFlight = !landsInMs.HasValue && flightVelocity.HasValue && !melee && triggerTime > 0;
            missile.FlightVelocity = melee ? null : flightVelocity;

            // Wound up where its target stands now: a cone points there, a ring around a target
            // lands there, whoever is there when it goes off (CreatureWindups).
            if (missile.AfterWindup && targetActor != null)
                missile.AreaCentre = targetActor.Position;
            missile.ActionId = action.ActionId;
            missile.ActionArgId = action.ActionArgId;
            missile.IsAbility = false;

            if (action.Actor is Creature)
                missile.AreaDamage = damage;

            // A weapon's shot at somebody: the to-hit roll (ToHit), made as the trigger is
            // pulled. One that misses with nothing else to land - no splash, no cone - is not
            // held for its flight: the clients are told now, while the shooter's own is still
            // flying the round.
            if (targetActor != null && !melee && CreatureAttacks.IsWeaponAttack(missile))
            {
                missile.Missed = ToHit.Misses(missile);

                if (missile.Missed == true && missile.HeldForFlight && missile.SplashRadius <= 0
                    && (missile.ConeTargets == null || missile.ConeTargets.Count == 0))
                {
                    missile.TriggerTime = 0;
                    missile.HeldForFlight = false;
                }
            }

            // An escort's owner opening fire gives the escort its target (mission scenes).
            if (targetActor is Creature attackedCreature)
                CreatureManager.RecordOwnerAttack(mapChannel, action.Actor, attackedCreature);

            CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.ThreeArgs, missile.ActionId, missile.ActionArgId, missile.TargetEntityId));

            // Firing gives a cloaked shooter away, whoever they were shooting at, and is a combat
            // action to a Feedback on them.
            if (action.Actor is Manifestation shooter)
            {
                Stealth.Break(mapChannel, shooter);
                AbilityManager.OnPlayerActed(mapChannel, shooter, true);
            }

            mapChannel.QueuedMissiles.Add(missile);
        }

        /// <summary>
        /// A creature's blow that has already been wound up - KaelRushingBlow's, whose windup is
        /// the charge - resolved now, with no second windup sent: a melee hit on aimedAt if it is
        /// given, and on every player within area of centre. It resolves as any missile does,
        /// on the next tick, and its recovery goes out in the shape the action's class reads.
        /// </summary>
        public void CreatureStrike(MapChannel mapChannel, Creature attacker, CreatureAction action, Actor aimedAt, int damage, CreatureArea area, Vector3 centre)
        {
            CreatureStrike(mapChannel, attacker, action, aimedAt, damage, area, centre,
                attacker?.ScriptedCombatAuthorization);
        }

        internal void CreatureStrike(MapChannel mapChannel, Creature attacker, CreatureAction action,
            Actor aimedAt, int damage, CreatureArea area, Vector3 centre,
            Game.Missions.World.ScriptedCombatAuthorization sourceCombatAuthorization)
        {
            if (mapChannel == null || attacker == null)
                return;

            if (!CreatureWindups.HasCombatAuthorization(mapChannel, attacker, sourceCombatAuthorization))
                return;

            var missile = new Missile
            {
                DamageA = damage,
                DamageType = CreatureAttacks.DamageTypeOf(action),
                Source = attacker,
                SourceCombatAuthorization = sourceCombatAuthorization,
                IsMelee = true,
                CritChance = CriticalHits.AttackerChance(attacker, true, 0),
                ActionId = action.ActionId,
                ActionArgId = action.ActionArgId,
                AreaDamage = damage,
                CreatureAction = action,
                AreaOverride = area,
                AreaCentre = centre,
                TriggerTime = 0
            };

            if (aimedAt != null && aimedAt.State != CharacterState.Dead && aimedAt.State != CharacterState.Dying &&
                aimedAt.Attributes.TryGetValue(Attributes.Health, out var targetHealth) && targetHealth.Current > 0 &&
                IsOnMap(mapChannel, aimedAt))
            {
                missile.TargetActor = aimedAt;
                missile.TargetEntityId = aimedAt.EntityId;
            }

            mapChannel.QueuedMissiles.Add(missile);
        }

        /// <summary>
        /// A launcher's splash (Splash): every other hostile creature within the missile's
        /// SplashRadius of where it landed takes SplashDamage as a hit of its own, and is added
        /// to the missile's hits so the one recovery shows them all. A creature's splashes
        /// whatever it may fight there, players and creatures (CreatureAreaAttacks.FoesAround).
        /// </summary>
        private void SplashAround(MapChannel mapChannel, Missile missile)
        {
            if (missile.SplashRadius <= 0 || missile.SplashDamage <= 0 || !(missile.Source is Actor shooter)
                || missile.TargetActor == null || !IsOnMap(mapChannel, missile.TargetActor))
                return;

            var centre = missile.TargetActor.Position;
            var victims = shooter switch
            {
                Manifestation player => AbilityManager.VictimsWithin(mapChannel, player, centre, missile.SplashRadius),
                Creature creature when creature.State != CharacterState.Dead && IsOnMap(mapChannel, creature)
                    => CreatureAreaAttacks.FoesAround(mapChannel, creature, centre, missile.SplashRadius, missile.TargetActor),
                _ => new List<Actor>()
            };

            // The round flew as far as its target, and the splash is as weak as the round (RangeFalloff).
            var damage = RangeFalloff.Scale(missile.SplashDamage, missile.OptimalRange, shooter.Position, centre);

            foreach (var victim in victims)
                if (victim != missile.TargetActor)
                    ExtraHit(mapChannel, missile, shooter, victim, damage, false);
        }

        /// <summary>
        /// A cone weapon's other victims (ConeWeapons): each creature that was in the cone when the
        /// shot was fired and is still there to be hit takes the shot's damage with its own crit
        /// roll, and the hit carries what the shot carries - a shotgun's knockback chance.
        /// </summary>
        private void ConeHits(MapChannel mapChannel, Missile missile)
        {
            if (missile.ConeTargets == null || missile.ConeDamage <= 0 || !(missile.Source is Manifestation shooter))
                return;

            // Each at its own distance (RangeFalloff).
            foreach (var victim in missile.ConeTargets)
                if (victim != missile.TargetActor && IsOnMap(mapChannel, victim))
                    ExtraHit(mapChannel, missile, shooter, victim,
                        RangeFalloff.Scale(missile.ConeDamage, missile.OptimalRange, shooter.Position, victim.Position), true);
        }

        /// <summary>
        /// One more creature - or enemy player (Pvp) - hit by a missile, a splash or a cone,
        /// resolved as a missile of its own (resistance, armour, threat, what the hit carries) and
        /// added to the missile's hits.
        /// </summary>
        private void ExtraHit(MapChannel mapChannel, Missile missile, Actor shooter, Actor creature, int damage, bool canCrit)
        {
            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying || damage <= 0 ||
                !creature.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return;

            var extra = new Missile
            {
                DamageA = damage,
                DamageType = missile.DamageType,
                WeaponProcs = missile.WeaponProcs,
                ArmorBypassPercent = missile.ArmorBypassPercent,
                Source = shooter,
                TargetActor = creature,
                TargetEntityId = creature.EntityId,
                ActionId = missile.ActionId,
                ActionArgId = missile.ActionArgId,
                StunChance = missile.StunChance,
                StunMs = missile.StunMs,
                RootMs = canCrit ? missile.RootMs : 0,
                KnockbackChance = canCrit ? missile.KnockbackChance : 0,
                KnockbackStunMs = missile.KnockbackStunMs,
                CreatureAction = missile.CreatureAction
            };

            if (canCrit)
            {
                var amount = extra.DamageA;
                extra.IsCritical = CriticalHits.Resolve(shooter, creature, missile.IsMelee, missile.CritChance, ref amount);
                extra.DamageA = amount;
            }

            var hit = new HitData { EntityId = creature.EntityId, FinalAmt = extra.DamageA, IsCritical = extra.IsCritical ? 1 : 0 };

            extra.Args.HitEntities.Add(creature.EntityId);
            extra.Args.HitData.Add(hit);

            if (creature is Manifestation)
                DoDamageToPlayer(mapChannel, extra);
            else
                DoDamageToCreature(mapChannel, extra);

            hit.FinalAmt = extra.DamageA;
            hit.DeathBlow = creature.Attributes[Attributes.Health].Current <= 0 ? 1 : 0;

            missile.Args.HitEntities.Add(creature.EntityId);
            missile.Args.HitData.Add(hit);
        }

        /// <summary>
        /// A creature attack with an area (CreatureAreaAttacks): every other player it covers
        /// takes the attack's damage as a hit of its own - own crit roll, then everything a
        /// player's hit goes through in DoDamageToPlayer, knockback and stun included - and is
        /// added to the missile's hits so the one recovery shows them all.
        /// </summary>
        private void CreatureAreaHits(MapChannel mapChannel, Missile missile)
        {
            if (!(missile.Source is Creature attacker) || attacker.State == CharacterState.Dead || !IsOnMap(mapChannel, attacker))
                return;

            // An attack that is its effect alone (the Atta pheromone, the Miasma's gas cloud) does
            // no damage and still reaches everyone in its area.
            if (missile.AreaDamage <= 0 && CreatureEffectAttacks.KindOf(missile.ActionId, missile.ActionArgId) == CreatureEffectAttacks.Kind.None)
                return;

            // A missile that lands as a bomb (a Predator's) hits the one it was fired at: its
            // blast is what reaches the rest (CreatureBombs.GroundBlast), whatever cone it aims in.
            if (CreatureEffectAttacks.KindOf(missile.ActionId, missile.ActionArgId) == CreatureEffectAttacks.Kind.GroundBlast)
                return;

            var area = missile.AreaOverride ?? CreatureAreaAttacks.AreaOf(missile.ActionId, missile.ActionArgId);

            if (!area.IsArea)
                return;

            foreach (var player in CreatureAreaAttacks.PlayersCaught(mapChannel, attacker, area, missile.TargetActor, missile.AreaCentre))
                ExtraPlayerHit(mapChannel, missile, attacker, player, missile.AreaDamage);
        }

        /// <summary>One more player hit by a creature's attack, resolved as a missile of its own and added to the missile's hits.</summary>
        private void ExtraPlayerHit(MapChannel mapChannel, Missile missile, Creature attacker, Manifestation player, int damage)
        {
            var extra = new Missile
            {
                DamageA = damage,
                DamageType = missile.DamageType,
                ArmorBypassPercent = missile.ArmorBypassPercent,
                Source = attacker,
                TargetActor = player,
                TargetEntityId = player.EntityId,
                ActionId = missile.ActionId,
                ActionArgId = missile.ActionArgId,
                IsMelee = missile.IsMelee,
                CreatureAction = missile.CreatureAction
            };

            var amount = extra.DamageA;
            extra.IsCritical = CriticalHits.Resolve(attacker, player, missile.IsMelee, missile.CritChance, ref amount);
            extra.DamageA = amount;

            var hit = new HitData { EntityId = player.EntityId, FinalAmt = extra.DamageA, IsCritical = extra.IsCritical ? 1 : 0 };

            extra.Args.HitEntities.Add(player.EntityId);
            extra.Args.HitData.Add(hit);

            DoDamageToPlayer(mapChannel, extra);

            hit.FinalAmt = extra.DamageA;

            missile.Args.HitEntities.Add(player.EntityId);
            missile.Args.HitData.Add(hit);
        }

        /// <summary>
        /// Aims a cone weapon (ConeWeapons) as it is fired: every hostile creature within its reach
        /// and half-angle of the way the shooter faces. A cone weapon cannot lock a target ("you
        /// will not be able to lock onto a target while this type of weapon is equipped"), so
        /// whatever the shooter had selected plays no part: the missile flies at the nearest
        /// creature in the cone, the rest are its other victims, and an empty cone is a shot at
        /// nothing.
        /// </summary>
        private static void AimCone(MapChannel mapChannel, Manifestation shooter, ActionData action, Missile missile, float halfAngle, int damage)
        {
            var range = ConeWeapons.RangeOf(action.ActionId, action.ActionArgId) + ConeWeapons.RangeSlack;
            var inCone = AbilityManager.VictimsInCone(mapChannel, shooter, AbilityManager.FacingOf(shooter), range, halfAngle)
                .Where(c => c.State != CharacterState.Dead && c.State != CharacterState.Dying)
                .OrderBy(c => Vector3.DistanceSquared(c.Position, shooter.Position))
                .ToList();

            var primary = inCone.FirstOrDefault();

            action.TargetId = primary?.EntityId ?? 0;
            missile.ConeTargets = inCone.Skip(1).ToList();
            missile.ConeDamage = damage;
        }

        public void MissileTrigger(MapChannel mapChannel, Missile missile)
        {
            var targetType = EntityManager.Instance.GetEntityType(missile.TargetEntityId);

            // A destructible prop of the .map (WorldDestructibles) is an object, as it was at the launch.
            if (targetType == 0 && WorldDestructibles.IsOne(missile.TargetObject))
                targetType = EntityType.Object;

            // Checked again here: the missile was queued a tick ago, and the target can have
            // left the map (or the world) since.
            if (missile.TargetObject != null)
            {
                if (!PracticeTargetManager.CanHit(mapChannel, missile.Source, missile.TargetObject))
                    targetType = 0;
            }
            else if (missile.TargetEntityId != 0 && targetType != EntityType.Object &&
                (!IsOnMap(mapChannel, missile.TargetActor) ||
                 missile.TargetActor.State is CharacterState.Dead or CharacterState.Dying ||
                 !missile.TargetActor.Attributes.TryGetValue(Attributes.Health, out var targetHealth) || targetHealth.Current <= 0))
                targetType = 0;

            // A follower or a defender of a mission scene does not turn on a creature on its side.
            if (missile.Source is Creature companion &&
                (companion.SpawnPool?.FollowOwnerCharacterId > 0 || Game.Missions.World.CreatureGameplayRules.IsDefender(companion)) &&
                missile.TargetActor is Creature enemy &&
                !CreatureManager.IsHostileTarget(mapChannel, companion, enemy))
                targetType = 0;
            if (missile.Source is Creature manual &&
                !CreatureWindups.HasCombatAuthorization(mapChannel, manual, missile.SourceCombatAuthorization))
            {
                targetType = 0;
                missile.Args.HitEntities.Clear();
                missile.Args.HitData.Clear();
                if (missile.TargetEntityId != 0)
                {
                    missile.Args.MisstEntities.Add(missile.TargetEntityId);
                    missile.Args.Missdata.Add(MissTypeMiss);
                }
                Logger.WriteLog(LogType.Debug, $"Retired manual-combat missile from {manual.EntityId} was rejected.");
                CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                return;
            }

            // A staff drawn may deflect it (Staff, from pump 3): no damage at all, and the clients
            // are told it as a miss of misstype 4 - the staff parry animation and "Deflect". A
            // launcher's round still goes off where it was turned aside.
            if (targetType == EntityType.Character && missile.TargetActor is Manifestation defender
                && ManifestationManager.DeflectsWithStaff(defender))
            {
                missile.Args.MisstEntities.Add(missile.TargetEntityId);
                missile.Args.Missdata.Add(MissTypeDeflect);

                SplashAround(mapChannel, missile);
                ConeHits(mapChannel, missile);
                CreatureAreaHits(mapChannel, missile);

                CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                return;
            }

            // The to-hit roll (ToHit): a weapon's was made as it was fired, on how its target was
            // moving and the Chaff around it - "interferes with enemy targeting, making you and
            // nearby allies harder to hit" - and a creature ability's is made now, for the Chaff alone.
            // A shot that goes wide is a plain miss (misstype 1); what it would have done around
            // its target still happens, as a deflected one's does.
            if ((targetType == EntityType.Character || targetType == EntityType.Creature) && (missile.Missed ?? ToHit.Misses(missile)))
            {
                missile.Args.MisstEntities.Add(missile.TargetEntityId);
                missile.Args.Missdata.Add(MissTypeMiss);

                SplashAround(mapChannel, missile);
                ConeHits(mapChannel, missile);
                CreatureAreaHits(mapChannel, missile);

                CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                return;
            }

            // A creature's cone or ring, wound up where its target stood, lands on what is in it
            // when the windup is done: a target that walked out of it dodged (CreatureWindups).
            if (targetType == EntityType.Character && missile.AfterWindup && missile.Source is Creature winder
                && !CreatureWindups.StillCaught(winder, missile))
            {
                missile.Args.MisstEntities.Add(missile.TargetEntityId);
                missile.Args.Missdata.Add(MissTypeDodge);

                CreatureAreaHits(mapChannel, missile);

                CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                return;
            }

            // The crit comes first, before resistance, shields or armour take their share of it.
            var coverModifier = 1.0;

            if (targetType == EntityType.Creature || targetType == EntityType.Character)
            {
                var amount = missile.DamageA;

                // A weapon shot past its optimal range does less, before the crit multiplies what is left (RangeFalloff).
                if (missile.OptimalRange > 0 && missile.Source != null)
                    amount = RangeFalloff.Scale(amount, missile.OptimalRange, missile.Source.Position, missile.TargetActor.Position);

                missile.IsCritical = CriticalHits.Resolve(missile.Source, missile.TargetActor, missile.IsMelee, missile.CritChance, ref amount);

                // "Enemies deliver bonus damage to crouched targets": a melee hit on someone
                // crouched does CROUCHED_MELEE_DAMAGE_TAKEN of itself.
                if (missile.IsMelee && Cover.IsCrouching(missile.TargetActor))
                    amount = (int)Math.Round(amount * Cover.CrouchedMeleeDamageTaken);

                // A ranged hit does the share of itself that gets past the cover around the target.
                if (!missile.IsMelee && missile.Source != null)
                {
                    coverModifier = Cover.Modifier(mapChannel, missile.Source, missile.TargetActor);

                    if (coverModifier < 1.0)
                        amount = (int)Math.Round(amount * coverModifier);
                }

                missile.DamageA = amount;
            }

            var hitData = new HitData
            {
                FinalAmt = missile.DamageA,
                EntityId = missile.TargetEntityId,
                IsCritical = missile.IsCritical ? 1 : 0,
                CoverModifier = coverModifier
            };

            // A shot at nothing lists no hit: it used to list entity 0.
            if (missile.TargetEntityId != 0 && targetType != 0)
            {
                missile.Args.HitEntities.Add(missile.TargetEntityId);
                missile.Args.HitData.Add(hitData);
            }
            else if (missile.TargetEntityId != 0)
            {
                // A plain miss. It went out as misstype 0, which is no row of the client's
                // table: a KeyError there, and no "Miss!" (DamageInfoWriter.WriteMissTypes).
                missile.Args.MisstEntities.Add(missile.TargetEntityId);
                missile.Args.Missdata.Add(MissTypeMiss);
            }

            switch (targetType)
            {
                case 0:
                    // no target => ToDo
                    break;
                case EntityType.Creature:
                    DoDamageToCreature(mapChannel, missile);
                    break;
                case EntityType.Character:
                    DoDamageToPlayer(mapChannel, missile);
                    break;
                case EntityType.Object when missile.TargetObject != null:
                    EnterCombat(missile.Source);
                    break;
                case EntityType.Object:
                    // A Personal Waypoint, a Hortimonculus, or else a force field.
                    if (!PersonalWaypoints.TakeHit(missile) && !AbilityManager.PlantTakeHit(missile))
                        ForceFields.TakeHit(missile);

                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"WeaponAttackRecovery: Unsuported targetType {targetType}.");
                    break;
            }

            // What landed, after a smoke screen, resistance and a shield had their share: each
            // hit in the recovery carries its own amount now that a launcher lists several.
            hitData.FinalAmt = missile.DamageA;

            // A creature killed, or a player (PlayerDeath): the client plays the death from it.
            if ((targetType == EntityType.Creature || targetType == EntityType.Character)
                && missile.TargetActor?.Attributes.TryGetValue(Attributes.Health, out var struckHealth) == true && struckHealth.Current <= 0)
                hitData.DeathBlow = 1;

            // A creature's lightning: its extra damage and its arc, carried in this hit.
            if (targetType == EntityType.Creature || targetType == EntityType.Character)
                CreatureLightning.Extras(mapChannel, missile, hitData);

            SplashAround(mapChannel, missile);
            ConeHits(mapChannel, missile);
            CreatureAreaHits(mapChannel, missile);

            switch (missile.ActionId)
            {
                case ActionId.WeaponAttack:
                // Melee (174) is resolved exactly like a ranged attack: the original server's
                // missile_ActionRecoveryHandler_WeaponMelee forwarded to the WeaponAttack
                // handler "until there is better handling for melee weapons", and the recovery
                // packet is the same shape. It fell through to the default here, which did the
                // right thing but logged every swing as an unsupported action.
                case ActionId.WeaponMelee:
                // A Bane Mortar's shell (411): weapons.groundtarget is a RocketLauncherAttack, a
                // BaseWeaponAttack with the same recovery.
                case ActionId.WeaponGroundtarget:
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                    break;

                // A creature's own ability - the Forean's lightning, the Boargar's stun and
                // charge, the Amoeboid's slime, the Mox's energy attack, the Shield Drone's
                // strike. BaseActorAbility and BaseWeaponAttack both extend TargetedAction, whose
                // DoAction(actor, hits, misses, missdata, hitdata) this packet feeds, but what each
                // hitdata entry holds is the class's own business: CreatureAttacks.RecoveryFor
                // writes it in the shape the action's class unpacks. They came through the default
                // arm below while logging every swing as unsupported - which is how an attack that
                // was drawing nothing at all looked exactly like one that was fine.
                case var _ when AbilityManager.Instance.TryGetLevel(missile.ActionId, missile.ActionArgId, out _):
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                    break;
                //else if (missile->actionId == 203)
                //    missile_ActionHandler_CR_FOREAN_LIGHTNING(mapChannel, missile);
                //else if (missile->actionId == 211)
                //    missile_ActionHandler_CR_AMOEBOID_SLIME(mapChannel, missile);
                //else if (missile->actionId == 397)
                //    missile_ActionRecoveryHandler_ThraxKick(mapChannel, missile);
                default:
                    Logger.WriteLog(LogType.Debug, $"MissileLaunch: unsupported missile actionId {missile.ActionId} - using default: WeaponAttackRecovery");
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, CreatureAttacks.RecoveryFor(missile));
                    break;
            }

            if (targetType == EntityType.Object && missile.TargetObject != null && missile.DamageA > 0)
                PracticeTargetManager.RecordHit(mapChannel, missile.Source, missile.TargetObject, missile.ActionId,
                    damage: missile.DamageA);
        }
    }
}

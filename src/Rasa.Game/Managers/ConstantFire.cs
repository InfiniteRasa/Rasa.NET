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
    /// Constant-fire weapons, as the client plays them (client/actions/weapons/constantfire.py):
    /// holding the trigger puts a ConstantFireEffect on the shooter, attached with (weaponId,
    /// actionId, actionArgId, numShots, interval, burstRecoil), and every interval the server
    /// ticks it with that interval's pulses of hits; letting go takes it off, which is when the
    /// client stops charging the weapon. Nothing is fired as a missile, and there is no windup
    /// and recovery per shot - the effect's ticks are the shots.
    ///
    /// The leech gun (WEAPON_DENSITYGUN, CF_DENSITY_GUN_EFFECT 94) and the polarity gun
    /// (WEAPON_POLARITYGUN, CF_POLARITYGUN_EFFECT 239) are fired this way; both are charged
    /// actions in the client's actionModules. The effect's level is the weapon's damage type: the
    /// client picks the beam's FX by (typeId, level), and the data has FX for the leech gun at
    /// levels 1-4 (physical, fire, ice, virulent) and the polarity gun at 2, 3 and 13 (fire, ice,
    /// electrical - the three kinds of polarity gun in the item names).
    ///
    /// The leech gun's ticks also carry the leech: "Damage Conversion: 25% of damage done / Conversion Radius:
    /// 10m" at every pump of Leech Guns, which the skill describes as converting "a percentage of
    /// the damage done to a target into health or armor that is applied to the user and nearby
    /// squad members". The client's DensityGunEffect announces each as (entityId, healAmount,
    /// repairAmount); health is healed first and what a full health bar cannot take repairs
    /// armour.
    ///
    /// The polarity gun is "a sustained beam and a large release of damage when the weapon stops
    /// firing" (Polarity Guns, uielement 1232). The beam is the ticks. Each beam hit charges the
    /// target it lands on, and letting go discharges it: the recovery that ends the fire carries
    /// one hit on that target for ReleasePercent of the beam damage built up, which the client's
    /// BaseWeaponAttack.DoHits floats when the server resolves the charged action. How much the
    /// release is and how long a charge can build are not in anything we have, so they are a
    /// choice: half of what the beam did before resistance, over at most MaxChargePulses pulses.
    /// Moving the beam to another target starts the charge again, and a target that died or left
    /// takes the charge with it.
    ///
    /// The propellant gun (WEAPON_FLAMETHROWER 140, CF_PROPELLANT_EFFECT 109, FX at levels 2, 3,
    /// 4, 5 and 7) does "damage to targets in a cone in front of the user" (Propellant Guns,
    /// uielement 1214), and a cone weapon cannot lock onto a target (uielement 5374): its
    /// FlamethrowerAttack has no splat, and the client aims it with the shooter's yaw. So a
    /// pulse hits every hostile creature within PropellantRange of the shooter and ConeHalfAngleOf
    /// degrees either side of the way they face, each with its own crit roll and resistance, all
    /// listed as shots of the one pulse. The reach and the angle are ConeWeapons': the action's
    /// own range (maxRange 10 on every (140, arg) row) and 45 degrees either side. A
    /// pulse may leave a pool on the ground (PROPELLANT_POOL_EFFECT 10000047), which is
    /// <see cref="PropellantPools"/>'; PROPELLANT_PUMP_EFFECT 10000046 has no numbers or
    /// behaviour in the client and is not done.
    ///
    /// Machine guns (WEAPON_MACHINEGUN 149, a charged action in actionModules): the client's
    /// MachinegunAttack is a ConstantFireAttack like the rest - a looping windup while the
    /// trigger is held, the shots its effect's ticks - and each kind has its effect: the
    /// Chaingun, Pulse, Laser and Electric Chainguns CF_MACHINEGUN_EFFECT (107, FX at levels 1,
    /// 5, 6 and 13), the Series 2 and 3 Chainguns CF_MACHINEGUN_S2_EFFECT (443) and _S3_ (445),
    /// the Series 2 Laser Pistol CF_PISTOL_S2_EFFECT (455), the Series 3 Laser Pistols
    /// CF_PISTOL_S3_EFFECT (456) and the Auto Cannon CF_HUMAN_MECH_PAU (462), picked by the
    /// action argument each weapon class fires with (<see cref="EffectTypeOf"/>); the level is
    /// the damage type as for the others, and every kind has its FX at its own weapons' type.
    /// They were fired as single shots, which the client took as the charged attack's one
    /// shot: the windup ran, the recovery played a rifle's shot and nothing looped, and no
    /// effect ever came off to stop the charge (ConstantFireEffect.OnDetach calls
    /// StopChargingWeapon), so the weapon could not be put away until the next login.
    ///
    /// The server's auto-fire timer is what drives it: each refire the timer's shot comes here
    /// instead of MissileManager, the first one attaching the effect; StopAutoFire, a shot that
    /// could not be fired (out of ammo, jammed, weapon gone) or a client that stopped keeping the
    /// fire alive takes it off. One pulse of one shot a refire (800 ms for the leech guns in the
    /// data): SHOTS_PER_INTERVAL and DAMAGE_INTERVAL are weapon properties that survive in
    /// nothing we have.
    /// </summary>
    public static class ConstantFire
    {
        public const int DensityGunTypeId = 94;             // CF_DENSITY_GUN_EFFECT
        public const int PolarityGunTypeId = 239;           // CF_POLARITYGUN_EFFECT
        public const int PropellantTypeId = 109;            // CF_PROPELLANT_EFFECT

        /// <summary>The polarity gun's release: this percent of the beam damage the target was charged with.</summary>
        public const int ReleasePercent = 50;

        /// <summary>The most beam pulses a polarity charge builds over; the ones after it keep the beam going but add nothing.</summary>
        public const int MaxChargePulses = 10;

        /// <summary>"Damage Conversion: 25% of damage done".</summary>
        public const int LeechPercent = 25;

        /// <summary>"Conversion Radius: 10m".</summary>
        public const float LeechRadius = 10f;

        private sealed class Session
        {
            public Client Client;
            public GameEffect Effect;
            public ActionId ActionId;
            public uint ActionArgId;
            public DamageType DamageType;
            public double CritBonus;
            /// <summary>Polarity gun: who the beam is charging, with how much, over how many pulses.</summary>
            public ulong ChargeTargetId;
            public int Charge;
            public int ChargePulses;
        }

        private static readonly Dictionary<Client, Session> Sessions = new Dictionary<Client, Session>();
        private static readonly object SessionsLock = new object();

        /// <summary>Whether this weapon fires constantly rather than shot by shot.</summary>
        public static bool Handles(WeaponClassInfo weapon) => weapon != null && IsConstantFire(weapon.WeaponAttackActionId);

        public static bool IsConstantFire(ActionId actionId) =>
            actionId == ActionId.WeaponDensitygun || actionId == ActionId.WeaponPolaritygun || actionId == ActionId.WeaponFlamethrower
            || actionId == ActionId.WeaponMachinegun;

        public const int MachinegunTypeId = 107;            // CF_MACHINEGUN_EFFECT
        public const int MachinegunS2TypeId = 443;          // CF_MACHINEGUN_S2_EFFECT
        public const int MachinegunS3TypeId = 445;          // CF_MACHINEGUN_S3_EFFECT
        public const int PistolS2TypeId = 455;              // CF_PISTOL_S2_EFFECT
        public const int PistolS3TypeId = 456;              // CF_PISTOL_S3_EFFECT
        public const int MechPauTypeId = 462;               // CF_HUMAN_MECH_PAU

        /// <summary>
        /// The constant-fire effect an attack plays. A machine gun's is by the argument its weapon
        /// class fires with: 7 the Series 3 Chaingun, 8 the Series 2, 9 the Series 2 Laser Pistol,
        /// 10 the Series 3 Laser Pistols, 11, 12 and 16 the Auto Cannon; 1, 3, 4 and 5 (Chaingun,
        /// Pulse, Laser, Electric) and anything else the plain machine gun's.
        /// </summary>
        public static int EffectTypeOf(ActionId actionId, uint actionArgId = 0)
        {
            switch (actionId)
            {
                case ActionId.WeaponPolaritygun:
                    return PolarityGunTypeId;
                case ActionId.WeaponFlamethrower:
                    return PropellantTypeId;
                case ActionId.WeaponMachinegun:
                    switch (actionArgId)
                    {
                        case 7: return MachinegunS3TypeId;
                        case 8: return MachinegunS2TypeId;
                        case 9: return PistolS2TypeId;
                        case 10: return PistolS3TypeId;
                        case 11:
                        case 12:
                        case 16: return MechPauTypeId;
                        default: return MachinegunTypeId;
                    }
                default:
                    return DensityGunTypeId;
            }
        }


        /// <summary>
        /// A polarity charge after one more beam hit of amount on targetId: the same target adds
        /// to it up to MaxChargePulses pulses, another target starts it again.
        /// </summary>
        public static (ulong TargetId, int Charge, int Pulses) AddCharge(ulong chargeTargetId, int charge, int pulses, ulong targetId, int amount)
        {
            if (targetId != chargeTargetId)
                return (targetId, Math.Max(0, amount), 1);

            if (pulses >= MaxChargePulses)
                return (chargeTargetId, charge, pulses);

            return (chargeTargetId, charge + Math.Max(0, amount), pulses + 1);
        }

        /// <summary>What a polarity charge discharges for.</summary>
        public static int ReleaseOf(int charge) => charge <= 0 ? 0 : charge * ReleasePercent / 100;

        public static bool IsFiring(Client client)
        {
            lock (SessionsLock)
                return Sessions.ContainsKey(client);
        }

        /// <summary>How a leech's healing splits for one person: health up to what they are missing, the rest to armour.</summary>
        public static (int Heal, int Repair) Split(int amount, int healthMissing, int armorMissing)
        {
            if (amount <= 0)
                return (0, 0);

            var heal = Math.Min(amount, Math.Max(0, healthMissing));
            var repair = Math.Min(amount - heal, Math.Max(0, armorMissing));

            return (heal, repair);
        }

        /// <summary>
        /// One interval of fire at the shooter's target: the effect attached if it was not, the
        /// shot resolved, the leech shared out, and the tick sent.
        /// </summary>
        public static void Pulse(MapChannel mapChannel, Client client, Item weapon, ActionData action, int damage, DamageType damageType, double critBonus)
        {
            var player = client.Player;
            var session = Begin(mapChannel, client, weapon, action, damageType);

            // Weakened as any of the player's ranged attacks are (a Laser crit, Called Shot: Eye).
            damage = GameEffectManager.ApplyRangedDamage(player, damage);
            var leech = session.ActionId == ActionId.WeaponDensitygun;

            session.CritBonus = critBonus;

            // Firing is firing: it is a fight, and it gives a cloaked shooter away.
            ManifestationManager.Instance.EnterCombat(client);
            Stealth.Break(mapChannel, player);
            AbilityManager.OnPlayerActed(mapChannel, player, true);

            // The leech gun's tick is (healData, damageData); every other constant fire's is the pulses alone.
            var tick = new ConstantFireTickPacket(session.Effect.EffectId, leech);
            var pulse = new List<TickEntry>();

            tick.Pulses.Add(pulse);

            // A propellant gun sprays the cone in front of the shooter; the others hit what they aim
            // at. An enemy player across a wargame is a target like a creature (Pvp).
            var targets = new List<Actor>();
            List<Actor> pool = null;
            var poolReach = 0f;

            if (session.ActionId == ActionId.WeaponFlamethrower)
            {
                var range = ConeWeapons.RangeOf(session.ActionId, session.ActionArgId) + ConeWeapons.RangeSlack;
                var halfAngle = ConeWeapons.HalfAngleOf(weapon.ItemTemplate.WeaponInfo);
                var facing = AbilityManager.FacingOf(player);

                targets.AddRange(AbilityManager.HostilesInCone(mapChannel, player, facing, range, halfAngle));
                targets.AddRange(Pvp.EnemiesInCone(mapChannel, player, facing, range, halfAngle));

                // The pool it may leave is of the pulse as fired, and lands under those it reached
                // while they still stand.
                pool = targets.Where(target => target.State != CharacterState.Dead && target.State != CharacterState.Dying).ToList();
                poolReach = range - ConeWeapons.RangeSlack;
            }
            else if (ResolveTarget(mapChannel, player) is Actor aimed && (aimed is Creature || Pvp.IsEnemyTarget(player, aimed)))
                targets.Add(aimed);
            else if (PersonalWaypoints.TakeDamage(player, player.Target, damage) is int taken)
            {
                // An enemy's Personal Waypoint (PersonalWaypoints): the pulse lands on its own hit
                // points as it is - no crit, falloff or resistance, as a missile at one.
                pulse.Add(new TickEntry { EntityId = player.Target, Amount = taken, DamageType = damageType });
            }

            foreach (var target in targets)
            {
                if (target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                    continue;

                // Each target at its own distance: past the weapon's optimal range the pulse does less (RangeFalloff).
                var rolled = RangeFalloff.Scale(damage, weapon.ItemTemplate.WeaponInfo.Range, player.Position, target.Position);
                var crit = CriticalHits.Resolve(player, target, false, CriticalHits.AttackerChance(player, false, critBonus), ref rolled);
                var amount = GameEffectManager.ApplyResist(target, rolled, out var resisted, damageType);
                var landed = ActorManager.Instance.Damage(mapChannel, target, amount, player, out var outcome, damageType);

                pulse.Add(new TickEntry
                {
                    EntityId = target.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = damageType,
                    IsCritical = crit,
                    DeathBlow = landed > 0 && target.Attributes[Attributes.Health].Current <= 0
                });

                if (crit && !outcome.Immune && target.State != CharacterState.Dead && target.State != CharacterState.Dying && target.Attributes[Attributes.Health].Current > 0)
                    CritEffects.OnCritical(mapChannel, target, player, damageType, amount);

                if (leech)
                    Leech(mapChannel, player, landed, tick);
                else if (session.ActionId == ActionId.WeaponPolaritygun)
                    (session.ChargeTargetId, session.Charge, session.ChargePulses) =
                        AddCharge(session.ChargeTargetId, session.Charge, session.ChargePulses, target.EntityId, rolled);
            }

            PulseAtObject(mapChannel, player, session, damage, damageType, pulse);

            if (pool != null)
                PropellantPools.OnPulse(mapChannel, player, damage, damageType, poolReach, pool);

            CellManager.Instance.CellCallMethod(mapChannel, player, tick);
        }

        /// <summary>
        /// The pulse on an object the shooter is aiming at: a practice target, or one a mission
        /// has the player destroy (PracticeTargetManager). It is no actor, so ResolveTarget and
        /// the cone have nothing for it, and a chaingun, a laser chaingun or any other
        /// constant-fire weapon did nothing to an object a pistol's missile could destroy. The
        /// pulse lands as the missile does (MissileManager.MissileTrigger): the object is listed
        /// in the tick and the damage goes to RecordHit as it is. No critical hit, range falloff,
        /// resistance, leech or polarity charge: those are an actor's, there as here.
        /// </summary>
        private static void PulseAtObject(MapChannel mapChannel, Manifestation player, Session session,
            int damage, DamageType damageType, List<TickEntry> pulse)
        {
            if (damage <= 0 || player.Target == 0 ||
                !PracticeTargetManager.TryGetTarget(mapChannel, player.Target, out var target) ||
                !PracticeTargetManager.CanHit(mapChannel, player, target))
                return;

            var distance = System.Numerics.Vector3.Distance(target.Position, player.Position);
            var reach = session.ActionId == ActionId.WeaponFlamethrower
                ? ConeWeapons.RangeOf(session.ActionId, session.ActionArgId) + ConeWeapons.RangeSlack
                : MissileManager.MaxTargetDistance;

            if (!float.IsFinite(distance) || distance > reach)
                return;

            pulse.Add(new TickEntry { EntityId = target.EntityId, Amount = damage, DamageType = damageType });
            PracticeTargetManager.RecordHit(mapChannel, player, target, session.ActionId, damage: damage);
        }

        /// <summary>
        /// The trigger is let go, the weapon cannot fire, or the shooter is gone: the effect comes
        /// off, and the action it was part of is ended on the clients - with a polarity gun's
        /// release in it, unless the shooter is leaving the map (release false).
        /// </summary>
        public static void Stop(Client client, bool release = true)
        {
            Session session;

            lock (SessionsLock)
            {
                if (!Sessions.TryGetValue(client, out session))
                    return;

                Sessions.Remove(client);
            }

            PropellantPools.Stopped(client.Player);

            var mapChannel = client.Player?.MapChannel;

            if (mapChannel == null || client.Player == null)
                return;

            GameEffectManager.Instance.DettachEffect(mapChannel, client.Player, session.Effect);

            var args = new MissileArgs();

            if (release && session.ActionId == ActionId.WeaponPolaritygun)
                Release(mapChannel, client.Player, session, args);

            CellManager.Instance.CellCallMethod(mapChannel, client.Player,
                new PerformRecoveryPacket(PerformType.ListOfArgs, session.ActionId, session.ActionArgId, args));
        }

        /// <summary>
        /// The polarity gun let go: the charge the beam built on its target discharges into it
        /// as one hit - a weapon hit, so it can crit - listed in args for the recovery.
        /// </summary>
        private static void Release(MapChannel mapChannel, Manifestation player, Session session, MissileArgs args)
        {
            var amount = ReleaseOf(session.Charge);

            if (amount <= 0 || session.ChargeTargetId == 0)
                return;

            // A creature, or an enemy player who is still one (Pvp).
            if (!(EntityManager.Instance.GetActor(session.ChargeTargetId) is Actor target) || target.MapContextId != mapChannel.MapInfo.MapContextId
                || target.State == CharacterState.Dead || target.State == CharacterState.Dying
                || !(target is Creature || Pvp.IsEnemyTarget(player, target)))
                return;

            var crit = CriticalHits.Resolve(player, target, false, CriticalHits.AttackerChance(player, false, session.CritBonus), ref amount);
            var dealt = GameEffectManager.ApplyResist(target, amount, out var resisted, session.DamageType);
            var landed = ActorManager.Instance.Damage(mapChannel, target, dealt, player, out var outcome, session.DamageType);

            args.HitEntities.Add(target.EntityId);
            args.HitData.Add(new HitData
            {
                EntityId = target.EntityId,
                DamageType = session.DamageType,
                Resisted = (uint)resisted,
                FinalAmt = outcome.Delivered,
                Absorbed = (uint)outcome.Absorbed,
                WasImune = outcome.Immune ? 1 : 0,
                IsCritical = crit ? 1 : 0,
                DeathBlow = landed > 0 && target.Attributes[Attributes.Health].Current <= 0 ? 1 : 0
            });

            if (crit && !outcome.Immune && target.State != CharacterState.Dead && target.State != CharacterState.Dying && target.Attributes[Attributes.Health].Current > 0)
                CritEffects.OnCritical(mapChannel, target, player, session.DamageType, dealt);
        }

        private static Session Begin(MapChannel mapChannel, Client client, Item weapon, ActionData action, DamageType damageType)
        {
            lock (SessionsLock)
                if (Sessions.TryGetValue(client, out var running) && client.Player.ActiveEffects.ContainsKey(running.Effect.EffectId))
                    return running;

            var player = client.Player;
            var info = weapon.ItemTemplate.WeaponInfo;
            var interval = (int)Math.Max(100, info.Refire);

            var effect = new GameEffect
            {
                TypeId = EffectTypeOf(action.ActionId, action.ActionArgId),
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                // The beam's FX: specialFX is keyed (typeId, level), the level a damage type.
                EffectLevel = (uint)(damageType == 0 ? DamageType.Physical : damageType),
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = true,
                AnnounceOnAttach = true
            };

            // The windup the other clients see the shooter hold for as long as the fire lasts.
            CellManager.Instance.CellCallMethod(mapChannel, player,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, action.TargetId));

            // ConstantFireEffect.OnAttach(target, weaponId, actionId, actionArgId, numShots, interval, burstRecoil).
            GameEffectManager.Instance.Attach(mapChannel, player, effect,
                weapon.EntityId, (int)action.ActionId, (int)action.ActionArgId, 1, interval, (int)info.RecoilAmount);

            var session = new Session
            {
                Client = client,
                Effect = effect,
                ActionId = action.ActionId,
                ActionArgId = action.ActionArgId,
                DamageType = damageType == 0 ? DamageType.Physical : damageType
            };

            lock (SessionsLock)
                Sessions[client] = session;

            return session;
        }

        /// <summary>What the shooter is aiming at, if it is still there to be hit.</summary>
        private static Actor ResolveTarget(MapChannel mapChannel, Manifestation player)
        {
            if (player.Target == 0)
                return null;

            var target = EntityManager.Instance.GetActor(player.Target);

            if (target == null || target.MapContextId != mapChannel.MapInfo.MapContextId
                || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return null;

            return target;
        }

        /// <summary>
        /// A quarter of what the shot took off the target, healed onto the shooter and the squad
        /// within ten metres - health first, then armour - and listed for the client to announce.
        /// </summary>
        private static void Leech(MapChannel mapChannel, Manifestation player, int landed, ConstantFireTickPacket tick)
        {
            var amount = landed * LeechPercent / 100;

            if (amount <= 0)
                return;

            foreach (var ally in AbilityManager.SquadWithin(mapChannel, player, LeechRadius))
            {
                var health = ally.Attributes[Attributes.Health];
                var armor = ally.Attributes[Attributes.Armor];
                var (heal, repair) = Split(amount, health.CurrentMax - health.Current, armor.CurrentMax - armor.Current);

                if (heal > 0)
                    heal = ActorManager.Instance.Heal(ally, heal, player.EntityId);

                if (repair > 0)
                {
                    armor.Current += repair;
                    CellManager.Instance.CellCallMethod(mapChannel, ally, new UpdateArmorPacket(armor, player.EntityId));
                }

                if (heal > 0 || repair > 0)
                    tick.Heals.Add((ally.EntityId, heal, repair));
            }
        }
    }
}

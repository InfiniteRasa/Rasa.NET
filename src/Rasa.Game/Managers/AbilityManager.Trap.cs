using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Trap (AA_ENGINEER_TRAP 384, abilities.trap): "Creates a special turret in front of the
    /// user for a time. It will fire at enemies and create a high amount of hate, drawing the
    /// enemy to attack it. When it is destroyed, it damages the enemy and then disappears. Only 1
    /// Trap turret can be active at a time. Can be targeted and destroyed." (uielement 2390)
    ///
    /// From the client and its data:
    /// - it "looks and acts like a Turret" (uielement 511): the ability turret's class
    ///   (Ability_Turret_AFS 7280) with the ability turret weapon of the pump's damage type
    ///   (Weapon_Creature_AbilityTurret_Physical / _Sonic / _EMP / _Laser / _Virulent), whose
    ///   attack is WEAPON_ABILITY_TURRET (296) - a constant-fire weapon, CF_ABILITY_TURRET_EFFECT
    ///   (174) ticked with its shots;
    /// - TRAP_DEATH_EFFECT (10000041) sits on it with the pump's FX
    ///   (ABILITY_TRAP_OBJECT_EFFECT / _ATTACHED_*); its OnTick(killerId) announces
    ///   TRAP_EXPLOSION_EFFECT (10000042, a BombEffect) on whoever destroyed it;
    /// - TRAP_SELF_EFFECT (10000040) is a plain effect with nothing on the client side;
    /// - per pump: DURATION 60 s, DAMAGE_MODIFIER_PERCENT -75 (its shots), THREAT_MODIFIER_PERCENT
    ///   2000, HATE_TRANSFER_PERCENT 10, and the retaliation - 300-376 exponentially scaled of the
    ///   pump's DAMAGE_TYPE, EFFECT_RADIUS 10 m.
    ///
    /// The server's part:
    /// - the trap is a FRIENDLY, scripted creature at the spot (or 2 m in front of the player),
    ///   held still, so hostile creatures shoot at it; a new trap takes the old one away; it aims
    ///   at HOSTILE creatures only (AbilityManager.EnemiesWithin), leaving NEUTRAL ones be;
    /// - TRAP_SELF_EFFECT carries THREAT_MODIFIER_PERCENT, so what the trap does is hated twenty
    ///   times over (Threat.ThreatModifierOf);
    /// - every TrapShotMs it fires its turret weapon at the nearest hostile creature within the
    ///   attack's range: the weapon's damage scaled to the owner's level as Polymorph scales a
    ///   creature weapon, less 75%, as CF_ABILITY_TURRET_EFFECT ticks. Its shots are its own, so
    ///   the hate is on it; a kill it makes is its owner's (CreatureManager.HandleCreatureKill);
    /// - HATE_TRANSFER_PERCENT: of the hate the owner earns on a creature within the trap's
    ///   range, 10% goes to the trap instead (AbilityManager.HateSinkFor, Threat);
    /// - destroyed, it ticks TRAP_DEATH_EFFECT with its killer and strikes every hostile creature
    ///   within EFFECT_RADIUS of the killer, credited to the owner, the damage announced through
    ///   the death effect; it is gone a moment later. Run out, it goes quietly.
    ///
    /// Turret (AA_ENGINEER_TURRET 197, abilities.turret) is the same machine without the decoy:
    /// "Creates a turret at a location to assist the user in combat for a set time. Only 1 of each
    /// type of turret can be active at a time. Can be targeted and destroyed." (uielement 1206).
    /// It wears VISUAL_TURRET_EFFECT (439, FX ABILITY_DEPLOYABLE_TURRET_*_EFFECT by damage type)
    /// instead of the Trap's two effects, has no threat modifier, each shot is the ability's
    /// DAMAGE_AMOUNT (22-23, 70 at level 6) scaled to the owner's level by ATTR_SCALE_TYPE, its
    /// HATE_TRANSFER_PERCENT is 30, it lasts DURATION (60 s, 120 at level 6), one of each pump
    /// per player, and destroyed it is simply gone. The client's TURRET_OWNER_EFFECT and
    /// TURRET_OWNEE_EFFECT name classes turret.py does not have, so they are not attached.
    ///
    /// Not in the client, so chosen: TrapShotMs 1000, and the trap's health, TrapBaseHealth at
    /// level 1 scaled like its damage.
    /// </summary>
    public partial class AbilityManager
    {
        public const string TrapModule = "abilities.trap";
        public const int TrapSelfTypeId = 10000040;              // TRAP_SELF_EFFECT
        public const int TrapDeathTypeId = 10000041;             // TRAP_DEATH_EFFECT
        public const int TurretEffectTypeId = 174;               // CF_ABILITY_TURRET_EFFECT
        public const int VisualTurretTypeId = 439;               // VISUAL_TURRET_EFFECT
        public const string TurretModule = "abilities.turret";
        public const EntityClasses TrapClass = (EntityClasses)7280; // Ability_Turret_AFS

        public const int TrapShotMs = 1000;
        public const int TrapBaseHealth = 200;
        private const int TrapLingerMs = 2000;

        /// <summary>The ability turret weapon of each damage type a pump can have.</summary>
        public static uint TrapWeaponClassFor(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Sonic: return 7647;      // Weapon_Creature_AbilityTurret_Sonic
                case DamageType.EMP: return 7646;        // Weapon_Creature_AbilityTurret_EMP
                case DamageType.Laser: return 4401;      // Weapon_Creature_AbilityTurret_Laser
                case DamageType.Virulent: return 7648;   // Weapon_Creature_AbilityTurret_Virulent
                default: return 7645;                    // Weapon_Creature_AbilityTurret_Physical
            }
        }

        /// <summary>A trap shot: the turret weapon's level-50 damage at the owner's level, with DAMAGE_MODIFIER_PERCENT on it.</summary>
        public static int TrapShotDamage(int weaponDamage, int ownerLevel, int damageModifierPercent)
        {
            var scaled = ScaleToLevel(weaponDamage, ownerLevel);

            return Math.Max(1, scaled * Math.Max(0, 100 + damageModifierPercent) / 100);
        }

        private sealed class Trap
        {
            /// <summary>A Trap (decoy: threat, strike on death) rather than a Turret.</summary>
            public bool IsDecoy = true;
            public ActionId Action;
            public uint Pump;
            public int ShotMin;
            public int ShotMax;
            public int ShotScale;
            public MapChannel MapChannel;
            public Creature Creature;
            public Manifestation Owner;
            public GameEffect Death;
            public GameEffect Firing;
            public ActionId AttackAction;
            public uint AttackArg;
            public float Range;
            public int ShotDamage;
            public DamageType ShotType;
            public int StrikeMin;
            public int StrikeMax;
            public int StrikeScale;
            public DamageType StrikeType;
            public float StrikeRadius;
            public int HateTransferPercent;
            public uint Level;
            public Actor Aim;
            public long ExpiresAt;
            public long NextShotAt;
            public long RemoveAt;
        }

        private static readonly List<Trap> Traps = new List<Trap>();
        private static readonly object TrapsLock = new object();

        public static bool IsTrap(Creature creature)
        {
            lock (TrapsLock)
                return Traps.Any(t => t.Creature == creature);
        }

        /// <summary>Plants a trap turret at the spot, taking the player's last one away.</summary>
        private void PlantTrap(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            PlaceTurret(mapChannel, player, info, action, true);
        }

        /// <summary>
        /// A Trap (decoy) or a Turret at the spot. "Only 1 Trap turret can be active at a time";
        /// "Only 1 of each type of turret can be active at a time" - a new one takes the old one's
        /// place.
        /// </summary>
        /// <param name="defaultDuration">Seconds it lasts when the ability has no DURATION: 60, or the Thrax Technician's Mini Turret's 30.</param>
        private void PlaceTurret(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action, bool decoy, int defaultDuration = 60)
        {
            List<Trap> old;

            lock (TrapsLock)
                old = Traps.Where(t => t.Owner == player && t.RemoveAt == 0 && t.IsDecoy == decoy && (decoy || (t.Pump == info.Level && t.Action == info.ActionId))).ToList();

            foreach (var trap in old)
                RemoveTrap(trap);

            var level = Math.Max(1u, (uint)player.Level);
            var health = Scale((int)level, TrapBaseHealth, 2);
            var strikeType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var weaponClassId = TrapWeaponClassFor(strikeType);
            var weapon = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)weaponClassId, out var weaponClass)
                ? weaponClass.WeaponClassInfo
                : null;
            var spot = NavMeshManager.SnapToGround(mapChannel, action.TargetLocation ?? player.Position + FacingOf(player) * 2f);

            var turret = new Creature
            {
                EntityClass = TrapClass,
                TargetCategory = TargetCategory.Friendly,
                Level = level,
                MaxHitPoints = (uint)health,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>
                {
                    [EquipmentData.Weapon] = new AppearanceData { SlotId = EquipmentData.Weapon, Class = weaponClassId, Color = Color.RandomColor(), Hue2 = Color.RandomColor() }
                },
                State = CharacterState.Idle,
                IsScripted = true,
                MasterEntityId = player.EntityId,
                Name = decoy ? "Trap" : "Turret"
            };

            turret.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 1, 1, 1, 0, 0));
            turret.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 1, 1, 1, 0, 0));
            turret.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 1, 1, 1, 0, 0));
            turret.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 0, 0));
            turret.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            turret.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            turret.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            turret.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0));
            turret.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            turret.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));

            CreatureManager.Instance.SetLocation(turret, spot, player.Rotation, player.MapContextId);
            turret.LastYaw = (float)player.Rotation;
            CellManager.Instance.AddToWorld(mapChannel, turret);

            var now = Environment.TickCount64;
            var durationSeconds = info.Get(AbilityProperty.Duration, defaultDuration);

            GameEffect death;

            if (decoy)
            {
                // Hated twenty times over for what it does.
                var self = NewEffect(mapChannel, player, info, TrapSelfTypeId, durationSeconds);
                self.ThreatModifierPercent = info.Get(AbilityProperty.ThreatModifierPercent, 2000);
                self.AllowDetach = false;
                GameEffectManager.Instance.Attach(mapChannel, turret, self);

                // The trap's look, and what goes off when it is destroyed.
                death = NewEffect(mapChannel, player, info, TrapDeathTypeId, durationSeconds);
            }
            else
            {
                // The turret's look: VISUAL_TURRET_EFFECT, its FX by damage type
                // (ABILITY_DEPLOYABLE_TURRET_*_EFFECT).
                death = NewEffect(mapChannel, player, info, VisualTurretTypeId, durationSeconds);
                death.EffectLevel = (uint)strikeType;
            }

            death.AnnounceOnAttach = true;
            death.AllowDetach = false;
            GameEffectManager.Instance.Attach(mapChannel, turret, death);

            var min = info.Get(AbilityProperty.DamageAmountMin);

            lock (TrapsLock)
                Traps.Add(new Trap
                {
                    IsDecoy = decoy,
                    Action = info.ActionId,
                    Pump = info.Level,
                    ShotMin = min,
                    ShotMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                    ShotScale = info.Get(AbilityProperty.AttrScaleType, 2),
                    MapChannel = mapChannel,
                    Creature = turret,
                    Owner = player,
                    Death = death,
                    AttackAction = weapon?.WeaponAttackActionId ?? ActionId.WeaponAbilityTurret,
                    AttackArg = weapon?.WeaponAttackArgId ?? 3,
                    Range = TryGetLevel(weapon?.WeaponAttackActionId ?? ActionId.WeaponAbilityTurret, weapon?.WeaponAttackArgId ?? 3, out var attack) && attack.MaxRange > 0 ? attack.MaxRange : 40,
                    ShotDamage = TrapShotDamage(weapon?.MaxDamage ?? 1584, player.Level, info.Get(AbilityProperty.DamageModifierPercent, -75)),
                    ShotType = weapon != null && weapon.DamageType != 0 ? (DamageType)weapon.DamageType : strikeType,
                    StrikeMin = min,
                    StrikeMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                    StrikeScale = info.Get(AbilityProperty.DamageScaleType),
                    StrikeType = strikeType,
                    StrikeRadius = info.Get(AbilityProperty.EffectRadius, 10),
                    HateTransferPercent = info.Get(AbilityProperty.HateTransferPercent),
                    Level = Math.Max(1u, info.Level),
                    ExpiresAt = now + durationSeconds * 1000L,
                    NextShotAt = now + TrapShotMs
                });
        }

        /// <summary>Runs the traps on this map: shoot, draw the hate, run out, clear away.</summary>
        internal void TrapWorker(MapChannel mapChannel)
        {
            List<Trap> traps;

            lock (TrapsLock)
                traps = Traps.Where(t => t.MapChannel == mapChannel).ToList();

            if (traps.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var trap in traps)
            {
                var turret = trap.Creature;

                if (trap.RemoveAt != 0)
                {
                    if (now >= trap.RemoveAt)
                        RemoveTrap(trap);

                    continue;
                }

                if (now >= trap.ExpiresAt || trap.Owner.MapChannel != mapChannel || trap.Owner.MapContextId != turret.MapContextId)
                {
                    RemoveTrap(trap);
                    continue;
                }

                // Held by an enemy's Hack (or any stun): it holds its fire.
                if (Stuns.IsStunned(turret))
                {
                    StopTrapFire(trap);
                    continue;
                }

                if (now < trap.NextShotAt)
                    continue;

                trap.NextShotAt = now + TrapShotMs;

                Shoot(trap);
            }
        }

        /// <summary>One interval of the turret's fire at the nearest enemy in range, or the fire stopped when there is none.</summary>
        private void Shoot(Trap trap)
        {
            var mapChannel = trap.MapChannel;
            var turret = trap.Creature;

            if (trap.Aim == null || trap.Aim.State == CharacterState.Dead || trap.Aim.State == CharacterState.Dying
                || Vector3.Distance(trap.Aim.Position, turret.Position) > trap.Range || !StillAFoe(trap.Owner, trap.Aim))
                trap.Aim = FoesWithin(mapChannel, trap.Owner, turret.Position, trap.Range)
                    .Where(c => c.State != CharacterState.Dead && c.State != CharacterState.Dying)
                    .OrderBy(c => Vector3.DistanceSquared(c.Position, turret.Position))
                    .FirstOrDefault();

            if (trap.Aim == null)
            {
                StopTrapFire(trap);
                return;
            }

            var target = trap.Aim;

            turret.LastYaw = YawTowards(turret.Position, target.Position);

            if (trap.Firing == null || !turret.ActiveEffects.ContainsKey(trap.Firing.EffectId))
            {
                trap.Firing = new GameEffect
                {
                    TypeId = TurretEffectTypeId,
                    EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                    EffectLevel = (uint)trap.ShotType,
                    SourceId = turret.EntityId,
                    Source = turret,
                    SourceLevel = (int)turret.Level,
                    IsBuff = true,
                    AnnounceOnAttach = true,
                    AllowDetach = false
                };

                CellManager.Instance.CellCallMethod(turret, new PerformWindupPacket(PerformType.ThreeArgs, trap.AttackAction, trap.AttackArg, target.EntityId));

                // ConstantFireEffect.OnAttach(target, weaponId, actionId, actionArgId, numShots, interval, burstRecoil).
                GameEffectManager.Instance.Attach(mapChannel, turret, trap.Firing, 0UL, (int)trap.AttackAction, (int)trap.AttackArg, 1, TrapShotMs, 0);
            }

            // A Trap's shot is its weapon's, cut by DAMAGE_MODIFIER_PERCENT; a Turret's is the
            // ability's own DAMAGE_AMOUNT, scaled to the owner's level by ATTR_SCALE_TYPE.
            var shot = trap.IsDecoy
                ? trap.ShotDamage
                : Scale(trap.Owner.Level, BombRandom.Next(trap.ShotMin, trap.ShotMax + 1), trap.ShotScale);
            var amount = GameEffectManager.ApplyResist(target, shot, out var resisted, trap.ShotType);
            var taken = ActorManager.Instance.Damage(mapChannel, target, amount, turret, out var outcome, trap.ShotType);

            var tick = new ConstantFireTickPacket(trap.Firing.EffectId, false);
            tick.Pulses.Add(new List<TickEntry>
            {
                new TickEntry
                {
                    EntityId = target.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = trap.ShotType,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                }
            });

            CellManager.Instance.CellCallMethod(turret, tick);
        }

        /// <summary>
        /// Whether what a turret is aiming at is still one it would pick (FoesWithin): an enemy
        /// player without PvP Safety, or a creature its owner's summons go after. The aim is kept
        /// from shot to shot, and used to be kept after the duel, feud or battleground that made a
        /// player an enemy had ended, for as long as the turret lasted.
        /// </summary>
        private static bool StillAFoe(Manifestation owner, Actor aim) => aim switch
        {
            Manifestation player => Pvp.AreEnemies(owner, player) && !Pvp.IsSafe(player),
            Creature creature => TargetCategories.AlliesSeek(creature.TargetCategory) || Pvp.IsEnemySummon(owner, creature),
            _ => false
        };

        /// <summary>No one to shoot: the fire effect comes off and the attack is ended on the clients.</summary>
        private static void StopTrapFire(Trap trap)
        {
            if (trap.Firing == null)
                return;

            if (trap.Creature.ActiveEffects.ContainsKey(trap.Firing.EffectId))
                GameEffectManager.Instance.DettachEffect(trap.MapChannel, trap.Creature, trap.Firing);

            CellManager.Instance.CellCallMethod(trap.Creature, new PerformRecoveryPacket(PerformType.ListOfArgs, trap.AttackAction, trap.AttackArg, new MissileArgs()));

            trap.Firing = null;
        }

        /// <summary>
        /// The trap was destroyed: its death effect ticks with the killer, which the client answers
        /// with the explosion on them, and every hostile creature within the radius of the killer is
        /// struck, credited to the owner.
        /// </summary>
        internal void TrapKilled(MapChannel mapChannel, Creature creature, Actor killedBy)
        {
            Trap trap;

            lock (TrapsLock)
                trap = Traps.FirstOrDefault(t => t.Creature == creature);

            if (trap == null || trap.RemoveAt != 0)
                return;

            var turret = trap.Creature;
            var owner = trap.Owner;

            StopTrapFire(trap);

            trap.RemoveAt = Environment.TickCount64 + TrapLingerMs;
            turret.State = CharacterState.Dead;
            turret.Attributes[Attributes.Health].Current = 0;

            // A Turret is simply gone; only a Trap strikes back.
            if (!trap.IsDecoy || killedBy == null || killedBy.MapContextId != turret.MapContextId)
                return;

            // TrapDeathEffect.OnTick(target, killerId).
            var tick = new GameEffectTickPacket(trap.Death.EffectId, GameEffectTickPacket.TickKind.EntityId);
            tick.Entries.Add(new TickEntry { EntityId = killedBy.EntityId });
            CellManager.Instance.CellCallMethod(turret, tick);

            var strike = new GameEffectAnnounceDamagePacket(trap.Death.EffectId);
            var critChance = CriticalHits.AttackerChance(owner, false);

            foreach (var victim in VictimsWithin(mapChannel, owner, killedBy.Position, trap.StrikeRadius))
            {
                if (victim.State == CharacterState.Dead || victim.State == CharacterState.Dying || victim.Attributes[Attributes.Health].Current <= 0)
                    continue;

                var rolled = GameEffectManager.ApplyDamageDealt(owner, Scale(owner.Level, BombRandom.Next(trap.StrikeMin, trap.StrikeMax + 1), trap.StrikeScale));
                var crit = CriticalHits.Resolve(owner, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, trap.StrikeType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, owner, out var outcome, trap.StrikeType);

                var entry = new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = trap.StrikeType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                };

                strike.Hits.Add(entry);

                // A crit's side effect is announced by the strike (HitEffects).
                if (crit && !outcome.Immune && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    using (HitEffects.On(victim, owner, entry.TargetEffectIds))
                        CritEffects.OnCritical(mapChannel, victim, owner, trap.StrikeType, amount);
            }

            if (strike.Hits.Count > 0)
                CellManager.Instance.CellCallMethod(turret, strike);
        }

        /// <summary>Takes the trap out of the world, its effects and its entry on everyone's hate table with it.</summary>
        private static void RemoveTrap(Trap trap)
        {
            lock (TrapsLock)
                if (!Traps.Remove(trap))
                    return;

            var mapChannel = trap.MapChannel;
            var turret = trap.Creature;

            if (turret.State != CharacterState.Dead)
                StopTrapFire(trap);

            foreach (var cell in CellManager.CellsIn(mapChannel, turret.Cells))
                foreach (var creature in cell.CreatureList)
                    creature.Hate.Remove(turret.EntityId);

            GameEffectManager.Instance.ClearEffects(mapChannel, turret);

            turret.State = CharacterState.Dead;
            turret.Attributes[Attributes.Health].Current = 0;

            CellManager.Instance.RemoveCreatureFromWorld(mapChannel, turret);
        }
    }
}

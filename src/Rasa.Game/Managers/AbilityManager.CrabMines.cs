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
    /// Crab Mines (AA_SAPPER_CRAB_MINES 282, abilities.crabmines): "Creates a mobile mine that will
    /// seek out a nearby single enemy target and explode. The explosion will damage all enemies
    /// within the blast radius. The user may have up to 3 Crab Mines active at once. Crab Mines
    /// will self-detonate after a set time. Can be targeted and destroyed." (uielement 1285)
    ///
    /// From the client:
    /// - the mine is a creature of class Ability_Crab_Mine (10521, creature augmentation only);
    /// - CR_CRAB_MINE_SELF_DESTRUCT (413) has a 3000 ms windup on animation 1154 - the timer
    ///   running out - and CR_CRAB_MINE_DEATH (409) is the explosion: a recovery performed by the
    ///   mine, its argument the pump, playing animation 1154 over 3000 ms with the pump's
    ///   ABILITY_CREATURE_CRAB_MINE_SELF_DESTRUCT_*_RESOLVE FX (physical, fire, EMP, sonic,
    ///   light), and CrabMineDeathAbility.DoAbility announcing each hit from a hitdata that is the
    ///   bare rawInfo;
    /// - each pump's damage (150-213, exponentially scaled), type (physical, incendiary,
    ///   electric, sonic, photonic) and EFFECT_RADIUS (12 m); PER_PUMP_MOD 10 is the proficiency
    ///   bonus, "a damage bonus for each pump level the user has, regardless of what pump level is
    ///   used": +10% for every pump of Crab Mines the player owns.
    ///
    /// The server's part is the mine itself. It is a FRIENDLY creature of the player's level at
    /// the player's feet, so hostile creatures can find and kill it and the player cannot. It has no
    /// behaviour of its own (BehaviorManager leaves it alone); CrabMineWorker runs it each tick:
    /// - it seeks the nearest HOSTILE creature (AbilityManager.EnemiesWithin - not a NEUTRAL one)
    ///   within SeekRange of itself and runs at it at
    ///   MineSpeed, facing where it goes;
    /// - within DetonateRange of it, it explodes;
    /// - killed, it explodes where it fell;
    /// - after Lifetime, it plays the self-destruct windup where it stands and explodes after it;
    /// - its owner leaving the map takes it away unexploded.
    /// The explosion damages every hostile creature within EFFECT_RADIUS of the mine, credited to
    /// the owner, and the mine is taken out of the world CrabMineBlastLingerMs later - long enough
    /// for the clients to have the blast and start its FX, and not the three seconds of 409's
    /// recovery animation, which left the mine standing there after it had gone off. The mine
    /// is stepped every CrabMineStepMs by the worker rather than on BehaviorManager's slower think.
    ///
    /// Not in the client, so chosen: SeekRange 20 m, DetonateRange 3 m, MineSpeed 6 m/s,
    /// Lifetime 20 s, and the mine's health, MineBaseHealth at level 1 scaled like its damage.
    /// </summary>
    public partial class AbilityManager
    {
        public const string CrabMinesModule = "abilities.crabmines";
        public const int CrabMinesSkillId = 111;
                public const EntityClasses CrabMineClass = (EntityClasses)10521; // Ability_Crab_Mine

        public const int MaxCrabMines = 3;
        public const float CrabMineSeekRange = 20f;
        public const float CrabMineDetonateRange = 3f;
        public const float CrabMineSpeed = 6f;
        public const int CrabMineLifetimeMs = 20000;
        public const int CrabMineSelfDestructMs = 3000;            // CR_CRAB_MINE_SELF_DESTRUCT's windup
        public const int CrabMineBaseHealth = 100;
        public const int CrabMineDeathRecoveryMs = 3000;           // CR_CRAB_MINE_DEATH's recovery, animation 1154

        /// <summary>
        /// How long a blown mine stays in the world. CR_CRAB_MINE_DEATH's recovery is 3 s
        /// (animation 1154), and waiting it out kept the model standing where it exploded for
        /// those three seconds; the blast and its FX are sent the moment it goes off, so it only
        /// has to outlive the packet.
        /// </summary>
        public const int CrabMineBlastLingerMs = 500;

        /// <summary>How often a running mine is moved and its movement sent.</summary>
        public const int CrabMineStepMs = 100;

        private sealed class CrabMine
        {
            public MapChannel MapChannel;
            public Creature Creature;
            public Manifestation Owner;
            public int DamageMin;
            public int DamageMax;
            public int ScaleType;
            public DamageType DamageType;
            public float Radius;
            public int BonusPercent;
            public uint Level;
            public long ExpiresAt;
            public long DetonateAt;       // the self-destruct windup's end; 0 when not arming
            public long RemoveAt;         // after the blast has played; 0 until it has gone off
            public long LastStepAt;       // when it was last moved
        }

        private static readonly List<CrabMine> CrabMines = new List<CrabMine>();
        private static readonly object CrabMinesLock = new object();

        /// <summary>The proficiency bonus: PER_PUMP_MOD percent for every pump of the skill owned (at least the pump used).</summary>
        public static int CrabMineBonusPercent(int perPumpMod, int pumpsOwned, uint levelUsed)
        {
            return Math.Max(0, perPumpMod) * Math.Max(pumpsOwned, (int)levelUsed);
        }

        /// <summary>The player's mines still in play.</summary>
        public static int CrabMinesOf(Manifestation player)
        {
            lock (CrabMinesLock)
                return CrabMines.Count(m => m.Owner == player && m.RemoveAt == 0);
        }

        public static bool IsCrabMine(Creature creature)
        {
            lock (CrabMinesLock)
                return CrabMines.Any(m => m.Creature == creature);
        }

        /// <summary>Puts a crab mine down at the player's feet.</summary>
        private void DeployCrabMine(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var level = Math.Max(1u, (uint)player.Level);
            var health = Scale((int)level, CrabMineBaseHealth, info.Get(AbilityProperty.AttrScaleType, 2));

            var mine = new Creature
            {
                EntityClass = CrabMineClass,
                TargetCategory = TargetCategory.Friendly,
                Level = level,
                MaxHitPoints = (uint)health,
                RunSpeed = CrabMineSpeed,
                WalkSpeed = CrabMineSpeed,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                IsScripted = true,
                MasterEntityId = player.EntityId,
                Name = "Crab Mine"
            };

            mine.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 1, 1, 1, 0, 0));
            mine.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 1, 1, 1, 0, 0));
            mine.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 1, 1, 1, 0, 0));
            mine.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 0, 0));
            mine.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            mine.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            mine.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            mine.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0));
            mine.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            mine.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));

            var at = NavMeshManager.SnapToGround(mapChannel, player.Position + FacingOf(player) * 1.5f);

            CreatureManager.Instance.SetLocation(mine, at, player.Rotation, player.MapContextId);
            mine.LastYaw = (float)player.Rotation;
            CellManager.Instance.AddToWorld(mapChannel, mine);

            var min = info.Get(AbilityProperty.DamageAmountMin);

            lock (CrabMinesLock)
                CrabMines.Add(new CrabMine
                {
                    MapChannel = mapChannel,
                    Creature = mine,
                    Owner = player,
                    DamageMin = min,
                    DamageMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                    ScaleType = info.Get(AbilityProperty.DamageScaleType),
                    DamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical),
                    Radius = info.Get(AbilityProperty.EffectRadius, 12),
                    BonusPercent = CrabMineBonusPercent(info.Get(AbilityProperty.PerPumpMod, 10), ManifestationManager.SkillPump(player, CrabMinesSkillId), info.Level),
                    Level = Math.Max(1u, info.Level),
                    ExpiresAt = Environment.TickCount64 + CrabMineLifetimeMs,
                    LastStepAt = Environment.TickCount64
                });

            UpdateCrabMineBlock(player);
        }

        /// <summary>
        /// Blocks Crab Mines (ActionBlocks.CrabMineLimit) while the player has
        /// <see cref="MaxCrabMines"/> mines in play, and unblocks it when one goes: the client has
        /// no such limit (abilities/crabmines.py), so it asked for a fourth and wound up for the
        /// refusal. Run whenever that count changes - a mine put down, blown up or cleared away.
        /// </summary>
        private static void UpdateCrabMineBlock(Manifestation owner)
        {
            if (owner == null)
                return;

            var client = Server.Clients.Find(c => c.Player == owner);

            if (client == null)
                return;

            var atLimit = CrabMinesOf(owner) >= MaxCrabMines;

            foreach (var actionId in Instance.CrabMineActionIds())
                ActionBlocks.Set(client, actionId, ActionBlocks.CrabMineLimit, atLimit);
        }

        /// <summary>The actions that put a crab mine down: every action of the crab mines module.</summary>
        private IEnumerable<ActionId> CrabMineActionIds()
        {
            return _actions.Values.Where(a => a.Module == CrabMinesModule).Select(a => a.ActionId).ToList();
        }

        /// <summary>Runs the crab mines on this map for a tick: seek, run, detonate, time out, clear away.</summary>
        internal void CrabMineWorker(MapChannel mapChannel)
        {
            List<CrabMine> mines;

            lock (CrabMinesLock)
                mines = CrabMines.Where(m => m.MapChannel == mapChannel).ToList();

            if (mines.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var mine in mines)
            {
                var creature = mine.Creature;

                // Blown: gone once the blast has played.
                if (mine.RemoveAt != 0)
                {
                    if (now >= mine.RemoveAt)
                        RemoveCrabMine(mine);

                    continue;
                }

                // Its owner left the map, or the game: it goes quietly.
                if (mine.Owner.MapChannel != mapChannel || mine.Owner.MapContextId != creature.MapContextId)
                {
                    RemoveCrabMine(mine);
                    continue;
                }

                // Arming after its time ran out.
                if (mine.DetonateAt != 0)
                {
                    if (now >= mine.DetonateAt)
                        Detonate(mine);

                    continue;
                }

                if (now >= mine.ExpiresAt)
                {
                    mine.DetonateAt = now + CrabMineSelfDestructMs;
                    creature.KnockbackTo = null;
                    BehaviorManager.Instance.StopMoving(creature);
                    CellManager.Instance.CellCallMethod(creature, new PerformWindupPacket(PerformType.ThreeArgs, ActionId.CrCrabMineSelfDestruct, 1, 0));
                    continue;
                }

                var prey = EnemiesWithin(mapChannel, mine.Owner, creature.Position, CrabMineSeekRange)
                    .Where(c => c.State != CharacterState.Dead && c.State != CharacterState.Dying)
                    .OrderBy(c => Vector3.DistanceSquared(c.Position, creature.Position))
                    .FirstOrDefault();

                if (prey == null)
                {
                    if (creature.KnockbackTo != null)
                    {
                        creature.KnockbackTo = null;
                        BehaviorManager.Instance.StopMoving(creature);
                    }

                    continue;
                }

                if (Vector3.Distance(prey.Position, creature.Position) <= CrabMineDetonateRange)
                {
                    Detonate(mine);
                    continue;
                }

                // Run at it, facing the way it runs, a step every CrabMineStepMs.
                creature.KnockbackTo = prey.Position;
                creature.KnockbackSpeed = CrabMineSpeed;
                creature.KnockbackIsPull = true;

                var sinceStep = now - mine.LastStepAt;

                if (sinceStep >= CrabMineStepMs)
                {
                    mine.LastStepAt = now;
                    BehaviorManager.Instance.StepCarry(mapChannel, creature, sinceStep);
                }
            }
        }

        /// <summary>A crab mine was killed: it goes off where it fell ("explode on death").</summary>
        internal void CrabMineKilled(MapChannel mapChannel, Creature creature)
        {
            CrabMine mine;

            lock (CrabMinesLock)
                mine = CrabMines.FirstOrDefault(m => m.Creature == creature);

            if (mine != null && mine.RemoveAt == 0)
                Detonate(mine);
        }

        /// <summary>
        /// The blast: CR_CRAB_MINE_DEATH performed by the mine, its recovery listing every hostile
        /// creature within the radius with its damage - credited to the owner - as the bare
        /// rawInfo CrabMineDeathAbility reads. The mine is dead from here, and taken away
        /// CrabMineBlastLingerMs later.
        /// </summary>
        private void Detonate(CrabMine mine)
        {
            var mapChannel = mine.MapChannel;
            var creature = mine.Creature;
            var owner = mine.Owner;

            mine.RemoveAt = Environment.TickCount64 + CrabMineBlastLingerMs;
            UpdateCrabMineBlock(owner);
            creature.KnockbackTo = null;
            creature.State = CharacterState.Dead;
            creature.Attributes[Attributes.Health].Current = 0;     // off every scan and every fight

            var blast = new AbilityRecoveryPacket(ActionId.CrCrabMineDeath, mine.Level, AbilityRecoveryPacket.HitDataKind.RawInfo);
            var critChance = CriticalHits.AttackerChance(owner, false);
            var hitAny = false;

            foreach (var victim in HostilesWithin(mapChannel, owner, creature.Position, mine.Radius))
            {
                if (victim == creature || victim.State == CharacterState.Dead || victim.State == CharacterState.Dying || victim.Attributes[Attributes.Health].Current <= 0)
                    continue;

                var rolled = GameEffectManager.ApplyDamageDealt(owner, Scale(owner.Level, BombRandom.Next(mine.DamageMin, mine.DamageMax + 1), mine.ScaleType), mine.BonusPercent);
                var crit = CriticalHits.Resolve(owner, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, mine.DamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, owner, out var absorbed, mine.DamageType);

                blast.Hits.Add(new AbilityHit
                {
                    EntityId = victim.EntityId,
                    Amount = amount - absorbed,
                    Absorbed = absorbed,
                    Resisted = resisted,
                    DamageType = mine.DamageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                });

                hitAny = true;

                if (crit && victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    CritEffects.OnCritical(mapChannel, victim, owner, mine.DamageType, amount);
            }

            CellManager.Instance.CellCallMethod(creature, blast);

            if (hitAny)
            {
                var client = mapChannel.ClientList.Find(c => c?.Player == owner);

                if (client != null)
                    ManifestationManager.Instance.EnterCombat(client);
            }
        }

        private static void RemoveCrabMine(CrabMine mine)
        {
            lock (CrabMinesLock)
                CrabMines.Remove(mine);

            UpdateCrabMineBlock(mine.Owner);

            if (mine.MapChannel != null)
                CellManager.Instance.RemoveCreatureFromWorld(mine.MapChannel, mine.Creature);
        }
    }
}

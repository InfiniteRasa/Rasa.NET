using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>One thing a weapon's module does on a hit: a steal, or a debuff of one resistance.</summary>
    public sealed class ModuleProc
    {
        /// <summary>The attribute a steal takes; null for a resist debuff.</summary>
        public Attributes? Steals { get; }

        /// <summary>The damage type whose resistance a debuff cuts; 0 for a steal.</summary>
        public DamageType Debuffs { get; }

        /// <summary>The effect a debuff is shown as on the target: one of DEBUFF_*_RESIST, 312 to 319; 0 for a steal.</summary>
        public int DebuffTypeId { get; }

        /// <summary>What a steal takes; for a debuff, what it adds to the resistance - a negative number.</summary>
        public int Amount { get; }

        /// <summary>How long a debuff lasts.</summary>
        public int Seconds { get; }

        public ModuleProc(Attributes steals, int amount)
        {
            Steals = steals;
            Amount = amount;
        }

        public ModuleProc(DamageType debuffs, int debuffTypeId, int amount, int seconds)
        {
            Debuffs = debuffs;
            DebuffTypeId = debuffTypeId;
            Amount = amount;
            Seconds = seconds;
        }
    }

    /// <summary>What the modules in a player's armor and in the weapon in their hand add up to (ItemModuleBonuses.Of).</summary>
    public sealed class ModuleTotals
    {
        public static readonly ModuleTotals None = new ModuleTotals();

        /// <summary>"Body / Mind / Spirit: %(amount)s": points of the attribute.</summary>
        public int Body { get; internal set; }
        public int Mind { get; internal set; }
        public int Spirit { get; internal set; }

        /// <summary>"Health: %(amount)s" and "Power: %(amount)s": points of the maximum.</summary>
        public int Health { get; internal set; }
        public int Power { get; internal set; }

        /// <summary>"Regen: %(amount)s%%": points of the Regen attribute, which is a percentage.</summary>
        public int Regen { get; internal set; }

        /// <summary>"Regen Health: %(amount)s HP/sec", "Regen Power: %(amount)s per sec", "Regen Armor: %(amount)s HP/sec".</summary>
        public int HealthRegen { get; internal set; }
        public int PowerRegen { get; internal set; }
        public int ArmorRegen { get; internal set; }

        /// <summary>"Crit Hit Chance: %(amount)s%%".</summary>
        public int CritChance { get; internal set; }

        /// <summary>"Perceived Threat: %(amount)s%%": added to the 100 a player's threat starts at; the modules lower it.</summary>
        public int ThreatPercent { get; internal set; }

        /// <summary>"Armor Piercing: %(amount)s%%".</summary>
        public int ArmorPierce { get; internal set; }

        /// <summary>"Movement Speed: %(amount)s%%".</summary>
        public int MovementPercent { get; internal set; }

        /// <summary>"%(amount)s%% experience from kills."</summary>
        public int ExperiencePercent { get; internal set; }

        /// <summary>"Resist: $damageType%(arg1)s %(amount)s", by type: the eight kinds of damage, and being knocked back, stunned, slowed, held or blinded.</summary>
        public Dictionary<DamageType, int> Resist { get; } = new Dictionary<DamageType, int>();

        /// <summary>"Total Armor: %(amount)s", by the piece it is in: "a bonus to the item's total damage absorption capacity".</summary>
        public Dictionary<ulong, int> ArmorOf { get; } = new Dictionary<ulong, int>();

        /// <summary>What the weapon in hand does on a hit.</summary>
        public List<ModuleProc> Procs { get; } = new List<ModuleProc>();

        /// <summary>
        /// The part of it a player's client has been told of - attributes, regeneration, armor,
        /// resistances, speed - as one value to compare: when it has not changed, there is
        /// nothing to tell (ItemModuleBonuses.Changed).
        /// </summary>
        public string Shown =>
            $"{Body},{Mind},{Spirit},{Health},{Power},{Regen},{HealthRegen},{PowerRegen},{ArmorRegen},{MovementPercent}" +
            $"|{string.Join(",", Resist.Where(r => r.Value != 0).OrderBy(r => r.Key).Select(r => $"{(int)r.Key}:{r.Value}"))}" +
            $"|{string.Join(",", ArmorOf.Where(a => a.Value != 0).OrderBy(a => a.Key).Select(a => $"{a.Key}:{a.Value}"))}";
    }

    /// <summary>
    /// What item modules do. A module is a row of module_class with rows of module_effect, each
    /// a client game effect and the three terms of an amount (ItemModules); the amount on an item
    /// is worked out for the item's level, as the tooltip shows it. The modules that count are
    /// those in the armor a player has on and in the weapon - or tool - in their hand; a module
    /// in a broken item does nothing, as the item does nothing.
    ///
    /// No effect is put on the player for them, but for the run speed's. The totals are read
    /// off the items when they are wanted (<see cref="Of"/>), so there is no copy of them to go
    /// stale, and each number goes where the server already works that stat out:
    ///
    ///  - Body, Mind, Spirit, Health, Power, Regen, Total Armor and the three regeneration
    ///    rates: ManifestationManager.UpdateStatsValues. Body, Mind and Spirit are the
    ///    attribute's modified value, which the attributes window shows in green beside the
    ///    base, and what is derived from the three - health, armor, the crit chance of Spirit -
    ///    is derived from the modified value;
    ///  - Resist, for a kind of damage: the player's resistance list (RebuildResistances), which
    ///    the character window shows and GameEffectManager.ApplyResist takes off damage;
    ///  - Resist, for being knocked back, stunned, slowed, held or blinded: the chance in
    ///    percent that it does not land (<see cref="ControlResistPercent"/>), as Graviton
    ///    Armor's "Knockback / Stun Resist" is - a stun has no part to take off;
    ///  - Crit Hit Chance: CriticalHits.AttackerChance, so weapons and abilities both;
    ///  - Perceived Threat: Threat.ThreatModifierOf;
    ///  - Armor Piercing: the share of a weapon hit that goes past armor, with the weapon
    ///    skill's own (MissileManager.MissileLaunch, ConstantFire);
    ///  - Movement Speed: a standing effect only the server knows of, put on with the skills'
    ///    (ManifestationManager.SyncSkillPassives, SyncModulePassives) and multiplied in with
    ///    the rest;
    ///  - experience from kills: KillShares.AwardExperience.
    ///
    /// The weapon modules that fire on a hit (<see cref="OnWeaponHit"/>), each on a roll of its
    /// own for every creature or enemy player the weapon hits and hurts:
    ///
    ///  - Steal Health, Power, Adrenaline, Armor: VampiricDamage.Steal for the amount. What is
    ///    stolen is what the target has: a creature has health and armor, and no power or
    ///    adrenaline to take;
    ///  - Debuff Resist: the client's "Debuff: Resist - Fire" (312 to 319) on the target for
    ///    the row's seconds, cutting its resistance to that one kind of damage by the amount -
    ///    for everyone who hits it. A weaker one does not take a stronger one's place.
    ///
    /// The module items say "a chance"; the client has no number for it, and neither for how
    /// long a debuff lasts. The chances are appsettings.json's (ItemModulesConfig), the seconds
    /// the module's row's (module_effect.arg2), where the tooltip reads them too.
    /// </summary>
    public static class ItemModuleBonuses
    {
        public const uint BodyEffect = 330;             // LOOT_BODY_AMOUNT
        public const uint MindEffect = 332;             // LOOT_MIND_AMOUNT
        public const uint SpiritEffect = 334;           // LOOT_SPIRIT_AMOUNT
        public const uint HealthEffect = 336;           // LOOT_HEALTH_AMOUNT
        public const uint PowerEffect = 338;            // LOOT_POWER_AMOUNT
        public const uint RegenEffect = 340;            // LOOT_REGEN_AMOUNT
        public const uint ArmorRegenEffect = 108;       // LOOT_ARMOR_REGEN
        public const uint PowerRegenEffect = 110;       // LOOT_POWER_REGEN
        public const uint HealthRegenEffect = 111;      // LOOT_HEALTH_REGEN
        public const uint ThreatEffect = 151;           // LOOT_THREAT_PERCENT_DONE
        public const uint CritChanceEffect = 166;       // MODULE_CRIT_CHANCE
        public const uint MovementEffect = 227;         // MODULE_MOVEMENT_MODIFIER
        public const uint StealHealthEffect = 320;      // VAMP_HEALTH_PROC
        public const uint StealPowerEffect = 321;       // VAMP_POWER_PROC
        public const uint StealChiEffect = 322;         // VAMP_CHI_PROC
        public const uint StealArmorEffect = 323;       // VAMP_ARMOR_PROC
        public const uint ExperienceEffect = 402;       // MODULE_XP_MODIFIER
        public const uint ResistEffect = 413;           // MODULE_RESIST_EFFECT
        public const uint ArmorEffect = 10000049;       // LOOT_ARMOR_AMOUNT
        public const uint ArmorPierceEffect = 10000052; // LOOT_ARMOR_PIERCE_PERCENT

        /// <summary>How long a resist debuff lasts when its row does not say (module_effect.arg2).</summary>
        public const int DefaultDebuffSeconds = 10;

        /// <summary>
        /// The eight DEBUFF_*_RESIST_PROC effects a module is shown with, to the kind of damage
        /// and the DEBUFF_*_RESIST effect the target is shown with.
        /// </summary>
        private static readonly Dictionary<uint, (DamageType Type, int TypeId)> ResistDebuffs = new Dictionary<uint, (DamageType, int)>
        {
            [9] = (DamageType.Physical, 312),
            [112] = (DamageType.Fire, 313),
            [113] = (DamageType.Ice, 314),
            [114] = (DamageType.Virulent, 315),
            [115] = (DamageType.EMP, 316),
            [171] = (DamageType.Laser, 317),
            [172] = (DamageType.Sonic, 318),
            [173] = (DamageType.Electrical, 319)
        };

        /// <summary>The resistances that are a chance not to be put under something, not a share taken off damage.</summary>
        private static readonly HashSet<DamageType> ControlTypes = new HashSet<DamageType>
        {
            DamageType.KnockBack, DamageType.Stun, DamageType.Snare, DamageType.Root, DamageType.Blind
        };

        /// <summary>gameeffectdata.MODULE_MOVEMENT_MODIFIER: the standing effect the run speed modules are held as.</summary>
        public const int MovementTypeId = (int)MovementEffect;

        public static ItemModulesConfig Config { get; set; } = new ItemModulesConfig();

        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        /// <summary>Whether a roll at this percent chance comes up; a test's to replace.</summary>
        internal static Func<double, bool> Roll { get; set; } = DefaultRoll;

        internal static bool DefaultRoll(double chancePercent)
        {
            return chancePercent > 0 && (chancePercent >= 100 || Rng.Value.NextDouble() * 100.0 < chancePercent);
        }

        /// <summary>The first and last of the equipment slots that take armor, and the one between them that mirrors the weapon in hand.</summary>
        private const int FirstSlot = 1;
        private const int LastSlot = 21;
        private const int WeaponSlot = (int)EquipmentData.Weapon;

        /// <summary>What the modules in the actor's armor and the weapon in their hand add up to; nothing for anyone but a player.</summary>
        public static ModuleTotals Of(Actor actor)
        {
            var equipped = (actor as Manifestation)?.Inventory?.EquippedInventory;

            if (equipped == null)
                return ModuleTotals.None;

            ModuleTotals totals = null;

            for (var slot = FirstSlot; slot <= LastSlot && slot < equipped.Count; slot++)
            {
                if (equipped[slot] == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(equipped[slot]);

                if (item?.ItemTemplate == null || !HasModule(item) || Durability.IsBroken(item))
                    continue;

                Add(totals ??= new ModuleTotals(), item, slot == WeaponSlot);
            }

            return totals ?? ModuleTotals.None;
        }

        private static bool HasModule(Item item)
        {
            foreach (var moduleId in item.ModuleIds)
                if (moduleId != 0)
                    return true;

            return false;
        }

        private static void Add(ModuleTotals totals, Item item, bool inHand)
        {
            var level = ItemModules.LevelOf(item);

            foreach (var moduleId in item.ModuleIds)
            {
                if (moduleId == 0 || !ItemModules.TryGet(moduleId, out var module))
                    continue;

                foreach (var effect in module.Effects)
                {
                    // A set's bonus, for so many pieces of it: sets are not counted.
                    if (effect.SetLevel != 0)
                        continue;

                    var amount = effect.Amount(level);

                    switch (effect.EffectId)
                    {
                        case BodyEffect: totals.Body += amount; break;
                        case MindEffect: totals.Mind += amount; break;
                        case SpiritEffect: totals.Spirit += amount; break;
                        case HealthEffect: totals.Health += amount; break;
                        case PowerEffect: totals.Power += amount; break;
                        case RegenEffect: totals.Regen += amount; break;
                        case HealthRegenEffect: totals.HealthRegen += amount; break;
                        case PowerRegenEffect: totals.PowerRegen += amount; break;
                        case ArmorRegenEffect: totals.ArmorRegen += amount; break;
                        case CritChanceEffect: totals.CritChance += amount; break;
                        case ThreatEffect: totals.ThreatPercent += amount; break;
                        case ArmorPierceEffect: totals.ArmorPierce += amount; break;
                        case MovementEffect: totals.MovementPercent += amount; break;
                        case ExperienceEffect: totals.ExperiencePercent += amount; break;

                        case ArmorEffect:
                            totals.ArmorOf[item.EntityId] = totals.ArmorOf.GetValueOrDefault(item.EntityId) + amount;
                            break;

                        case ResistEffect:
                            if (effect.Arg1 is int type && type > 0)
                                totals.Resist[(DamageType)type] = totals.Resist.GetValueOrDefault((DamageType)type) + amount;
                            break;

                        // On a hit of the weapon they are in, and of no other.
                        case StealHealthEffect: if (inHand) totals.Procs.Add(new ModuleProc(Attributes.Health, amount)); break;
                        case StealPowerEffect: if (inHand) totals.Procs.Add(new ModuleProc(Attributes.Power, amount)); break;
                        case StealChiEffect: if (inHand) totals.Procs.Add(new ModuleProc(Attributes.Chi, amount)); break;
                        case StealArmorEffect: if (inHand) totals.Procs.Add(new ModuleProc(Attributes.Armor, amount)); break;

                        default:
                            if (inHand && amount != 0 && ResistDebuffs.TryGetValue(effect.EffectId, out var debuff))
                                totals.Procs.Add(new ModuleProc(debuff.Type, debuff.TypeId, -Math.Abs(amount),
                                    effect.Arg2 is int seconds && seconds > 0 ? seconds : DefaultDebuffSeconds));
                            break;
                    }
                }
            }
        }

        /// <summary>The player's resistance to each kind of damage from their modules: what goes into their resistance list.</summary>
        public static IEnumerable<KeyValuePair<DamageType, int>> DamageResistsOf(Manifestation player)
        {
            return Of(player).Resist.Where(resist => !ControlTypes.Contains(resist.Key));
        }

        /// <summary>
        /// The chance in percent, from their modules, that the actor is not knocked back,
        /// stunned, slowed, held or blinded: the module's Resist for that, at most 100.
        /// </summary>
        public static int ControlResistPercent(Actor actor, DamageType kind)
        {
            if (!ControlTypes.Contains(kind))
                return 0;

            return Math.Max(0, Math.Min(100, Of(actor).Resist.GetValueOrDefault(kind)));
        }

        /// <summary>
        /// What an effect about to go on a player would do to them that a module resists: held
        /// where they stand, slowed or blinded; 0 for anything else. A stun or a knockback is
        /// rolled where it is made (PlayerCrowdControl), before the player is thrown.
        /// </summary>
        public static DamageType ControlKindOf(GameEffect effect)
        {
            if (effect == null || effect.IsBuff || effect.Environmental || effect.IsStun)
                return 0;

            if (effect.ControlKind != 0)
                return effect.ControlKind;

            if (effect.IsRoot)
                return DamageType.Root;

            if (effect.Blinds)
                return DamageType.Blind;

            return effect.MovementModifierPercent > 0 && effect.MovementModifierPercent < 100 ? DamageType.Snare : 0;
        }

        /// <summary>Whether a player's modules turn away an effect that would hold, slow or blind them (GameEffectManager.Attach).</summary>
        public static bool ResistsControl(Actor actor, GameEffect effect)
        {
            if (!(actor is Manifestation) || effect?.Source == null || ReferenceEquals(effect.Source, actor))
                return false;

            var kind = ControlKindOf(effect);

            return kind != 0 && Roll(ControlResistPercent(actor, kind));
        }

        /// <summary>A kill's experience for this player with what their modules add to it.</summary>
        public static uint WithExperience(Manifestation player, uint experience)
        {
            var percent = Of(player).ExperiencePercent;

            if (percent <= 0 || experience == 0)
                return experience;

            return (uint)Math.Min(uint.MaxValue, Math.Round(experience * (100.0 + percent) / 100.0));
        }

        /// <summary>What the weapon in the player's hand does on a hit; empty for most.</summary>
        public static IReadOnlyList<ModuleProc> ProcsOf(Actor actor)
        {
            return Of(actor).Procs;
        }

        /// <summary>
        /// The player's weapon has hit and hurt a target: each steal and each debuff the weapon
        /// carries, on a roll of its own. procs is what the weapon carried when the shot was
        /// fired (Missile.WeaponProcs), or the weapon in hand's when not given.
        /// </summary>
        public static void OnWeaponHit(MapChannel mapChannel, Actor shooter, Actor target, IReadOnlyList<ModuleProc> procs = null)
        {
            if (mapChannel == null || !(shooter is Manifestation) || target == null || ReferenceEquals(shooter, target))
                return;

            procs ??= ProcsOf(shooter);

            if (procs.Count == 0)
                return;

            foreach (var proc in procs)
            {
                // A steal of health can be what kills it.
                if (target.State == CharacterState.Dead || target.State == CharacterState.Dying
                    || !target.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                    return;

                if (proc.Steals.HasValue)
                {
                    if (proc.Amount > 0 && Roll(Config.StealChancePercent))
                        VampiricDamage.Steal(mapChannel, shooter, target, proc.Steals.Value, proc.Amount);
                }
                else if (proc.Amount < 0 && Roll(Config.ResistDebuffChancePercent))
                    DebuffResist(mapChannel, shooter, target, proc);
            }
        }

        /// <summary>
        /// "Debuff: Resist - Fire" on the target: its resistance to that kind of damage is cut
        /// by the amount for the seconds, for every hit of that kind from anyone. A fresh one
        /// takes the place of the one before - the clock starts over - unless the one already
        /// there cuts deeper.
        /// </summary>
        private static void DebuffResist(MapChannel mapChannel, Actor shooter, Actor target, ModuleProc proc)
        {
            foreach (var present in target.ActiveEffects.Values)
                if (present.TypeId == proc.DebuffTypeId && !present.IsExpired && present.ResistModifier < proc.Amount)
                    return;

            var effect = new GameEffect
            {
                TypeId = proc.DebuffTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = shooter.EntityId,
                Source = shooter,
                SourceLevel = (shooter as Manifestation)?.Level ?? 1,
                IsBuff = false,
                ExpiresTick = Environment.TickCount64 + proc.Seconds * 1000L,
                ResistDamageType = proc.Debuffs,
                ResistModifier = proc.Amount
            };

            // "Resist Fire: %(amount)s%%".
            effect.Tooltip["amount"] = proc.Amount;

            GameEffectManager.Instance.Attach(mapChannel, target, effect);
        }

        /// <summary>
        /// The run speed modules as the standing effect the server holds them as: nobody is told
        /// of the effect, and its modifier is multiplied in with the rest
        /// (GameEffectManager.UpdateMovementMod), which is what tells the clients the speed and
        /// what the move check measures against. Null with no such module.
        /// </summary>
        public static GameEffect MovementPassive(MapChannel mapChannel, Manifestation player)
        {
            var percent = Of(player).MovementPercent;

            if (percent <= 0 || mapChannel == null)
                return null;

            return new GameEffect
            {
                TypeId = MovementTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsSkillPassive = true,
                ServerOnly = true,
                AnnounceOnAttach = false,
                AnnounceToNewcomers = false,
                MovementModifierPercent = 100 + percent
            };
        }

        /// <summary>
        /// Once a tick: the players on the map whose equipment changed since the last one
        /// (Manifestation.ModulesChanged). A weapon swapped for another is two changes, and one
        /// look. A player still loading into the map - their items are put in their slots as
        /// they are read - keeps the mark until they are in the world, where their client has
        /// the actor to be told about.
        /// </summary>
        public static void Worker(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList.ToList())
            {
                var player = client?.Player;

                if (player == null || !player.ModulesChanged)
                    continue;

                if (client.State != ClientState.Ingame || player.Disconected || player.MapChannel != mapChannel || !CellManager.Instance.IsInWorld(client))
                    continue;

                player.ModulesChanged = false;
                Changed(client);
            }
        }

        /// <summary>
        /// Something a player wears or holds may have changed what their modules give them: the
        /// weapon in hand is another, a module went into or came out of something they have on,
        /// a piece broke or was mended. When what their client was told of has changed, it is
        /// worked out again and told: the attributes and the bars, the resistances, the speed.
        /// The rest - crit chance, threat, armor piercing, experience, what the weapon does on a
        /// hit - is read off the items each time and needs no telling.
        /// </summary>
        public static void Changed(Client client)
        {
            var player = client?.Player;

            if (player?.MapChannel == null)
                return;

            var shown = Of(player).Shown;

            if (shown == (player.ModulesShown ?? ModuleTotals.None.Shown))
                return;

            player.ModulesShown = shown;

            // Attributes, health, power and their regeneration; the armor bar and its.
            ManifestationManager.Instance.RefreshStats(client);
            CellManager.Instance.CellCallMethod(player.MapChannel, player, new UpdateArmorPacket(player.Attributes[Attributes.Armor], 0));

            // The resistance list, and the run speed.
            ManifestationManager.Instance.SyncModulePassives(client);
        }
    }
}

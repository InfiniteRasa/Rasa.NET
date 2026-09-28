using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// Player abilities, driven by the action tables (action, action_level, action_cost,
    /// action_property, action_item_requirement, item_template_action) that are the client's
    /// generated.client.actiondata row for row.
    ///
    /// The exchange with the client, from client/actions/baseactoraction.py and
    /// abilities/baseactorability.py:
    ///
    ///  1. The client checks the ability itself - cooldown, cost, target - then plays its own
    ///     windup and sends RequestPerformAbility(actionId, level, target, itemId). It does not
    ///     wait for the server to start the windup.
    ///  2. The server checks the same things and, if any fails, answers UserActionFailed with
    ///     the reason; the client shows it and cancels its windup. Otherwise everyone else is
    ///     sent PerformWindup so they see the animation, and the action is queued for the
    ///     windup time.
    ///  3. When the windup has run the server resolves the ability: takes the cost, starts the
    ///     cooldown, applies the effect, and sends PerformRecovery with the list of who was hit
    ///     and what it did to them. The performer's client runs the ability's DoAbility over
    ///     that list to show the numbers; everyone else's plays the recovery.
    ///
    /// What an ability does comes from its properties (AbilityProperty) and the client module
    /// that runs it (ActionInfo.Module). Direct damage - lightning, the knockbacks and
    /// strikes, the waves - is resolved here; sprint and the timed buffs, debuffs and
    /// damage-over-times (Rage, Resistance, Sacrifice, Ruin, Scourge, Reconstruction, the
    /// Regeneration and Base waves) become GameEffects, built in AbilityManager.Effects.cs and
    /// run by GameEffectManager. An ability this does not know how to resolve is refused with
    /// "cannot perform action now" and logged once, so it costs the player nothing and the gap
    /// is visible in the log.
    /// </summary>
    public partial class AbilityManager
    {
        private static AbilityManager _instance;
        private static readonly object InstanceLock = new object();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly MissionApplication _missionManager;
        private readonly Dictionary<ActionId, ActionInfo> _actions = new Dictionary<ActionId, ActionInfo>();
        private readonly Dictionary<uint, (ActionId ActionId, uint Level)> _itemTemplateActions = new Dictionary<uint, (ActionId, uint)>();
        private readonly HashSet<ActionId> _reportedUnsupported = new HashSet<ActionId>();
        private readonly Random _random = new Random();

        private sealed class LightningLanding
        {
            internal Creature Primary;
            internal DynamicObject PracticeTarget;
            internal Vector3 PrimaryPosition;
            internal float ArcRadius;
            internal int ArcDamage;
            internal IReadOnlyList<Creature> ArcTargets;
        }

        /// <summary>
        /// Metres past an ability's range a target may be and still be hit. The client checks
        /// range before it asks, so a refusal here is either a moving target or a cheat; the
        /// slack covers the former.
        /// </summary>
        private const float RangeSlack = 2.5f;

        /// <summary>
        /// The client modules whose DoAbility reads hit data as (rawInfo, onHitData) - the
        /// DamageBase subclasses in client/actions/abilities. These are the damage abilities
        /// resolved here; a damage ability of another class has some other shape and its own
        /// mechanics, and waits.
        /// </summary>
        private static readonly HashSet<string> DirectDamageModules = new HashSet<string>
        {
            "abilities.lightning", "abilities.knockback", "abilities.rushingblow", "abilities.shrapnel",
            "abilities.tectonicstrike", "abilities.stun", "abilities.concussivewave", "abilities.energywave",
            "abilities.vortex", "abilities.deathdamage", "abilities.stalkereggattack"
        };

        /// <summary>
        /// The client modules whose abilities are a GameEffect with a duration - on the
        /// performer, the squad around them, or an enemy - resolved by ResolveTimedEffect.
        /// </summary>
        private static readonly HashSet<string> TimedEffectModules = new HashSet<string>
        {
            "abilities.rage", "abilities.resistance", "abilities.sacrifice", "abilities.decay",
            "abilities.scourge", "abilities.reconstruction", "abilities.regenerationwave", "abilities.basewave",
            "abilities.medpack"
        };

        /// <summary>Of those, the ones aimed at a single enemy (client targetType TARGET_NON_FRIENDLY).</summary>
        private static readonly HashSet<string> HostileEffectModules = new HashSet<string> { "abilities.decay" };

        /// <summary>
        /// Abilities the client marks isToggle without a sourceGameEffect or targetGameEffect
        /// (Sacrifice), or whose toggle the player may also press again while it runs (Rage): a
        /// second request while the effect is on means "off", as it does for sprint.
        /// </summary>
        private static readonly HashSet<string> ToggleModules = new HashSet<string> { "abilities.sprint", "abilities.rage", "abilities.sacrifice" };

        public static AbilityManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new AbilityManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private AbilityManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
            : this(gameUnitOfWorkFactory, null)
        {
        }

        private AbilityManager(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            MissionApplication missionManager)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _missionManager = missionManager;
        }

        public int Count => _actions.Count;

        public bool TryGetAction(ActionId actionId, out ActionInfo action) => _actions.TryGetValue(actionId, out action);

        public bool TryGetLevel(ActionId actionId, uint level, out ActionLevelInfo info)
        {
            info = null;
            return _actions.TryGetValue(actionId, out var action) && action.Levels.TryGetValue(level, out info);
        }

        /// <summary>The action a usable item template performs, if it performs one.</summary>
        public bool TryGetItemAction(uint itemTemplateId, out ActionId actionId, out uint level)
        {
            if (_itemTemplateActions.TryGetValue(itemTemplateId, out var pair))
            {
                actionId = pair.ActionId;
                level = pair.Level;
                return true;
            }

            actionId = 0;
            level = 0;
            return false;
        }

        #region Loading

        public void AbilityInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            foreach (var entry in unitOfWork.Actions.GetActions())
                _actions[(ActionId)entry.Id] = new ActionInfo
                {
                    ActionId = (ActionId)entry.Id,
                    Name = entry.Name,
                    Module = entry.Module,
                    IsCharged = entry.IsCharged != 0
                };

            var levels = 0;

            foreach (var entry in unitOfWork.Actions.GetActionLevels())
            {
                if (!_actions.TryGetValue((ActionId)entry.ActionId, out var action))
                    continue;

                action.Levels[entry.Level] = new ActionLevelInfo
                {
                    ActionId = action.ActionId,
                    Level = entry.Level,
                    WindupMs = Math.Max(0, entry.WindupMs),
                    RecoveryMs = Math.Max(0, entry.RecoveryMs),
                    MaxRange = entry.MaxRange,
                    ReuseMs = Math.Max(0, entry.ReuseMs),
                    StartReuseOnPerform = entry.StartReuseOnPerform != 0
                };
                levels++;
            }

            var costs = 0;

            foreach (var entry in unitOfWork.Actions.GetActionCosts())
                if (TryGetLevel((ActionId)entry.ActionId, entry.Level, out var level))
                {
                    level.Costs.Add(new ActionCost { Attribute = (Attributes)entry.AttributeId, Amount = entry.Cost });
                    costs++;
                }

            var properties = 0;

            foreach (var entry in unitOfWork.Actions.GetActionProperties())
                if (TryGetLevel((ActionId)entry.ActionId, entry.Level, out var level))
                {
                    level.Properties[(AbilityProperty)entry.PropertyId] = entry.Value;
                    properties++;
                }

            var itemRequirements = 0;

            foreach (var entry in unitOfWork.Actions.GetActionItemRequirements())
                if (TryGetLevel((ActionId)entry.ActionId, entry.Level, out var level))
                {
                    level.ItemRequirements.Add(new ActionItemRequirement { ItemClass = (EntityClasses)entry.ItemClassId, Quantity = entry.Quantity });
                    itemRequirements++;
                }

            foreach (var entry in unitOfWork.Actions.GetItemTemplateActions())
                _itemTemplateActions[entry.ItemTemplateId] = ((ActionId)entry.ActionId, entry.Level);

            Logger.WriteLog(LogType.Initialize, $"Loaded {_actions.Count} actions with {levels} levels, {costs} costs, {properties} properties, {itemRequirements} item requirements; {_itemTemplateActions.Count} item templates perform an action.");
        }

        #endregion

        #region Request

        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            var player = client.Player;
            var mapChannel = player?.MapChannel;

            // Loading: the player is between maps, and their cells still belong to the old one.
            if (mapChannel == null || client.State != ClientState.Ingame)
                return;

            var actionId = packet.ActionId;
            var level = (uint)packet.ActionArgId;

            if (!_actions.TryGetValue(actionId, out var action) || !action.Levels.TryGetValue(level, out var info))
            {
                Logger.WriteLog(LogType.Debug, $"{player.FamilyName} asked for action {actionId} level {level}, which is not in the action tables");
                Fail(client, actionId, level, PlayerMessage.PmActionFailedBadData);
                return;
            }

            if (player.State == CharacterState.Dead)
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedActorDead);
                return;
            }

            // Is it theirs to use? A skill that grants the ability at this level, or a usable
            // item in their pack whose template performs exactly this action.
            var item = packet.ItemId != 0 ? EntityManager.Instance.GetItem(packet.ItemId) : null;
            if (packet.ItemId != 0 && item == null)
            {
                Fail(client, actionId, level, PlayerMessage.PmMissingReqItem);
                return;
            }

            if (!Grants(player, actionId, level, item))
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} asked for {action.Name} level {level} without a skill or item that grants it");
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            if (!CanResolve(action, info))
            {
                if (_reportedUnsupported.Add(actionId))
                    Logger.WriteLog(LogType.Error, $"Ability {action.Name} ({action.Module}) is not implemented on the server yet; refused.");

                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            if (player.ActionReuseUntil.TryGetValue(actionId, out var readyAt) && readyAt > Environment.TickCount64)
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // A sustained ability is a toggle: "Duration: Open-ended (toggle to deactivate)", the
            // sprint tooltip says. SprintAction does not set isToggle in the client code, so the
            // second press arrives as an ordinary request rather than a detach; ending the running
            // effect is what the player meant, and the silent refusal cancels the client's local
            // windup and takes nothing. Sacrifice is the same case with isToggle set but no
            // effect class named for the client to ask about, and Rage can arrive this way too.
            if (IsSustained(info) || ToggleModules.Contains(action.Module))
            {
                // Their own, not a copy of a squad mate's aura they happen to be standing in.
                var running = player.ActiveEffects.Values.FirstOrDefault(e => e.ActionId == actionId && e.Parent == null);

                if (running != null)
                {
                    GameEffectManager.Instance.DettachEffect(mapChannel, player, running);
                    Fail(client, actionId, level, null);
                    return;
                }
            }

            // The target, when the ability wants one. Area-around-source and cone abilities have
            // none; self abilities have none or the performer.
            Actor target = null;
            DynamicObject practiceTarget = null;

            if (!SelfCentred(info) && packet.Target.HasEntity && packet.Target.EntityId != player.EntityId)
            {
                target = ResolveTarget(mapChannel, packet.Target.EntityId);
                if (target == null)
                    practiceTarget = ResolvePracticeTarget(mapChannel, player, info, packet.Target.EntityId);

                if (target == null && practiceTarget == null)
                {
                    Fail(client, actionId, level, PlayerMessage.PmActionFailedNoTarget);
                    return;
                }

                if (target?.State == CharacterState.Dead)
                {
                    Fail(client, actionId, level, PlayerMessage.PmActionFailedTargetDead);
                    return;
                }

                var distance = Vector3.Distance(player.Position, practiceTarget?.Position ?? target.Position);
                if (!float.IsFinite(distance) || info.MaxRange > 0 && distance > info.MaxRange + RangeSlack)
                {
                    Fail(client, actionId, level, PlayerMessage.PmTargetOutOfRange);
                    return;
                }
            }

            var wantsHostile = IsDirectDamage(action, info) || HostileEffectModules.Contains(action.Module);

            if (wantsHostile && target == null && practiceTarget == null &&
                !SelfCentred(info) && packet.Target.Kind != ActionTargetKind.Location)
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedNoTarget);
                return;
            }

            if (wantsHostile && target != null && !IsHostile(player, target))
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedActorFriendly);
                return;
            }

            // Can they pay? Checked now so the refusal is immediate; taken when the ability
            // lands, so an interrupted windup costs nothing. A sustained ability has no price to
            // pay up front - its cost row is the same figure as its drain, and the drain is how it
            // is paid - but it does need something in the tank to start on.
            if (IsSustained(info))
            {
                if (!player.Attributes.TryGetValue(Attributes.Chi, out var chi) || chi.Current <= 0)
                {
                    Fail(client, actionId, level, PlayerMessage.PmConsumableNotEnoughChi);
                    return;
                }
            }
            else
            {
                var shortfall = CostShortfall(player, info);

                if (shortfall.HasValue)
                {
                    Fail(client, actionId, level, shortfall.Value == Attributes.Power ? PlayerMessage.PmConsumableNotEnoughPower : PlayerMessage.PmConsumableNotEnoughChi);
                    return;
                }
            }

            foreach (var requirement in info.ItemRequirements)
                if (InventoryManager.Instance.CountItemsByClass(client, requirement.ItemClass) < requirement.Quantity)
                {
                    Fail(client, actionId, level, PlayerMessage.PmMissingReqItem);
                    return;
                }

            // Accepted. Everyone else sees the windup; the performer's client already started its own.
            var targetId = practiceTarget?.EntityId ?? target?.EntityId ?? 0;
            SendToOthers(mapChannel, player, new PerformWindupPacket(PerformType.ThreeArgs, actionId, level, targetId));

            // One ability at a time: a new request replaces a pending one, as the client's own
            // action queue does.
            mapChannel.PerformRecovery.RemoveAll(a => a.Actor == player && a.ActionId == actionId);

            mapChannel.PerformRecovery.Add(new ActionData(player, actionId, level, targetId, info.WindupMs)
            {
                TargetObject = practiceTarget,
                TargetLocation = packet.Target.Kind == ActionTargetKind.Location ? packet.Target.Location : null,
                ItemId = packet.ItemId
            });
        }

        /// <summary>
        /// Whether the player may use the action at this level: a skill of theirs grants the
        /// ability at that level or higher, or the item they are using performs exactly it.
        /// </summary>
        private bool Grants(Manifestation player, ActionId actionId, uint level, Item item)
        {
            if (item != null)
            {
                if (item.OwnerId != player.Id || item.StackSize == 0 ||
                    !player.Inventory.PersonalInventory.Contains(item.EntityId))
                    return false;

                return _itemTemplateActions.TryGetValue(item.ItemTemplateId, out var performs) && performs.ActionId == actionId && performs.Level == level;
            }

            foreach (var skill in player.Skills.Values)
                if (skill.AbilityId == (int)actionId && skill.SkillLevel >= level)
                    return true;

            return false;
        }

        /// <summary>Whether this server knows how to apply the ability; see the class remarks.</summary>
        private static bool CanResolve(ActionInfo action, ActionLevelInfo info)
        {
            return action.Module == "abilities.sprint" || IsDirectDamage(action, info) || TimedEffectModules.Contains(action.Module);
        }

        /// <summary>
        /// An ability that stays on and pays for itself out of adrenaline while it does -
        /// DRAIN_PER_TICK_ADRENALINE is the tell. Sprint is the only one in the client's data.
        /// Its action_cost row carries the same number as the drain: that row is what the client
        /// checks against before asking, not a price the server should take on top.
        /// </summary>
        private static bool IsSustained(ActionLevelInfo info)
        {
            return info.Has(AbilityProperty.DrainPerTickAdrenaline);
        }

        private static bool IsDirectDamage(ActionInfo action, ActionLevelInfo info)
        {
            return DirectDamageModules.Contains(action.Module) && info.Has(AbilityProperty.DamageAmountMin);
        }

        /// <summary>Area-around-source and cone abilities are aimed from the performer, not at a target.</summary>
        private static bool SelfCentred(ActionLevelInfo info)
        {
            return info.Has(AbilityProperty.RadiusAroundSource) || info.Has(AbilityProperty.ConeRadius);
        }

        private static Actor ResolveTarget(MapChannel mapChannel, ulong entityId)
        {
            Actor target = EntityManager.Instance.GetEntityType(entityId) switch
            {
                EntityType.Creature => EntityManager.Instance.GetCreature(entityId),
                EntityType.Character => EntityManager.Instance.GetPlayer(entityId),
                _ => null
            };

            // Entity ids are global, cells are per map: a target on another map is not here.
            return IsOnMap(mapChannel, target) ? target : null;
        }

        private static DynamicObject ResolvePracticeTarget(
            MapChannel map, Manifestation player, ActionLevelInfo info, ulong entityId)
        {
            if (info.ActionId != ActionId.AaRecruitLightning || info.Level != 1 ||
                !PracticeTargetManager.TryGetTarget(map, entityId, out var target) ||
                !PracticeTargetManager.CanHit(map, player, target))
                return null;
            return target;
        }

        private static bool IsOnMap(MapChannel mapChannel, Actor actor)
            => MapInstanceScope.Contains(mapChannel, actor);

        /// <summary>
        /// Who a player's damage may land on: creatures that are not AFS. Other players are not
        /// targets - there is no PvP to speak of yet - and AFS creatures are the friendly NPCs.
        /// </summary>
        private static bool IsHostile(Manifestation player, Actor target)
        {
            return target is Creature creature &&
                   creature.Faction != Factions.AFS &&
                   creature.State != CharacterState.Dead &&
                   creature.State != CharacterState.Dying &&
                   creature.Attributes.TryGetValue(Attributes.Health, out var health) &&
                   health.Current > 0;
        }

        /// <summary>The first attribute the player cannot pay, or null if they can pay them all.</summary>
        private static Attributes? CostShortfall(Manifestation player, ActionLevelInfo info)
        {
            foreach (var cost in info.Costs)
            {
                if (!player.Attributes.TryGetValue(cost.Attribute, out var attribute))
                    continue;

                if (attribute.Current < ScaledCost(player, info, cost.Amount))
                    return cost.Attribute;
            }

            return null;
        }

        private static void Fail(Client client, ActionId actionId, uint level, PlayerMessage? message)
        {
            client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(actionId, level, message));
        }

        #endregion

        #region Recovery

        /// <summary>Called from ActorActionManager when the windup has run.</summary>
        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            var player = action.Actor as Manifestation;
            var client = player == null ? null : Server.Clients.Find(c => c.Player == player);

            if (client == null || client.State != ClientState.Ingame || player.MapChannel != mapChannel)
                return;

            if (!_actions.TryGetValue(action.ActionId, out var actionInfo) || !actionInfo.Levels.TryGetValue(action.ActionArgId, out var info))
                return;

            if (action.IsInrerrupted || player.State == CharacterState.Dead)
            {
                // The others are still showing the windup. The performer's client cancelled its
                // own when it sent the interrupt; a performer who died mid-windup is told, so the
                // request does not sit unresolved on their side.
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));

                if (!action.IsInrerrupted)
                    Fail(client, action.ActionId, action.ActionArgId, PlayerMessage.PmActionFailedActorDead);

                return;
            }

            // The request said they could start; this says it still lands. A windup is time the
            // world goes on in, and everything the request weighed can have changed inside it.
            var refusal = StillAllowed(mapChannel, client, player, action, actionInfo, info);

            if (refusal.HasValue)
            {
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));
                Fail(client, action.ActionId, action.ActionArgId, refusal.Value);
                return;
            }

            LightningLanding lightningLanding = null;
            if (IsDirectDamage(actionInfo, info) &&
                actionInfo.Module == "abilities.lightning" &&
                !TrySnapshotLightningLanding(
                    mapChannel,
                    player,
                    info,
                    action,
                    out lightningLanding))
            {
                SendToOthers(mapChannel, player,
                    new ActionInterruptPacket(
                        player.EntityId,
                        action.ActionId,
                        action.ActionArgId));
                Fail(client, action.ActionId, action.ActionArgId,
                    PlayerMessage.PmActionFailedNoTarget);
                return;
            }

            try
            {
                ConsumeAbilityItems(client, info, action.ItemId);
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                Logger.WriteLog(LogType.Error, $"Unable to consume ability items for character {player.Id}: {error.Message}");
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));
                Fail(client, action.ActionId, action.ActionArgId, PlayerMessage.PmMissingReqItem);
                return;
            }

            // Paid on landing, not on asking. A sustained ability pays as it runs, through its
            // effect's drain, not here.
            if (!IsSustained(info))
                TakeCosts(client, player, info);

            StartCooldown(client, player, info);

            if (actionInfo.Module == "abilities.sprint")
            {
                GameEffectManager.Instance.AttachSprint(mapChannel, player, info);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (IsDirectDamage(actionInfo, info))
            {
                ResolveDirectDamage(
                    mapChannel,
                    client,
                    player,
                    actionInfo,
                    info,
                    action,
                    lightningLanding);
                return;
            }

            if (TimedEffectModules.Contains(actionInfo.Module))
            {
                ResolveTimedEffect(mapChannel, client, player, actionInfo, info, action);
                return;
            }

            // Not reachable: RequestPerformAbility refuses what cannot be resolved. Finish the
            // client's action cleanly all the same.
            CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
        }

        private void ConsumeAbilityItems(Client client, ActionLevelInfo info, ulong sourceItemId)
        {
            if (sourceItemId == 0 && info.ItemRequirements.Count == 0)
                return;
            using var unit = _gameUnitOfWorkFactory.CreateChar();
            var quantities = new Dictionary<ulong, uint>();
            var source = sourceItemId == 0 ? null : EntityManager.Instance.GetItem(sourceItemId);
            if (sourceItemId != 0 && source == null)
                throw new GameplayRejectionException("The ability source item is no longer available.");
            if (source != null && Game.Missions.Persistence.MissionItemProtection.IsProtected(source, unit))
                throw new GameplayRejectionException("Assignment-owned items cannot pay ordinary ability costs.");
            if (source != null)
                quantities[source.EntityId] = 1;
            var sourceCredit = source == null ? 0U : 1U;
            foreach (var requirement in info.ItemRequirements)
            {
                var remaining = requirement.Quantity;
                if (sourceCredit > 0 && source.ItemTemplate.Class == requirement.ItemClass && remaining > 0)
                {
                    remaining--;
                    sourceCredit--;
                }
                foreach (var entityId in client.Player.Inventory.PersonalInventory.Where(id => id != 0))
                {
                    if (remaining == 0)
                        break;
                    var item = EntityManager.Instance.GetItem(entityId);
                    if (item?.ItemTemplate?.Class != requirement.ItemClass ||
                        Game.Missions.Persistence.MissionItemProtection.IsProtected(item, unit))
                        continue;
                    var reserved = quantities.GetValueOrDefault(entityId);
                    if (reserved > item.StackSize)
                        throw new GameplayRejectionException("The ability source stack is empty.");
                    var take = Math.Min(remaining, item.StackSize - reserved);
                    if (take == 0)
                        continue;
                    quantities[entityId] = reserved + take;
                    remaining -= take;
                }
                if (remaining != 0)
                    throw new GameplayRejectionException("Required ability items are no longer available.");
            }
            var consumption = new InventoryManager.InventoryConsumption();
            unit.ExecuteTransaction(() => consumption.PlanAndSave(client, quantities, unit));
            consumption.Publish(client);
            foreach (var progress in consumption.ProgressEvents)
                (_missionManager ?? MissionApplication.Instance).RecordProgress(client, progress);
        }

        /// <summary>
        /// Why a landing ability should not land after all, or null to let it through.
        ///
        /// The request weighs all of this and then the windup runs, which for some abilities is
        /// seconds. The recovery took the request's word for every bit of it, and the one thing
        /// it did repeat it repeated loosely: TakeCosts subtracts with Math.Max(0, ...), so an
        /// ability that could no longer be paid for emptied the pool and went off anyway. Only
        /// a second request for the same action replaces a pending one, so two different
        /// abilities could be started against the same adrenaline and both land on one pool's
        /// worth of it. A target could be walked out of reach during the windup and still be hit
        /// at any distance, which with an unbounded Move is anything on the map. And the item
        /// that granted the ability could be traded or sold in the meantime.
        ///
        /// Target identity, life, hostility, map membership and range are re-weighed here before
        /// costs are taken. Lightning then snapshots its still-valid primary and arc candidates
        /// at landing and revalidates each arc immediately before applying its damage.
        /// </summary>
        private PlayerMessage? StillAllowed(
            MapChannel mapChannel,
            Client client,
            Manifestation player,
            ActionData action,
            ActionInfo actionInfo,
            ActionLevelInfo info)
        {
            // Asked for with an item, so it is the item that has to still grant it - a skill the
            // player also happens to have does not stand in for the one they used.
            var item = action.ItemId != 0 ? EntityManager.Instance.GetItem(action.ItemId) : null;

            if (action.ItemId != 0 && item == null)
                return PlayerMessage.PmMissingReqItem;

            if (!Grants(player, action.ActionId, action.ActionArgId, item))
                return PlayerMessage.PmCannotPerformActionNow;

            // A sustained ability pays through its drain rather than up front, so all it needs
            // is something left in the tank - the same test the request makes.
            if (IsSustained(info))
            {
                if (!player.Attributes.TryGetValue(Attributes.Chi, out var chi) || chi.Current <= 0)
                    return PlayerMessage.PmConsumableNotEnoughChi;
            }
            else
            {
                var shortfall = CostShortfall(player, info);

                if (shortfall.HasValue)
                    return shortfall.Value == Attributes.Power ? PlayerMessage.PmConsumableNotEnoughPower : PlayerMessage.PmConsumableNotEnoughChi;
            }

            foreach (var requirement in info.ItemRequirements)
                if (InventoryManager.Instance.CountItemsByClass(client, requirement.ItemClass) < requirement.Quantity)
                    return PlayerMessage.PmMissingReqItem;

            if (action.TargetId != 0)
            {
                var target = ResolveTarget(mapChannel, action.TargetId);
                var practiceTarget = action.TargetObject == null ? null :
                    ResolvePracticeTarget(mapChannel, player, info, action.TargetId);

                if (action.TargetObject != null && !ReferenceEquals(practiceTarget, action.TargetObject))
                    return PlayerMessage.PmActionFailedNoTarget;
                if (target == null && practiceTarget == null)
                    return PlayerMessage.PmActionFailedNoTarget;

                if (target != null && IsDirectDamage(actionInfo, info) &&
                    !IsValidPrimaryTarget(mapChannel, player, target))
                    return target.State == CharacterState.Dead ||
                           target.State == CharacterState.Dying ||
                           !target.Attributes.TryGetValue(Attributes.Health, out var health) ||
                           health.Current <= 0
                        ? PlayerMessage.PmActionFailedTargetDead
                        : PlayerMessage.PmActionFailedActorFriendly;

                var distance = Vector3.Distance(player.Position, practiceTarget?.Position ?? target.Position);
                if (!float.IsFinite(distance) ||
                    info.MaxRange > 0 && distance > info.MaxRange + RangeSlack)
                    return PlayerMessage.PmTargetOutOfRange;
            }

            return null;
        }

        private static void TakeCosts(Client client, Manifestation player, ActionLevelInfo info)
        {
            foreach (var cost in info.Costs)
            {
                if (!player.Attributes.TryGetValue(cost.Attribute, out var attribute))
                    continue;

                attribute.Current = Math.Max(0, attribute.Current - ScaledCost(player, info, cost.Amount));

                switch (cost.Attribute)
                {
                    case Attributes.Power:
                        client.CallMethod(player.EntityId, new UpdatePowerPacket(attribute, 0));
                        break;
                    case Attributes.Chi:
                        client.CallMethod(player.EntityId, new UpdateChiPacket(attribute, 0));
                        break;
                    case Attributes.Health:
                        CellManager.Instance.CellCallMethod(client.Player.MapChannel, player, new UpdateHealthPacket(attribute, 0));
                        break;
                }
            }
        }

        /// <summary>
        /// Starts the cooldown. The client starts its own on perform when the action says so,
        /// counting recovery + reuse from the moment the windup ends - which is now - so the
        /// server counts the same, less a little so its clock never runs past the client's. For
        /// the others the server's word is the only one, and it says it with
        /// ActionReuseTimerRestarted.
        /// </summary>
        private static void StartCooldown(Client client, Manifestation player, ActionLevelInfo info)
        {
            if (info.ReuseMs <= 0)
                return;

            var total = info.ReuseMs + (info.StartReuseOnPerform ? info.RecoveryMs : 0);
            player.ActionReuseUntil[info.ActionId] = Environment.TickCount64 + Math.Max(0, total - 150);

            if (!info.StartReuseOnPerform)
                client.CallMethod(player.EntityId, new ActionReuseTimerRestartedPacket(info.ActionId, info.Level));
        }

        /// <summary>
        /// Rolls DAMAGE_AMOUNT_MIN..MAX, scales it to the performer's level, and applies it to
        /// every hostile in the ability's area - the target alone, everything within
        /// RADIUS_AROUND_TARGET of it, or everything within RADIUS_AROUND_SOURCE / CONE_RADIUS
        /// of the performer. A cone is taken as the full circle for now. Each target gets its
        /// own roll, as the client's per-hit rawInfo expects.
        /// </summary>
        private void ResolveDirectDamage(
            MapChannel mapChannel,
            Client client,
            Manifestation player,
            ActionInfo actionInfo,
            ActionLevelInfo info,
            ActionData action,
            LightningLanding lightningLanding)
        {
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var scaleType = info.Get(AbilityProperty.DamageScaleType);
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));

            if (action.TargetObject != null)
            {
                if (lightningLanding?.PracticeTarget == null ||
                    !PracticeTargetManager.CanHit(mapChannel, player, lightningLanding.PracticeTarget))
                    return;
                var practiceTarget = lightningLanding.PracticeTarget;
                var amount = GameEffectManager.ApplyDamageDealt(
                    player, Scale(player.Level, _random.Next(min, max + 1), scaleType));
                var result = new AbilityRecoveryPacket(
                    action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Damage)
                {
                    ArcData = true
                };
                result.Hits.Add(new AbilityHit
                {
                    EntityId = practiceTarget.EntityId,
                    Amount = amount,
                    DamageType = damageType
                });
                ManifestationManager.Instance.EnterCombat(client);
                CellManager.Instance.CellCallMethod(mapChannel, player, result);
                if (amount > 0)
                    PracticeTargetManager.RecordHit(
                        mapChannel, player, practiceTarget, action.ActionId, _missionManager);
                return;
            }

            var targets = new List<Creature>();
            var primary = action.TargetId != 0
                ? ResolveTarget(mapChannel, action.TargetId) as Creature
                : null;
            var lightning = actionInfo.Module == "abilities.lightning";
            var lightningArc = (Radius: 0f, Damage: 0, MaximumTargets: 0);
            var lightningPrimaryPosition = Vector3.Zero;
            IReadOnlyList<Creature> lightningArcTargets = Array.Empty<Creature>();

            if (lightning)
            {
                if (lightningLanding == null &&
                    !TrySnapshotLightningLanding(
                        mapChannel,
                        player,
                        info,
                        action,
                        out lightningLanding))
                    return;

                primary = lightningLanding.Primary;
                if (!IsValidPrimaryTarget(mapChannel, player, primary))
                    return;

                lightningPrimaryPosition =
                    lightningLanding.PrimaryPosition;
                lightningArc =
                    (lightningLanding.ArcRadius,
                        lightningLanding.ArcDamage,
                        lightningLanding.ArcTargets.Count);
                lightningArcTargets = lightningLanding.ArcTargets;
                targets.Add(primary);
            }
            else if (info.Has(AbilityProperty.RadiusAroundSource) || info.Has(AbilityProperty.ConeRadius))
            {
                var radius = Math.Max(info.Get(AbilityProperty.RadiusAroundSource), info.Get(AbilityProperty.ConeRadius));
                targets.AddRange(HostilesWithin(mapChannel, player, player.Position, radius));
            }
            else if (info.Has(AbilityProperty.RadiusAroundTarget))
            {
                var centre = primary?.Position ?? action.TargetLocation ?? player.Position;
                targets.AddRange(HostilesWithin(mapChannel, player, centre, info.Get(AbilityProperty.RadiusAroundTarget)));

                if (primary != null && !targets.Contains(primary) && IsHostile(player, primary))
                    targets.Add(primary);
            }
            else if (primary != null && IsHostile(player, primary))
            {
                targets.Add(primary);
            }

            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Damage)
            {
                ArcData = lightning
            };

            if (targets.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);

            foreach (var target in targets)
            {
                // Rolled, scaled to the performer's level, raised or lowered by the effects on
                // them (Rage, Sacrifice), and cut by what the target's effects resist.
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, _random.Next(min, max + 1), scaleType));
                var amount = GameEffectManager.ApplyResist(target, rolled, out var resisted);
                var taken = ActorManager.Instance.Damage(mapChannel, target, amount, player);

                var hit = new AbilityHit
                {
                    EntityId = target.EntityId,
                    Amount = amount,
                    Resisted = resisted,
                    DamageType = damageType,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                };

                if (lightning && target == primary)
                    ApplyLightningArcs(
                        mapChannel,
                        player,
                        primary.EntityId,
                        lightningPrimaryPosition,
                        lightningArc.Radius,
                        lightningArc.Damage,
                        damageType,
                        lightningArcTargets,
                        hit);

                recovery.Hits.Add(hit);
                if (taken > 0 &&
                    target.DbId != 0)
                    (_missionManager ?? MissionApplication.Instance).RecordProgress(
                        client,
                        MissionProgressEvent.AbilityHit(
                            (uint)action.ActionId,
                            target.DbId));
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        private static bool TrySnapshotLightningLanding(
            MapChannel mapChannel,
            Manifestation player,
            ActionLevelInfo info,
            ActionData action,
            out LightningLanding landing)
        {
            landing = null;
            if (action.TargetObject != null)
            {
                var practiceTarget = ResolvePracticeTarget(mapChannel, player, info, action.TargetId);
                if (practiceTarget == null || !ReferenceEquals(practiceTarget, action.TargetObject))
                    return false;
                landing = new LightningLanding
                {
                    PracticeTarget = practiceTarget,
                    PrimaryPosition = practiceTarget.Position,
                    ArcTargets = Array.Empty<Creature>()
                };
                return true;
            }
            var primary = action.TargetId != 0
                ? ResolveTarget(mapChannel, action.TargetId) as Creature
                : null;
            if (!IsValidPrimaryTarget(mapChannel, player, primary) ||
                !IsFinite(primary.Position))
                return false;

            var arc = GetLightningArcSpec(info, player.Level);
            landing = new LightningLanding
            {
                Primary = primary,
                PrimaryPosition = primary.Position,
                ArcRadius = arc.Radius,
                ArcDamage = arc.Damage,
                ArcTargets = SelectLightningArcTargets(
                    mapChannel,
                    player,
                    primary,
                    arc.Radius,
                    arc.MaximumTargets)
            };
            return true;
        }

        private static void ApplyLightningArcs(
            MapChannel mapChannel,
            Manifestation player,
            ulong primaryEntityId,
            Vector3 primaryPosition,
            float radius,
            int damage,
            DamageType damageType,
            IReadOnlyList<Creature> arcTargets,
            AbilityHit primaryHit)
        {
            if (damage <= 0 || !float.IsFinite(radius) || radius <= 0 ||
                !IsFinite(primaryPosition) || arcTargets == null)
                return;

            var radiusSquared = radius * radius;
            foreach (var arcTarget in arcTargets)
            {
                if (!IsValidLightningArcTarget(
                        mapChannel,
                        player,
                        arcTarget,
                        primaryEntityId,
                        primaryPosition,
                        radiusSquared))
                    continue;

                var arcTaken = ActorManager.Instance.Damage(
                    mapChannel, arcTarget, damage, player);
                primaryHit.Arcs.Add(new AbilityHit
                {
                    EntityId = arcTarget.EntityId,
                    Amount = damage,
                    DamageType = damageType,
                    DeathBlow = arcTaken > 0 &&
                                arcTarget.Attributes[Attributes.Health].Current <= 0
                });
            }
        }

        private static bool IsValidLightningArcTarget(
            MapChannel mapChannel,
            Manifestation player,
            Creature candidate,
            ulong primaryEntityId,
            Vector3 primaryPosition,
            float radiusSquared)
        {
            if (mapChannel == null || player == null || candidate == null ||
                candidate.EntityId == primaryEntityId ||
                candidate.MapContextId != mapChannel.MapInfo.MapContextId ||
                EntityManager.Instance.GetEntityType(candidate.EntityId) !=
                EntityType.Creature ||
                !EntityManager.Instance.Creatures.TryGetValue(
                    candidate.EntityId, out var registered) ||
                !ReferenceEquals(candidate, registered) ||
                !IsHostile(player, candidate) ||
                !IsFinite(candidate.Position))
                return false;

            var distance = Vector3.DistanceSquared(
                primaryPosition, candidate.Position);
            return float.IsFinite(distance) && distance <= radiusSquared;
        }

        /// <summary>Living, non-AFS creatures within radius metres of a point, from the cells around the performer.</summary>
        internal static List<Creature> HostilesWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            var found = new List<Creature>();

            if (radius <= 0)
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var creature in cell.CreatureList)
                    if (IsHostile(player, creature) && !found.Contains(creature) && Vector3.Distance(centre, creature.Position) <= radius)
                        found.Add(creature);

            return found;
        }

        internal static (float Radius, int Damage, int MaximumTargets) GetLightningArcSpec(
            ActionLevelInfo info,
            int actorLevel)
        {
            if (info == null)
                return (0, 0, 0);

            var radius = info.Get(AbilityProperty.ArcRadius);
            var baseDamage = info.Get(AbilityProperty.ArcDamage);
            if (radius <= 0 || baseDamage <= 0)
                return (0, 0, 0);

            return (
                radius,
                Scale(actorLevel, baseDamage, info.Get(AbilityProperty.DamageScaleType)),
                1);
        }

        internal static List<Creature> SelectLightningArcTargets(
            MapChannel mapChannel,
            Manifestation player,
            Creature primary,
            float radius,
            int maximumTargets)
        {
            if (mapChannel == null || player == null || primary == null ||
                maximumTargets <= 0 || !float.IsFinite(radius) || radius <= 0 ||
                !IsValidPrimaryTarget(mapChannel, player, primary) ||
                !IsFinite(primary.Position))
                return new List<Creature>();

            var radiusSquared = radius * radius;

            return mapChannel.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Where(candidate => candidate != null &&
                                    candidate.EntityId != primary.EntityId &&
                                    candidate.MapContextId == mapChannel.MapInfo.MapContextId &&
                                    candidate.RuntimeMapChannel == mapChannel &&
                                    candidate.State != CharacterState.Dead &&
                                    EntityManager.Instance.GetEntityType(candidate.EntityId) == EntityType.Creature &&
                                    EntityManager.Instance.Creatures.TryGetValue(candidate.EntityId, out var registered) &&
                                    ReferenceEquals(candidate, registered) &&
                                    IsHostile(player, candidate))
                .Select(candidate => new
                {
                    Target = candidate,
                    Distance = Vector3.DistanceSquared(primary.Position, candidate.Position)
                })
                .Where(entry => float.IsFinite(entry.Distance) && entry.Distance <= radiusSquared)
                .GroupBy(entry => entry.Target.EntityId)
                .Select(group => group.First())
                .OrderBy(entry => entry.Distance)
                .ThenBy(entry => entry.Target.EntityId)
                .Take(maximumTargets)
                .Select(entry => entry.Target)
                .ToList();
        }

        private static bool IsValidPrimaryTarget(
            MapChannel mapChannel,
            Manifestation player,
            Actor target)
        {
            return target is Creature creature &&
                   mapChannel != null &&
                   player != null &&
                   creature.MapContextId == mapChannel.MapInfo.MapContextId &&
                   creature.RuntimeMapChannel == mapChannel &&
                   EntityManager.Instance.GetEntityType(creature.EntityId) == EntityType.Creature &&
                   EntityManager.Instance.Creatures.TryGetValue(
                       creature.EntityId, out var registered) &&
                   ReferenceEquals(creature, registered) &&
                   IsHostile(player, creature);
        }

        private static bool IsFinite(Vector3 position) =>
            float.IsFinite(position.X) &&
            float.IsFinite(position.Y) &&
            float.IsFinite(position.Z);

        /// <summary>
        /// The performer and the living members of their squad within radius metres of them, from
        /// the cells around the performer. A player in no squad is a squad of one. This is who a
        /// "user and nearby squad members" ability reaches.
        /// </summary>
        internal static List<Manifestation> SquadWithin(MapChannel mapChannel, Manifestation player, float radius)
        {
            var found = new List<Manifestation> { player };

            if (radius <= 0 || player.PartyId == 0)
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var client in cell.ClientList)
                {
                    var other = client?.Player;

                    if (other == null || other == player || found.Contains(other))
                        continue;

                    if (other.PartyId != player.PartyId || other.State == CharacterState.Dead)
                        continue;

                    if (!other.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                        continue;

                    if (Vector3.Distance(player.Position, other.Position) <= radius)
                        found.Add(other);
                }
            return found;
        }

        #endregion

        #region Scaling

        /// <summary>
        /// shared/scaling.py ScaleActorAmount: how a base amount grows with the actor's level.
        /// Type 1 is linear, a third more per level; type 2 is exponential, doubling every eight
        /// levels; anything else is unscaled. Both truncate.
        /// </summary>
        public static int Scale(int actorLevel, int baseAmount, int scaleType)
        {
            var levelsUp = Math.Max(0, actorLevel - 1);

            return scaleType switch
            {
                1 => (int)(baseAmount / 100.0 * (100 + levelsUp * 100.0 / 3.0)),
                2 => (int)(baseAmount * Math.Pow(2, levelsUp / 8.0)),
                _ => baseAmount
            };
        }

        private static int ScaledCost(Manifestation player, ActionLevelInfo info, int amount)
        {
            return Scale(player.Level, amount, info.Get(AbilityProperty.ConsumableScaleType));
        }

        #endregion

        /// <summary>CellManager.CellCallMethod without the actor's own client.</summary>
        private static void SendToOthers(MapChannel mapChannel, Actor actor, PythonPacket packet)
        {
            foreach (var cell in CellManager.CellsIn(mapChannel, actor.Cells))
                foreach (var client in cell.ClientList)
                    if (client.Player != actor)
                        client.CallMethod(actor.EntityId, packet);
        }
    }
}

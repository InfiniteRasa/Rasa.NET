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
    using Repositories.Char;
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
    ///  2. The server checks the same things and, if any fails, answers with the reason
    ///     (ActorManager.RefuseRequest: UserActionFailed, which the client shows, and
    ///     ActionFailed, which cancels its windup). Otherwise everyone else is
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
        private readonly Dictionary<ActionId, ActionInfo> _actions = new Dictionary<ActionId, ActionInfo>();
        private readonly Dictionary<uint, (ActionId ActionId, uint Level)> _itemTemplateActions = new Dictionary<uint, (ActionId, uint)>();
        private readonly HashSet<ActionId> _reportedUnsupported = new HashSet<ActionId>();
        private readonly Random _random = new Random();

        /// <summary>
        /// Metres past an ability's range a target may be and still be hit. The client checks
        /// range before it asks, so a refusal here is either a moving target or a cheat; the
        /// slack covers the former.
        /// </summary>
        private const float RangeSlack = 2.5f;

        /// <summary>Vortex's client module: its targets are pulled in (CrowdControl.Pull) as well as hurt.</summary>
        public const string VortexModule = "abilities.vortex";

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
            "abilities.vortex", "abilities.deathdamage", "abilities.stalkereggattack",
            // Polymorph's creature attacks (AbilityManager.MorphAbilities).
            KaelSmashModule, KaelGroundPoundModule, CaretakerAttackModule
        };

        /// <summary>
        /// The client modules whose abilities are a GameEffect with a duration - on the
        /// performer, the squad around them, or an enemy - resolved by ResolveTimedEffect.
        /// </summary>
        private static readonly HashSet<string> TimedEffectModules = new HashSet<string>
        {
            "abilities.rage", "abilities.resistance", "abilities.sacrifice", "abilities.decay",
            "abilities.scourge", "abilities.reconstruction", "abilities.regenerationwave", "abilities.basewave",
            "abilities.shieldextender", "abilities.shieldwave", "abilities.bioaugmentation", "abilities.weaponenhancement",
            "abilities.damageconversion", "abilities.critwave", "abilities.painttarget", "abilities.polarityfield",
            "abilities.controlledfission", "abilities.explodingnanites", "abilities.disease", "abilities.firesupport",
            "abilities.selfdestruct", "abilities.scatterbombs", "abilities.calledshot", "abilities.feedback",
            "abilities.reflection", "abilities.conversion", "abilities.corpseexplode", "abilities.cure",
            "abilities.cloakwave", "abilities.tacticalevasion", "abilities.traitor", "abilities.hack", "abilities.mindcontrol",
            "abilities.medpack"
        };

        /// <summary>
        /// Of those, the ones aimed at a friend (client targetType TARGET_FRIENDLY): another
        /// player, or the performer when nobody else is targeted.
        /// </summary>
        private static readonly HashSet<string> FriendlyEffectModules = new HashSet<string>
        {
            "abilities.shieldextender", "abilities.bioaugmentation", "abilities.weaponenhancement"
        };

        /// <summary>Of those, the ones aimed at a single enemy (client targetType TARGET_NON_FRIENDLY).</summary>
        private static readonly HashSet<string> HostileEffectModules = new HashSet<string> { "abilities.decay", "abilities.painttarget", "abilities.polarityfield", "abilities.controlledfission", "abilities.explodingnanites", "abilities.disease", "abilities.traitor", "abilities.hack", "abilities.mindcontrol" };

        /// <summary>
        /// Abilities the client marks isToggle without a sourceGameEffect or targetGameEffect
        /// (Sacrifice), or whose toggle the player may also press again while it runs (Rage): a
        /// second request while the effect is on means "off", as it does for sprint.
        /// </summary>
        private static readonly HashSet<string> ToggleModules = new HashSet<string> { "abilities.sprint", "abilities.rage", "abilities.sacrifice", "abilities.selfdestruct", PolymorphModule };

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

        private readonly MissionApplication _missionManager;

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

        /// <summary>An action's client module and its level's data - a creature's attack as much as a player's ability.</summary>
        public bool TryGetAction(ActionId actionId, uint level, out string module, out ActionLevelInfo info)
        {
            module = null;
            info = null;

            if (!_actions.TryGetValue(actionId, out var action) || !action.Levels.TryGetValue(level, out info))
                return false;

            module = action.Module;
            return true;
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

            // The dead do nothing, bar the one thing that is for them: a Machina's Self Revive
            // (SelfResAction.canDoWhileDead).
            if (player.State == CharacterState.Dead && action.Module != SelfResModule)
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedActorDead);
                return;
            }

            // Stunned or knocked down (PlayerCrowdControl): nothing until it ends.
            if (Stuns.IsStunned(player))
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // Blocked (ActionBlocks): the client greys it out and refuses it itself, and this holds
            // to that for one that asks anyway.
            if (ActionBlocks.IsBlocked(player, actionId))
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // Is it theirs to use? A skill that grants the ability at this level, or a usable
            // item in their pack whose template performs exactly this action.
            var item = packet.ItemId != 0 ? EntityManager.Instance.GetItem((ulong)packet.ItemId) : null;

            if (!Grants(player, actionId, level, item))
            {
                // The item the client names is the one its tray slot was last sent with
                // (AbilityDrawerItems), which is not always the one in the pack now: the pet was
                // deleted and bought again, the medpack stack ran out with another behind it, or
                // the slot went out with no item because there was none at login. The client
                // judges an item slot by the item class in the pack where it checks at all
                // (BaseActorAbility.CheckConsumables), not by that id, so it shows the slot ready
                // and fires it; the request was refused until a relog sent the drawer again. It
                // is performed with the item in the pack that does perform it.
                var standIn = PackItemPerforming(player, actionId, level);

                if (standIn == null)
                {
                    if (packet.ItemId != 0 && item == null)
                    {
                        Fail(client, actionId, level, PlayerMessage.PmMissingReqItem);
                        return;
                    }

                    Logger.WriteLog(LogType.Security, $"{player.FamilyName} asked for {action.Name} level {level} without a skill or item that grants it");
                    Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                    return;
                }

                item = standIn;
            }

            // A skill's ability also wants its Logos in the Tabula (AbilityLogos). An item that
            // performs the action, and a Polymorph disguise's own abilities, do not.
            if (item == null && !IsMorphAbility(player, actionId, level) && !AbilityLogos.Has(player.Logos, actionId))
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotUseAbilityNoLogos);
                return;
            }

            if (!CanResolve(action, info))
            {
                if (_reportedUnsupported.Add(actionId))
                    Logger.WriteLog(LogType.Error, $"Ability {action.Name} ({action.Module}) is not implemented on the server yet; refused.");

                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // An emote or title the player already has, a rocket still in the air (Toys).
            var toyRefusal = ToyRefusal(mapChannel, player, action, info);

            if (toyRefusal.HasValue)
            {
                Fail(client, actionId, level, toyRefusal.Value);
                return;
            }

            // A Personal Waypoint, on a map with teams, by a player on none (PersonalWaypoints).
            if (PersonalWaypoints.Is(action) && PersonalWaypoints.Refusal(player) is { } waypointRefusal)
            {
                Fail(client, actionId, level, waypointRefusal);
                return;
            }

            // The pet that is out, used again: it goes home, and nothing is performed.
            if (TryDismissPet(player, action, level))
            {
                Fail(client, actionId, level, null);
                return;
            }

            // Polymorphed: "unable to access their inventory, abilities or consumables". The client
            // locks its own UI; this is the server holding to it. Polymorph itself still ends it.
            if (IsMorphed(player) && action.Module != PolymorphModule && !IsMorphAbility(player, actionId, level))
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // "The user may have up to 3 Crab Mines active at once."
            if (action.Module == CrabMinesModule && CrabMinesOf(player) >= MaxCrabMines)
            {
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

            // Or a Bootcamp practice target, for the recruit's lightning (PracticeTargetManager).
            DynamicObject practiceTarget = null;

            if (!SelfCentred(info) && packet.Target.HasEntity && packet.Target.EntityId != player.EntityId)
            {
                target = ResolveTarget(mapChannel, packet.Target.EntityId);

                if (target == null)
                    practiceTarget = ResolvePracticeTarget(mapChannel, player, info, packet.Target.EntityId);

                if (practiceTarget != null)
                {
                    var practiceDistance = Vector3.Distance(player.Position, practiceTarget.Position);

                    if (!float.IsFinite(practiceDistance) || info.MaxRange > 0 && practiceDistance > info.MaxRange + RangeSlack)
                    {
                        Fail(client, actionId, level, PlayerMessage.PmTargetOutOfRange);
                        return;
                    }
                }
                else if (target == null)
                {
                    Fail(client, actionId, level, PlayerMessage.PmActionFailedNoTarget);
                    return;
                }

                // A corpse ability (canTargetDead) wants the body; everything else, the living.
                // A practice target has no actor behind it and was range-checked above.
                if (target == null)
                {
                }
                else if (CorpseModules.Contains(action.Module))
                {
                    if (!IsUsableCorpse(target, player))
                    {
                        Fail(client, actionId, level, PlayerMessage.PmTargetInvalid);
                        return;
                    }
                }
                else if (action.Module == "abilities.cure" && Resuscitates(level) && target is Manifestation)
                {
                    // Resuscitate wants a dead player (CureAction.CheckAction); offered a revive.
                }
                else if (MorphReviveModules.Contains(action.Module))
                {
                    // A Polymorph's Resuscitate or Jumpstart wants a dead friend it can bring back.
                    if (!IsMorphRevivable(player, action.Module, target))
                    {
                        Fail(client, actionId, level, PlayerMessage.PmTargetInvalid);
                        return;
                    }
                }
                else if (target.State == CharacterState.Dead)
                {
                    Fail(client, actionId, level, PlayerMessage.PmActionFailedTargetDead);
                    return;
                }

                if (target != null && info.MaxRange > 0 && Vector3.Distance(player.Position, target.Position) > info.MaxRange + RangeSlack)
                {
                    Fail(client, actionId, level, PlayerMessage.PmTargetOutOfRange);
                    return;
                }
            }

            // And with nobody targeted, or themselves, there is nobody to bring back.
            if (MorphReviveModules.Contains(action.Module) && target == null)
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedNoTarget);
                return;
            }

            // A point on the ground, when the ability is aimed at one: where a turret, trap, rift or
            // fire support beacon goes, or the centre of an area blast. It was taken as sent, so any
            // of those could be put anywhere on the map - in the air, inside a base, on another
            // player - with the range the table gives the ability never consulted. Held to that range
            // like an entity target; an ability whose range is 0 has no use for a point and gets none.
            Vector3? location = null;

            if (packet.Target.Kind == ActionTargetKind.Location && info.MaxRange > 0)
            {
                if (!LocationInRange(player, packet.Target.Location, info))
                {
                    Fail(client, actionId, level, PlayerMessage.PmTargetOutOfRange);
                    return;
                }

                location = packet.Target.Location;
            }

            var wantsHostile = IsDirectDamage(action, info) || HostileEffectModules.Contains(action.Module);

            // A friendly buff lands on a player; a creature is not a friend to buff. A snowball goes
            // where nullability.py's TARGET_FRIENDLY lets it: a player, or a FRIENDLY creature - a
            // soldier, a vendor - as the Snowball Launcher's does (ToolActionManager).
            if (target != null && !(target is Manifestation) &&
                (FriendlyEffectModules.Contains(action.Module) ||
                 action.Module == SnowballModule && !(target is Creature { TargetCategory: TargetCategory.Friendly })))
            {
                Fail(client, actionId, level, PlayerMessage.PmTargetInvalid);
                return;
            }

            if (wantsHostile && target == null && practiceTarget == null && !SelfCentred(info) && packet.Target.Kind != ActionTargetKind.Location)
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedNoTarget);
                return;
            }

            // A damage or debuff ability may be aimed at an enemy player across a wargame too (Pvp).
            // Hack takes a machine alone, which the client holds to (HackAction.CheckAction).
            if (wantsHostile && target != null && !IsAttackable(player, target))
            {
                Fail(client, actionId, level, PlayerMessage.PmActionFailedActorFriendly);
                return;
            }

            // An enemy's Mind Control: P2-P5 let them attack nobody, P4-P5 help nobody else
            // either (Pvp.MayNotAttack, MayNotAssist). The client knows nothing of it, so the
            // server says no.
            if (Pvp.MayNotAttack(player) && IsAttackAction(action, info)
                || Pvp.MayNotAssist(player) && !wantsHostile && !CorpseModules.Contains(action.Module) && target is Manifestation helped && helped != player)
            {
                Fail(client, actionId, level, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            // And a friendly one not at an enemy player: the client refuses it too
            // (targetedaction.py SetTarget, Actor.GetWargameParticipantStatus).
            if (!wantsHostile && FriendlyEffectModules.Contains(action.Module) && target is Manifestation foe && Pvp.AreEnemies(player, foe))
            {
                Fail(client, actionId, level, PlayerMessage.PmTargetInvalid);
                return;
            }

            // Nor at a player in a duel, a squad wargame or a team's match from outside their
            // side of it (Pvp.MayHelp). The client does not know the rule, so the server says no;
            // what gets here some other way is stopped where it lands.
            if (!wantsHostile && target is Manifestation closedTo && closedTo != player && !Pvp.MayHelp(player, closedTo))
            {
                Fail(client, actionId, level, PlayerMessage.PmTargetInvalid);
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

            // Rushing Blow's windup is the charge: timed as the client times it, the distance at
            // 70 m/s, with the performer carried to the target through it.
            var windupMs = (long)info.WindupMs;

            if (action.Module == RushingBlowModule && target != null)
            {
                windupMs = ChargeWindupMs(Vector3.Distance(player.Position, target.Position));
                StartCharge(mapChannel, client, player, target, actionId, windupMs);
            }

            mapChannel.PerformRecovery.Add(new ActionData(player, actionId, level, targetId, windupMs)
            {
                TargetObject = practiceTarget,
                TargetLocation = location,
                ItemId = item?.EntityId ?? 0
            });
        }

        /// <summary>
        /// Whether this item of the player's performs the action at this level: theirs, in their
        /// pack, not used up, and its template's action is exactly this one (ItemTemplateActions).
        /// What a tray slot holding an item has to be (ManifestationManager.RequestSetAbilitySlot).
        /// </summary>
        public bool ItemPerforms(Manifestation player, Item item, ActionId actionId, uint level) =>
            player != null && item != null && Grants(player, actionId, level, item);

        /// <summary>
        /// An item in the player's pack that performs the action at this level, or null: what a
        /// request is carried out with when the item it names is not one (RequestPerformAbility).
        /// A mission's item is passed over, as it is when the ability's cost is taken.
        /// </summary>
        private Item PackItemPerforming(Manifestation player, ActionId actionId, uint level)
        {
            foreach (var entityId in player.Inventory.PersonalInventory)
            {
                if (entityId == 0)
                    continue;

                var candidate = EntityManager.Instance.GetItem(entityId);

                if (candidate != null && Grants(player, actionId, level, candidate) &&
                    !Game.Missions.Persistence.MissionItemProtection.IsProtected(candidate, _gameUnitOfWorkFactory))
                    return candidate;
            }

            return null;
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

            // Polymorphed: the creature's combat actions are theirs while it lasts.
            if (IsMorphAbility(player, actionId, level))
                return true;

            foreach (var skill in player.Skills.Values)
                if (skill.AbilityId == (int)actionId && skill.SkillLevel >= level)
                    return true;

            return false;
        }

        /// <summary>
        /// Blocks (ActionBlocks.Unimplemented) every ability the player's skills grant that this
        /// server cannot perform at any level they have - the ones RequestPerformAbility would
        /// refuse as not implemented - and unblocks the rest, so the drawer shows them grey
        /// instead of winding up for a refusal. Run when the character enters a map (with
        /// <paramref name="send"/> false, ActionBlocks.Resend following) and when their skills
        /// change.
        /// </summary>
        public void RefreshUnimplementedBlocks(Client client, bool send = true)
        {
            var player = client?.Player;

            if (player == null)
                return;

            var blocked = new List<ActionId>();

            foreach (var skill in player.Skills.Values)
            {
                if (skill.AbilityId <= 0 || skill.SkillLevel <= 0)
                    continue;

                var actionId = (ActionId)skill.AbilityId;

                // An id the tables do not have is refused as bad data, not blocked: an empty table
                // would otherwise grey out every ability there is.
                if (!_actions.TryGetValue(actionId, out var action))
                    continue;

                if (!action.Levels.Any(l => l.Key <= (uint)skill.SkillLevel && CanResolve(action, l.Value)))
                    blocked.Add(actionId);
            }

            ActionBlocks.SetAll(client, ActionBlocks.Unimplemented, blocked, send);
        }

        /// <summary>
        /// Whether the tables know this action at this level (0 meaning no particular level): what
        /// an ability drawer slot may hold. Nothing checked the drawer, so any id and level were
        /// saved and shown to everyone who met the player.
        /// </summary>
        public bool IsKnownAction(long actionId, long level)
        {
            if (actionId <= 0 || actionId > uint.MaxValue || level < 0 || level > uint.MaxValue)
                return false;

            return _actions.TryGetValue((ActionId)actionId, out var action)
                   && (level == 0 || action.Levels.ContainsKey((uint)level));
        }

        /// <summary>The action's name from the tables, or null for an id they do not have.</summary>
        public string ActionName(ActionId actionId) => _actions.TryGetValue(actionId, out var action) ? action.Name : null;

        /// <summary>
        /// A target point that is a real position within the ability's range of the performer,
        /// height included. A NaN or infinite coordinate fails every comparison, which would have
        /// read as in range, so those are refused outright.
        /// </summary>
        private static bool LocationInRange(Manifestation player, Vector3 location, ActionLevelInfo info)
        {
            if (!float.IsFinite(location.X) || !float.IsFinite(location.Y) || !float.IsFinite(location.Z))
                return false;

            return info.MaxRange > 0 && Vector3.Distance(player.Position, location) <= info.MaxRange + RangeSlack;
        }

        /// <summary>Whether this server knows how to apply the ability; see the class remarks.</summary>
        private static bool CanResolve(ActionInfo action, ActionLevelInfo info)
        {
            return action.Module == "abilities.sprint" || action.Module == PolymorphModule || action.Module == SelfResModule || action.Module == CrabMinesModule || action.Module == RealityRipperModule || action.Module == TrapModule || action.Module == TurretModule
                || action.Module == HortimonculusModule || action.Module == ReanimationModule || action.Module == ReanimationWaveModule
                || action.Module == SpotterModule || action.Module == BotConstructionModule || action.Module == CreateCloneModule
                || MorphSupportModules.Contains(action.Module)
                || IsDirectDamage(action, info) || TimedEffectModules.Contains(action.Module)
                || IsToy(action, info) || PersonalWaypoints.Is(action);
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

            // Entity ids are global, cells are per map: a target on another map - or another copy
            // of this one - is not here.
            return IsOnMap(mapChannel, target) ? target : null;
        }

        /// <summary>The Bootcamp practice target or mission prop that the recruit's lightning can hit from here.</summary>
        private static DynamicObject ResolvePracticeTarget(
            MapChannel map, Manifestation player, ActionLevelInfo info, ulong entityId)
        {
            if (info.ActionId != ActionId.AaRecruitLightning ||
                !PracticeTargetManager.TryGetTarget(map, entityId, out var target) ||
                !PracticeTargetManager.CanHit(map, player, target) ||
                target.MissionDestruction == null && info.Level != 1)
                return null;
            return target;
        }

        private static bool IsOnMap(MapChannel mapChannel, Actor actor)
            => MapInstanceScope.Contains(mapChannel, actor);

        /// <summary>
        /// A target for a player's damage ability: a creature they may attack (IsHostile), or an
        /// enemy player across a wargame (Pvp.IsEnemyTarget).
        /// </summary>
        internal static bool IsAttackable(Manifestation player, Actor target)
        {
            return IsHostile(player, target) || Pvp.IsEnemyTarget(player, target);
        }

        /// <summary>
        /// Which creatures a player's damage may land on: HOSTILE and NEUTRAL ones (TargetCategories.
        /// PlayerMayAttack), and those that belong to an enemy of theirs across a wargame (Pvp).
        /// FRIENDLY creatures are the friendly NPCs. Players are not here: an enemy player is
        /// IsAttackable, and the areas reach them through VictimsWithin.
        /// </summary>
        internal static bool IsHostile(Manifestation player, Actor target)
        {
            return target is Creature creature && (TargetCategories.PlayerMayAttack(creature.TargetCategory) || Pvp.IsEnemySummon(player, creature))
                   && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying
                   && creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
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
            ActorManager.RefuseRequest(client, actionId, level, message);
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

            if (action.Completed)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Ability recovery {action.ActionId}/{action.ActionArgId} for character {player.Id} was already resolved.");
                return;
            }
            action.Completed = true;

            if (action.IsInrerrupted || player.State == CharacterState.Dead && actionInfo.Module != SelfResModule)
            {
                // The others are still showing the windup. The performer's client cancelled its
                // own when it sent the interrupt; a performer who died mid-windup is told, so the
                // request does not sit unresolved on their side.
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));

                if (!action.IsInrerrupted)
                    Fail(client, action.ActionId, action.ActionArgId, PlayerMessage.PmActionFailedActorDead);
                else
                    ActorManager.ResolveInterruptedRequest(client, action.ActionId, action.ActionArgId);

                return;
            }

            // The request said they could start; this says it still lands. A windup is time the
            // world goes on in, and everything the request weighed can have changed inside it.
            var refusal = StillAllowed(mapChannel, client, player, action, info);

            if (refusal.HasValue)
            {
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));
                Fail(client, action.ActionId, action.ActionArgId, refusal.Value);
                return;
            }

            // The items it takes, and the one it was used from, in one write; never a mission's.
            // An emote's flag or a title goes in the same write; a rocket or a pet is kept (Toys).
            // This is the last thing that can still refuse the ability, so it comes before the
            // costs: power or chi used to be taken first, and an ability refused here for want
            // of an item - used up or moved while the windup ran - had been paid for all the
            // same, with nothing to give it back.
            var toyCommit = new ToyCommit();

            try
            {
                ConsumeAbilityItems(client, info, KeepsSourceItem(actionInfo) ? 0 : action.ItemId, ToyWrite(actionInfo, info, player, toyCommit));
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                Logger.WriteLog(LogType.Error, $"Unable to consume ability items for character {player.Id}: {error.Message}");
                SendToOthers(mapChannel, player, new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));
                Fail(client, action.ActionId, action.ActionArgId, PlayerMessage.PmMissingReqItem);
                return;
            }

            // Paid on landing, not on asking, and once nothing can refuse it. A sustained ability
            // pays as it runs, through its effect's drain, not here.
            if (!IsSustained(info))
                TakeCosts(client, player, info);

            StartCooldown(client, player, info);

            // A Feedback on them watches what they do (Feedback).
            OnPlayerActed(mapChannel, player, IsCombatAction(actionInfo, info, action.ItemId));

            // The charge has arrived: the blow lands from where it ends.
            if (actionInfo.Module == RushingBlowModule)
                FinishCharge(player);

            if (MorphSupportModules.Contains(actionInfo.Module))
            {
                ResolveMorphSupport(mapChannel, client, player, actionInfo, info, action);
                return;
            }

            if (IsToy(actionInfo, info))
            {
                ResolveToy(mapChannel, client, player, actionInfo, info, action, toyCommit);
                return;
            }

            if (PersonalWaypoints.Is(actionInfo))
            {
                PersonalWaypoints.Deploy(mapChannel, player, info.Level, info);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (actionInfo.Module == TurretModule)
            {
                PlaceTurret(mapChannel, player, info, action, false);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (actionInfo.Module == TrapModule)
            {
                PlantTrap(mapChannel, player, info, action);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (actionInfo.Module == SpotterModule)
            {
                SummonSpotter(mapChannel, client, player, info, action);
                return;
            }

            if (actionInfo.Module == BotConstructionModule)
            {
                ConstructBot(mapChannel, client, player, info, action);
                return;
            }

            if (actionInfo.Module == CreateCloneModule)
            {
                CreateClone(mapChannel, client, player, info, action);
                return;
            }

            if (actionInfo.Module == SelfResModule)
            {
                // SelfResAction.DoAbility announces the revive on the performer's own client.
                if (PlayerDeath.SelfRevive(client))
                    CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                else
                    Fail(client, action.ActionId, action.ActionArgId, PlayerMessage.PmCannotPerformActionNow);

                return;
            }

            if (actionInfo.Module == ReanimationModule)
            {
                Reanimate(mapChannel, player, info, action);
                return;
            }

            if (actionInfo.Module == ReanimationWaveModule)
            {
                ReanimationWave(mapChannel, player, info, action);
                return;
            }

            if (actionInfo.Module == HortimonculusModule)
            {
                GrowHortimonculus(mapChannel, player, info, action);
                return;
            }

            if (actionInfo.Module == RealityRipperModule)
            {
                OpenRealityRipper(mapChannel, player, info, action);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (actionInfo.Module == CrabMinesModule)
            {
                DeployCrabMine(mapChannel, player, info, action);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (actionInfo.Module == PolymorphModule)
            {
                ResolvePolymorph(mapChannel, client, player, info, action);
                return;
            }

            if (actionInfo.Module == "abilities.sprint")
            {
                GameEffectManager.Instance.AttachSprint(mapChannel, player, info);
                CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
                return;
            }

            if (IsDirectDamage(actionInfo, info))
            {
                ResolveDirectDamage(mapChannel, client, player, actionInfo, info, action);
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
        /// What is not re-weighed: a target that died or despawned mid-windup. The ability was
        /// performed, it costs what it costs, and it hits nothing - which is what happens now.
        /// </summary>
        private void ConsumeAbilityItems(Client client, ActionLevelInfo info, ulong sourceItemId, Action<ICharUnitOfWork> alsoWrite = null)
        {
            if (sourceItemId == 0 && info.ItemRequirements.Count == 0)
            {
                if (alsoWrite != null)
                {
                    using var writeUnit = _gameUnitOfWorkFactory.CreateChar();
                    writeUnit.ExecuteTransaction(() => alsoWrite(writeUnit));
                }

                return;
            }
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
            // A kept source with requirements of quantity 0 (a pet summoner): nothing to take.
            if (quantities.Count == 0)
            {
                if (alsoWrite != null)
                    unit.ExecuteTransaction(() => alsoWrite(unit));

                return;
            }
            var consumption = new InventoryManager.InventoryConsumption();
            unit.ExecuteTransaction(() =>
            {
                consumption.PlanAndSave(client, quantities, unit);
                alsoWrite?.Invoke(unit);
            });
            consumption.Publish(client);
            foreach (var progress in consumption.ProgressEvents)
                (_missionManager ?? MissionApplication.Instance).RecordProgress(client, progress);
        }

        private PlayerMessage? StillAllowed(MapChannel mapChannel, Client client, Manifestation player, ActionData action, ActionLevelInfo info)
        {
            // Asked for with an item, so it is the item that has to still grant it - a skill the
            // player also happens to have does not stand in for the one they used.
            var item = action.ItemId != 0 ? EntityManager.Instance.GetItem((ulong)action.ItemId) : null;

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

            // A practice target still has to be the one it was aimed at, and still hittable.
            if (action.TargetObject != null)
            {
                var practiceTarget = ResolvePracticeTarget(mapChannel, player, info, action.TargetId);

                if (!ReferenceEquals(practiceTarget, action.TargetObject))
                    return PlayerMessage.PmActionFailedNoTarget;

                var practiceDistance = Vector3.Distance(player.Position, practiceTarget.Position);

                if (!float.IsFinite(practiceDistance) || info.MaxRange > 0 && practiceDistance > info.MaxRange + RangeSlack)
                    return PlayerMessage.PmTargetOutOfRange;
            }
            else if (action.TargetId != 0 && info.MaxRange > 0)
            {
                var target = ResolveTarget(mapChannel, action.TargetId);

                if (target != null && Vector3.Distance(player.Position, target.Position) > info.MaxRange + RangeSlack)
                    return PlayerMessage.PmTargetOutOfRange;
            }

            // The point too, since the performer can have walked away from it during the windup.
            if (action.TargetLocation.HasValue && !LocationInRange(player, action.TargetLocation.Value, info))
                return PlayerMessage.PmTargetOutOfRange;

            // A second emote or title item for the same one, used during the first one's windup.
            if (_actions.TryGetValue(action.ActionId, out var actionInfo) && AlreadyHasReward(player, actionInfo, info))
                return PlayerMessage.PmCannotPerformActionNow;

            return null;
        }

        internal static void TakeCosts(Client client, Manifestation player, ActionLevelInfo info)
        {
            foreach (var cost in info.Costs)
            {
                if (!player.Attributes.TryGetValue(cost.Attribute, out var attribute))
                    continue;

                attribute.Current = Math.Max(0, attribute.Current - ScaledCost(player, info, cost.Amount));

                switch (cost.Attribute)
                {
                    case Attributes.Power:
                        // With the rate the effects on them make. The client takes the amount in
                        // every UpdatePower as its rate from then on, and it is the client's own
                        // count that decides whether the next ability is asked for.
                        client.CallMethod(player.EntityId, new UpdatePowerPacket(GameEffectManager.WithRegen(player, attribute), 0));
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
        private void ResolveDirectDamage(MapChannel mapChannel, Client client, Manifestation player, ActionInfo actionInfo, ActionLevelInfo info, ActionData action)
        {
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DefaultDamageTypeOf(actionInfo.Module));
            var scaleType = info.Get(AbilityProperty.DamageScaleType);
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));

            // The recruit's lightning at a Bootcamp practice target: a hit on the target, nothing else.
            if (action.TargetObject != null)
            {
                var practiceTarget = ResolvePracticeTarget(mapChannel, player, info, action.TargetId);

                if (practiceTarget == null || !ReferenceEquals(practiceTarget, action.TargetObject))
                    return;

                var practiceAmount = GameEffectManager.ApplyDamageDealt(
                    player, Scale(player.Level, _random.Next(min, max + 1), scaleType));
                var practice = new AbilityRecoveryPacket(
                    action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.Damage)
                {
                    ArcData = true
                };
                practice.Hits.Add(new AbilityHit
                {
                    EntityId = practiceTarget.EntityId,
                    Amount = practiceAmount,
                    DamageType = damageType
                });
                ManifestationManager.Instance.EnterCombat(client);
                CellManager.Instance.CellCallMethod(mapChannel, player, practice);
                if (practiceAmount > 0)
                    PracticeTargetManager.RecordHit(
                        mapChannel, player, practiceTarget, action.ActionId, _missionManager, practiceAmount);
                return;
            }

            // Creatures a player may attack, and enemy players across a wargame (Pvp).
            var targets = new List<Actor>();
            var primary = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) : null;

            if (primary != null && !IsAttackable(player, primary))
                primary = null;

            // Lightning's bolt strikes its target alone; what else it reaches is its arc and its
            // storm (ResolveLightningExtras), not an area around the target.
            if (actionInfo.Module == "abilities.lightning")
            {
                if (primary != null)
                    targets.Add(primary);
            }
            else if (info.Has(AbilityProperty.ConeRadius))
            {
                // A cone: the action's range long, CONE_RADIUS degrees either side of the aim -
                // at the target when there is one, otherwise the way the player faces.
                var aim = primary != null
                    ? primary.Position - player.Position
                    : FacingOf(player);
                var range = Math.Max(1, info.MaxRange) + RangeSlack;

                targets.AddRange(HostilesInCone(mapChannel, player, aim, range, info.Get(AbilityProperty.ConeRadius)));
                targets.AddRange(Pvp.EnemiesInCone(mapChannel, player, aim, range, info.Get(AbilityProperty.ConeRadius)));
            }
            else if (info.Has(AbilityProperty.RadiusAroundSource))
            {
                targets.AddRange(HostilesWithin(mapChannel, player, player.Position, info.Get(AbilityProperty.RadiusAroundSource)));
                targets.AddRange(Pvp.EnemiesWithin(mapChannel, player, player.Position, info.Get(AbilityProperty.RadiusAroundSource)));
            }
            else if (info.Has(AbilityProperty.RadiusAroundTarget))
            {
                var centre = primary?.Position ?? action.TargetLocation ?? player.Position;
                targets.AddRange(HostilesWithin(mapChannel, player, centre, info.Get(AbilityProperty.RadiusAroundTarget)));
                targets.AddRange(Pvp.EnemiesWithin(mapChannel, player, centre, info.Get(AbilityProperty.RadiusAroundTarget)));

                if (primary != null && !targets.Contains(primary))
                    targets.Add(primary);
            }
            else if (primary != null)
            {
                targets.Add(primary);
            }

            // A creature attack's client class wants each hit's rawInfo as it is (KaelSmashAbility and
            // the rest); the players' abilities, DamageBase's (rawInfo, onHitData).
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId,
                RawInfoModules.Contains(actionInfo.Module) ? AbilityRecoveryPacket.HitDataKind.RawInfo : AbilityRecoveryPacket.HitDataKind.Damage)
            {
                ArcData = actionInfo.Module == "abilities.lightning"
            };

            if (targets.Count > 0)
                ManifestationManager.Instance.EnterCombat(client);

            var critChance = CriticalHits.AttackerChance(player, false);
            var stun = Stuns.OfAbility(actionInfo.Module, info);
            var knockback = info.Get(AbilityProperty.KnockbackDistance);

            foreach (var target in targets)
            {
                // Rolled, scaled to the performer's level, raised or lowered by the effects on
                // them (Rage, Sacrifice), made a crit or not, and cut by what the target's
                // effects resist.
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, _random.Next(min, max + 1), scaleType));
                var crit = CriticalHits.Resolve(player, target, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(target, rolled, out var resisted, damageType);
                var taken = ActorManager.Instance.Damage(mapChannel, target, amount, player, out var outcome, damageType);

                var hit = new AbilityHit
                {
                    EntityId = target.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = damageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
                };

                recovery.Hits.Add(hit);

                // A hit on a creature a mission counts.
                if (taken > 0 && target is Creature counted && counted.DbId != 0)
                    (_missionManager ?? MissionApplication.Instance).RecordProgress(
                        client,
                        MissionProgressEvent.AbilityHit((uint)action.ActionId, counted.DbId));

                // What the hit puts on its target is announced by the hit, as the client plays it
                // where the bolt lands (HitEffects): attached quietly and named in its entry.
                var strikeMs = ShotFlight.Ms(System.Numerics.Vector3.Distance(player.Position, target.Position), info.Get(AbilityProperty.VfxVelocity));

                // An enemy player still standing: what the hit does to a creature, with the player's
                // own stun, knockback and pull (PlayerCrowdControl), for PVP_EFFECT_DURATION_MODIFIER
                // of the time (Pvp).
                if (target is Manifestation enemy)
                {
                    if (taken > 0 && enemy.Attributes[Attributes.Health].Current > 0 && !Pvp.IsSafe(enemy))
                    {
                        using var announced = HitEffects.On(enemy, player, hit.TargetEffectIds, strikeMs);

                        if (crit && !outcome.Immune)
                            CritEffects.OnCritical(mapChannel, enemy, player, damageType, amount);

                        if (stun.Ms > 0 && Stuns.Roll(stun.Chance))
                            PlayerCrowdControl.Stun(mapChannel, enemy, player, Pvp.ScaleDuration(player, enemy, stun.Ms));

                        if (knockback > 0)
                            PlayerCrowdControl.Knockback(mapChannel, enemy, player, knockback);

                        if (actionInfo.Module == VortexModule)
                            PlayerCrowdControl.Pull(mapChannel, enemy, player, (int)info.RecoveryMs);
                    }

                    // Lightning's arc, extra sonic damage and storm.
                    if (recovery.ArcData)
                        ResolveLightningExtras(mapChannel, player, info, enemy, rolled, hit);

                    continue;
                }

                // Still standing: the stuns the hit carries - the ability's own, and an Ice or
                // Sonic crit's - each of which opens the Critical Death window if it is low enough.
                var creature = (Creature)target;

                if (creature.State != CharacterState.Dead && creature.State != CharacterState.Dying && creature.Attributes[Attributes.Health].Current > 0)
                {
                    using var announced = HitEffects.On(creature, player, hit.TargetEffectIds, strikeMs);

                    if (crit && !outcome.Immune)
                        CritEffects.OnCritical(mapChannel, creature, player, damageType, amount);

                    if (creature.State != CharacterState.Dying && stun.Ms > 0 && Stuns.Roll(stun.Chance))
                        Stuns.Apply(mapChannel, creature, player, Stuns.StunTypeId, stun.Ms, damageType);

                    // KNOCKBACK_DISTANCE: Tectonic Strike, Concussive Wave, Rushing Blow, Force Blast P6/P7.
                    if (creature.State != CharacterState.Dying && knockback > 0)
                        CrowdControl.Knockback(mapChannel, creature, player, knockback, CrowdControl.KnockbackTypeId, damageType);

                    // Vortex drags them in, over the flail the client plays on them for the
                    // action's recovery time.
                    if (creature.State != CharacterState.Dying && actionInfo.Module == VortexModule)
                        CrowdControl.Pull(mapChannel, creature, player, (int)info.RecoveryMs);
                }

                // Lightning's arc, extra sonic damage and storm (AbilityManager.Lightning.cs).
                if (recovery.ArcData)
                    ResolveLightningExtras(mapChannel, player, info, creature, rolled, hit);
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        /// <summary>
        /// The way a player faces, flat: their yaw (Actor.Rotation, the client's
        /// Movement.ViewDirection.X) in the convention creature movement uses - facing along
        /// (-sin yaw, -cos yaw).
        /// </summary>
        public static Vector3 FacingOf(Actor actor)
        {
            return new Vector3((float)-Math.Sin(actor.Rotation), 0f, (float)-Math.Cos(actor.Rotation));
        }

        /// <summary>Whether a point lies within a cone from an origin: within range, and within halfAngleDegrees of the aim on the flat.</summary>
        public static bool InCone(Vector3 origin, Vector3 aim, Vector3 point, float range, float halfAngleDegrees)
        {
            var to = new Vector2(point.X - origin.X, point.Z - origin.Z);
            var distance = to.Length();

            if (distance > range)
                return false;

            // Standing on the caster counts as in front of them.
            if (distance < 0.5f)
                return true;

            var dir = new Vector2(aim.X, aim.Z);

            if (dir.LengthSquared() < 1e-6f)
                return true;

            var cos = Vector2.Dot(Vector2.Normalize(dir), to / distance);

            return cos >= Math.Cos(halfAngleDegrees * Math.PI / 180.0);
        }

        /// <summary>Living creatures a player may attack in a cone from the performer.</summary>
        internal static List<Creature> HostilesInCone(MapChannel mapChannel, Manifestation player, Vector3 aim, float range, float halfAngleDegrees)
        {
            return HostilesWithin(mapChannel, player, player.Position, range)
                .Where(c => InCone(player.Position, aim, c.Position, range, halfAngleDegrees))
                .ToList();
        }

        /// <summary>
        /// Living creatures a player's summon goes looking for within radius metres of a point:
        /// HOSTILE only (TargetCategories.AlliesSeek) - a turret or a crab mine leaves NEUTRAL
        /// creatures alone, as a FRIENDLY creature's scan does. Blasts and areas use HostilesWithin.
        /// </summary>
        internal static List<Creature> EnemiesWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            return HostilesWithin(mapChannel, player, centre, radius)
                .Where(c => TargetCategories.AlliesSeek(c.TargetCategory) || Pvp.IsEnemySummon(player, c)).ToList();
        }

        /// <summary>
        /// What a player's summon goes looking for within radius metres of a point: the creatures
        /// EnemiesWithin finds, and the player's enemies across a wargame (Pvp).
        /// </summary>
        internal static List<Actor> FoesWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            var found = EnemiesWithin(mapChannel, player, centre, radius).Cast<Actor>().ToList();

            // A summon leaves an enemy under PvP Safety alone: its shots could not land.
            found.AddRange(Pvp.EnemiesWithin(mapChannel, player, centre, radius).Where(p => !Pvp.IsSafe(p)));
            return found;
        }

        /// <summary>
        /// Everything a player's blast or area catches within radius metres of a point: the
        /// creatures they may attack (HostilesWithin) and their enemies across a wargame (Pvp).
        /// </summary>
        internal static List<Actor> VictimsWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            var found = HostilesWithin(mapChannel, player, centre, radius).Cast<Actor>().ToList();
            found.AddRange(Pvp.EnemiesWithin(mapChannel, player, centre, radius));
            return found;
        }

        /// <summary>
        /// Stuns what a player's blast caught: a creature through Stuns, an enemy player through
        /// PlayerCrowdControl for half the time (Pvp.ScaleDuration).
        /// </summary>
        internal static bool StunVictim(MapChannel mapChannel, Actor victim, Manifestation player, int durationMs, DamageType damageType)
        {
            return victim switch
            {
                Creature creature => Stuns.Apply(mapChannel, creature, player, Stuns.StunTypeId, durationMs, damageType),
                Manifestation enemy => PlayerCrowdControl.Stun(mapChannel, enemy, player, Pvp.ScaleDuration(player, enemy, durationMs)),
                _ => false
            };
        }

        /// <summary>VictimsWithin, in a cone from the performer.</summary>
        internal static List<Actor> VictimsInCone(MapChannel mapChannel, Manifestation player, Vector3 aim, float range, float halfAngleDegrees)
        {
            var found = HostilesInCone(mapChannel, player, aim, range, halfAngleDegrees).Cast<Actor>().ToList();
            found.AddRange(Pvp.EnemiesInCone(mapChannel, player, aim, range, halfAngleDegrees));
            return found;
        }

        /// <summary>Living creatures a player may attack within radius metres of a point, from the cells around the performer.</summary>
        internal static List<Creature> HostilesWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            var found = new List<Creature>();

            if (!(radius > 0))
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var creature in cell.CreatureList)
                    if (IsHostile(player, creature) && !found.Contains(creature) && Vector3.Distance(centre, creature.Position) <= radius)
                        found.Add(creature);

            return found;
        }

        /// <summary>
        /// The performer and the living members of their squad within radius metres of them, from
        /// the cells around the performer. A player in no squad is a squad of one. This is who a
        /// "user and nearby squad members" ability reaches.
        /// </summary>
        internal static List<Manifestation> SquadWithin(MapChannel mapChannel, Manifestation player, float radius)
        {
            var found = new List<Manifestation> { player };

            // Under an enemy's Mind Control P4-P5 they help nobody but themselves (Pvp.MayNotAssist).
            if (radius <= 0 || player.PartyId == 0 || Pvp.MayNotAssist(player))
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var client in cell.ClientList)
                {
                    var other = client?.Player;

                    if (other == null || other == player || found.Contains(other))
                        continue;

                    if (other.PartyId != player.PartyId || other.State == CharacterState.Dead)
                        continue;

                    // In a duel of their own: their squad is outside it (Pvp.MayHelp).
                    if (!Pvp.MayHelp(player, other))
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

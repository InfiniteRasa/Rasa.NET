using System;
using System.Collections.Generic;
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
    /// Equipped tools: the healing disc, the field repair tool, armour augmentation, the cipher,
    /// the harvesting tools, and the snowball launcher.
    ///
    /// These are weapons as far as the inventory is concerned - armed in the weapon slot, drawn,
    /// reloaded, jammed - but firing one sends RequestToolAction rather than RequestWeaponAttack.
    /// The client makes that choice by itself from the armed item's entity class, so the server
    /// cannot steer a tool down the weapon path: it either answers this opcode or the player is
    /// disconnected by the terminator check on an opcode with no handler. 719 of the item
    /// templates we seed are tools, and two of them - the level 5-9 field repair tool and area
    /// healing disc - are on service-NPC vendors, so this is reachable by a new character.
    ///
    /// Four of the six do something: the healing disc, the field repair tool, armour augmentation
    /// and the two harvesting tools, which share one action id. The cipher is validated and then
    /// refused silently, because asking for it is not a mistake - it needs the usable-hack flow,
    /// which does not exist yet.
    ///
    /// Harvesting is the one of these that makes an item out of nothing rather than moving a
    /// number that already existed, so it is the one a forged request is worth sending. Its rules
    /// live in <see cref="Harvest"/> and every one of them is enforced twice - once at the
    /// request and again at the recovery - because the windup gives the world 800ms to change
    /// underneath a claim that was true when it was made.
    ///
    /// Validation runs whatever happens, and it is the half that has to be right either way: the
    /// client's own CheckAction is a courtesy to the player, not a constraint on the wire.
    ///
    /// A use runs in two halves, as everything with a windup does. The request settles the
    /// amount and queues an ActionData on the map channel; ActorActionManager fires
    /// <see cref="PerformRecovery"/> when the windup has run, and that is where anything changes.
    /// Ammo and heat are spent at the request, like a shot, so a windup that gets interrupted
    /// still costs what it costs.
    ///
    /// The amounts are recovered rather than invented. Each tool's entity class carries min and
    /// max damage, equal on every class of all three, and that number is what the client's own
    /// tooltip prints next to "Healing:" or "Repair:" - _AddWeaponToolInfo is handed maxDamage as
    /// its maxAmt. A level 5-9 healing disc heals 127, its field repair counterpart repairs 190,
    /// and both scale to five figures at the cap.
    ///
    /// Three things the client does that the server cannot match yet, marked ToDo rather than
    /// guessed at: creature flags (BIOLOGICAL / MECHANICAL / MACHINA) are not loaded server-side,
    /// there is no cipherable flag on dynamic objects, and the cone and radial variants are
    /// treated as single-target because ae_radius is a placeholder 1 on all 2440
    /// itemtemplate_weapon rows. All three refuse or narrow conservatively.
    /// </summary>
    public class ToolActionManager
    {
        private static ToolActionManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// The six action ids whose client module lives under <c>client/actions/tools/</c>, which
        /// is exactly the set that can arrive on this opcode. Read out of the client's own
        /// <c>actiondata.actionModules</c> table; <c>tools/bufftool.py</c> exists but no action id
        /// maps to it, so it is not here.
        /// </summary>
        public static readonly HashSet<ActionId> ToolActions = new HashSet<ActionId>
        {
            ActionId.ToolHealingDisc,           // 147, tools.healdisc
            ActionId.ToolHarvest,               // 172, tools.harvest
            ActionId.ToolFieldRepair,           // 198, tools.repairtool
            ActionId.ToolArmorAugmentation,     // 199, tools.armoraug
            ActionId.ToolCipher,                // 258, tools.cipher
            ActionId.ToolNerfweapon             // 527, tools.nerfweapon
        };

        /// <summary>
        /// The five that do something. Nerfweapon is the one left: it is the snowball launcher,
        /// and there is no effect to apply. It is refused silently, since there is nothing wrong
        /// with asking.
        /// </summary>
        public static readonly HashSet<ActionId> Applies = new HashSet<ActionId>
        {
            ActionId.ToolHealingDisc,
            ActionId.ToolFieldRepair,
            ActionId.ToolArmorAugmentation,
            ActionId.ToolHarvest,
            ActionId.ToolCipher
        };

        /// <summary>
        /// The roll. Seeded once and locked, because a fresh Random per call seeded from the
        /// clock returns the same number to every request inside a tick - which on a harvest is
        /// a player learning that spamming during one tick gives the same answer every time.
        /// </summary>
        private static readonly Random Roll = new Random();
        private static readonly object RollLock = new object();

        private static int Next(int maxExclusive)
        {
            lock (RollLock)
                return Roll.Next(maxExclusive);
        }

        /// <summary>
        /// The healing skill level at which healdisc.py sets <c>canTargetDead</c>, letting the
        /// healing disc and field repair tool be aimed at a corpse.
        /// </summary>
        public const int CorpseTargetHealingSkill = 3;

        /// <summary>
        /// The self variants, from the client's actiondata table:
        /// TOOL_USE_HEALING_DISC_SELF = 6 and TOOL_USE_FIELD_REPAIR_SELF = 4. Neither appears on
        /// any weapon class we have - the classes are all direct (1), cone (healing disc 4, field
        /// repair 2) and radial (5 and 3) - but the client will send them if a class ever does.
        /// </summary>
        public const uint HealingDiscSelf = 6;
        public const uint FieldRepairSelf = 4;

        /// <summary>
        /// Used when the tool has no itemtemplate_weapon row to give a windup. 519 of the 719
        /// tool templates have none; the 160 healing disc and field repair rows that do all read
        /// 800.
        /// </summary>
        public const long DefaultWindupMs = 800;

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public static ToolActionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ToolActionManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private ToolActionManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void RequestToolAction(Client client, RequestToolActionPacket packet)
        {
            if (client?.Player == null || client.State != ClientState.Ingame)
                return;

            var refusal = Validate(client, packet);

            if (refusal != null)
            {
                Fail(client, packet, refusal);
                return;
            }

            // The snowball launcher still does nothing, so it is refused silently - with msgId
            // None. The client cancels the action and pops it off __unresolvedActions either way;
            // what it skips is the message, which is deliberate: the player did nothing wrong,
            // and telling them off on every click of a tool that is simply not finished says less
            // than nothing.
            if (!Applies.Contains(packet.ActionId))
            {
                Fail(client, packet, null);
                return;
            }

            Begin(client, packet);
        }

        /// <summary>
        /// Starts the windup. The amount is settled here, from the tool that is armed now, and
        /// carried on the queued action - so swapping tools mid-windup cannot change what lands,
        /// and <see cref="PerformRecovery"/> checks the tool is still the same one anyway.
        /// </summary>
        private void Begin(Client client, RequestToolActionPacket packet)
        {
            var mapChannel = client.Player.MapChannel;

            if (mapChannel == null)
            {
                Fail(client, packet, null);
                return;
            }

            // Nothing else caps this list, and a client can send faster than a windup resolves -
            // so without a limit here a player could queue harvests without bound and spend the
            // world loop resolving them. Counted per player rather than per map so that one
            // client cannot crowd out everybody else's actions either.
            var queued = 0;

            foreach (var pending in mapChannel.PerformRecovery)
                if (pending.Actor == client.Player)
                    queued++;

            if (queued >= Harvest.MaxQueuedPerPlayer)
            {
                Fail(client, packet, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            var tool = InventoryManager.Instance.CurrentWeapon(client);
            var classInfo = ClassInfoOf(tool);

            // Validate established both of these; re-read rather than pass them down so there is
            // one way to get at them.
            if (classInfo == null)
            {
                Fail(client, packet, null);
                return;
            }

            // What the tool is worth, which is an amount for the three that put a number back and
            // a percentage for the two that roll. Min equals max on every class of all five of
            // those, so which column is read has never mattered - and _AddWeaponToolInfo is
            // handed maxDamage as its maxAmt, so max is the one the client prints.
            //
            // The cipher is the one place the two differ. Its ten classes run 55 to 100 in min -
            // the same ladder salvage and tissue extraction use for their chance, rising with the
            // tool's level - against a max that climbs 588 to 29025 and is a percentage of
            // nothing. So min is the decode chance, and the tooltip prints the other one.
            var amount = packet.ActionId == ActionId.ToolCipher
                ? classInfo.MinDamage
                : classInfo.MaxDamage;

            SpendShot(client, tool);

            var targetId = TargetOf(client, packet);

            // The actor's own client is already showing the windup it started; the others need
            // telling. The recovery goes to everyone, including the caster, because the amount is
            // only known here.
            SendToOthers(mapChannel, client.Player,
                new PerformWindupPacket(PerformType.ThreeArgs, packet.ActionId, packet.ActionArgId, targetId));

            var windup = tool.ItemTemplate.WeaponInfo?.Windup ?? DefaultWindupMs;

            mapChannel.PerformRecovery.Add(
                new ActionData(client.Player, packet.ActionId, packet.ActionArgId, targetId, windup)
                {
                    Args = (uint)Math.Max(amount, 0)
                });
        }

        /// <summary>
        /// Who the tool is being used on. A self variant is always the caster whatever the client
        /// named, and a request that named nobody falls back to the caster: all three of these
        /// tools are TARGET_FRIENDLY or TARGET_SELF, so there is no other sensible recipient, and
        /// the client only leaves the target out when it had none.
        /// </summary>
        private static ulong TargetOf(Client client, RequestToolActionPacket packet)
        {
            if (IsSelfVariant(packet.ActionId, packet.ActionArgId))
                return client.Player.EntityId;

            return packet.Target.HasEntity ? packet.Target.EntityId : client.Player.EntityId;
        }

        /// <summary>
        /// The arg ids from the client's actiondata table. Cone and radial are area variants, and
        /// they are treated as single-target here: ae_radius is a placeholder 1 on all 2440
        /// itemtemplate_weapon rows, so there is no honest radius to gather targets inside. With
        /// ae_type sent as None the client stops nulling their targets, so a radial disc aims at
        /// whoever the player has selected, which is the closest thing to right that the data
        /// supports.
        /// </summary>
        private static bool IsSelfVariant(ActionId actionId, uint argId)
        {
            return (actionId == ActionId.ToolHealingDisc && argId == HealingDiscSelf)
                   || (actionId == ActionId.ToolFieldRepair && argId == FieldRepairSelf);
        }

        /// <summary>Ammo and heat, exactly as firing a weapon spends them.</summary>
        private void SpendShot(Client client, Item tool)
        {
            var perShot = tool.ItemTemplate.WeaponInfo?.AmmoPerShot ?? 0;

            if (perShot > 0 && tool.CurrentAmmo >= perShot)
            {
                tool.CurrentAmmo -= perShot;
                client.CallMethod(tool.EntityId, new WeaponAmmoInfoPacket(tool.CurrentAmmo));

                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Items.UpdateAmmo(tool);
            }

            // A tool barrel heats like any other. Done after the shot is paid for, so a shot that
            // did not happen heats nothing.
            ManifestationManager.Instance.AddWeaponHeat(client, tool);
        }

        /// <summary>
        /// Called from ActorActionManager once the windup has run. What actually happens to the
        /// target, and the only place it does.
        /// </summary>
        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            var client = Server.Clients.Find(c => c.Player == action.Actor);

            // The windup was queued with a delay and the player can be gone by the time it fires.
            if (client?.Player == null || client.State != ClientState.Ingame)
                return;

            if (action.IsInrerrupted)
            {
                SendToOthers(mapChannel, action.Actor,
                    new ActionInterruptPacket(action.Actor.EntityId, action.ActionId, action.ActionArgId));
                return;
            }

            // Still holding the tool that started this? Stowing or swapping mid-windup means
            // nothing lands, which is why the check is here and not only at the request.
            var classInfo = ClassInfoOf(InventoryManager.Instance.CurrentWeapon(client));

            var stillArmed = classInfo != null
                             && classInfo.WeaponAttackActionId == action.ActionId
                             && classInfo.WeaponAttackArgId == action.ActionArgId;

            // The cipher's target is a usable rather than an actor, so it never goes through
            // ResolveTarget - and like harvesting it resolves with a message or with nothing,
            // never with hit data, since cipher.py's DoHits reads none.
            if (action.ActionId == ActionId.ToolCipher)
            {
                var cipherRefusal = stillArmed ? Decoded(client, action) : null;

                if (cipherRefusal != null)
                    Tell(client, action, cipherRefusal.Value);
                else
                    Resolve(mapChannel, action, new List<ToolHit>());

                return;
            }

            // The windup gives the target 800ms to die, be looted away, or log out.
            var target = ResolveTarget(action.TargetId);

            var amount = (int)action.Args;
            var hits = new List<ToolHit>();

            // Nothing lands if the tool was swapped away or the target is gone. Both of these used
            // to return here and send nothing, which left the client holding the action in
            // __unresolvedActions forever - so the action is resolved on the way out instead: no
            // effect and no message, but closed.
            if (!stillArmed || target == null)
            {
                Resolve(mapChannel, action, hits);
                return;
            }

            switch (action.ActionId)
            {
                case ActionId.ToolHealingDisc:
                {
                    var healed = ActorManager.Instance.Heal(target, amount, action.Actor.EntityId);

                    if (healed > 0)
                        hits.Add(new ToolHit(target.EntityId, healed));

                    break;
                }

                case ActionId.ToolArmorAugmentation:
                {
                    var armored = ActorManager.Instance.RestoreArmor(target, amount, action.Actor.EntityId);

                    if (armored > 0)
                        hits.Add(new ToolHit(target.EntityId, armored));

                    break;
                }

                case ActionId.ToolHarvest:
                {
                    // Harvesting resolves one way or the other, never both: a refusal carries the
                    // reason and pops the action off the client's unresolved list, and anything
                    // else falls through to the ordinary resolution below with no hit data -
                    // which is all harvest.py wants, since its DoHits reads none.
                    var refusal = Harvested(client, action, target as Creature);

                    if (refusal != null)
                    {
                        Tell(client, action, refusal.Value);
                        return;
                    }

                    break;
                }

                case ActionId.ToolFieldRepair:
                {
                    // Armour on a player, health on a creature. repairtool.py refuses a dead
                    // player but allows a machina, so health is what brings a downed bot back
                    // and armour is what the tool does for a trooper. The hit data carries both
                    // slots either way and the client announces whichever is non-zero.
                    var isPlayer = EntityManager.Instance.Players.ContainsKey(target.EntityId);

                    var armored = isPlayer
                        ? ActorManager.Instance.RestoreArmor(target, amount, action.Actor.EntityId)
                        : 0;

                    var healed = isPlayer
                        ? 0
                        : ActorManager.Instance.Heal(target, amount, action.Actor.EntityId);

                    if (armored > 0 || healed > 0)
                        hits.Add(new ToolHit(target.EntityId, armored, healed));

                    break;
                }
            }

            Resolve(mapChannel, action, hits);
        }

        /// <summary>
        /// Closes the action out. Sent even with nothing in it: the client's DoAction is what
        /// clears the action out of __unresolvedActions, and an empty hit list simply announces
        /// nothing. Goes to the whole cell rather than to the caster alone, because the amount is
        /// only known here.
        /// </summary>
        private static void Resolve(MapChannel mapChannel, ActionData action, List<ToolHit> hits)
        {
            CellManager.Instance.CellCallMethod(mapChannel, action.Actor,
                new ToolActionRecoveryPacket(action.ActionId, action.ActionArgId, hits));
        }

        /// <summary>
        /// One harvest attempt, resolved. Everything the request checked is checked again here,
        /// because the windup gives the world 800ms to change underneath it: the corpse can be
        /// looted and gone, the player can have walked away, someone else's attempt can have
        /// taken the last charge.
        ///
        /// The attempt is spent whether the roll succeeds or not. That is the point of counting
        /// attempts: a tool with a 55% chance retried until it works is a tool with a 100%
        /// chance, and the number of asks is the only thing limiting what one corpse is worth.
        ///
        /// What is deliberately not re-checked here is the substance and skill pair, because
        /// neither can move while this action is in flight: the substance is a flag on the
        /// creature's class, and the tool that decides which harvest this is has already been
        /// checked by <see cref="PerformRecovery"/> to still be the same action pair that started
        /// it, so the arg id that chose between Salvage and Tissue Extraction cannot have
        /// changed either.
        /// </summary>
        private PlayerMessage? Harvested(Client client, ActionData action, Creature creature)
        {
            // The corpse was looted away, or the target was never a creature to begin with.
            if (creature == null || creature.State != CharacterState.Dead)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            if (creature.HarvestOwnerEntityId != action.Actor.EntityId)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            if (creature.HarvestAttemptsLeft <= 0)
                return PlayerMessage.PmHarvestFailDepleted;

            if (Vector3.Distance(action.Actor.Position, creature.Position)
                > Harvest.MaxRange + Harvest.RangeTolerance)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            var species = SpeciesOf(creature);

            if (species == null || !HarvestYield.Has(species.Value))
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // Spent before the roll, so that a failure costs the same as a success and there is
            // nothing to be gained by aborting a harvest that is about to miss.
            creature.HarvestAttemptsLeft--;

            // The chance is the tool class's max damage, which is what the client's own tooltip
            // prints: "Salvage Chance: 55%" on a level 5 tool, rising to 100 at 50.
            var chance = (int)action.Args;

            if (Next(100) >= chance)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            var templates = HarvestYield.BySpecies[species.Value];
            var item = ItemManager.Instance.CreateFromTemplateId(templates[Next(templates.Length)], 1);

            if (item == null)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // A full pack means the harvest does not happen rather than the item vanishing into
            // one: AddItemToInventory returns null with nowhere to put it, and the item it was
            // handed is already registered, so it has to be taken back out again.
            if (InventoryManager.Instance.GrantItemToInventory(client, item) == null)
            {
                EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                return PlayerMessage.PmYourInventoryIsFull;
            }

            return null;
        }

        /// <summary>
        /// One decode attempt, resolved.
        ///
        /// Everything the request checked is checked again, through the same method, because the
        /// windup gives the world time to change underneath a claim that was true when it was
        /// made: someone else can have opened the lock, the object can have been put out of
        /// order, and the player can have walked away.
        ///
        /// Sharing <see cref="CanCipher"/> with the request rather than writing the checks out
        /// twice is deliberate here. Harvesting has two copies because the two halves genuinely
        /// read different things - a packet on one side, an ActionData on the other. A lock needs
        /// neither, so one method cannot drift from itself.
        /// </summary>
        private PlayerMessage? Decoded(Client client, ActionData action)
        {
            var obj = EntityManager.Instance.DynamicObjects.TryGetValue(action.TargetId, out var found)
                ? found
                : null;

            var refusal = CanCipher(client, obj);

            if (refusal != null)
                return refusal;

            // Spent before the roll, so that a failure costs the same as a success. Without this
            // the decode chance is decoration: a 55% cipher retried until it lands is a 100%
            // cipher that takes longer, and the tool levels stop meaning anything.
            obj.Lock.CipherAttemptsLeft--;

            // The chance is the tool class's min damage - 55 at level 5, 100 at 50.
            if (Next(100) >= (int)action.Args)
                return PlayerMessage.PmUseObjectUsableLocked;

            Unlock(obj);
            return null;
        }

        /// <summary>
        /// Whether this player may cipher this object right now, or the reason they may not.
        /// cipher.py's CheckAction and usable.py's CanCipher() behind it, server-side: the client
        /// runs both, and both read the client's own copy of the world.
        /// </summary>
        private static PlayerMessage? CanCipher(Client client, DynamicObject obj)
        {
            // "if not isinstance(target, Usable) ... PM_TARGET_INVALID". An id that resolves to
            // no object at all lands here too, which covers a request naming a creature, an item,
            // or a number nothing answers to.
            if (obj == null)
                return PlayerMessage.PmTargetInvalid;

            // On the same map. Entity ids are unique across the server, so without this a client
            // could name an object on a map it is not standing on.
            if (obj.MapContextId != client.Player.MapContextId)
                return PlayerMessage.PmTargetInvalid;

            // _SetEnabled(0) takes a usable out of service; it is not a lock to be picked.
            if (!obj.IsEnabled)
                return PlayerMessage.PmTargetInvalid;

            var usableLock = obj.Lock;

            // "not target.IsCipherable()" - no lock, no cipher level, already open, or sitting in
            // a state its lock does not apply to. All four are the same refusal to the player,
            // which is what the client would have said.
            if (usableLock == null || !usableLock.IsCipherableIn(obj.StateId))
                return PlayerMessage.PmTargetInvalid;

            // Already worked over. Checked before the range and skill rules so that a lock nobody
            // can open any more says so rather than sending the player off to train.
            if (usableLock.CipherAttemptsLeft <= 0)
                return PlayerMessage.PmUseObjectUsableLocked;

            // Standing at it. The client will not aim past the tool's range, but the entity id
            // arrives over the wire and nothing else on this path checks a distance.
            if (Vector3.Distance(client.Player.Position, obj.Position)
                > Cipher.MaxRange + Cipher.RangeTolerance)
                return PlayerMessage.PmTargetOutOfRange;

            // CanCipher(): "skillLevel >= self.__lockCipherLevel", where skillLevel is the
            // player's in the tool's own skill. That the armed tool is a cipher at all was
            // settled by the action-pair check, and all 22 cipher templates require this skill at
            // level 1 - so what is left to check is the level against the lock's.
            if (SkillLevel(client, SkillId.SpecialistTools) < usableLock.CipherLevel)
                return PlayerMessage.PmCipherFailSkillTooLow;

            return null;
        }

        /// <summary>
        /// The lock gives. Told to everyone in the cells rather than to the player who opened it,
        /// because a door that opened is open for the room - and the lock is marked open rather
        /// than merely moved, so an object that later cycles back through its locked state does
        /// not silently re-lock itself.
        /// </summary>
        private static void Unlock(DynamicObject obj)
        {
            obj.Lock.Unlocked = true;

            // A lock with no unlocked state is one whose opening is the point rather than any
            // movement - the object stays where it is and simply stops refusing.
            if (obj.Lock.UnlockedStateId != 0)
            {
                obj.StateId = obj.Lock.UnlockedStateId;
                CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(obj.StateId, 0));
            }

            // Resent so every client's own IsLocked() agrees with the server's. Without it the
            // object would look open and still answer "locked" to the next thing that asked.
            CellManager.Instance.CellCallMethod(obj, new LockInfoPacket(obj.Lock));
        }

        /// <summary>
        /// Resolves the action with a reason. harvest.py's DoHits does nothing with hit data, so
        /// what a player sees of a harvest is the message and the item - but the action still has
        /// to be resolved, or the client keeps it in __unresolvedActions forever.
        /// </summary>
        private static void Tell(Client client, ActionData action, PlayerMessage message)
        {
            client.CallMethod(client.Player.EntityId,
                new UserActionFailedPacket(action.ActionId, action.ActionArgId, message));
        }

        private static Actor ResolveTarget(ulong entityId)
        {
            if (EntityManager.Instance.Players.TryGetValue(entityId, out var player))
                return player;

            return EntityManager.Instance.GetCreature(entityId);
        }

        /// <summary>The species flag on this creature's class, or null if it has none.</summary>
        private static CreatureFlag? SpeciesOf(Creature creature)
        {
            foreach (var flag in FlagsOf(creature))
                if (flag.ToString().StartsWith("Species", StringComparison.Ordinal))
                    return flag;

            return null;
        }

        /// <summary>Biological, Mechanical, or null when the class carries neither.</summary>
        private static CreatureFlag? SubstanceOf(Creature creature)
        {
            foreach (var flag in FlagsOf(creature))
                if (flag == CreatureFlag.Biological || flag == CreatureFlag.Mechanical)
                    return flag;

            return null;
        }

        private static IEnumerable<CreatureFlag> FlagsOf(Creature creature)
        {
            if (creature != null
                && EntityClassManager.Instance.LoadedEntityClasses
                    .TryGetValue(creature.EntityClass, out var entityClass)
                && entityClass?.CreatureFlags != null)
                return entityClass.CreatureFlags;

            return System.Array.Empty<CreatureFlag>();
        }

        private static WeaponClassInfo ClassInfoOf(Item tool)
        {
            if (tool?.ItemTemplate == null)
                return null;

            return EntityClassManager.Instance.LoadedEntityClasses
                .TryGetValue(tool.ItemTemplate.Class, out var entityClass)
                ? entityClass?.WeaponClassInfo
                : null;
        }

        private static void SendToOthers(MapChannel mapChannel, Actor actor, PythonPacket packet)
        {
            foreach (var cellSeed in actor.Cells)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    if (client.Player != actor)
                        client.CallMethod(actor.EntityId, packet);
        }

        /// <summary>
        /// The message this request should be refused with, or null if there is nothing wrong
        /// with it. Ordered as the client's own CheckAction chain is, so the player sees the same
        /// reason they would have seen had their client caught it first.
        /// </summary>
        private PlayerMessage? Validate(Client client, RequestToolActionPacket packet)
        {
            // An action id from outside the tool set did not come from a tool module, whatever
            // the client claims - the six are the complete set that can reach this opcode.
            if (!ToolActions.Contains(packet.ActionId))
                return PlayerMessage.PmCannotPerformActionNow;

            if (client.Player.State == CharacterState.Dead)
                return PlayerMessage.PmActionFailedActorDead;

            // basetoolaction.py: "if not actor.IsWeaponReady(): PM_CANNOT_PERFORM_ACTION_NOW"
            if (!client.Player.WeaponReady)
                return PlayerMessage.PmCannotPerformActionNow;

            var tool = InventoryManager.Instance.CurrentWeapon(client);

            // ...then "if not isinstance(tool, Weapon): PM_INVALID_WEAPON"
            if (tool?.ItemTemplate == null)
                return PlayerMessage.PmInvalidWeapon;

            var classInfo = ClassInfoOf(tool);

            if (classInfo == null)
                return PlayerMessage.PmInvalidWeapon;

            // The client picks the action module from the armed item's own class, so a genuine
            // request always names that item's action pair. Anything else is a client claiming to
            // swing a tool it is not holding - which is the whole reason the action id may not be
            // taken at its word when it decides what happens next.
            if (classInfo.WeaponAttackActionId != packet.ActionId || classInfo.WeaponAttackArgId != packet.ActionArgId)
                return PlayerMessage.PmInvalidWeapon;

            if (tool.IsJammed)
                return PlayerMessage.PmWeaponJammed;

            // basetoolaction.py checks ammo only when the tool has an ammo class at all - "if
            // tool.GetAmmoClassId() is not None and tool.GetCurrentAmmo() <= 0". A weapon class
            // with no ammo class stores 0; 294 of the 298 tool classes do have one.
            if (classInfo.AmmoClassId != 0 && tool.CurrentAmmo <= 0)
                return PlayerMessage.PmWeaponOutOfAmmo;

            return ValidateTarget(client, packet);
        }

        private PlayerMessage? ValidateTarget(Client client, RequestToolActionPacket packet)
        {
            // No tool sets TARGET_LOCATION, so a location here did not come from a tool module.
            if (packet.Target.Kind == ActionTargetKind.Location)
                return PlayerMessage.PmTargetInvalid;

            // An area tool sets TARGET_NONE and sends None; the cipher and the harvest tools
            // always need something to point at.
            if (!packet.Target.HasEntity)
            {
                return packet.ActionId switch
                {
                    ActionId.ToolHarvest => PlayerMessage.PmActionFailedNoTarget,
                    ActionId.ToolCipher => PlayerMessage.PmActionFailedNoTarget,
                    _ => null
                };
            }

            var targetId = packet.Target.EntityId;

            if (packet.ActionId == ActionId.ToolCipher)
                return ValidateCipherTarget(client, targetId);

            var creature = EntityManager.Instance.GetCreature(targetId);
            var player = EntityManager.Instance.Players.TryGetValue(targetId, out var targetPlayer)
                ? targetPlayer
                : null;

            if (creature == null && player == null)
                return PlayerMessage.PmActionFailedNoTarget;

            Actor targetActor = creature != null ? creature : player;

            if (packet.ActionId == ActionId.ToolHarvest)
                return ValidateHarvestTarget(client, packet, creature, targetId);

            // healdisc, repairtool, armoraug and nerfweapon are all TARGET_FRIENDLY or
            // TARGET_SELF. ToDo: target category - HOSTILE creatures should be refused here, but
            // faction and the client's target categories do not line up yet.

            // repairtool.py refuses a dead player outright; healdisc.py allows a corpse only at
            // Healing 3 or better. ToDo: repairtool also refuses dead BIOLOGICAL creatures, which
            // needs creature flags.
            if (targetActor.State == CharacterState.Dead)
            {
                if (packet.ActionId == ActionId.ToolFieldRepair && player != null)
                    return PlayerMessage.PmTargetInvalid;

                if (packet.ActionId == ActionId.ToolHealingDisc && !CanTargetCorpse(client))
                    return PlayerMessage.PmActionFailedTargetDead;

                if (packet.ActionId == ActionId.ToolArmorAugmentation || packet.ActionId == ActionId.ToolNerfweapon)
                    return PlayerMessage.PmActionFailedTargetDead;
            }

            return null;
        }

        private static PlayerMessage? ValidateHarvestTarget(Client client, RequestToolActionPacket packet,
            Creature creature, ulong targetId)
        {
            // harvest.py is TARGET_NON_SELF.
            if (targetId == client.Player.EntityId)
                return PlayerMessage.PmTargetInvalid;

            // "if not isinstance(target, _creature.Creature): PM_HARVEST_FAIL_NOT_HARVESTABLE",
            // and the same message again for a target that is not dead. Harvesting is done to a
            // corpse; a live one is not harvestable yet rather than invalid.
            if (creature == null || creature.State != CharacterState.Dead)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // The arg id says which harvest this is, and it is the class's own - the action-pair
            // check above already refused anything else - so this only rejects the one broken
            // weapon_class row that carries a harvest action with an arg that is not a skill.
            if (packet.ActionArgId != Harvest.SalvageArg && packet.ActionArgId != Harvest.TissueExtractionArg)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // Whose kill it was. Corpse looting is already owner-only and harvesting hands out
            // items the same way, so it follows the same rule - and a corpse nobody earned, which
            // carries owner 0, belongs to nobody.
            if (creature.HarvestOwnerEntityId == 0 || creature.HarvestOwnerEntityId != client.Player.EntityId)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // Already stripped. Checked before the substance and skill rules so that a depleted
            // corpse says so rather than sending the player off to find a different tool.
            if (creature.HarvestAttemptsLeft <= 0)
                return PlayerMessage.PmHarvestFailDepleted;

            // Standing over it. The client will not aim past the tool's range, but the entity id
            // arrives over the wire, and nothing else on this server checks a distance.
            if (Vector3.Distance(client.Player.Position, creature.Position)
                > Harvest.MaxRange + Harvest.RangeTolerance)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            var species = SpeciesOf(creature);

            // harvest.py: Salvage refuses a BIOLOGICAL creature, Tissue Extraction refuses a
            // MECHANICAL one. A creature with no flags at all - the 128 classes no species was
            // matched to - is refused by both, since nothing is known about what it is made of.
            var substance = SubstanceOf(creature);

            if (substance == null)
                return PlayerMessage.PmHarvestFailNotHarvestable;

            if (packet.ActionArgId == Harvest.SalvageArg && substance == CreatureFlag.Biological)
                return PlayerMessage.PmTargetInvalid;

            if (packet.ActionArgId == Harvest.TissueExtractionArg && substance == CreatureFlag.Mechanical)
                return PlayerMessage.PmTargetInvalid;

            // Nothing is known to come off this species. Refused before the roll so the player is
            // not charged ammo and an attempt for a corpse that could never have paid out.
            if (species == null || !HarvestYield.Has(species.Value))
                return PlayerMessage.PmHarvestFailNotHarvestable;

            // No skill check. There is nothing to check: harvest.py has none, none of the 48
            // harvest tool templates carry a skill requirement, and 168 and 169 are arg ids
            // rather than skill ids whatever harvest.py calls its constants - skilldata has no
            // such skills, so a check for one refused every harvest ever made. What gates a
            // harvest is holding the tool, and the item requirements gate that.
            return null;
        }

        private static PlayerMessage? ValidateCipherTarget(Client client, ulong targetId)
        {
            return CanCipher(client,
                EntityManager.Instance.DynamicObjects.TryGetValue(targetId, out var obj) ? obj : null);
        }

        private static bool CanTargetCorpse(Client client)
        {
            return SkillLevel(client, SkillId.SpecialistTools) >= CorpseTargetHealingSkill;
        }

        /// <summary>This player's level in a skill, or 0 if they have never trained it.</summary>
        private static int SkillLevel(Client client, SkillId skillId)
        {
            return client.Player.Skills.TryGetValue(skillId, out var skill) ? skill.SkillLevel : 0;
        }

        private static void Fail(Client client, RequestToolActionPacket packet, PlayerMessage? message)
        {
            client.CallMethod(client.Player.EntityId,
                new UserActionFailedPacket(packet.ActionId, packet.ActionArgId, message));
        }
    }
}

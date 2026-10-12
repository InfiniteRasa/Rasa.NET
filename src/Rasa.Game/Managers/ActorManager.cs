using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class ActorManager
    {
        /*      Actor Packets:
         *  - PreloadData(self, weaponId, abilities)
         *  - AppearanceData(self, appearanceData)
         *  - Level(self, level)
         *  - AttributeInfo(self, attrDict)
         *  - ResistanceData(self, resistDataDict)
         *  - TargetCategory(self, targetCategory)
         *  - ToBePerceivedModifier(self, mod)
         *  - ToPerceiveModifier(self, mod)
         *  - UpdateHealth(self, current, currentMax, refreshAmount, whoId)
         *  - UpdatePower(self, current, currentMax, refreshAmount, whoId)
         *  - UpdateArmor(self, current, currentMax, refreshAmount, whoId)
         *  - UpdateChi(self, current, currentMax, refreshAmount, whoId)
         *  - UpdateAttributes(self, attributeDataList, whoId)
         *  - UpdateRegions(self, regionIdList)
         *  - ActionBlockChange(self, actionId, isBlocked)                   => implemented, ActionBlocks
         *  - PerformWindup(self, actionId, actionArgId, *args)
         *  - PerformRecovery(self, actionId, actionArgId, *args)
         *  - StateChange(self, stateIdList)
         *  - StateCorrection(self, stateList)
         *  - Abilities(self, abilityList)
         *  - Skills(self, skillList)
         *  - UserActionFailed(self, actionId, actionArgId, msgId)
         *  - ActionFailed(self, actionId, actionArgId)                        => implemented, RefuseRequest
         *  - PreTeleport(self, teleportType = None)
         *  - TeleportFailed(self)
         *  - PostTeleport(self)
         *  - TeleportArrival(self, delayMs = 0, delayEffectMs = 0, doFade = 0)
         *  - Teleport(self, position, yaw, teleportType = None, delay = 0, doCancel = 1)
         *  - SetTrackingTarget(self, targetId)
         *  - WeaponReady(self, isWeaponReady)
         *  - EquipmentInfo(self, equipmentInfo)
         *  - IsRunning(self, isRunning)
         *  - ActorInfo(self, stateIds, yaw, trackingTarget, movementMod, desiredPostureId, isHoldingCombatMode)
         *  - ActorControllerInfo(self, isPlayer)
         *  - ActorName(self, name)
         *  - SetDesiredCrouchState(self, desiredStateId)
         *  - RequestVisualCombatMode(self, goToCombatMode)
         *  - LevelUp(self, newLevel)
         *  - PlayerDead(self, sourceId, graveyardList, canRevive = 0)   => implemented (PlayerDeath)
         *  - AnnounceMapDamage(self, rawInfo)
         *  - MadeDead(self)
         *  - ActorKilled(self)
         *  - Revived(self, sourceId)
         *  - DeadOnArrival(self, canRevive)
         *  - ActionReuseTimes(self, reuseTimeList)
         *  - ActionReuseTimerRestarted(self, actionId, actionArgId)
         *  - TargetId(self, targetId)
         *  - ActionInterrupt(self, sourceId, actionId, actionArgId)
         *  - MovementModChange(self, newMovementMod)
         *  - WargameData(self, wargameData)
         *  
         *      Actor Hanlders:
         *  - BuryMe                    => implemented, PlayerDeath (to the hospital)
         *  - ReviveMe                  => implemented, PlayerDeath (to the hospital chosen)
         *  - RequestActionInterrupt    => ToDo
         *  - RequestDetachGameEffect   => gesture effects only, GestureManager
         *  - RequestVisualCombatMode   => implemented, RequestVisualCombatMode (relayed to the others)
         *  - SetDesiredCrouchState     => implemented, ManifestationManager
         *  - TeleportAcknowledge       => ToDo
         */

        private static ActorManager _instance;
        private static readonly object InstanceLock = new object();
        public static ActorManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ActorManager();
                    }
                }

                return _instance;
            }
        }

        private ActorManager()
        {
        }

        /// <summary>
        /// Refuses a request the player made: UserActionFailed to show why (or nothing, for a null
        /// message) and take the request off the client's unresolved list, then ActionFailed to
        /// cancel the action their client started on its own - its windup, or the recovery it
        /// plays locally once the windup has run. The one without the other either left the
        /// character performing an action that never happened or left the request pending.
        /// Only the player is told: nobody else is shown an action before the server accepts it.
        /// </summary>
        public static void RefuseRequest(Client client, ActionId actionId, uint actionArgId, PlayerMessage? message)
        {
            if (client?.Player == null)
                return;

            client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(actionId, actionArgId, message));
            client.CallMethod(client.Player.EntityId, new ActionFailedPacket(actionId, actionArgId));
        }

        /// <summary>
        /// Closes a request the player interrupted. Their client cancelled the action itself when
        /// it sent RequestActionInterrupt, but keeps the request on its unresolved list until the
        /// server answers it; a silent UserActionFailed is that answer. No ActionFailed: there is
        /// nothing left to cancel, and it could only catch a newer action with the same id.
        /// </summary>
        public static void ResolveInterruptedRequest(Client client, ActionId actionId, uint actionArgId)
        {
            if (client?.Player == null)
                return;

            client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(actionId, actionArgId, null));
        }
        #region Handlers

        public void RequestActionInterrupt(Client client, RequestActionInterruptPacket packet)
        {
            foreach (var action in client.Player.MapChannel.PerformRecovery)
                if (action.Actor == client.Player)
                    if (action.ActionId == packet.ActionId)
                        if (action.ActionArgId == packet.ActionArgId)
                        {
                            action.IsInrerrupted = true;
                            break;
                        }
        }

        /// <summary>
        /// RequestVisualCombatMode from the player's own client: the stance it asks to hold. The
        /// engine sends it on every camera profile change - whether the profile locks facing - and
        /// the client has already taken the stance itself ("Notify server so that it can notify
        /// other clients").
        /// </summary>
        public void RequestVisualCombatMode(Client client, bool combatMode)
        {
            client.Player.RequestedCombatMode = combatMode;
            UpdateCombatMode(client);
        }

        /// <summary>Auto-fire starting or stopping: the trigger held is a stance held, to the others who see the player.</summary>
        public void SetAutoFireCombatMode(Client client, bool firing)
        {
            client.Player.AutoFireCombatMode = firing;
            UpdateCombatMode(client);
        }

        /// <summary>The stance a player holds: the one their client asked for, or the trigger held down.</summary>
        public static bool CombatModeOf(Manifestation player) => player.RequestedCombatMode || player.AutoFireCombatMode;

        /// <summary>
        /// Keeps InCombatMode - what ActorInfo gives a newcomer as isHoldingCombatMode - to
        /// CombatModeOf, and tells everyone else who can see the player when it changes. Never the
        /// player: their client holds its own stance, and the auto-fire half of it arriving there
        /// once took away the hold its camera had set (Recv_RequestVisualCombatMode(False) removes
        /// the one hold there is).
        /// </summary>
        private static void UpdateCombatMode(Client client)
        {
            var player = client.Player;
            var mode = CombatModeOf(player);

            if (mode == player.InCombatMode)
                return;

            player.InCombatMode = mode;

            if (client.State == ClientState.Ingame && player.MapChannel != null)
                client.CellIgnoreSelfCallMethod(client, new RequestVisualCombatModePacket(mode));
        }

        #endregion

        #region Health

        /// <summary>
        /// Raises an actor's health and tells everyone who can see them. Returns how much was
        /// actually put back, which is not what was asked for when the actor was nearly full.
        ///
        /// This is the one place health goes up. Everything that heals - an ability, armour or
        /// health regeneration, a medkit, a console command - goes through here, so the clamp,
        /// the refusal to heal the dead and the update to every onlooker are decided once.
        /// </summary>
        /// <param name="sourceEntityId">
        /// Whose doing it was, and it decides whether the client announces the change itself.
        /// Actor.UpdateAttribute in the client starts with
        /// "announce = not entitymanager.HasEntity(whoId)": if the client knows the entity it
        /// stays quiet, because whatever that entity did is expected to announce the healing
        /// with the source attached - HealAbility.DoAbility walks its own hit list and calls
        /// target.AnnounceHealing(sourceId, amount) client-side. If the client does not know the
        /// entity, and 0 is never a real one, it announces the change itself. So pass the
        /// healer's entity id when a visible actor's ability did it, and leave it 0 for
        /// regeneration, a command, or anything else with no actor behind it.
        /// </param>
        public int Heal(Actor target, int amount, ulong sourceEntityId = 0, ulong hateEntityId = 0)
        {
            if (target == null || amount <= 0)
                return 0;

            if (!target.Attributes.TryGetValue(Attributes.Health, out var health))
                return 0;

            // Healing the dead is not healing. Health above zero would not bring them back -
            // nothing about their state would have changed - and it would leave a corpse
            // standing there with a full bar. Coming back is Revived, which is not wired yet.
            if (target.State == CharacterState.Dead || health.Current <= 0)
                return 0;

            // Disease P5: "All Healing: Disabled".
            if (GameEffectManager.HealingBlocked(target))
                return 0;

            // Under an enemy's Mind Control P4-P5 a player heals nobody but themselves (Pvp.MayNotAssist).
            if (sourceEntityId != 0 && sourceEntityId != target.EntityId
                && EntityManager.Instance.Actors.TryGetValue(sourceEntityId, out var healer) && Pvp.MayNotAssist(healer))
                return 0;

            // A player in a duel, a squad wargame or a team's match is healed by their own side of
            // it and nobody else (Pvp.MayHelp).
            if (sourceEntityId != 0 && sourceEntityId != target.EntityId
                && EntityManager.Instance.Actors.TryGetValue(sourceEntityId, out var outsider) && !Pvp.MayHelp(outsider, target))
                return 0;

            // A player's heal on a player in a fight with another player: PVP_HEALING_MODIFIER of it (Pvp).
            if (sourceEntityId != 0 && target is Manifestation)
                amount = Pvp.ScaleHealing(target, EntityManager.Instance.GetActor(sourceEntityId), amount);

            var applied = Math.Min(amount, health.CurrentMax - health.Current);

            if (applied <= 0)
                return 0;

            health.Current += applied;

            // For the healer's row of a battleground's scoreboard.
            Battlegrounds.Instance.Healed(sourceEntityId, target, applied);

            var mapChannel = target switch
            {
                Manifestation player => player.MapChannel,
                _ => target.RuntimeMapChannel
            };

            // Healing someone creatures hate draws their hate to the healer - or to what did the
            // healing for them, when that is no actor (a Hortimonculus: hateEntityId).
            if (hateEntityId != 0 && target is Manifestation)
                Threat.FromHealing(mapChannel, hateEntityId, EntityManager.Instance.GetActor(sourceEntityId), target, applied);
            else if (sourceEntityId != 0 && sourceEntityId != target.EntityId && target is Manifestation)
                Threat.FromHealing(mapChannel, EntityManager.Instance.GetActor(sourceEntityId), target, applied);

            // No map means nobody can see them, which is not a reason to refuse the heal - the
            // health is still theirs. It is a reason not to try to broadcast it.
            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, target, new UpdateHealthPacket(health, sourceEntityId));

            return applied;
        }

        /// <summary>
        /// "Immune" over the target for everyone around (GameEffectAttachFailed, IMMUNE, which the
        /// client turns into COMBAT_IMMUNE_ANNOUNCED): a hit it does not take, for a caller with no
        /// hit record of its own to carry wasImmune.
        /// </summary>
        public static void AnnounceImmune(MapChannel mapChannel, Actor target, Actor source)
        {
            if (mapChannel == null || target == null)
                return;

            CellManager.Instance.CellCallMethod(mapChannel, target,
                new GameEffectAttachFailedPacket(0, GameEffectAttachFailedPacket.FailReason.Immune, source?.EntityId ?? 0));
        }

        /// <summary>
        /// Takes health from an actor, armour first: damage eats the armour bar until it is
        /// empty and the rest comes off health. Returns what was actually taken off health and
        /// armour together. A creature brought to zero is killed and its killer credited; one
        /// that survives and was minding its own business turns on the attacker. A player brought
        /// to zero dies, unless a duel defeats them or they are a GM who may not die (PlayerDeath).
        /// A player's hit on a player is PvP (Pvp): halved, and stopped by PvP Safety.
        /// </summary>
        /// <param name="source">Who did it; credited with a kill, and what a surviving creature turns on.</param>
        /// <param name="damageType">What it was, for the death animation of a creature it brings to its Critical Death window.</param>
        /// <param name="isPeriodic">Ticks contribute damage credit without issuing a new escort attack order.</param>
        public int Damage(MapChannel mapChannel, Actor target, int amount, Actor source, DamageType damageType = DamageType.Physical, bool isPeriodic = false)
        {
            var taken = Damage(mapChannel, target, amount, source, out var outcome, damageType, isPeriodic);

            // No hit record here to carry wasImmune, so the clients are told on their own.
            if (outcome.Immune)
                AnnounceImmune(mapChannel, target, source);

            return taken;
        }

        /// <summary>
        /// Damage, with what became of it for the hit the caller reports (DamageOutcome). A target
        /// immune to it (DamageImmunity: an invulnerable or leashing creature, a type or blanket
        /// immunity, an effect granting one) takes nothing, and the hit is reported with
        /// wasImmune so the clients show "Immune" as it lands. Otherwise a shield (Shield
        /// Extender, Shield Wave: GameEffectManager.ApplyAbsorb) takes its share before the
        /// armour, as it does of weapon fire in MissileManager: what comes in here is the amount
        /// after resistance, and the hit to report is outcome.Delivered with outcome.Absorbed
        /// beside it. Falling is not combat damage and does not come through here
        /// (FallDamage.Apply): a shield does not soften a fall, nor does an immunity stop one.
        /// </summary>
        /// <param name="armorBypassPercent">Percent of the hit that goes past the armour for the attacker's own part - a constant-fire weapon's "Armor Piercing" modules (ItemModuleBonuses) - on top of the target's Target Painting.</param>
        public int Damage(MapChannel mapChannel, Actor target, int amount, Actor source, out DamageOutcome outcome, DamageType damageType = DamageType.Physical, bool isPeriodic = false, int armorBypassPercent = 0)
        {
            outcome = new DamageOutcome { Delivered = amount };

            if (target == null || amount <= 0 || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return 0;

            if (source is Creature companion &&
                (companion.SpawnPool?.FollowOwnerCharacterId > 0 || Game.Missions.World.CreatureGameplayRules.IsDefender(companion)) &&
                target is Creature enemy && !CreatureManager.IsHostileTarget(mapChannel, companion, enemy))
                return 0;

            if (!target.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                return 0;

            // A player's damage, or their creature's, lands on another player only across a
            // wargame they are on opposite sides of (Pvp.AreEnemies). What a duel, a feud or a
            // battleground left behind used to go on landing after it ended - a damage-over-time
            // effect ticking on, a turret on its old aim, a shot in flight - and could kill, with
            // Rez Trauma and equipment wear as any death. It lands as Immune, as a hit PvP Safety
            // stops.
            if (Pvp.IsPvp(source, target) && !Pvp.AreEnemies(Pvp.Controller(source), (Manifestation)target))
            {
                outcome = new DamageOutcome { Immune = true };
                return 0;
            }

            // Immune: nothing taken, nothing started - a creature running home after a leash
            // (BehaviorManager.Leash) does not turn round for it. Nor a player's hit on a player
            // who holds PvP Safety. A fresh hit on an enemy player ends the attacker's own Safety
            // (Pvp.Attack); a tick of something started earlier does not.
            if (DamageImmunity.IsImmune(target, damageType)
                || (isPeriodic ? Pvp.Shielded(source, target) : Pvp.Attack(mapChannel, source, target)))
            {
                outcome = new DamageOutcome { Immune = true };
                return 0;
            }

            // A player's hit on a player does PVP_DAMAGE_MODIFIER of itself (Pvp).
            if (Pvp.IsPvp(source, target))
            {
                amount = Pvp.ScaleDamage(source, target, amount);
                outcome = new DamageOutcome { Delivered = amount };
                Pvp.RecordEngagement(source, target);
                Pvp.OnHit(mapChannel, source, target);
                Battlegrounds.Instance.Damaged(source, target, amount);
            }

            if (!isPeriodic && target is Creature attackedCreature)
                CreatureManager.RecordOwnerAttack(mapChannel, source, attackedCreature);

            // A shield takes its share first; a hit it takes all of goes no further.
            amount = GameEffectManager.Instance.ApplyAbsorb(mapChannel, target, amount, out var absorbed);
            outcome = new DamageOutcome { Delivered = amount, Absorbed = absorbed };

            if (amount <= 0)
                return 0;

            var armorTaken = 0;

            // Armour first - unless an EMP crit is suppressing it, when it all goes to health.
            if (target.Attributes.TryGetValue(Attributes.Armor, out var armor) && armor.Current > 0 && !GameEffectManager.ArmorSuppressed(target))
            {
                // Target Painting: that share of the hit goes past the armour.
                // And the attacker's own piercing, the two added and at most all of it.
                var bypass = Math.Min(100, GameEffectManager.ArmorPiercePercentOf(target) + Math.Max(0, armorBypassPercent));

                armorTaken = Math.Min(amount - amount * bypass / 100, armor.Current);
                armor.Current -= armorTaken;
                CellManager.Instance.CellCallMethod(mapChannel, target, target is Creature
                    ? new UpdateArmorPacket(GameEffectManager.WithRegen(target, armor), target.EntityId)
                    : new UpdateArmorPacket(armor, 0));
            }

            var healthTaken = Math.Min(amount - armorTaken, health.Current);
            if (target is Creature damagedCreature)
                CreatureManager.RecordCombatDamage(mapChannel, damagedCreature, source, armorTaken + healthTaken);
            health.Current -= healthTaken;
            CellManager.Instance.CellCallMethod(mapChannel, target, new UpdateHealthPacket(health, target is Creature ? target.EntityId : 0));

            // A hit a player endures wears the armour they have on (Durability) - a creature's
            // lightning, an effect's tick, as weapon fire does in MissileManager.
            if (target is Manifestation struck && armorTaken + healthTaken > 0)
                Durability.WearArmor(mapChannel, struck);

            if (target is Creature creature)
            {
                if (health.Current <= 0)
                {
                    // No regeneration for the dead: health and armour stay where they fell.
                    health.Current = 0;
                    health.RefreshAmount = 0;
                    health.RefreshPeriod = 0;

                    if (armor != null)
                    {
                        armor.Current = 0;
                        armor.RefreshAmount = 0;
                        armor.RefreshPeriod = 0;
                    }

                    if (source != null)
                        CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, source);
                    else
                        Logger.WriteLog(LogType.Error, $"Creature {creature.EntityId} was killed with no source to credit; it stays at zero.");
                }
                else if (CritDeathManager.Instance.TryEnterPreDeath(mapChannel, creature, source, damageType))
                {
                    // Held near death for a finisher; it does not turn on anyone.
                }
                else if (source != null)
                {
                    // The source is hated for what landed - the resistance came off before this
                    // was called and is not known here - and a wandering creature turns on them.
                    Threat.FromDamage(creature, source, armorTaken + healthTaken);
                }

                // Explosive Nanites go off on damage taken.
                if (healthTaken + armorTaken > 0 && health.Current > 0)
                    AbilityManager.OnCreatureDamaged(mapChannel, creature);
            }
            else if (target is Manifestation victim && armorTaken + healthTaken > 0)
            {
                // Self Destruct goes off on the next damage its holder takes, and Conversion
                // turns what landed into healing for the squad.
                AbilityManager.OnPlayerDamaged(mapChannel, victim, armorTaken + healthTaken);

                // A hit, not a tick, is cried at (PlayerVoices); one that brings them down is not.
                if (!isPeriodic && health.Current > 0)
                    PlayerVoices.Hurt(victim, armorTaken + healthTaken);
            }

            // A player at zero dies - or is defeated, or stands back up (PlayerDeath).
            if (target is Manifestation fallen && health.Current <= 0)
                PlayerDeath.AtZero(mapChannel, fallen, source);

            return armorTaken + healthTaken;
        }

        /// <summary>
        /// One second of regeneration for every player on the map: health, armour and power
        /// each gain their RefreshAmount once every RefreshPeriod seconds, up to their
        /// maximum. Nothing is sent - the client predicts the same thing from the RefreshAmount
        /// and RefreshPeriod it was last given (ActorAttribute adds elapsed * amount / period), so
        /// the two sides move together, and the next real update - a cost, a hit - carries the
        /// exact value. Without this the server's copy never moved: a cost check read a power
        /// bar that was full at map entry and only ever went down. The C++ server did this for
        /// health and armour in manifestation_updatePlayer; power is the interim rule described
        /// at UpdateStatsValues. Chi (adrenaline) is not regenerated: it is gained on kills
        /// (ManifestationManager.GainAdrenaline) and spent by sprint and the like. In combat the
        /// health period is five times longer (CombatRegen), which this honours by ticking it
        /// every fifth second, and armour's amount is 0 (ManifestationManager.ApplyRegenPeriod).
        /// </summary>
        public void Regenerate(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList)
            {
                var player = client?.Player;

                if (player == null || player.State == CharacterState.Dead)
                    continue;

                if (!player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                    continue;

                player.RegenSeconds++;

                Regenerate(player, health, player.RegenSeconds);

                if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
                    Regenerate(player, armor, player.RegenSeconds);

                if (player.Attributes.TryGetValue(Attributes.Power, out var power))
                    Regenerate(player, power, player.RegenSeconds);
            }

            // Creatures' armour, and their health out of a fight (CreatureHealth).
            CreatureArmor.Regenerate(mapChannel);
        }

        /// <summary>
        /// One attribute's regeneration for one second. The amount is the attribute's own
        /// RefreshAmount as the effects on the player make it - Regeneration Wave and Base Wave
        /// multiply it (GameEffectManager.RegenAmount), and the client was told the same figure
        /// when the effect went on, so the two sides still move together.
        /// </summary>
        private static void Regenerate(Actor actor, ActorAttributes attribute, long second)
        {
            var amount = GameEffectManager.RegenAmount(actor, attribute);

            if (amount <= 0 || attribute.Current >= attribute.CurrentMax)
                return;

            // Never 0 once the stats have been worked out (ManifestationManager.ApplyRegenPeriod);
            // this is only a guard against dividing by one.
            var period = Math.Max(1, attribute.RefreshPeriod);

            if (second % period != 0)
                return;

            attribute.Current = Math.Min(attribute.CurrentMax, attribute.Current + amount);
        }

        /// <summary>Puts an actor back to its maximum, and says how much that took.</summary>
        public int HealToFull(Actor target, ulong sourceEntityId = 0)
        {
            if (target == null || !target.Attributes.TryGetValue(Attributes.Health, out var health))
                return 0;

            return Heal(target, health.CurrentMax, sourceEntityId);
        }

        /// <summary>
        /// Raises an actor's armour and tells everyone who can see them, returning how much went
        /// back on. The armour counterpart of <see cref="Heal"/>, and the same reasoning applies
        /// to all of it: one place for the clamp, one for the broadcast, and the dead are
        /// refused - MissileManager zeroes armour and its regeneration on death, so putting a
        /// number back would leave a corpse wearing armour that never ticks.
        ///
        /// <paramref name="sourceEntityId"/> works as it does for healing: an entity the client
        /// knows means the client leaves the announcement to whatever that entity is doing.
        /// </summary>
        public int RestoreArmor(Actor target, int amount, ulong sourceEntityId = 0)
        {
            if (target == null || amount <= 0)
                return 0;

            if (!target.Attributes.TryGetValue(Attributes.Armor, out var armor))
                return 0;

            if (target.State == CharacterState.Dead)
                return 0;

            // As with healing: nobody outside their side of a closed wargame (Pvp.MayHelp).
            if (sourceEntityId != 0 && sourceEntityId != target.EntityId
                && EntityManager.Instance.Actors.TryGetValue(sourceEntityId, out var outsider) && !Pvp.MayHelp(outsider, target))
                return 0;

            var applied = Math.Min(amount, armor.CurrentMax - armor.Current);

            if (applied <= 0)
                return 0;

            armor.Current += applied;

            var mapChannel = target switch
            {
                Manifestation player => player.MapChannel,
                _ => target.RuntimeMapChannel
            };

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, target, new UpdateArmorPacket(armor, target.EntityId));

            return applied;
        }

        #endregion

        #region State correction

        /// <summary>
        /// Puts the actor's states right on everyone who can see it, its own client included
        /// (StateCorrection, which the client applies without the filters StateChange has).
        ///
        /// Of the server's own copy only the posture is kept here - CROUCHED or STANDING sets
        /// IsCrouching - since that is all a correction on its own can mean to the rules. Alive
        /// or dead is the business of whoever brings an actor back or kills it, with its health;
        /// a DEAD or NORMAL sent through this changes how the actor looks, not what it is.
        /// </summary>
        public static void CorrectState(MapChannel mapChannel, Actor actor, IEnumerable<CharacterState> states)
        {
            if (mapChannel == null || actor == null)
                return;

            var list = states?.Distinct().ToList() ?? new List<CharacterState>();

            if (list.Count == 0)
                return;

            if (list.Contains(CharacterState.Crouched))
                actor.IsCrouching = true;
            else if (list.Contains(CharacterState.Standing))
                actor.IsCrouching = false;

            CellManager.Instance.CellCallMethod(mapChannel, actor, new StateCorrectionPacket(list));
        }

        /// <summary>
        /// A state by the client's name for it (STANDING, lying_down, "Combat Engaged") or its id;
        /// false for anything the client has not got.
        /// </summary>
        public static bool TryParseState(string value, out CharacterState state)
        {
            state = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (uint.TryParse(value, out var id))
            {
                state = (CharacterState)id;
                return Enum.IsDefined(typeof(CharacterState), state);
            }

            var name = value.Replace("_", "").Replace(" ", "");

            return Enum.TryParse(name, true, out state) && Enum.IsDefined(typeof(CharacterState), state);
        }

        #endregion
    }
}

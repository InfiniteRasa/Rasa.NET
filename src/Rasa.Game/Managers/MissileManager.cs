using System;
using System.Collections.Generic;
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

        private MissileManager()
        {
        }

        private void DoDamageToCreature(MapChannel mapChannel, Missile missile)
        {
            var creature = EntityManager.Instance.GetCreature(missile.TargetEntityId);

            if (creature.Actor.State == CharacterState.Dead)
                return;

            // decrease armor first
            var armorDecrease = Math.Min(missile.DamageA, creature.Actor.Attributes[Attributes.Armor].Current);
            creature.Actor.Attributes[Attributes.Armor].Current -= armorDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, creature.Actor, new UpdateArmorPacket(creature.Actor.Attributes[Attributes.Armor], creature.Actor.EntityId));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(missile.DamageA - armorDecrease, creature.Actor.Attributes[Attributes.Health].Current);
            creature.Actor.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, creature.Actor, new UpdateHealthPacket(creature.Actor.Attributes[Attributes.Health], creature.Actor.EntityId));
            
            if (creature.Actor.Attributes[Attributes.Health].Current <= 0)
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
            else
            {
                // shooting at wandering creatures makes them ANGRY
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionWander || creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFollowingPath)
                    BehaviorManager.Instance.SetActionFighting(creature, missile.Source.EntityId);
            }
        }

        private void DoDamageToPlayer(MapChannel mapChannel, Missile missile)
        {
            var actor = EntityManager.Instance.GetActor(missile.TargetEntityId);
            
            if (actor.State == CharacterState.Dead)
                return;

            // decrease armor first
            var armorDecrease = Math.Min(missile.DamageA, actor.Attributes[Attributes.Armor].Current);

            actor.Attributes[Attributes.Armor].Current -= armorDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(actor.Attributes[Attributes.Armor], 0));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(missile.DamageA - armorDecrease, actor.Attributes[Attributes.Health].Current);

            actor.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(actor.Attributes[Attributes.Health], 0));

            if(actor.Attributes[Attributes.Health].Current == 0)
            {
                // we won't die yet :D
                actor.Attributes[Attributes.Health].Current = actor.Attributes[Attributes.Health].CurrentMax;
                //actor.State = CharacterState.Dying;
            }

            if (actor.State == CharacterState.Dying)
            {

            }
        }

        public void RequestWeaponAttack(Client client, RequestWeaponAttackPacket packet)
        {
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

        public void DoWork(MapChannel mapChannel, long delta)
        {
            // ToDo: add check for triggerMissile timer
            foreach (var missile in mapChannel.QueuedMissiles)
                MissileTrigger(mapChannel, missile);

            // empty List
            mapChannel.QueuedMissiles.Clear();
        }

        public void MissileLaunch(MapChannel mapChannel, ActionData action, int damage)
        {
            var missile = new Missile
            {
                DamageA = damage,
                Source = action.Actor
            };

            // get distance between actors
            Actor targetActor = null;
            var triggerTime = 0; // time between windup and recovery

            if (action.TargetId != 0)
            {
                // target on entity
                var targetType = EntityManager.Instance.GetEntityType(action.TargetId);

                if (targetType == 0)
                {
                    Logger.WriteLog(LogType.Error, $"The missile target doesnt exist: {action.TargetId}");
                    // entity does not exist
                    return;
                }
                switch (targetType)
                {
                    case EntityType.Creature:
                        {
                            var creature = EntityManager.Instance.GetCreature(action.TargetId);
                            targetActor = creature.Actor;
                            missile.TargetEntityId = action.TargetId;
                        }
                        break;
                    case EntityType.Player:
                        {
                            var player = EntityManager.Instance.GetPlayer(action.TargetId);
                            targetActor = player.Player.Actor;
                            missile.TargetEntityId = action.TargetId;
                        }
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"Can't shoot that object");
                        return;
                };

                if (targetActor.State == CharacterState.Dead)
                    return; // actor is dead, cannot be shot at

                var distance = Vector3.Distance(targetActor.Position, action.Actor.Position);
                triggerTime = (int)(distance * 0.5f);
            }
            else
            {
                // has no target -> Shoot towards looking angle
                targetActor = null;
                triggerTime = 0;
            }

            // is the missile/action an ability that need needs to use Recv_PerformAbility?
            var isAbility = false;
            if (action.ActionId == ActionId.AaRecruitLightning) // recruit lighting ability
                isAbility = true;
            
            missile.TargetActor = targetActor;
            missile.TriggerTime = triggerTime;
            missile.ActionId = action.ActionId;
            missile.ActionArgId = action.ActionArgId;
            missile.IsAbility = isAbility;

            // send windup and append to queue (only for non-abilities)
            if (isAbility == false)
            {
                CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.ThreeArgs, missile.ActionId, missile.ActionArgId, missile.TargetEntityId));
                
                // add to list
                mapChannel.QueuedMissiles.Add(missile);
            }
            else
            {
                // abilities get applied directly without delay
                MissileTrigger(mapChannel, missile);
            }
        }

        public void MissileTrigger(MapChannel mapChannel, Missile missile)
        {
            // ToDo: Some weapons can hit multiple targets
            var targetType = EntityManager.Instance.GetEntityType(missile.TargetEntityId);
            var hitData = new HitData
            {
                FinalAmt = missile.DamageA,
                EntityId = missile.TargetEntityId
            };

            missile.Args.HitEntities.Add(missile.TargetEntityId);
            missile.Args.HitData.Add(hitData);     // ToDo: add suport for multiple targets

            switch (targetType)
            {
                case 0:
                    // no target => ToDo
                    break;
                case EntityType.Creature:
                    DoDamageToCreature(mapChannel, missile);
                    break;
                case EntityType.Player:
                    DoDamageToPlayer(mapChannel, missile);
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"WeaponAttackRecovery: Unsuported targetType {targetType}.");
                    break;
            }

            switch (missile.ActionId)
            {
                case ActionId.WeaponAttack:
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new WeaponAttackRecovery(missile));
                    break;
                //else if (missile->actionId == 174)
                //    missile_ActionRecoveryHandler_WeaponMelee(mapChannel, missile);
                case ActionId.AaRecruitLightning:
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new LightningRecovery(missile));
                    break;
                //else if (missile->actionId == 203)
                //    missile_ActionHandler_CR_FOREAN_LIGHTNING(mapChannel, missile);
                //else if (missile->actionId == 211)
                //    missile_ActionHandler_CR_AMOEBOID_SLIME(mapChannel, missile);
                //else if (missile->actionId == 397)
                //    missile_ActionRecoveryHandler_ThraxKick(mapChannel, missile);
                default:
                    Logger.WriteLog(LogType.Debug, $"MissileLaunch: unsupported missile actionId {missile.ActionId} - using default: WeaponAttackRecovery");
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new WeaponAttackRecovery(missile));
                    break;
            }
        }
    }
}

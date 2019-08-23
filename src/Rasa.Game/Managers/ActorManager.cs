namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;

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
         *  - ActionBlockChange(self, actionId, isBlocked)
         *  - PerformWindup(self, actionId, actionArgId, *args)
         *  - PerformRecovery(self, actionId, actionArgId, *args)
         *  - StateChange(self, stateIdList)
         *  - StateCorrection(self, stateList)
         *  - Abilities(self, abilityList)
         *  - Skills(self, skillList)
         *  - UserActionFailed(self, actionId, actionArgId, msgId)
         *  - ActionFailed(self, actionId, actionArgId)
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
         *  - PlayerDead(self, sourceId, graveyardList, canRevive = 0)
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
         *  - BuryMe                    => ToDo
         *  - ReviveMe                  => ToDo
         *  - RequestActionInterrupt    => ToDo
         *  - RequestDetachGameEffect   => ToDo
         *  - RequestVisualCombatMode   => ToDo
         *  - SetDesiredCrouchState     => ToDo
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
        #region Handlers

        public void RequestActionInterrupt(Client client, RequestActionInterruptPacket packet)
        {
            foreach (var action in client.MapClient.MapChannel.PerformRecovery)
                if (action.Actor == client.MapClient.Player.Actor)
                    if (action.ActionId == packet.ActionId)
                        if (action.ActionArgId == packet.ActionArgId)
                        {
                            action.IsInrerrupted = true;
                            break;
                        }
        }

        public void RequestVisualCombatMode(Client client, bool combatMode)
        {
            client.MapClient.Player.Actor.InCombatMode = combatMode;
            client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new RequestVisualCombatModePacket(combatMode));
        }

        public void SetDesiredCrouchState(Client client, ActorState state)
        {
            client.CellIgnoreSelfCallMethod(client, new SetDesiredCrouchStatePacket { DesiredStateId = state });
        }

        #endregion
    }
}

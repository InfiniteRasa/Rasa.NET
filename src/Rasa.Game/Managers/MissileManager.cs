using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Packets.MapChannel.Server;
    using Packets;
    using Timer;
    using Structures;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using System;

    public class MissileManager
    {
        private static MissileManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Timer Timer = new Timer();
        public List<Missile> QueuedMissiles = new List<Missile>();

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

        public void DoDamage(Creature creature, int damage, Client client)
        {
            if (creature.Actor.State == ActorState.Dead)
                return;

            // decrease armor first
            var armorDecrease = Math.Min(damage, creature.Actor.Attributes[Attributes.Armor].Current);
        }

        public void RequestWeaponAttack(Client client, RequestWeaponAttackPacket packet)
        {
            /*
	            // has ammo?
	            if( item->weaponData.ammoCount < item->itemTemplate->weapon.ammoPerShot )
		            return; // not enough ammo for a single shot
	            // decrease ammo
	            item->weaponData.ammoCount -= item->itemTemplate->weapon.ammoPerShot;
	            // update ammo in database
	            DataInterface_Character_updateCharacterAmmo(cm->tempCharacterData->characterID, item->locationSlotIndex, item->weaponData.ammoCount);
	            // weapon ammo info
	            pym_init(&pms);
	            pym_tuple_begin(&pms);
	            pym_addInt(&pms, item->weaponData.ammoCount);
	            pym_tuple_end(&pms);
	            netMgr_pythonAddMethodCallRaw(cm->cgm, item->entityId, WeaponAmmoInfo, pym_getData(&pms), pym_getLen(&pms));
	            // calculate damage
	            sint32 damageRange = item->itemTemplate->weapon.maxDamage-item->itemTemplate->weapon.minDamage;
	            damageRange = max(damageRange, 1); // to avoid division by zero in the next line
	            sint32 damage = item->itemTemplate->weapon.minDamage+(rand()%damageRange);
	            // for now we just ignore no-target attacks
	            //if( cm->player->targetEntityId == 0 )
	            //	return;
	            // launch correct missile type depending on weapon type
	            if( item->itemTemplate->weapon.altActionId == 1 )
	            {
		            // weapon range attacks
		            if( item->itemTemplate->weapon.altActionArg == 133 ) // physical pistol
			            missile_launch(cm->mapChannel, cm->player->actor, cm->player->targetEntityId, damage, item->itemTemplate->weapon.altActionId, item->itemTemplate->weapon.altActionArg); // pistol
		            else
			            printf("missile_playerTryFireWeapon: Unsupported weapon altActionArg (action %d/%d)\n", item->itemTemplate->weapon.altActionId, item->itemTemplate->weapon.altActionArg);
	            }
	            else
		            printf("missile_playerTryFireWeapon: Unsupported weapon altActionId (action %d/%d)\n", item->itemTemplate->weapon.altActionId, item->itemTemplate->weapon.altActionArg);

            */

            var weapon = InventoryManager.Instance.CurrentWeapon(client.MapClient);

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
        }

        public void DoWork(long delta)
        {
            foreach (var missile in QueuedMissiles)
            {
                // do work
            }

            // empty List
            QueuedMissiles.Clear();
        }

        public void PlayerTryFireWeapon(Client client)
        {
            Logger.WriteLog(LogType.Debug, "PlayerTryFireWeapon");
        }
    }
}

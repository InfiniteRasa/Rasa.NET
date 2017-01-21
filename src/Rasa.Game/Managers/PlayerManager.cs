using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Database.Tables.World;
    using Game;
    using Structures;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Game.Server;

    public class PlayerManager
    {
        // constant skillId data
        public static readonly int[] SkillIById = {
            1,8,14,19,20,21,22,23,24,
            25,26,28,30,31,32,34,35,
            36,37,39,40,43,47,48,49,
            50,54,55,57,58,63,66,67,
            68,72,73,77,79,80,82,89,
            92,102,110,111,113,114,121,135,
            136,147,148,149,150,151,152,153,
            154,155,156,157,158,159,160,161,
            162,163,164,165,166,172,173,174
        };
        // table for skillId to skillIndex mapping
        private static int[] SkillId2Idx =
        {
            -1,0,-1,-1,-1,-1,-1,-1,1,-1,-1,-1,-1,-1,2,-1,-1,-1,-1,3,
            4,5,6,7,8,9,10,-1,11,-1,12,13,14,-1,15,16,17,18,-1,19,
            20,-1,-1,21,-1,-1,-1,22,23,24,25,-1,-1,-1,26,27,-1,28,29,-1,
            -1,-1,-1,30,-1,-1,31,32,33,-1,-1,-1,34,35,-1,-1,-1,36,-1,37,
            38,-1,39,-1,-1,-1,-1,-1,-1,40,-1,-1,41,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,42,-1,-1,-1,-1,-1,-1,-1,43,44,-1,45,46,-1,-1,-1,-1,-1,
            -1,47,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,48,49,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,50,51,52,53,54,55,56,57,58,59,60,61,62,
            63,64,65,66,67,68,69,-1,-1,-1,-1,-1,70,71,72,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1
        };
        // table for skillIndex to ability mapping
        public static int[] SkillIdx2AbilityID =
        {
            -1, -1, -1, -1, 137, -1, -1, -1, -1, 178, 177, 158, -1, -1,
            197, 186, 188, 162, 187, -1, -1, 233, 234, -1, 194, -1, -1,
            -1, -1, -1, 301, -1, -1, 185, 251, 240, 302, 232, 229, -1,
            231, 305, 392, 252, 282, 381, 267, 298, 246, 253, 307, 393,
            281, 390, 295, 304, 386, 193, 385, 176, 260, 384, 383, 303,
            388, 389, 387, 380, 401, 430, 262, 421, 446
        };
        public static int[] requiredSkillLevelPoints = { 0, 1, 3, 6, 10, 15 };

        public static void AssignPlayer(MapChannel mapChannel, MapChannelClient mapClient)
        {
            var player = mapClient.Player;
            var actor = mapClient.Player.Actor;
            player.Client.SendPacket(5, new SetControlledActorIdPacket { EntetyId = actor.EntityId });

            player.Client.SendPacket(7, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            player.Client.SendPacket(5, new SetCurrentContextIdPacket { MapContextId = actor.MapContextId });

            player.Client.SendPacket(actor.EntityId, new UpdateRegionsPacket { RegionIdList = mapClient.MapChannel.MapInfo.BaseRegionId });  // ToDo this should be some list of regions

            player.Client.SendPacket(actor.EntityId, new AllCreditsPacket { Credits = player.Credits, Prestige = player.Prestige });

            player.Client.SendPacket(actor.EntityId, new AdvancementStatsPacket
            {
                Level = player.Level,
                Experience = player.Experience,
                Attributes = GetAvailableAttributePoints(mapClient),
                TrainPts = 0,       // trainPoints (are not used by the client??)
                SkillPts = GetSkillPointsAvailable(mapClient)
            });

            player.Client.SendPacket(actor.EntityId, new SkillsPacket (player.Skills));

            player.Client.SendPacket(actor.EntityId, new AbilitiesPacket(player.Skills));

            // don't send this packet if abilityDrawer is empty
            if (player.Abilities.Count > 0)
                player.Client.SendPacket(actor.EntityId, new AbilityDrawerPacket ( player.Abilities ));
        }

        public static void AutoFireKeepAlive(Client client, int keepAliveDelay)
        {
            // ToDo (after reload continue auto fire????)
        }

        public static void CellDiscardClientToPlayers(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].ClientEntityId == client.ClientEntityId)
                    continue;
                client.Client.SendPacket(5, new DestroyPhysicalEntityPacket{ EntityId = client.Player.Actor.EntityId });

            }
        }

        public static void CellDiscardPlayersToClient(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].Player == null)
                    continue;
                if (mapChannel.PlayerList[i].ClientEntityId == client.ClientEntityId)
                    continue;
                client.Client.SendPacket(5, new DestroyPhysicalEntityPacket { EntityId = mapChannel.PlayerList[i].Player.Actor.EntityId });
            }

        }

        public static void CellIntroduceClientToPlayers(MapChannel mapChannel, MapChannelClient client, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(5, new CreatePyhsicalEntityPacket( (int)client.Player.Actor.EntityId, (int)client.Player.Actor.EntityClassId));
            }

            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new AttributeInfoPacket { ActorStats = client.Player.Actor.Stats } );
            }
            
            // PreloadData
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new PreloadDataPacket());    // ToDo
            }
            // Recv_AppearanceData
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new AppearanceDataPacket{ AppearanceData = client.Player.AppearanceData});
            }
            // set controller (Recv_ActorControllerInfo )
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new ActorControllerInfoPacket{ IsPlayer = true });
            }
            // set level
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new LevelPacket{ Level = client.Player.Level});
            }
            // set class
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new CharacterClassPacket{ CharacterClass = client.Player.ClassId });
            }
            // set charname (name)
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new CharacterNamePacket{ CharacterName = client.Player.Actor.Name });
            }
            // set actor name
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new ActorNamePacket{ CharacterFamily = client.Player.Actor.FamilyName });
            }
            // set running
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new IsRunningPacket{ IsRunning = client.Player.Actor.IsRunning });
            }
            // set logos tabula
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new LogosStoneTabulaPacket());       // ToDo
            }
            // Recv_Abilities (id: 10, desc: must only be sent for the local manifestation)
            // We dont need to send ability data to every client, but only the owner (which is done in PlayerManager.AssignPlayer)
            // Skills -> Everything that the player can learn via the skills menu (Sprint, Firearms...) Abilities -> Every skill gained by logos?
            // Recv_WorldLocationDescriptor
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    Position = new Position
                    {
                        PosX = client.Player.Actor.Position.PosX,
                        PosY = client.Player.Actor.Position.PosY,
                        PosZ = client.Player.Actor.Position.PosZ
                    },
                    RotationX = 0.0D,
                    RotationY = 0.0D,
                    RotationZ = 0.0D,
                    Unknwon = 1.0D          // Camera poss?
                });
            }
            // set target category
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new TargetCategoryPacket { TargetCategory = 0 });    // 0 frendly
            }
            // player flags
            for (var i = 0; i < playerCount; i++)
            {
                client.Client.SendPacket(client.Player.Actor.EntityId, new PlayerFlagsPacket());
            }
            // ToDo
            // send inital movement packet
            //netCompressedMovement_t netMovement = { 0 };
            //netMovement.entityId = client->player->actor->entityId;
            //netMovement.posX24b = client->player->actor->posX * 256.0f;
            //netMovement.posY24b = client->player->actor->posY * 256.0f;
            //netMovement.posZ24b = client->player->actor->posZ * 256.0f;
            //for (sint32 i = 0; i < playerCount; i++)
            //{
            //    netMgr_sendEntityMovement(playerList[i]->cgm, &netMovement);
            //}*/
        }

        public static void CellIntroducePlayersToClient(MapChannel mapChannel, MapChannelClient mapClient, int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                if (mapChannel.PlayerList[i].ClientEntityId == mapClient.ClientEntityId)
                    continue;

                var tempClient = mapChannel.PlayerList[i];

                mapClient.Client.SendPacket(5, new CreatePyhsicalEntityPacket((int)tempClient.Player.Actor.EntityId, (int)tempClient.Player.Actor.EntityClassId));

                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new AttributeInfoPacket { ActorStats = tempClient.Player.Actor.Stats });
                // doesnt seem important (its only for loading gfx early?)
                //PreloadData
                //client.Client.SendPacket(tempClient.Player.Actor.EntityId, new PreloadDataPacket());
                // Recv_AppearanceData
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new AppearanceDataPacket());
                // set controller
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorControllerInfoPacket{IsPlayer = true});
                // set level
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new LevelPacket{ Level = tempClient.Player.Level });
                // set class
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterClassPacket{ CharacterClass = tempClient.Player.ClassId });
                // set charname (name)
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterNamePacket{ CharacterName = tempClient.Player.Actor.Name });
                // set actor name (familyName)
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorNamePacket{ CharacterFamily = tempClient.Player.Actor.FamilyName });
                // set running
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new IsRunningPacket{ IsRunning = tempClient.Player.Actor.IsRunning });
                // Recv_WorldLocationDescriptor
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    Position = new Position
                    {
                        PosX = tempClient.Player.Actor.Position.PosX,
                        PosY = tempClient.Player.Actor.Position.PosY,
                        PosZ = tempClient.Player.Actor.Position.PosZ
                    },
                    RotationX = 0.0D,
                    RotationY = 0.0D,
                    RotationZ = 0.0D,
                    Unknwon = 1.0D          // Camera poss?
                });
                // set target category
                mapClient.Client.SendPacket(tempClient.Player.Actor.EntityId, new TargetCategoryPacket{ TargetCategory = 0 });  // 0 frendly
                // ToDo
                // send inital movement packet
                //netCompressedMovement_t netMovement = { 0 };
                //netMovement.entityId = tempClient->player->actor->entityId;
                //netMovement.posX24b = tempClient->player->actor->posX * 256.0f;
                //netMovement.posY24b = tempClient->player->actor->posY * 256.0f;
                //netMovement.posZ24b = tempClient->player->actor->posZ * 256.0f;
                //netMgr_sendEntityMovement(client->cgm, &netMovement);
            }
        }

        public static int GetAvailableAttributePoints(MapChannelClient mapClient)
        {
            var points = mapClient.Player.Level * 2 - 2;
            points -= mapClient.Player.SpentBody;
            points -= mapClient.Player.SpentMind;
            points -= mapClient.Player.SpentSpirit;
            points = Math.Max(points, 0);
            return points;
        }

        public static int GetSkillIndexById(int skillId)
        {
            return skillId < 0 ? -1 : skillId >= 200 ? -1 : SkillId2Idx[skillId];
        }

        public static int GetSkillPointsAvailable(MapChannelClient mapClient)
        {
            var pointsAvailable = (mapClient.Player.Level - 1) * 2;
            pointsAvailable += 5; // add five points because of the recruit skills that start at level 1
            // subtract spent skill levels
            foreach (var skill in mapClient.Player.Skills)
            {
                var skillLevel = skill.Value.SkillLevel;
                if (skillLevel < 0 || skillLevel > 5)
                    continue; // should not be possible
                pointsAvailable -= requiredSkillLevelPoints[skillLevel];
            }
            return Math.Max(0, pointsAvailable);
        }

        public static void LevelSkills(Client client, LevelSkillsPacket packet)
        {
            var mapClient = client.MapClient;
            var skillPointsAvailable = GetSkillPointsAvailable(mapClient);
            var skillLevelupArray = new Dictionary<int, SkillsData>(); // used to temporarily safe skill level updates
            for (var i = 0;  i< packet.ListLenght; i++)
            {
                var skillId = packet.SkillIds[i];
                if ( skillId == -1)
                    throw new Exception("LevelSkills: Invalid skillID received. Modified or outdated client?");
                var oldSkillLevel = mapClient.Player.Skills[skillId].SkillLevel;
                var newSkillLevel = packet.SkillLevels[i];
                if (newSkillLevel < oldSkillLevel || newSkillLevel > 5)
                {
                    throw new Exception("LevelSkills: Invalid skill level received\n");
                }
                var additionalSkillPointsRequired = requiredSkillLevelPoints[newSkillLevel] - requiredSkillLevelPoints[oldSkillLevel];
                skillPointsAvailable -= additionalSkillPointsRequired;
                skillLevelupArray.Add(skillId, new SkillsData { SkillId = skillId, SkillLevel = newSkillLevel - oldSkillLevel });

            }
            // do we have enough skill points for the skill level ups?
            if (skillPointsAvailable < 0)
                throw new Exception("PlayerManager.LevelSkills: Not enough skill points. Modified or outdated client?\n");
            // everything ok, update skills!
            foreach (var skill in skillLevelupArray)
                mapClient.Player.Skills[skill.Value.SkillId].SkillLevel += skillLevelupArray[skill.Value.SkillId].SkillLevel;
            // send skill update to client
            client.SendPacket(mapClient.Player.Actor.EntityId, new SkillsPacket(mapClient.Player.Skills));
            // set abilities
            client.SendPacket(mapClient.Player.Actor.EntityId, new AbilitiesPacket(mapClient.Player.Skills));   // ToDo
            // update allocation points
            client.SendPacket(mapClient.Player.Actor.EntityId, new AvailableAllocationPointsPacket
            {
                AvailableAttributePoints = GetAvailableAttributePoints(mapClient),
                TrainPoints = 0,        // not used?
                AvailableSkillPoints = GetSkillPointsAvailable(mapClient)
            });
            // update database with new character skills
            foreach (var skill in skillLevelupArray)
                CharacterSkillsTable.UpdateCharacterSkill(mapClient.Player.CharacterId, mapClient.Player.Skills[skill.Key].SkillId, mapClient.Player.Skills[skill.Key].SkillLevel);
        }

        public static void RemovePlayerCharacter(MapChannel mapChannel, MapChannelClient mapClient)
        {
            // ToDo do we need remove something, or it's done already 
        }

        public static void RemoveAppearanceItem(PlayerData player, int itemClassId)
        {
            var equipmentSlotId = EquipableClassEquipmentSlotTable.GetSlotId((uint)itemClassId);
            if (equipmentSlotId == 0)
                return;
            player.AppearanceData[equipmentSlotId].ClassId = 0;
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(player.CharacterId, equipmentSlotId, 0, 0);
        }

        public static void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            client.MapClient.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            // ToDo do we need upate Database???
            client.SendPacket(client.MapClient.Player.Actor.EntityId, new AbilityDrawerSlotPacket { AbilityDrawerSlot = abilityDrawerSlot });
        }

        public static void RequestArmWeapon(Client client, int requestedWeaponDrawerSlot)
        {
            client.MapClient.Inventory.ActiveWeaponDrawer = requestedWeaponDrawerSlot;
            // 574 Recv_WeaponDrawerSlot(self, slotNum, bRequested = True):
            client.SendPacket(client.MapClient.Player.Actor.EntityId, new WeaponDrawerSlotPacket { RequestedWeaponDrawerSlot = requestedWeaponDrawerSlot });
            //tell client to change weapon appearance
            InventoryManager.NotifyEquipmentUpdate(client.MapClient);
            var tempItem = EntityManager.GetItem(client.MapClient.Inventory.WeaponDrawer[client.MapClient.Inventory.ActiveWeaponDrawer]);
            if (tempItem == null)
                return;
            SetAppearanceItem(client.MapClient.Player, tempItem.ItemTemplate.ClassId, -2139062144);
            UpdateAppearance(client.MapClient);
            // update ammo info
            client.SendPacket(tempItem.EntityId, new WeaponAmmoInfoPacket{ AmmoInfo = tempItem.WeaponAmmoCount });
        }

        public static void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            /*
            ServerArgs = (self.actionId, self.actionArgId, target, self.itemId)
            if self.useClientYaw: serverArgs += (actor.body.GetYaw())
            gameclient.SendCallActorMethod('RequestPerformAbility', serverArgs)
            */

            switch (packet.ActionId)
            {
                /*case 194: // Lightning
                    printf("Lightning: Target %u\n", (uint32)targetEntityId);
                    //missile_launch(cm->mapChannel, cm->player->actor, targetEntityId, MISSILE_LIGHTNING, 40);
                    missile_launch(cm->mapChannel, cm->player->actor, targetEntityId, 180 + (rand() % 61), 194, 1);
                    //_test_PerformAbility();

                    //gameEffect_attach(cm->mapChannel, targetEntityId, 86, 1); // stun
                    return;*/
                case 401: // Sprint
                    GameEffectManager.AttachSprint(client.MapClient.MapChannel, client.MapClient.Player, packet.ActionArgId, 5000);
                    return;
                default:
                    Console.WriteLine("Unknown Ability: ID {0} ArgID {1} Target {2}\n", packet.ActionId, packet.ActionArgId, packet.Target);
                    return;
            };
        }

        public static void RequestSetAbilitySlot(Client client, RequestSetAbilitySlotPacket packet)
        {
            // todo: do we need to check if ability is available ??
            if (packet.AbilityId == 0)
            {
                // remove ability is used
                client.MapClient.Player.Abilities.Remove(packet.SlotId);
            }
            else
            {
                // added new ability
                AbilityDrawerData ability;
                client.MapClient.Player.Abilities.TryGetValue(packet.SlotId, out ability);
                if (ability == null)
                {
                    client.MapClient.Player.Abilities.Add(packet.SlotId, new AbilityDrawerData { AbilitySlotId = packet.SlotId, AbilityId = (int)packet.AbilityId, AbilityLevel = (int)packet.AbilityLevel });
                }
                else
                {
                    client.MapClient.Player.Abilities[packet.SlotId].AbilityId = (int)packet.AbilityId;
                    client.MapClient.Player.Abilities[packet.SlotId].AbilityLevel = (int)packet.AbilityLevel;
                    client.MapClient.Player.Abilities[packet.SlotId].AbilitySlotId = (int)packet.SlotId;
                }
            }
            // update database with new drawer slot ability
            CharacterAbilityDrawerTable.UpdateCharacterAbility(client.MapClient.Player.CharacterId, (int)packet.SlotId, (int)packet.AbilityId, (int)packet.AbilityLevel);
            // send packet
            client.MapClient.Player.Client.SendPacket(client.MapClient.Player.Actor.EntityId, new AbilityDrawerPacket(client.MapClient.Player.Abilities));
        }

        public static void RequestSwapAbilitySlots(Client client, RequestSwapAbilitySlotsPacket packet)
        {
            AbilityDrawerData toSlot;
            var abilities = client.MapClient.Player.Abilities;
            var fromSlot = abilities[packet.FromSlot];
            abilities.TryGetValue(packet.ToSlot, out toSlot);
            if (toSlot == null)
            {
                abilities.Add(packet.ToSlot, new AbilityDrawerData { AbilitySlotId = packet.ToSlot, AbilityId = fromSlot.AbilityId, AbilityLevel = fromSlot.AbilityLevel });
                abilities.Remove(packet.FromSlot);
            }
            else
            {
                abilities[packet.ToSlot] = abilities[packet.FromSlot];
                abilities[packet.FromSlot] = toSlot;
            }
            // Do we need to update database here ???
            // update database with new drawer slot ability
            CharacterAbilityDrawerTable.UpdateCharacterAbility(
                client.MapClient.Player.CharacterId,
                abilities[packet.ToSlot].AbilitySlotId,
                abilities[packet.ToSlot].AbilityId,
                abilities[packet.ToSlot].AbilityLevel);
            // check if fromSlot isn't empty now
            AbilityDrawerData tempSlot;
            abilities.TryGetValue(packet.FromSlot, out tempSlot);
            if (tempSlot != null)
                CharacterAbilityDrawerTable.UpdateCharacterAbility(
                    client.MapClient.Player.CharacterId,
                    abilities[packet.FromSlot].AbilitySlotId,
                    abilities[packet.FromSlot].AbilityId,
                    abilities[packet.FromSlot].AbilityLevel);
            else
                CharacterAbilityDrawerTable.UpdateCharacterAbility(client.MapClient.Player.CharacterId, packet.FromSlot, 0, 0);
            // send packet
            client.MapClient.Player.Client.SendPacket(client.MapClient.Player.Actor.EntityId, new AbilityDrawerPacket(abilities));
        }

        public static void RequestVisualCombatMode(Client client, int combatMode)
        {
            if (combatMode > 0) // Enter combat mode
            {
                client.MapClient.Player.Actor.InCombatMode = true;
                // ToDo need to write new function, we cannot use client.SendPacket();
                //netMgr_cellDomain_pythonAddMethodCallRaw(client->mapChannel, client->player->actor, client->player->actor->entityId, 753, pym_getData(&pms), pym_getLen(&pms));
            }
            else // Exit combat mode
            {
                client.MapClient.Player.Actor.InCombatMode = false;
                // ToDo need to write new function, we cannot use client.SendPacket();
                //netMgr_cellDomain_pythonAddMethodCallRaw(client->mapChannel, client->player->actor, client->player->actor->entityId, 753, pym_getData(&pms), pym_getLen(&pms));
            }
        }

        public static void SetAppearanceItem(PlayerData player, int itemClassId, int hueAARRGGBB)
        {
            var equipmentSlotId = EquipableClassEquipmentSlotTable.GetSlotId((uint)itemClassId);
            if (equipmentSlotId == 0)
                return;
            player.AppearanceData[equipmentSlotId].ClassId = itemClassId;
            player.AppearanceData[equipmentSlotId].Color = new Color(hueAARRGGBB);
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(player.CharacterId, equipmentSlotId, itemClassId, hueAARRGGBB);
        }

        public static void SetDesiredCrouchState(Client client, int stateId)
        {
            // ToDo incrace accuracy or something 
            // stateId's 1 = standing, 14 = crouched
        }

        public static void StartAutoFire(Client client, double retryDelayMs)
        {
            // ToDo
        }

        public static void UpdateAppearance(MapChannelClient mapClient)
        {
            if (mapClient.Player == null)
                return;
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new AppearanceDataPacket { AppearanceData = mapClient.Player.AppearanceData });
        }

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        public static void UpdateStatsValues(MapChannelClient mapClient, bool fullreset)
        {
            var player = mapClient.Player;
            var stats = player.Actor.Stats;
            // body
            stats.Body.NormalMax = 10 + (player.Level - 1) * 2 + player.SpentBody;
            var bodyBonus = 0;
            stats.Body.CurrentMax = stats.Body.NormalMax + bodyBonus;
            stats.Body.Current = stats.Body.CurrentMax;
            // mind
            stats.Mind.NormalMax = 10 + (player.Level - 1) * 2 + player.SpentMind;
            var mindBonus = 0;
            stats.Mind.CurrentMax = stats.Mind.NormalMax + mindBonus;
            stats.Mind.Current = stats.Mind.CurrentMax;
            // spirit
            stats.Spirit.NormalMax = 10 + (player.Level - 1) * 2 + player.SpentSpirit;
            var spiritBonus = 0;
            stats.Spirit.CurrentMax = stats.Spirit.NormalMax + spiritBonus;
            stats.Spirit.Current = stats.Spirit.CurrentMax;
            // health
            stats.Health.NormalMax = 100 + (player.Level - 1) * 2 * 8 + player.SpentBody * 6;
            var healthBonus = 0;
            stats.Health.CurrentMax = stats.Health.NormalMax + healthBonus;
            if (fullreset)
                stats.Health.Current = stats.Health.CurrentMax;
            else
                stats.Health.Current = Math.Min(stats.Health.Current, stats.Health.CurrentMax);
            // chi/adrenaline
            stats.Chi.NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentSpirit * 3;
            var chiBonus = 0;
            stats.Chi.CurrentMax = stats.Chi.NormalMax + chiBonus;
            if (fullreset)
                stats.Chi.Current = stats.Chi.CurrentMax;
            else
                stats.Chi.Current = Math.Min(stats.Chi.Current, stats.Chi.CurrentMax);
            // update regen rate
            stats.Regen.NormalMax = 100 + (player.Level - 1) * 2 + Math.Max(0, (stats.Spirit.CurrentMax - 10)) * 6; // regenRate in percent
            var regenBonus = 0;
            stats.Regen.CurrentMax = stats.Regen.NormalMax + regenBonus;
            stats.Regen.RefreshAmount = 2 * (stats.Regen.CurrentMax / 100); // 2.0 per second is the base regeneration for health
            // calculate armor max
            var armorMax = 0.0d;
            //float armorBonus = 0; // todo! (From item modules)
            var armorBonusPct = player.SpentBody * 0.0066666d;
            var armorRegenRate = 0;
            for (var i = 0; i < 22; i++)
            {
                if (mapClient.Inventory.EquippedInventory[i] == 0)
                    continue;
                var equipmentItem = EntityManager.GetItem(mapClient.Inventory.EquippedInventory[i]);
                if (equipmentItem == null)
                {
                    // this is very bad, how can the item disappear while it is still linked in the inventory?
                    Console.WriteLine("manifestation_updateStatsValues: Equipment item has no physical copy");
                    continue;
                }
                if (equipmentItem.ItemTemplate.ItemType != (int)ItemTypes.Armor)
                    continue; // how can the player equip non-armor?
                armorMax += equipmentItem.ItemTemplate.Armor.ArmorValue;
                armorRegenRate += equipmentItem.ItemTemplate.Armor.RegenRate;
                // what about damage absorbed? Was it used at all?
            }
            armorMax = armorMax * (1.0d + armorBonusPct);
            stats.Armor.Current = armorRegenRate;
            stats.Armor.NormalMax = armorMax;
            stats.Armor.CurrentMax = armorMax;
            if (fullreset)
                stats.Armor.Current = stats.Armor.CurrentMax;
            else
                stats.Armor.Current = Math.Min(stats.Armor.Current, armorMax);
            // added by krssrb
            // power test
            stats.Power.NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentMind * 3;
            var powerBonus = 0;
            stats.Power.CurrentMax = stats.Power.NormalMax + powerBonus;
            if (fullreset)
                stats.Power.Current = stats.Power.CurrentMax;
            else
                stats.Power.Current = Math.Min(stats.Power.Current, stats.Power.CurrentMax);

            // Send Data to client
            mapClient.Client.SendPacket(mapClient.Player.CharacterId, new AttributeInfoPacket { ActorStats = mapClient.Player.Actor.Stats });
        }
    }
}
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
        private static PlayerManager _instance;
        private static readonly object InstanceLock = new object();

        public static PlayerManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PlayerManager();
                    }
                }

                return _instance;
            }
        }

        private PlayerManager()
        {
        }

        // constant skillId data
        public readonly int[] SkillIById = {
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
        private readonly int[] SkillId2Idx =
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
        public readonly int[] SkillIdx2AbilityId =
        {
            -1, -1, -1, -1, 137, -1, -1, -1, -1, 178, 177, 158, -1, -1,
            197, 186, 188, 162, 187, -1, -1, 233, 234, -1, 194, -1, -1,
            -1, -1, -1, 301, -1, -1, 185, 251, 240, 302, 232, 229, -1,
            231, 305, 392, 252, 282, 381, 267, 298, 246, 253, 307, 393,
            281, 390, 295, 304, 386, 193, 385, 176, 260, 384, 383, 303,
            388, 389, 387, 380, 401, 430, 262, 421, 446
        };
        public readonly int[] requiredSkillLevelPoints = { 0, 1, 3, 6, 10, 15 };

        public void AssignPlayer(MapChannelClient mapClient)
        {
            var player = mapClient.Player;
            var actor = mapClient.Player.Actor;
            player.Client.SendPacket(5, new SetControlledActorIdPacket { EntetyId = actor.EntityId });

            player.Client.SendPacket(7, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            player.Client.SendPacket(5, new SetCurrentContextIdPacket { MapContextId = mapClient.MapChannel.MapInfo.MapId });

            player.Client.SendPacket(actor.EntityId, new UpdateRegionsPacket { RegionIdList = mapClient.MapChannel.MapInfo.BaseRegionId });  // ToDo this should be list of regions? or just curent region wher player is

            player.Client.SendPacket(actor.EntityId, new AllCreditsPacket { Credits = player.Credits, Prestige = player.Prestige });

            player.Client.SendPacket(actor.EntityId, new AdvancementStatsPacket
            {
                Level = player.Level,
                Experience = player.Experience,
                Attributes = GetAvailableAttributePoints(mapClient.Player),
                TrainPts = 0,       // trainPoints (are not used by the client??)
                SkillPts = GetSkillPointsAvailable(mapClient.Player)
            });

            player.Client.SendPacket(actor.EntityId, new SkillsPacket(player.Skills));

            player.Client.SendPacket(actor.EntityId, new AbilitiesPacket(player.Skills));

            // don't send this packet if abilityDrawer is empty
            if (player.Abilities.Count > 0)
                player.Client.SendPacket(actor.EntityId, new AbilityDrawerPacket(player.Abilities));

            player.Client.SendPacket(actor.EntityId, new TitlesPacket(player.Titles));
        }

        public void AutoFireKeepAlive(Client client, int keepAliveDelay)
        {
            // ToDo (after reload continue auto fire????)
        }

        public void CellDiscardClientToPlayers(MapChannelClient mapClient, List<MapChannelClient> notifyPlayers)
        {
            foreach (var t in notifyPlayers)
            {
                if (t == mapClient)
                    continue;

                t.Player.Client.SendPacket(5, new DestroyPhysicalEntityPacket{ EntityId = mapClient.Player.Actor.EntityId });
            }
        }

        public void CellDiscardPlayersToClient(MapChannelClient mapClient, List<MapChannelClient> notifyPlayers)
        {
            foreach (var t in notifyPlayers)
            {
                if (t == null)
                    continue;
                if (t == mapClient)
                    continue;
                mapClient.Player.Client.SendPacket(5, new DestroyPhysicalEntityPacket { EntityId = t.Player.Actor.EntityId });
            }

        }

        public void CellIntroduceClientToPlayers(MapChannelClient mapClient, List<MapChannelClient> playerList)
        {
            var netMovement = new NetCompressedMovement();
            netMovement.EntityId = mapClient.Player.Actor.EntityId;
            netMovement.Flag = 0;
            netMovement.PosX24b = (uint)mapClient.Player.Actor.Position.PosX * 256;
            netMovement.PosY24b = (uint)mapClient.Player.Actor.Position.PosY * 256;
            netMovement.PosZ24b = (uint)mapClient.Player.Actor.Position.PosZ * 256;

            foreach (var tempPlayer in playerList)
            {
                tempPlayer.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(mapClient.Player.Actor.EntityId, (int)mapClient.Player.Actor.EntityClassId));
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new AttributeInfoPacket { ActorStats = mapClient.Player.Actor.Stats });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new PreloadDataPacket());    // ToDo
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new AppearanceDataPacket { AppearanceData = mapClient.Player.AppearanceData });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new ActorControllerInfoPacket { IsPlayer = true });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new LevelPacket { Level = mapClient.Player.Level });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new CharacterClassPacket { CharacterClass = mapClient.Player.ClassId });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new CharacterNamePacket { CharacterName = mapClient.Player.Actor.Name });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new ActorNamePacket { CharacterFamily = mapClient.Player.Actor.FamilyName });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new IsRunningPacket { IsRunning = mapClient.Player.Actor.IsRunning });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new LogosStoneTabulaPacket { Logos = mapClient.Player.Logos });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    Position = mapClient.Player.Actor.Position,
                    RotationX = 0.0D,
                    RotationY = 0.0D,
                    RotationZ = 0.0D,
                    RotationW = 1.0D
                });
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new TargetCategoryPacket { TargetCategory = 0 });    // 0 frendly
                tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new PlayerFlagsPacket());

                if (tempPlayer.Player.Actor.EntityId == mapClient.Player.Actor.EntityId)
                    continue;

                //tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, netMovement);
            }

            // Recv_Abilities (id: 10, desc: must only be sent for the local manifestation)
            // We dont need to send ability data to every client, but only the owner (which is done in PlayerManager.AssignPlayer)
            // Skills -> Everything that the player can learn via the skills menu (Sprint, Firearms...) Abilities -> Every skill gained by logos?
            // Recv_WorldLocationDescriptor

        }

        public void CellIntroducePlayersToClient(MapChannel mapChannel, MapChannelClient mapClient, List<MapChannelClient> playerList)
        {
            foreach (var tempClient in playerList)
            {
                // don't send data about yourself
                if (mapClient == tempClient)
                    continue;

                mapClient.Player.Client.SendPacket(5, new CreatePhysicalEntityPacket(tempClient.Player.Actor.EntityId, (int)tempClient.Player.Actor.EntityClassId));
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new AttributeInfoPacket { ActorStats = tempClient.Player.Actor.Stats });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new AppearanceDataPacket { AppearanceData = tempClient.Player.AppearanceData });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorControllerInfoPacket { IsPlayer = true });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new LevelPacket { Level = tempClient.Player.Level });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterClassPacket { CharacterClass = tempClient.Player.ClassId });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new CharacterNamePacket { CharacterName = tempClient.Player.Actor.Name });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new ActorNamePacket { CharacterFamily = tempClient.Player.Actor.FamilyName });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new IsRunningPacket { IsRunning = tempClient.Player.Actor.IsRunning });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new WorldLocationDescriptorPacket
                {
                    Position = tempClient.Player.Actor.Position,
                    RotationX = 0.0D,
                    RotationY = 0.0D,
                    RotationZ = 0.0D,
                    RotationW = 1.0D
                });
                mapClient.Player.Client.SendPacket(tempClient.Player.Actor.EntityId, new TargetCategoryPacket { TargetCategory = 0 });  // 0 frendly
                // ToDo
                // send inital movement packet
                //netCompressedMovement_t netMovement = { 0 };
                //var netMovement = new NetCompressedMovement();
                //netMovement.entityId = tempClient->player->actor->entityId;
                //netMovement.posX24b = tempClient->player->actor->posX * 256.0f;
                //netMovement.posY24b = tempClient->player->actor->posY * 256.0f;
                //netMovement.posZ24b = tempClient->player->actor->posZ * 256.0f;
                //netMgr_sendEntityMovement(client->cgm, &netMovement);
            }
        }

        public void ChangeTitle(Client client, int titleId)
        {
            if (titleId != 0)
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new ChangeTitlePacket { TitleId = titleId });
            else
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new TitleRemovedPacket());
        }

        public int GetAvailableAttributePoints(PlayerData player)
        {
            var points = player.Level * 2 - 2;
            points -= player.SpentBody;
            points -= player.SpentMind;
            points -= player.SpentSpirit;
            points = Math.Max(points, 0);
            return points;
        }

        public int GetSkillIndexById(int skillId)
        {
            return skillId < 0 ? -1 : skillId >= 200 ? -1 : SkillId2Idx[skillId];
        }

        public int GetSkillPointsAvailable(PlayerData player)
        {
            var pointsAvailable = (player.Level - 1) * 2;
            pointsAvailable += 5; // add five points because of the recruit skills that start at level 1
            // subtract spent skill levels
            foreach (var skill in player.Skills)
            {
                var skillLevel = skill.Value.SkillLevel;
                if (skillLevel < 0 || skillLevel > 5)
                    continue; // should not be possible
                pointsAvailable -= requiredSkillLevelPoints[skillLevel];
            }
            return Math.Max(0, pointsAvailable);
        }

        public void GiveLogos(MapChannelClient mapClient, int logosId)
        {
            mapClient.Player.Logos.Add(logosId);
            CharacterLogosTable.SetLogos(mapClient.Player.CharacterId, logosId);
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new LogosStoneTabulaPacket { Logos = mapClient.Player.Logos });
        }

        public void LevelSkills(Client client, LevelSkillsPacket packet)
        {
            var mapClient = client.MapClient;
            var skillPointsAvailable = GetSkillPointsAvailable(mapClient.Player);
            var skillLevelupArray = new Dictionary<int, SkillsData>(); // used to temporarily safe skill level updates

            for (var i = 0;  i< packet.ListLenght; i++)
            {
                var skillId = packet.SkillIds[i];

                if ( skillId == -1)
                    throw new Exception("LevelSkills: Invalid skillId received. Modified or outdated client?");

                var oldSkillLevel = 0;

                if (mapClient.Player.Skills.ContainsKey(skillId))
                    oldSkillLevel = mapClient.Player.Skills[skillId].SkillId;
                else
                {
                    // create new entry in character skils and db
                    var abilityId = SkillIdx2AbilityId[GetSkillIndexById(packet.SkillIds[i])];

                    mapClient.Player.Skills.Add(skillId, new SkillsData { SkillId = skillId, AbilityId = abilityId, SkillLevel = 0 });
                    CharacterSkillsTable.SetCharacterSkill(mapClient.Player.CharacterId, skillId, abilityId, 0);
                }

                var newSkillLevel = packet.SkillLevels[i];

                if (newSkillLevel < oldSkillLevel || newSkillLevel > 5)
                    throw new Exception("LevelSkills: Invalid skill level received\n");

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
                AvailableAttributePoints = GetAvailableAttributePoints(mapClient.Player),
                TrainPoints = 0,        // not used?
                AvailableSkillPoints = GetSkillPointsAvailable(mapClient.Player)
            });
            // update database with new character skills
            foreach (var skill in skillLevelupArray)
                CharacterSkillsTable.UpdateCharacterSkill(mapClient.Player.CharacterId, mapClient.Player.Skills[skill.Key].SkillId, mapClient.Player.Skills[skill.Key].SkillLevel);
        }

        public void NotifyEquipmentUpdate(MapChannelClient mapClient, EquipmentInfo equipmentInfo)
        {
            mapClient.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, new EquipmentInfoPacket { EquipmentInfo = equipmentInfo });
        }

        public void PerformAbilitie(MapChannel mapChannel, PerformAbilityData abilityData)
        {
            switch (abilityData.ActionId)
            {
                case 129: // Weapon Stow
                    abilityData.MapClient.Player.Client.CellSendPacket(abilityData.MapClient, abilityData.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket { ActionId = abilityData.ActionId, ActionArgId = abilityData.ActionArgId });
                    abilityData.MapClient.Player.WeaponReady = false;
                    return;
                case 130: // Weapon Draw
                    abilityData.MapClient.Player.Client.CellSendPacket(abilityData.MapClient, abilityData.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket { ActionId = abilityData.ActionId, ActionArgId = abilityData.ActionArgId });
                    abilityData.MapClient.Player.WeaponReady = true;
                    return;
                case 134: // Reload Weapon
                    WeaponReload(abilityData.MapClient, abilityData.ActionId, abilityData.ActionArgId);
                    return;
                case 194: // Lightning
                    abilityData.MapClient.Player.Client.CellSendPacket(abilityData.MapClient, abilityData.MapClient.Player.Actor.EntityId, new PerformWindupPacket { ActionId = abilityData.ActionId, ActionArgId = abilityData.ActionArgId });
                    Console.WriteLine("ToDo : Lightning");
                    return;
                case 401: // Sprint
                    GameEffectManager.Instance.AttachSprint(abilityData.MapClient, abilityData.MapClient.Player.Actor, abilityData.ActionArgId, 500);
                    return;
                default:
                    Console.WriteLine($"Unknown Ability: Id {abilityData.ActionId}, ArgId {abilityData.ActionArgId}, Target {abilityData.TargetId}");
                    return;
            };
        }

        public void RemovePlayerCharacter(MapChannel mapChannel, MapChannelClient mapClient)
        {
            // ToDo do we need remove something, or it's done already 
        }

        public void RemoveAppearanceItem(PlayerData player, int equipmentSlotId)
        {
            if (equipmentSlotId == 0)
                return;
            player.AppearanceData[equipmentSlotId].ClassId = 0;
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(player.CharacterId, equipmentSlotId, 0, 0);
        }

        public void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            client.MapClient.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            // ToDo do we need upate Database???
            client.SendPacket(client.MapClient.Player.Actor.EntityId, new AbilityDrawerSlotPacket { AbilityDrawerSlot = abilityDrawerSlot });
        }

        public void RequestArmWeapon(Client client, int requestedWeaponDrawerSlot)
        {
            client.MapClient.Inventory.ActiveWeaponDrawer = requestedWeaponDrawerSlot;
            // 574 Recv_WeaponDrawerSlot(self, slotNum, bRequested = True):
            client.SendPacket(client.MapClient.Player.Actor.EntityId, new WeaponDrawerSlotPacket { RequestedWeaponDrawerSlot = requestedWeaponDrawerSlot });

            var tempItem = EntityManager.Instance.GetItem(client.MapClient.Inventory.WeaponDrawer[client.MapClient.Inventory.ActiveWeaponDrawer]);

            if (tempItem == null)
                return;

            client.MapClient.Inventory.EquippedInventory[13] = tempItem.EntityId;
            
            NotifyEquipmentUpdate(client.MapClient, new EquipmentInfo { SlotId = 13, EntityId = tempItem.EntityId } );

            SetAppearanceItem(client.MapClient.Player, tempItem);
            UpdateAppearance(client.MapClient);
            // update ammo info
            client.SendPacket(tempItem.EntityId, new WeaponAmmoInfoPacket{ AmmoInfo = tempItem.CurrentAmmo });
        }

        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            client.MapClient.MapChannel.QueuedPerformAbilities.Enqueue(new PerformAbilityData(client.MapClient, packet.ActionId, packet.ActionArgId, packet.Target, packet.ItemId));
        }

        public void RequestSetAbilitySlot(Client client, RequestSetAbilitySlotPacket packet)
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

        public void RequestSwapAbilitySlots(Client client, RequestSwapAbilitySlotsPacket packet)
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

        public void RequestVisualCombatMode(Client client, int combatMode)
        {
            if (combatMode > 0)
            {
                client.MapClient.Player.Actor.InCombatMode = true;
                client.CellSendPacket(client.MapClient, client.MapClient.Player.Actor.EntityId, new RequestVisualCombatModePacket { CombatMode = 1 });
            }
            else
            {
                client.MapClient.Player.Actor.InCombatMode = false;
                client.CellSendPacket(client.MapClient, client.MapClient.Player.Actor.EntityId, new RequestVisualCombatModePacket { CombatMode = 0 });
            }
        }

        public void RequestWeaponDraw(Client client)
        {
            // todo: Use correct argId depending on weapon type
            client.MapClient.MapChannel.QueuedPerformAbilities.Enqueue(new PerformAbilityData(client.MapClient, 130, 1, 0, 0));
        }

        public void RequestWeaponReload(Client client)
        {
            // todo: Use correct argId depending on weapon type
            client.MapClient.MapChannel.QueuedPerformAbilities.Enqueue(new PerformAbilityData(client.MapClient, 134, 1, 0, 0));
        }

        public void RequestWeaponStow(Client client)
        {
            // todo: Use correct argId depending on weapon type
            client.MapClient.MapChannel.QueuedPerformAbilities.Enqueue(new PerformAbilityData(client.MapClient, 129, 1, 0, 0));
        }

        public void SetAppearanceItem(PlayerData player, Item item)
        {
            var equipmentSlotId = item.ItemTemplate.Equipment.EquiptmentSlotType;
            if (equipmentSlotId == 0)
                return;
            if (!player.AppearanceData.ContainsKey(equipmentSlotId))
            {
                // Add new appearance slot to character and db
                player.AppearanceData.Add(equipmentSlotId, new AppearanceData { SlotId = equipmentSlotId } );
                CharacterAppearanceTable.SetAppearance(player.CharacterId, equipmentSlotId, 0, 0);
            }

            player.AppearanceData[equipmentSlotId].ClassId = item.ItemTemplate.ClassId;
            player.AppearanceData[equipmentSlotId].Color = new Color(item.Color);
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(player.CharacterId, equipmentSlotId, item.ItemTemplate.ClassId, item.Color);
        }

        public void SetDesiredCrouchState(Client client, int stateId)
        {
            // ToDo incrace accuracy or something 
            // stateId's 1 = standing, 14 = crouched
        }

        public void StartAutoFire(Client client, double fromUi)
        {
            if (!client.MapClient.Player.WeaponReady)
            {
                RequestWeaponDraw(client);
                return;
            }
            Console.WriteLine($"StartAutOFire data = {fromUi}");
            // do we need to reload?
            var itemWeapon = InventoryManager.Instance.CurrentWeapon(client.MapClient);
            if (itemWeapon == null)
                return; // no weapon armed
            if (itemWeapon.CurrentAmmo < itemWeapon.ItemTemplate.Weapon.AmmoPerShot)
            {
                PlayerManager.Instance.RequestWeaponReload(client);
                return;
            }

            client.MapClient.Player.Actor.InCombatMode = true;
            client.CellSendPacket(client.MapClient, client.MapClient.Player.Actor.EntityId, new RequestVisualCombatModePacket { CombatMode = 1 });

            //##################### Begin: if target is Mapchannel Client #################
            //desc: have to use mapchannel-client id instead of player-entity-id because
            //player-entity-id isnt registered, when new player is created(enter world)
            if (client.MapClient.Player.TargetEntityId == 0)
                return;

            var targetType = EntityManager.Instance.GetEntityType(client.MapClient.Player.TargetEntityId);
            if (targetType ==EntityType.Player) //1:client-type,0=player-type
            {
                var mapCell = CellManager.Instance.TryGetCell(
                    client.MapClient.MapChannel,
                    client.MapClient.Player.Actor.CellLocation.CellPosX,
                     client.MapClient.Player.Actor.CellLocation.CellPosY);
                if (mapCell != null)
                {
                    var playerList = client.MapClient.MapChannel.PlayerList;
                    foreach (var targetPlayer in playerList)
                        if (targetPlayer.Player.Actor.EntityId == client.MapClient.Player.TargetEntityId)
                        {
                            client.MapClient.Player.TargetEntityId = targetPlayer.ClientEntityId;
                            break;
                        }
                }

            }	
        }

        public void UpdateAppearance(MapChannelClient mapClient)
        {
            if (mapClient.Player == null)
                return;
            mapClient.Player.Client.CellSendPacket(mapClient, mapClient.Player.Actor.EntityId, new AppearanceDataPacket { AppearanceData = mapClient.Player.AppearanceData });
        }

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        public void UpdateStatsValues(MapChannelClient mapClient, bool fullreset)
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
            var armorBonusPct = player.Actor.Stats.Body.CurrentMax * 0.0066666d;
            var armorRegenRate = 0;
            for (var i = 0; i < 22; i++)
            {
                if (mapClient.Inventory.EquippedInventory[i] == 0)
                    continue;
                var equipmentItem = EntityManager.Instance.GetItem(mapClient.Inventory.EquippedInventory[i]);
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
            mapClient.Client.SendPacket(mapClient.Player.Actor.EntityId, new AttributeInfoPacket { ActorStats = mapClient.Player.Actor.Stats });
        }

        public void WeaponReload(MapChannelClient mapClient, int actionId, int actionArgId)
        {
            var weapon = EntityManager.Instance.GetItem(mapClient.Inventory.WeaponDrawer[mapClient.Inventory.ActiveWeaponDrawer]);
            var ammoClassId = weapon.ItemTemplate.Weapon.AmmoClassId;
            var foundAmmo = false;
            var foundAmmoAmount = 0;

            for (var i = 0; i < 50; i++)
            {
                if (mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(mapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                if (weaponAmmo == null)
                    return;

                if (weaponAmmo.ItemTemplate.ClassId == ammoClassId)
                {
                    // consume ammo
                    int ammoToGrab = Math.Min(weapon.ItemTemplate.Weapon.ClipSize - foundAmmoAmount - weapon.CurrentAmmo, weaponAmmo.Stacksize);
                    foundAmmoAmount = ammoToGrab + weapon.CurrentAmmo;
                    InventoryManager.Instance.ReduceStackCount(mapClient, weaponAmmo, ammoToGrab);
                    foundAmmo = true;
                    if (foundAmmoAmount == weapon.ItemTemplate.Weapon.ClipSize)
                        break;
                }
            }

            if (foundAmmo == false)
                return; // no ammo found -> ToDo: Tell the client?

            mapClient.Player.Client.CellSendPacket(mapClient, mapClient.Player.Actor.EntityId, new PerformWindupPacket { ActionId = actionId, ActionArgId = actionArgId });
            ActorActionManager.Instance.RequestWeaponReload(mapClient, mapClient.Player.Actor, weapon, foundAmmoAmount);
        }
    }
}
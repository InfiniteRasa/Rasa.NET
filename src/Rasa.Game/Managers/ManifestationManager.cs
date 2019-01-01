using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Database.Tables.Character;
    using Game;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Game.Server;
    using Structures;

    public class ManifestationManager
    {
        /* Actor: Player "bodies" (ManifestationClass)
         * 
         *    Manifestation Packets:
         *  - CurrentCharacterId                => implemented
         *  - AllCredits                        => implemented
         *  - UpdateCredits                     => implemented
         *  - LockboxFunds                      => implemented
         *  - WonBattleground                   => ToDo
         *  - LostBattleground                  => ToDo
         *  - WeaponDrawerSlot                  => implemented
         *  - AbilityDrawerSlot                 => implemented
         *  - AbilityDrawer                     => implemented
         *  - ArmWeaponFailed                   => ToDo
         *  - ArmAbilityFailed                  => ToDo
         *  - AdvancementStats                  => implemented
         *  - ExperienceChanged                 => ToDo
         *  - LevelChanged                      => ToDo
         *  - CharacterClass                    => implemented
         *  - AvailableAllocationPoints
         *  - AvailableCharacterClasses
         *  - TierAdvancementInfo
         *  - InvitedToJoinFriend
         *  - InvitationDeclined
         *  - InvitationCancelled
         *  - CannotInvite
         *  - InvitedToAddAndJoinFriend
         *  - RequestToJoin
         *  - JoinFriendDeclined
         *  - JoinFriendCancelled
         *  - CannotJoin
         *  - ForceConverse
         *  - LogosStoneTabula
         *  - LogosStoneAdded
         *  - LogosStoneRemoved
         *  - ShowHelmetChanged
         *  - Titles
         *  - TitleChanged
         *  - TitleAdded
         *  - TitleRemoved
         *  - PlayerFlags
         *  - CloneCredits
         *  - WaypointGained
         *  - GraveyardGained
         *  - CharacterName
         *  - RaceId
         *  - PlayerAfk
         *  - PlayerInactiveWarning
         *  - ClanId
         *  - IsTrialAccount
         *  - PlayerEnteredCombat
         *  - PlayerExitedCombat
         *  - MinionAdded
         *  - MinionStayAck
         *  - MinionGoAck
         *  - MinionFollowMeAck
         *  - MinionFollowTargetAck
         *  - MinionTargetMeAck
         *  - MinionTargetAck
         *  - MinionAssistMeAck
         *  - MinionAssistTargetAck
         *  - MinionTemperamentAck
         *  - MinionCommandAck
         *  
         *  Manifestation Handlrs:
         *  - AutoFireKeepAlive         => ToDo
         *  - ChangeShowHelmet          => ToDo
         *  - ChangeTitle               => implemented, but need more work on it
         *  - RespondToAddAndJoinFriend => ToDo
         *  - RespondToJoinFriend       => ToDo
         *  - RespondToRequestToJoin    => ToDo
         *  - RequestArmAbility         => implemented
         *  - RequestArmWeapon          => implemented
         *  - RequestSetAbilitySlot     => implemented
         *  - RequestSwapAbilitySlots   => implemented
         *  - StartAutoFire             => implemented, but need more work on it
         *  - StopAutoFire              => implemented, but need more work on it
         */

        private static ManifestationManager _instance;
        private static readonly object InstanceLock = new object();
        private static List<AutoFireTimer> AutoFire = new List<AutoFireTimer>();

        public static ManifestationManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ManifestationManager();
                    }
                }

                return _instance;
            }
        }

        private ManifestationManager()
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

        #region Handlers
        public void AutoFireKeepAlive(Client client, int keepAliveDelay)
        {
            foreach (var timer in AutoFire)
                if (timer.Client == client)
                    timer.MaxAliveTime = keepAliveDelay*4; // 10 sec should be enof for all weapons
        }

        public void ChangeShowHelmet(Client client, ChangeShowHelmetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo ChangeShowHelmet");
        }

        public void ChangeTitle(Client client, uint titleId)
        {
            //if (titleId != 0)
            //{
            client.MapClient.Player.CurrentTitle = titleId;
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new TitleChangedPacket(titleId));
            /*}
            else
            {
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new TitleRemovedPacket(client.MapClient.Player.CurrentTitle));
                client.MapClient.Player.CurrentTitle = titleId;
            }

            client.MapClient.Player.CurentTitle = titleId;*/
        }

        public void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            client.MapClient.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            // ToDo do we need upate Database???
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new AbilityDrawerSlotPacket(abilityDrawerSlot));
        }

        public void RequestArmWeapon(Client client, uint requestedWeaponDrawerSlot)
        {
            client.MapClient.Inventory.ActiveWeaponDrawer = requestedWeaponDrawerSlot;

            client.CallMethod(client.MapClient.Player.Actor.EntityId, new WeaponDrawerSlotPacket(requestedWeaponDrawerSlot, true));

            var weapon = EntityManager.Instance.GetItem(client.MapClient.Inventory.WeaponDrawer[(int)client.MapClient.Inventory.ActiveWeaponDrawer]);

            if (weapon == null)
                return;

            client.MapClient.Inventory.EquippedInventory[13] = weapon.EntityId;

            NotifyEquipmentUpdate(client);
            SetAppearanceItem(client, weapon);
            UpdateAppearance(client);
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.ActiveWeapon, (byte)requestedWeaponDrawerSlot);
            // update ammo info
            client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));
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
                client.MapClient.Player.Abilities.TryGetValue(packet.SlotId, out AbilityDrawerData ability);
                if (ability == null)
                {
                    client.MapClient.Player.Abilities.Add(packet.SlotId, new AbilityDrawerData { AbilitySlotId = packet.SlotId, AbilityId = (int)packet.AbilityId, AbilityLevel = (int)packet.AbilityLevel });
                }
                else
                {
                    client.MapClient.Player.Abilities[packet.SlotId].AbilityId = (int)packet.AbilityId;
                    client.MapClient.Player.Abilities[packet.SlotId].AbilityLevel = (int)packet.AbilityLevel;
                    client.MapClient.Player.Abilities[packet.SlotId].AbilitySlotId = packet.SlotId;
                }
            }
            // update database with new drawer slot ability
            CharacterAbilityDrawerTable.UpdateCharacterAbility(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, packet.SlotId, (int)packet.AbilityId, (int)packet.AbilityLevel);
            // send packet
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new AbilityDrawerPacket(client.MapClient.Player.Abilities));
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
                client.AccountEntry.Id,
                client.AccountEntry.SelectedSlot,
                abilities[packet.ToSlot].AbilitySlotId,
                abilities[packet.ToSlot].AbilityId,
                abilities[packet.ToSlot].AbilityLevel);
            // check if fromSlot isn't empty now
            abilities.TryGetValue(packet.FromSlot, out AbilityDrawerData tempSlot);
            if (tempSlot != null)
                CharacterAbilityDrawerTable.UpdateCharacterAbility(
                    client.AccountEntry.Id,
                    client.AccountEntry.SelectedSlot,
                    abilities[packet.FromSlot].AbilitySlotId,
                    abilities[packet.FromSlot].AbilityId,
                    abilities[packet.FromSlot].AbilityLevel);
            else
                CharacterAbilityDrawerTable.UpdateCharacterAbility(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, packet.FromSlot, 0, 0);
            // send packet
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new AbilityDrawerPacket(abilities));
        }

        public void StartAutoFire(Client client, double yaw)
        {
            if (!client.MapClient.Player.WeaponReady)
            {
                RequestWeaponDraw(client);
                return;
            }

            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            // do we need to reload?
            if (weapon.CurrentAmmo < weapon.ItemTemplate.WeaponInfo.AmmoPerShot)
            {
                RequestWeaponReload(client, true);
                return;
            }

            // send first hit to cell, ignore self
            client.CellIgnoreSelfSendPacket(client, new PerformRecoveryPacket(PerformType.ListOfArgs, weaponClassInfo.WeaponAttackActionId, (uint)weaponClassInfo.WeaponAttackArgId));
            
            RegisterAutoFire(client, weapon);

            /*
            // decrease ammo count
            weapon.CurrentAmmo -= weapon.ItemTemplate.WeaponInfo.AmmoPerShot;

            // start auto fire loop
            RegisterAutoFire(client, weapon);

            /*
            client.SendPacket(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));

            // update ammo in Db
            ItemsTable.UpdateItemCurrentAmmo(weapon.ItemId, weapon.CurrentAmmo);

            if (client.MapClient.Player.TargetEntityId == 0)
                return; // do not continue if no target

            var targetType = EntityManager.Instance.GetEntityType(client.MapClient.Player.TargetEntityId);
            
            if (targetType == EntityType.Player) //1:client-type,0=player-type
            {
                var mapCell = CellManager.Instance.TryGetCell(
                    client.MapClient.MapChannel,
                    client.MapClient.Player.Actor.CellLocation.CellPosX,
                    client.MapClient.Player.Actor.CellLocation.CellPosY
                    );

                if (mapCell != null)
                {
                    var clientList = client.MapClient.MapChannel.ClientList;

                    foreach (var targetPlayer in clientList)
                        if (targetPlayer.MapClient.Player.Actor.EntityId == client.MapClient.Player.TargetEntityId)
                        {
                            client.MapClient.Player.TargetEntityId = targetPlayer.MapClient.Player.Actor.EntityId;
                            break;
                        }
                }

            }

            var weaponClassInfo = EntityClassManager.Instance.LoadedEntityClasses[weapon.ItemTemplate.ClassId].WeaponClassInfo;

            var weaponAttack = new RequestWeaponAttackPacket
            {
                ActionId = weaponClassInfo.WeaponAttackActionId,
                ActionArgId = weaponClassInfo.WeaponAttackArgId,
                TargetId = client.MapClient.Player.TargetEntityId
            };

            MissileManager.Instance.RequestWeaponAttack(client, weaponAttack);
            */
        }

        public void StopAutoFire(Client client)
        {
            // go backwards through list
            for (var i = AutoFire.Count - 1; i >= 0; i--)
            {
                var timer = AutoFire[i];

                if (timer.Client == client)
                {
                    AutoFire.RemoveAt(i);
                    break;
                }
            }
        }

        #endregion

        #region Helper Functions

        public void AllocateAttributePoints(Client client, AllocateAttributePointsPacket packet)
        {
            client.MapClient.Player.SpentBody += packet.Body;
            client.MapClient.Player.SpentMind += packet.Mind;
            client.MapClient.Player.SpentSpirit += packet.Spirit;

            UpdateStatsValues(client, false);

            // update DB
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Attributes, null);

            // Send Data to client
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new AttributeInfoPacket(client.MapClient.Player.Actor.Attributes));
        }

        public void AssignPlayer(Client client)
        {
            var player = client.MapClient.Player;
            var actor = player.Actor;
            
            // get charaterOptions
            var optionsList = CharacterOptionsTable.GetCharacterOptions(player.CharacterId);

            foreach (var characterOption in optionsList)
                player.CharacterOptions.Add(new CharacterOptions((CharacterOption)characterOption.OptionId, characterOption.Value));

            client.CallMethod(SysEntity.ClientMethodId, new CharacterOptionsPacket(player.CharacterOptions));

            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(actor.EntityId));

            client.CallMethod(SysEntity.ClientGameMapId, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(client.MapClient.MapChannel.MapInfo.MapContextId));

            client.CallMethod(actor.EntityId, new UpdateRegionsPacket { RegionIdList = client.MapClient.MapChannel.MapInfo.BaseRegionId });  // ToDo this should be list of regions? or just curent region wher player is
            
            client.CallMethod(actor.EntityId, new AdvancementStatsPacket(
                player.Level,
                player.Experience,
                GetAvailableAttributePoints(player),
                0,       // trainPoints (are not used by the client??)
                GetSkillPointsAvailable(player)
            ));

            client.CallMethod(actor.EntityId, new SkillsPacket(player.Skills));

            client.CallMethod(actor.EntityId, new AbilitiesPacket(player.Skills));

            // don't send this packet if abilityDrawer is empty
            if (player.Abilities.Count > 0)
                client.CallMethod(actor.EntityId, new AbilityDrawerPacket(player.Abilities));

            client.CallMethod(actor.EntityId, new TitlesPacket(player.Titles));

            client.CallMethod(actor.EntityId, new UpdateAttributesPacket(actor.Attributes, 0));

            client.CallMethod(actor.EntityId, new UpdateHealthPacket(actor.Attributes[Attributes.Health], 0));

            client.CallMethod(actor.EntityId, new LogosStoneTabulaPacket(player.Logos));
        }

        public void AutoFireTimerDoWork(long delta)
        {
            // go backwards through list
            for (var i = AutoFire.Count - 1; i >= 0; i--)
            {
                var timer = AutoFire[i];
                // we dont want to server keep fireing if client crash 
                timer.MaxAliveTime -= delta;

                if (timer.MaxAliveTime <= 0)
                {
                    AutoFire.RemoveAt(i);
                    continue;
                }

                timer.Delay -= delta;

                if (timer.Delay <= 0)
                {
                    MissileManager.Instance.PlayerTryFireWeapon(timer.Client);
                    timer.Delay = timer.RefireTime;
                }
            }
        }

        public void CellDiscardClientToPlayers(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(client.MapClient.Player.Actor.EntityId));
            }
        }

        public void CellDiscardPlayersToClient(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(tempClient.MapClient.Player.Actor.EntityId));
            }

        }

        public void CellIntroduceClientToPlayers(Client client, List<Client> clientList)
        {
            var player = client.MapClient.Player;
            var netMovement = new NetCompressedMovement
            {
                EntityId = player.Actor.EntityId,
                Flag = 0,
                PosX24b = (uint)player.Actor.Position.X * 256,
                PosY24b = (uint)player.Actor.Position.Y * 256,
                PosZ24b = (uint)player.Actor.Position.Z * 256
            };

            foreach (var tempClient in clientList)
            {
                Logger.WriteLog(LogType.Debug, $"ClientToPlayers:\n {tempClient.MapClient.Player.Actor.EntityId} is reciving data about {player.Actor.EntityId}");

                tempClient.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(player.Actor.EntityId, player.Actor.EntityClassId, CreatePlayerEntityData(client)));

                //tempPlayer.Player.Client.SendPacket(mapClient.Player.Actor.EntityId, netMovement);
            }

            // Recv_Abilities (id: 10, desc: must only be sent for the local manifestation)
            // We dont need to send ability data to every client, but only the owner (which is done in PlayerManager.AssignPlayer)
            // Skills -> Everything that the player can learn via the skills menu (Sprint, Firearms...) Abilities -> Every skill gained by logos?
            // Recv_WorldLocationDescriptor

        }

        public void CellIntroducePlayersToClient(Client client, List<Client> clientList)
        {
            foreach (var tempClient in clientList)
            {
                // don't send data about yourself
                if (client == tempClient)
                    continue;

                Logger.WriteLog(LogType.Debug, $"PlayersToClient:\n {client.MapClient.Player.Actor.EntityId} is reciving data about {tempClient.MapClient.Player.Actor.EntityId}");
                client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(tempClient.MapClient.Player.Actor.EntityId, tempClient.MapClient.Player.Actor.EntityClassId, CreatePlayerEntityData(tempClient)));
                
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

        public List<PythonPacket> CreatePlayerEntityData(Client client)
        {
            var player = client.MapClient.Player;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo((EntityClassId)player.Actor.EntityClassId).TargetFlag),
                new WorldLocationDescriptorPacket(player.Actor.Position, player.Actor.Rotation),
                // Manifestation
                new CurrentCharacterIdPacket(player.CharacterId),
                new AllCreditsPacket(player.Credits),
                new LockboxFundsPacket(player.LockboxCredits),
                new CharacterClassPacket(player.Class),

                new AttributeInfoPacket(player.Actor.Attributes),
                new PreloadDataPacket(client.MapClient.Inventory.EquippedInventory[13], player.Abilities),
                new AppearanceDataPacket(player.AppearanceData),
                new ResistanceDataPacket(player.ResistanceData),
                new ActorControllerInfoPacket(true),
                new LevelPacket(player.Level),
                new CharacterNamePacket(player.Actor.Name),
                new ActorNamePacket(player.Actor.FamilyName),
                new IsRunningPacket(player.Actor.IsRunning),
                new TargetCategoryPacket(0),    // 0 frendly ToDo
                new PlayerFlagsPacket(),
                new EquipmentInfoPacket(client.MapClient.Inventory.EquippedInventory)
            };

            return entityData;
        }

        public void GainCredits(Client client, uint credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, (long)credits);
            // send player message
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(262, new Dictionary<string, string> { { "amount", credits.ToString() } }, MsgFilterId.LootObtained));
        }

        public void LossCredits(Client client, uint credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, -credits);
        }

        public int GetAvailableAttributePoints(Manifestation player)
        {
            var points = player.Level * 2 - 2;
            points -= player.SpentBody;
            points -= player.SpentMind;
            points -= player.SpentSpirit;
            points = Math.Max(points, 0);
            return points;
        }

        public void GetCustomizationChoices(Client client, GetCustomizationChoicesPacket packet)
        {
            // ToDo
            var test = EntityManager.Instance.GetEntityType((uint)packet.EntityId);
            var testChoices = new Dictionary<int, int>
            {
                { 3663, 36 },
                { 3672, 42 },
                { 3812, 60 }
            };
            client.CallMethod(SysEntity.ClientMethodId, new CustomizationChoicesPacket((uint)packet.EntityId, testChoices));
        }

        public int GetSkillIndexById(int skillId)
        {
            return skillId < 0 ? -1 : skillId >= 200 ? -1 : SkillId2Idx[skillId];
        }

        public int GetSkillPointsAvailable(Manifestation player)
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

        public void LevelSkills(Client client, LevelSkillsPacket packet)
        {
            var mapClient = client.MapClient;
            var skillPointsAvailable = GetSkillPointsAvailable(mapClient.Player);
            var skillLevelupArray = new Dictionary<int, SkillsData>(); // used to temporarily safe skill level updates

            for (var i = 0; i < packet.ListLenght; i++)
            {
                var skillId = packet.SkillIds[i];

                if (skillId == -1)
                    throw new Exception("LevelSkills: Invalid skillId received. Modified or outdated client?");

                var oldSkillLevel = 0;
                var abilityId = SkillIdx2AbilityId[GetSkillIndexById(packet.SkillIds[i])];

                if (mapClient.Player.Skills.ContainsKey(skillId))
                    oldSkillLevel = mapClient.Player.Skills[skillId].SkillLevel;
                else
                {
                    // create new entry in character skils and db
                    mapClient.Player.Skills.Add(skillId, new SkillsData(skillId, abilityId, 0));
                    CharacterSkillsTable.SetCharacterSkill(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, skillId, abilityId, 0);
                }

                var newSkillLevel = packet.SkillLevels[i];

                if (newSkillLevel < oldSkillLevel || newSkillLevel > 5)
                    throw new Exception("LevelSkills: Invalid skill level received\n");

                var additionalSkillPointsRequired = requiredSkillLevelPoints[newSkillLevel] - requiredSkillLevelPoints[oldSkillLevel];

                skillPointsAvailable -= additionalSkillPointsRequired;
                skillLevelupArray.Add(skillId, new SkillsData(skillId, abilityId, newSkillLevel - oldSkillLevel));

            }
            // do we have enough skill points for the skill level ups?
            if (skillPointsAvailable < 0)
                throw new Exception("PlayerManager.LevelSkills: Not enough skill points. Modified or outdated client?\n");
            // everything ok, update skills!
            foreach (var skill in skillLevelupArray)
                mapClient.Player.Skills[skill.Value.SkillId].SkillLevel += skillLevelupArray[skill.Value.SkillId].SkillLevel;
            // send skill update to client
            client.CallMethod(mapClient.Player.Actor.EntityId, new SkillsPacket(mapClient.Player.Skills));
            // set abilities
            client.CallMethod(mapClient.Player.Actor.EntityId, new AbilitiesPacket(mapClient.Player.Skills));   // ToDo
            // update allocation points
            client.CallMethod(mapClient.Player.Actor.EntityId, new AvailableAllocationPointsPacket
            {
                AvailableAttributePoints = GetAvailableAttributePoints(mapClient.Player),
                TrainPoints = 0,        // not used?
                AvailableSkillPoints = GetSkillPointsAvailable(mapClient.Player)
            });
            // update database with new character skills
            foreach (var skill in skillLevelupArray)
                CharacterSkillsTable.UpdateCharacterSkill(client.AccountEntry.Id, client.AccountEntry.SelectedSlot, mapClient.Player.Skills[skill.Key].SkillId, mapClient.Player.Skills[skill.Key].SkillLevel);
        }

        public void NotifyEquipmentUpdate(Client client)
        {
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new EquipmentInfoPacket(client.MapClient.Inventory.EquippedInventory));
        }
        
        public void RegisterAutoFire(Client client, Item weapon)
        {
            // create timer
            var timer = new AutoFireTimer(client, weapon.ItemTemplate.WeaponInfo.RefireTime, weapon.ItemTemplate.WeaponInfo.RefireTime);
            
            AutoFire.Add(timer);

            // launch missile
           // MissileManager.Instance.PlayerTryFireWeapon(timer.Client);
        }

        public void RemovePlayerCharacter(Client client)
        {
            // ToDo do we need remove something, or it's done already 
        }

        public void RemoveAppearanceItem(Client client, EquipmentData equipmentSlotId)
        {
            if (equipmentSlotId == 0)
                return;

            client.MapClient.Player.AppearanceData[equipmentSlotId].Class = 0;
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(client.MapClient.Player.CharacterId, (uint)equipmentSlotId, 0, 0);
        }
        
        public void RequestCustomization(Client client, RequestCustomizationPacket packet)
        {
            // ToDo
        }

        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            // ToDo
            //client.MapClient.MapChannel.PerformActions.Add(new ActionData(client, packet.ActionId, packet.ActionArgId, packet.Target));
        }        
        
        public void RequestToggleRun(Client client)
        {
            client.MapClient.Player.Actor.IsRunning = !client.MapClient.Player.Actor.IsRunning;

            client.CallMethod(client.MapClient.Player.Actor.EntityId, new IsRunningPacket(client.MapClient.Player.Actor.IsRunning));
        }
        
        public void RequestWeaponDraw(Client client)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            client.MapClient.MapChannel.PerformActions.Add(new ActionData(client, ActionId.WeaponDraw, (uint)weaponClassInfo.DrawActionId));

            WeaponReady(client, true);
        }

        public void RequestWeaponReload(Client client, bool isRequested)
        {
            // here we only check, can we reload weapon
            // actual weapon reload happen if reaload action isn't interupted
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);
            var foundAmmo = 0u;

            for (var i = 0; i < 50; i++)
            {
                if (client.MapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(client.MapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                // check is empty slot
                if (weaponAmmo == null)
                    return;

                if (weaponAmmo.ItemTemplate.Class == weaponClassInfo.AmmoClassId)
                {
                    // consume ammo
                    var ammoToGrab = (uint)Math.Min(weaponClassInfo.ClipSize - foundAmmo - weapon.CurrentAmmo, weaponAmmo.Stacksize);
                    foundAmmo = ammoToGrab + weapon.CurrentAmmo;
                }

                if (foundAmmo == weaponClassInfo.ClipSize)
                    break;
            }

            if (foundAmmo == 0)
                return; // no ammo found -> ToDo: Tell the client?

            if (isRequested)
                client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));
            else
                client.CellIgnoreSelfCallMethod(client, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));

            client.MapClient.MapChannel.PerformActions.Add(new ActionData(client, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId, foundAmmo, weapon.ItemTemplate.WeaponInfo.ReloadTime));
        }

        public void RequestWeaponStow(Client client)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            client.MapClient.MapChannel.PerformActions.Add(new ActionData(client, ActionId.WeaponStow, (uint)weaponClassInfo.StowActionId));
            
            WeaponReady(client, false);
        }

        public void SaveCharacterOptions(Client client, SaveCharacterOptionsPacket packet)
        {
            if (packet.OptionsList.Count == 0)
                return;

            client.MapClient.Player.CharacterOptions = packet.OptionsList;

            var value = "";

            foreach (var option in client.MapClient.Player.CharacterOptions)
                value = value + $" ('{client.MapClient.Player.CharacterId}', '{(uint)option.OptionId}', '{option.Value}'),";

            // remove last comma
            value = value.Remove(value.Length - 1);

            CharacterOptionsTable.DeleteCharacterOptions(client.MapClient.Player.CharacterId);
            CharacterOptionsTable.AddCharacterOption(value + ";");
        }

        // maybe move this to other manager becose it's account related
        public void SaveUserOptions(Client client, SaveUserOptionsPacket packet)
        {
            client.UserOptions = packet.OptionsList;

            var value = "";

            foreach (var option in client.UserOptions)
                value = value + $" ('{client.AccountEntry.Id}', '{(uint)option.OptionId}', '{option.Value}'),";
            
            // remove last comma
            value = value.Remove(value.Length - 1);

            UserOptionsTable.DeleteUserOptions(client.AccountEntry.Id);
            UserOptionsTable.AddUserOption(value + ";");
            
        }

        public void SetAppearanceItem(Client client, Item item)
        {
            var equipmentSlotId = EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId;
            var player = client.MapClient.Player;

            if (!player.AppearanceData.ContainsKey(equipmentSlotId))
            {
                // Add new appearance slot to character and db
                player.AppearanceData.Add(equipmentSlotId, new AppearanceData { SlotId = equipmentSlotId });

                var newAppearence = new CharacterAppearanceEntry(player.CharacterId, (uint)equipmentSlotId, 0, 0);

                CharacterAppearanceTable.AddAppearance(player.CharacterId, newAppearence);
            }

            player.AppearanceData[equipmentSlotId].Class = item.ItemTemplate.Class;
            player.AppearanceData[equipmentSlotId].Color = new Color(item.Color);
            
            // update appearance data in database
            CharacterAppearanceTable.UpdateCharacterAppearance(client.MapClient.Player.CharacterId, (uint)equipmentSlotId, (uint)item.ItemTemplate.Class, item.Color);
        }

        public void SetTargetId(Client client, long entityId)
        {
            client.MapClient.Player.TargetEntityId = (uint)entityId;
        }

        public void SetTrackingTarget(Client client, uint entityId)
        {
            client.MapClient.Player.TrackingTargetEntityId = entityId;
        }

        public void UpdateAppearance(Client client)
        {
            if (client.MapClient.Player == null)
                return;

            client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new AppearanceDataPacket(client.MapClient.Player.AppearanceData));
        }

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        public void UpdateStatsValues(Client client, bool fullreset)
        {
            var player = client.MapClient.Player;
            var attribute = player.Actor.Attributes;
            // body
            attribute[Attributes.Body].NormalMax = 10 + (player.Level - 1) * 2 + player.SpentBody;
            var bodyBonus = 0;
            attribute[Attributes.Body].CurrentMax = attribute[Attributes.Body].NormalMax + bodyBonus;
            attribute[Attributes.Body].Current = attribute[Attributes.Body].CurrentMax;
            // mind
            attribute[Attributes.Mind].NormalMax = 10 + (player.Level - 1) * 2 + player.SpentMind;
            var mindBonus = 0;
            attribute[Attributes.Mind].CurrentMax = attribute[Attributes.Mind].NormalMax + mindBonus;
            attribute[Attributes.Mind].Current = attribute[Attributes.Mind].CurrentMax;
            // spirit
            attribute[Attributes.Spirit].NormalMax = 10 + (player.Level - 1) * 2 + player.SpentSpirit;
            var spiritBonus = 0;
            attribute[Attributes.Spirit].CurrentMax = attribute[Attributes.Spirit].NormalMax + spiritBonus;
            attribute[Attributes.Spirit].Current = attribute[Attributes.Spirit].CurrentMax;
            // health
            attribute[Attributes.Health].NormalMax = 100 + (player.Level - 1) * 2 * 8 + player.SpentBody * 6;
            var healthBonus = 0;
            attribute[Attributes.Health].CurrentMax = attribute[Attributes.Health].NormalMax + healthBonus;
            if (fullreset)
                attribute[Attributes.Health].Current = attribute[Attributes.Health].CurrentMax;
            else
                attribute[Attributes.Health].Current = Math.Min(attribute[Attributes.Health].Current, attribute[Attributes.Health].CurrentMax);
            // chi/adrenaline
            attribute[Attributes.Chi].NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentSpirit * 3;
            var chiBonus = 0;
            attribute[Attributes.Chi].CurrentMax = attribute[Attributes.Chi].NormalMax + chiBonus;
            if (fullreset)
                attribute[Attributes.Chi].Current = attribute[Attributes.Chi].CurrentMax;
            else
                attribute[Attributes.Chi].Current = Math.Min(attribute[Attributes.Chi].Current, attribute[Attributes.Chi].CurrentMax);
            // update regen rate
            attribute[Attributes.Regen].NormalMax = 100 + (player.Level - 1) * 2 + Math.Max(0, (attribute[Attributes.Spirit].CurrentMax - 10)) * 6; // regenRate in percent
            var regenBonus = 0;
            attribute[Attributes.Regen].CurrentMax = attribute[Attributes.Regen].NormalMax + regenBonus;
            attribute[Attributes.Regen].RefreshAmount = (int)Math.Round(2D * (attribute[Attributes.Regen].CurrentMax / 100), 0); // 2.0 per second is the base regeneration for health
            // calculate armor max
            var armorMax = 0.0d;
            //float armorBonus = 0; // todo! (From item modules)
            var armorBonusPct = player.Actor.Attributes[Attributes.Body].CurrentMax * 0.0066666d;
            var armorRegenRate = 0;

            for (var i = 1; i < 21; i++)
            {
                if (client.MapClient.Inventory.EquippedInventory[i] == 0)
                    continue;

                // skip weapon slot
                if (i == 13)
                    continue;

                var equipmentItem = EntityManager.Instance.GetItem(client.MapClient.Inventory.EquippedInventory[i]);
                var classInfo = EntityClassManager.Instance.GetClassInfo(equipmentItem.ItemTemplate.Class);

                if (equipmentItem == null)
                {
                    // this is very bad, how can the item disappear while it is still linked in the inventory?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Equipment item has no physical copy (item is missing)");
                    continue;
                }
                if (classInfo.ArmorClassInfo == null)
                {
                    // how can the player equip non-armor?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Player try to equip non_armor item");
                    continue;
                }
                armorMax += equipmentItem.ItemTemplate.ArmorValue;      // ToDo
                armorRegenRate += classInfo.ArmorClassInfo.RegenRate;
                
                // what about damage absorbed? Was it used at all?
            }
            armorMax = armorMax * (1.0d + armorBonusPct);
            attribute[Attributes.Armor].Current = armorRegenRate;
            attribute[Attributes.Armor].NormalMax = (int)Math.Round(armorMax, 0);
            attribute[Attributes.Armor].CurrentMax = attribute[Attributes.Armor].NormalMax;
            if (fullreset)
                attribute[Attributes.Armor].Current = attribute[Attributes.Armor].CurrentMax;
            else
                attribute[Attributes.Armor].Current = Math.Min(attribute[Attributes.Armor].Current, attribute[Attributes.Armor].CurrentMax);
            // added by krssrb
            // power test
            attribute[Attributes.Power].NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentMind * 3;
            var powerBonus = 0;
            attribute[Attributes.Power].CurrentMax = attribute[Attributes.Power].NormalMax + powerBonus;
            if (fullreset)
                attribute[Attributes.Power].Current = attribute[Attributes.Power].CurrentMax;
            else
                attribute[Attributes.Power].Current = Math.Min(attribute[Attributes.Power].Current, attribute[Attributes.Power].CurrentMax);
        }

        public void WeaponReady(Client client, bool isReady)
        {
            client.MapClient.Player.WeaponReady = isReady;
            client.CallMethod(client.MapClient.Player.Actor.EntityId, new WeaponReadyPacket(isReady));
        }

        public void WeaponReload(ActionData action)
        {
            // we reload weapon here
            var client = action.Client;
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            
            if (weapon == null)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon); ;
            var ammoClassId = weaponClassInfo.AmmoClassId;
            var foundAmmo = 0U;

            for (var i = 0; i < 50; i++)
            {
                if (client.MapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(client.MapClient.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                // check is empty slot
                if (weaponAmmo == null)
                    return;

                if (weaponAmmo.ItemTemplate.Class == weaponClassInfo.AmmoClassId)
                {
                    // consume ammo
                    var ammoToGrab = (uint)Math.Min(weaponClassInfo.ClipSize - foundAmmo - weapon.CurrentAmmo, weaponAmmo.Stacksize);
                    foundAmmo = ammoToGrab + weapon.CurrentAmmo;
                    InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, weaponAmmo, ammoToGrab);
                }

                if (foundAmmo == weaponClassInfo.ClipSize)
                    break;
            }

            // update the ammo count
            weapon.CurrentAmmo = foundAmmo;

            // update db
            ItemsTable.UpdateItemCurrentAmmo(weapon.ItemId, foundAmmo);
            
            // set current action to 0
            client.MapClient.Player.Actor.CurrentAction = 0;

            // send data to client
            client.CellCallMethod(client, client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, foundAmmo));
        }
        
        #endregion
    }
}

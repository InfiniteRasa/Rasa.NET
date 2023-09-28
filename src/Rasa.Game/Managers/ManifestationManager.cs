using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Communicator.Server;
    using Packets.Game.Server;
    using Packets.Manifestation.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
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
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        private static List<AutoFireTimer> AutoFire = new List<AutoFireTimer>();
        public static byte MaxPlayerLevel = 50;
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
                            _instance = new ManifestationManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private ManifestationManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
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
            client.Player.CurrentTitle = titleId;
            client.CallMethod(client.Player.EntityId, new TitleChangedPacket(titleId));
            /*}
            else
            {
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new TitleRemovedPacket(client.MapClient.Player.CurrentTitle));
                client.MapClient.Player.CurrentTitle = titleId;
            }

            client.MapClient.Player.CurentTitle = titleId;*/
        }

        public bool PlayerTryFireWeapon(Client client)
        {
            // ToDo: isOverheated, isJammed, and some other checks
            if (!client.Player.WeaponReady)
            {
                RequestWeaponDraw(client);
                return false;
            }

            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            // do we need to reload?
            if (weapon.CurrentAmmo < weapon.ItemTemplate.WeaponInfo.AmmoPerShot)
            {
                RequestWeaponReload(client, true);
                return false;
            }

            // decrease ammo count
            weapon.CurrentAmmo -= weapon.ItemTemplate.WeaponInfo.AmmoPerShot;
            client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));

            // should we update db per shot? it will be a lot of db calls
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Items.UpdateAmmo(weapon);

            // let's calculate damage
            var damageRange = weaponClassInfo.MaxDamage - weaponClassInfo.MinDamage;
            var damage = weaponClassInfo.MinDamage + new Random().Next(0, damageRange + 1);
            var action = new ActionData(client.Player, weaponClassInfo.WeaponAttackActionId, weaponClassInfo.WeaponAttackArgId, client.Player.Target, 0);
            // launch correct missile type depending on weapon type
            MissileManager.Instance.MissileLaunch(client.Player.MapChannel, action, damage);
            
            return true;
        }

        public void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            client.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            // ToDo do we need upate Database???
            client.CallMethod(client.Player.EntityId, new AbilityDrawerSlotPacket(abilityDrawerSlot));
        }

        public void RequestArmWeapon(Client client, uint requestedWeaponDrawerSlot)
        {
            client.Player.ActiveWeapon = (byte)requestedWeaponDrawerSlot;

            client.CallMethod(client.Player.EntityId, new WeaponDrawerSlotPacket(requestedWeaponDrawerSlot, true));

            var weapon = EntityManager.Instance.GetItem(client.Player.Inventory.WeaponDrawer[client.Player.ActiveWeapon]);

            if (weapon == null)
                return;

            client.Player.Inventory.EquippedInventory[13] = weapon.EntityId;

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
                client.Player.Abilities.Remove(packet.SlotId);
            }
            else
            {
                // added new ability
                client.Player.Abilities.TryGetValue(packet.SlotId, out AbilityDrawerData ability);
                if (ability == null)
                {
                    client.Player.Abilities.Add(packet.SlotId, new AbilityDrawerData(packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel));
                }
                else
                {
                    client.Player.Abilities[packet.SlotId].AbilityId = (int)packet.AbilityId;
                    client.Player.Abilities[packet.SlotId].AbilityLevel = (uint)packet.AbilityLevel;
                    client.Player.Abilities[packet.SlotId].AbilitySlotId = packet.SlotId;
                }
            }
            // update database with new drawer slot ability
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel);
            // send packet
            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(client.Player.Abilities));
        }

        public void RequestSwapAbilitySlots(Client client, RequestSwapAbilitySlotsPacket packet)
        {
            AbilityDrawerData toSlot;
            var abilities = client.Player.Abilities;
            var fromSlot = abilities[packet.FromSlot];
            abilities.TryGetValue(packet.ToSlot, out toSlot);
            if (toSlot == null)
            {
                abilities.Add(packet.ToSlot, new AbilityDrawerData(packet.ToSlot, fromSlot.AbilityId, fromSlot.AbilityLevel));
                abilities.Remove(packet.FromSlot);
            }
            else
            {
                abilities[packet.ToSlot] = abilities[packet.FromSlot];
                abilities[packet.ToSlot].AbilitySlotId = packet.ToSlot;
                abilities[packet.FromSlot] = toSlot;
                abilities[packet.FromSlot].AbilitySlotId = packet.FromSlot;
            }
            // Do we need to update database here ???
            // update database with new drawer slot ability
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                client.Player.Id,
                abilities[packet.ToSlot].AbilitySlotId,
                abilities[packet.ToSlot].AbilityId,
                abilities[packet.ToSlot].AbilityLevel);
            // check if fromSlot isn't empty now
            abilities.TryGetValue(packet.FromSlot, out AbilityDrawerData tempSlot);
            if (tempSlot != null)
                unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                    client.Player.Id,
                    abilities[packet.FromSlot].AbilitySlotId,
                    abilities[packet.FromSlot].AbilityId,
                    abilities[packet.FromSlot].AbilityLevel);
            else
                unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.FromSlot, 0, 0);
            // send packet
            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(abilities));
        }

        public void StartAutoFire(Client client, double yaw)
        {
            // ToDo:
            // yaw is probobly used to mach player and target orientation,
            // some creatures recive more damage from back then from front

            if (PlayerTryFireWeapon(client))
            {
                ActorManager.Instance.RequestVisualCombatMode(client, true);
                RegisterAutoFire(client);
            }
        }

        public void StopAutoFire(Client client)
        {
            ActorManager.Instance.RequestVisualCombatMode(client, false);

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
            client.Player.SpentBody += packet.Body;
            client.Player.SpentMind += packet.Mind;
            client.Player.SpentSpirit += packet.Spirit;

            UpdateStatsValues(client, false);

            // update DB
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Attributes, null);

            // Send Data to client
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
        }

        public void AssignPlayer(Client client)
        {
            var player = client.Player;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // get charaterOptions
            var optionsList = unitOfWork.CharacterOptions.Get(player.Id);

            foreach (var characterOption in optionsList)
                player.CharacterOptions.Add(new CharacterOptions((CharacterOption)characterOption.OptionId, characterOption.Value));

            client.CallMethod(SysEntity.ClientMethodId, new CharacterOptionsPacket(player.CharacterOptions));

            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(player.EntityId));

            client.CallMethod(player.EntityId, new WeaponDrawerSlotPacket(player.ActiveWeapon, false));

            client.CallMethod(SysEntity.ClientGameMapId, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(client.Player.MapChannel.MapInfo.MapContextId));

            SocialManager.Instance.SetSocialContactList(client);

            client.CallMethod(player.EntityId, new ActorInfoPacket(player));

            client.CallMethod(player.EntityId, new UpdateRegionsPacket { RegionIdList = client.Player.MapChannel.MapInfo.BaseRegionId });  // ToDo this should be list of regions? or just curent region wher player is

            client.CallMethod(player.EntityId, new AdvancementStatsPacket(
                player.Level,
                player.Experience,
                GetAvailableAttributePoints(player),
                0,       // trainPoints (are not used by the client??)
                GetSkillPointsAvailable(player)
            ));

            client.CallMethod(player.EntityId, new SkillsPacket(player.Skills));

            client.CallMethod(player.EntityId, new AbilitiesPacket(player.Skills));

            // don't send this packet if abilityDrawer is empty
            if (player.Abilities.Count > 0)
                client.CallMethod(player.EntityId, new AbilityDrawerPacket(player.Abilities));

            client.CallMethod(player.EntityId, new TitlesPacket(player.Titles));

            client.CallMethod(player.EntityId, new UpdateAttributesPacket(player.Attributes, 0));

            client.CallMethod(player.EntityId, new UpdateHealthPacket(player.Attributes[Attributes.Health], 0));

            client.CallMethod(player.EntityId, new LogosStoneTabulaPacket(player.Logos));

            client.CallMethod(player.EntityId, new AllCreditsPacket(player.Credits));

            client.CallMethod(player.EntityId, new LockboxFundsPacket(player.LockboxCredits));
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
                    PlayerTryFireWeapon(timer.Client);
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

                tempClient.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(client.Player.EntityId));
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

                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(tempClient.Player.EntityId));
            }

        }

        public void CellIntroduceClientToPlayers(Client client, List<Client> clientList)
        {
            var player = client.Player;

            foreach (var tempClient in clientList)
            {
                // don't send data about yourself
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, CreatePlayerEntityData(client)));

            }
        }

        public void CellIntroduceClientToSefl(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(client.Player.EntityId, client.Player.EntityClass, CreatePlayerEntityData(client)));
        }

        public void CellIntroducePlayersToClient(Client client, List<Client> clientList)
        {
            foreach (var tempClient in clientList)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(tempClient.Player.EntityId, tempClient.Player.EntityClass, CreatePlayerEntityData(tempClient)));
            }
        }
		
		public List<PythonPacket> CreatePlayerEntityData(Client client)
        {
            var player = client.Player;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(player.EntityClass).TargetFlag),
                new WorldLocationDescriptorPacket(player.Position, player.Rotation),
                // Manifestation
                new CurrentCharacterIdPacket(player.EntityId),
                new CharacterClassPacket(player.Class),
                new AttributeInfoPacket(player.Attributes),
                new PreloadDataPacket(client.Player.Inventory.EquippedInventory[13], player.Abilities),
                new AppearanceDataPacket(player.AppearanceData),
                new ResistanceDataPacket(player.ResistanceData),
                new ActorControllerInfoPacket(true),
                new LevelPacket(player.Level),
                new CharacterNamePacket(player.Name),
                new ActorNamePacket(player.FamilyName),
                new IsRunningPacket(player.IsRunning),
                new TargetCategoryPacket(Factions.AFS),
                new PlayerFlagsPacket(),
                new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory)
            };

            return entityData;
        }

        internal void GainExperience(Client client, uint experience)
        {
            if (client.Player.Level >= MaxPlayerLevel)
                return; // cannot gain xp over level 50

            client.Player.Experience += experience;

            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Expirience, client.Player.Experience);

            var xpInfo = new XPInfo(client.Player.Experience, experience, experience);

            client.CallMethod(client.Player.EntityId, new ExperienceChangedPacket(xpInfo));

            // check for level up
            while (client.Player.Level < MaxPlayerLevel)
            {
                var xpForLevelUp = GetLevelNeededExperience(client.Player.Level);

                if (xpForLevelUp == -1)
                    break;

                if (client.Player.Experience >= xpForLevelUp)
                {
                    // level up
                    client.Player.Level++;



                    // update database
                    CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Level);

                    // update client
                    client.CallMethod(client.Player.EntityId, new LevelUpPacket(client.Player.Level));

                    var msgArg = new Dictionary<string, string>
                    {
                        { "level", client.Player.Level.ToString() },
                        { "attributePts", GetAvailableAttributePoints(client.Player).ToString() },  // todo: send correct number of new attribute points
                        { "skillPts", GetSkillPointsAvailable(client.Player).ToString() }           // todo: send correct number of new skill points
                    };

                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmLevelIncreased, msgArg, MsgFilterId.LeveledUp));

                    // todo: For all others send Recv_setLevel() to update level display for this player
                    // update stats
                    UpdateStatsValues(client, true);
                    client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
                    SendAvailableAllocationPoints(client);
                }
                else
                    break;
            }


        }

        public void DebugChgPlayerClass(Client client, uint newClassId)
        {
            client.Player.Class = newClassId;
            client.CallMethod(client.Player.EntityId, new CharacterClassPacket(client.Player.Class));
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Class, client.Player.Class);
        }

        public void GainCredits(Client client, int credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, credits);
            // send player message
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmGotMoneyLootFromUnknown, new Dictionary<string, string> { { "amount", credits.ToString() } }, MsgFilterId.LootObtained));
        }

        public void LossCredits(Client client, int credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, credits);
        }

        public int GetAvailableAttributePoints(Manifestation player)
        {
            var points = 3 * (player.Level - 1);
            points -= player.SpentBody;
            points -= player.SpentMind;
            points -= player.SpentSpirit;
            //points = Math.Max(points, 0); Probably do not need this? (StaticVariable)
            return points;
        }

        public void GetCustomizationChoices(Client client, GetCustomizationChoicesPacket packet)
        {
            // ToDo
            var test = EntityManager.Instance.GetEntityType(packet.EntityId);
            var testChoices = new Dictionary<int, int>
            {
                { 3663, 36 },
                { 3672, 42 },
                { 3812, 60 }
            };
            client.CallMethod(SysEntity.ClientMethodId, new CustomizationChoicesPacket(packet.EntityId, testChoices));
        }

        private int GetLevelNeededExperience(int level)
        {
            if (level < 1 || level >= 50)
                return -1;

            return ExpPerLevel.ExpRequred[level];
        }

        public int GetSkillIndexById(int skillId)
        {
            return skillId < 0 ? -1 : skillId >= 200 ? -1 : SkillId2Idx[skillId];
        }

        public int GetSkillPointsAvailable(Manifestation player)
        {
            var level = player.Level;

            var pointsAvailable = (player.Level - 1) * 2;
            pointsAvailable += 5; // add five points because of the recruit skills that start at level 1

            if (level >= 5)
                pointsAvailable += 2;

            if (level >= 15)
                pointsAvailable += 2;

            if (level >= 30)
                pointsAvailable += 2;

            if (level >= 50)
                pointsAvailable += 4;

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
            var skillPointsAvailable = GetSkillPointsAvailable(client.Player);
            var skillLevelupArray = new Dictionary<SkillId, SkillsData>(); // used to temporarily safe skill level updates
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            for (var i = 0; i < packet.ListLenght; i++)
            {
                var skillId = (SkillId)packet.SkillIds[i];

                if (skillId == SkillId.None)
                    throw new Exception("LevelSkills: Invalid skillId received. Modified or outdated client?");

                var oldSkillLevel = 0;
                var abilityId = SkillIdx2AbilityId[GetSkillIndexById(packet.SkillIds[i])];

                if (client.Player.Skills.ContainsKey(skillId))
                    oldSkillLevel = client.Player.Skills[skillId].SkillLevel;
                else
                {
                    // create new entry in character skils
                    client.Player.Skills.Add(skillId, new SkillsData(skillId, abilityId, 0));
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
                client.Player.Skills[skill.Value.SkillId].SkillLevel += skillLevelupArray[skill.Value.SkillId].SkillLevel;
            // send skill update to client
            client.CallMethod(client.Player.EntityId, new SkillsPacket(client.Player.Skills));
            // set abilities
            client.CallMethod(client.Player.EntityId, new AbilitiesPacket(client.Player.Skills));   // ToDo
            // update allocation points
            SendAvailableAllocationPoints(client);
            // update database with new character skills
            foreach (var skill in skillLevelupArray)
            {
                var skillToUpdate = client.Player.Skills[skill.Value.SkillId];
                unitOfWork.CharacterSkills.AddOrUpdate(client.Player.Id, (uint)skillToUpdate.SkillId, skillToUpdate.AbilityId, skillToUpdate.SkillLevel);
            }
        }

        public void NotifyEquipmentUpdate(Client client)
        {
            client.CallMethod(client.Player.EntityId, new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory));
        }

        public void RegisterAutoFire(Client client)
        {
            // create timer
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var timer = new AutoFireTimer(client, weapon.ItemTemplate.WeaponInfo.Refire, weapon.ItemTemplate.WeaponInfo.Refire);

            AutoFire.Add(timer);

            // launch missile
            //MissileManager.Instance.PlayerTryFireWeapon(timer.Client);
        }

        public void RemovePlayerCharacter(Client client)
        {
            // ToDo do we need remove something, or it's done already 
        }

        public void RemoveAppearanceItem(Client client, EquipmentData equipmentSlotId)
        {
            if (equipmentSlotId == 0)
                return;

            client.Player.AppearanceData[equipmentSlotId].Class = 0;
            // update appearance data in database
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAppearances.AddOrUpdate(client.Player.Id, new CharacterAppearanceEntry((uint)equipmentSlotId, 0, 0));
            unitOfWork.Complete();
        }

        public void RequestCustomization(Client client, RequestCustomizationPacket packet)
        {
            // ToDo
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestCustomization");
        }

        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, (uint)packet.ActionArgId, packet.Target, 0));
        }

        public void RequestToggleRun(Client client)
        {
            client.Player.IsRunning = !client.Player.IsRunning;

            client.CallMethod(client.Player.EntityId, new IsRunningPacket(client.Player.IsRunning));
        }

        public void RequestWeaponDraw(Client client)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.WeaponDraw, weaponClassInfo.DrawActionId, 500));

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
                if (client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                // check is empty slot
                if (weaponAmmo == null)
                    return;

                if (weaponAmmo.ItemTemplate.Class == weaponClassInfo.AmmoClassId)
                {
                    // consume ammo
                    var ammoToGrab = Math.Min(weaponClassInfo.ClipSize - foundAmmo - weapon.CurrentAmmo, weaponAmmo.StackSize);
                    foundAmmo = ammoToGrab + weapon.CurrentAmmo;
                }

                if (foundAmmo == weaponClassInfo.ClipSize)
                    break;
            }

            if (foundAmmo == 0)
                return; // no ammo found -> ToDo: Tell the client?

            if (isRequested)
                client.CellCallMethod(client, client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));
            else
                client.CellIgnoreSelfCallMethod(client, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));

            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId, foundAmmo, weapon.ItemTemplate.WeaponInfo.ReloadTime));
        }

        public void RequestWeaponStow(Client client)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.WeaponStow, (uint)weaponClassInfo.StowActionId, 500));

            WeaponReady(client, false);
        }

        public void SaveCharacterOptions(Client client, SaveCharacterOptionsPacket packet)
        {
            if (packet.OptionsList.Count == 0)
                return;

            client.Player.CharacterOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.Player.CharacterOptions)
                unitOfWork.CharacterOptions.AddOrUpdate(client.Player.Id, (uint)option.OptionId, option.Value);
        }

        // maybe move this to other manager becose it's account related
        public void SaveUserOptions(Client client, SaveUserOptionsPacket packet)
        {
            client.UserOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.UserOptions)
                unitOfWork.UserOptions.AddOrUpdate(client.AccountEntry.Id, (uint)option.OptionId, option.Value);

            unitOfWork.Complete();
        }

        internal void SendAvailableAllocationPoints(Client client)
        {
            // update available allocation points (attributes, trainPts, skillPts)

            var attributePoints = GetAvailableAttributePoints(client.Player);
            var trainPoints = 0;    // not used by te client
            var skillPoints = GetSkillPointsAvailable(client.Player);

            client.CallMethod(client.Player.EntityId, new AvailableAllocationPointsPacket(attributePoints, trainPoints, skillPoints));
        }

        public void SetAppearanceItem(Client client, Item item)
        {
            var equipmentSlotId = EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (!client.Player.AppearanceData.ContainsKey(equipmentSlotId))
                client.Player.AppearanceData.Add(equipmentSlotId, new AppearanceData { SlotId = equipmentSlotId });

            client.Player.AppearanceData[equipmentSlotId].Class = (uint)item.ItemTemplate.Class;
            client.Player.AppearanceData[equipmentSlotId].Color = new Color(item.Color);
            client.Player.AppearanceData[equipmentSlotId].Hue2 = new Color(item.Color);

            // update appearance data in database

            unitOfWork.CharacterAppearances.AddOrUpdate(client.Player.Id, new CharacterAppearanceEntry((uint)equipmentSlotId, (uint)item.ItemTemplate.Class, item.Color));
            unitOfWork.Complete();
        }

        public void SetDesiredCrouchState(Client client, bool crouching)
        {
            client.Player.IsCrouching = crouching;

            client.CallMethod(client.Player.EntityId, new SetDesiredCrouchStatePacket(client.Player.IsCrouching ? CharacterState.Crouched : CharacterState.Standing));
        }

        public void SetTargetId(Client client, ulong entityId)
        {
            client.Player.Target = entityId;
        }

        public void SetTrackingTarget(Client client, ulong entityId)
        {
            client.Player.TrackingTargetEntityId = entityId;
        }

        public void UpdateAppearance(Client client)
        {
            if (client.Player == null)
                return;

            client.CellCallMethod(client, client.Player.EntityId, new AppearanceDataPacket(client.Player.AppearanceData));
        }

        // Health calculation:
        //levelBasedHealth = 20000.0
        //for (int i = level; i< 50; ++i)
        //levelBasedHealth = levelBasedHealth - 0.082995 * levelBasedHealth;

        private readonly float[] HealthBaselinePerLevel =
         {
            286.5784148f,
            312.51565127f,
            340.8003787f,
            371.6450605f,
            405.28138942f,
            441.96202792f,
            481.96250612f,
            525.58329139f,
            573.15204539f,
            625.02608535f,
            681.59506802f,
            743.28391668f,
            810.55601298f,
            883.91667764f,
            963.91696626f,
            1051.15780858f,
            1146.29452247f,
            1250.04173638f,
            1363.17875735f,
            1486.55542483f,
            1621.09849437f,
            1767.818599f,
            1927.81784069f,
            2102.29806892f,
            2292.56990847f,
            2500.06260431f,
            2726.33475751f,
            2973.08603281f,
            3242.1699258f,
            3535.60768567f,
            3855.60349799f,
            4204.56104164f,
            4585.10154431f,
            5000.08347207f,
            5452.62400104f,
            5946.1224323f,
            6484.28572615f,
            7071.15634718f,
            7711.14262974f,
            8409.05189147f,
            9170.12654399f,
            10000.08347172f,
            10905.15697485f,
            11892.14559882f,
            12968.4632023f,
            14142.19464703f,
            15422.15652808f,
            16817.9634005f,
            18340.1f,
            20000f
        };

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        public void UpdateStatsValues(Client client, bool fullreset)
        {
            var player = client.Player;
            var attribute = player.Attributes;

            int level = player.Level;

            // We don't want things to blow up just in case something wrong happens to level
            if (level < 0) level  = 1;
            if (level > 50) level = 50;

            int levelBasedBody   = 0;
            int levelBasedMind   = 0;
            int levelBasedSpirit = 0;

            switch (player.Race)
            {
                case Race.Human:
                    levelBasedBody = levelBasedMind = levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Forean:
                    levelBasedBody = (level - 1) + 10;
                    levelBasedMind = 3 * (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Brann:
                    levelBasedBody = levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 4 * (level - 1) + 10;
                    break;

                case Race.Thrax:
                    levelBasedBody = 3 * (level - 1) + 10;
                    levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;
            }

            int totalBody   = levelBasedBody   + player.SpentBody;
            int totalMind   = levelBasedMind   + player.SpentMind;
            int totalSpirit = levelBasedSpirit + player.SpentSpirit;

            // Health
            float levelBasedHealth = HealthBaselinePerLevel[level - 1];
            levelBasedHealth = levelBasedHealth / (2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10);
            int totalHealth = (int)(levelBasedHealth * (totalSpirit + 2 * totalBody));

            // Power
            float basePower = basePower = (3 * level + 100) / (2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10);
            int totalPower  = (int)(basePower * (totalBody + 2 * totalMind));

            // Regen
            float baseRegen = (2 * level + 100) / (2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10);
            int totalRegen = (int)(baseRegen * (totalMind + 2 * totalSpirit));
          
            // Bonuses
            var bodyBonus = 0;
            var mindBonus = 0;
            var spiritBonus = 0;

            var healthBonus = 0;
            var chiBonus    = 0;
            var regenBonus  = 0;

            float armorBonusPercent = (float)Math.Max(0.0, (totalBody - (2 * (level - 1) + 10)) * 0.667);   // every body attribute over the default base attribute gives 0.667% bonus armo;
            float logosBonusPercent = (float)Math.Max(0.0, (totalMind - (2 * (level - 1) + 10)) * 0.375);   // every mind attribute over the default base attribute gives 0.375% bonus logos damage
            float critBonusPercent = (float)Math.Max(0.0, (totalSpirit - (2 * (level - 1) + 10)) * 0.065);  // every spirit attribute over the default base attribute gives 0.065% bonus crit chance;


            // body
            attribute[Attributes.Body].NormalMax    = totalBody;
            attribute[Attributes.Body].CurrentMax   = attribute[Attributes.Body].NormalMax + bodyBonus;
            attribute[Attributes.Body].Current      = attribute[Attributes.Body].Current;

            attribute[Attributes.Mind].NormalMax    = totalMind;
            attribute[Attributes.Mind].CurrentMax   = attribute[Attributes.Mind].NormalMax + mindBonus;
            attribute[Attributes.Mind].Current      = attribute[Attributes.Mind].Current;

            attribute[Attributes.Spirit].NormalMax  = totalSpirit;
            attribute[Attributes.Spirit].CurrentMax = attribute[Attributes.Spirit].NormalMax + spiritBonus;
            attribute[Attributes.Spirit].Current    = attribute[Attributes.Spirit].CurrentMax;

            // health
            attribute[Attributes.Health].NormalMax  = totalHealth;
            attribute[Attributes.Health].CurrentMax = totalHealth;

            // chi/adrenaline
            attribute[Attributes.Chi].NormalMax     = totalPower;
            attribute[Attributes.Chi].CurrentMax    = totalPower;

            attribute[Attributes.Regen].NormalMax   = totalRegen; // regenRate in percent
            attribute[Attributes.Regen].CurrentMax  = totalRegen;

            if (fullreset)
            {
                attribute[Attributes.Health].Current = attribute[Attributes.Health].CurrentMax;
                attribute[Attributes.Chi].Current = attribute[Attributes.Chi].CurrentMax;
            }
            else
            {
                attribute[Attributes.Health].Current = Math.Min(attribute[Attributes.Health].Current, attribute[Attributes.Health].CurrentMax);
                attribute[Attributes.Chi].Current = Math.Min(attribute[Attributes.Chi].Current, attribute[Attributes.Chi].CurrentMax);
            }


            // update regen rate
            attribute[Attributes.Regen].RefreshAmount = (int)Math.Round(2D * (attribute[Attributes.Regen].CurrentMax / 100), 0);
            // 2.0 per second is the base regeneration for health
            // calculate armor max
            var armorMax = 0.0d;
            //float armorBonus = 0; // todo! (From item modules)
            var armorBonusPct = player.Attributes[Attributes.Body].CurrentMax * 0.0066666d;
            var armorRegenRate = 0;

            for (var i = 1; i < 21; i++)
            {
                if (client.Player.Inventory.EquippedInventory[i] == 0)
                    continue;

                // skip weapon slot
                if (i == 13)
                    continue;

                var equipmentItem = EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[i]);
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
            client.Player.WeaponReady = isReady;
            client.CallMethod(client.Player.EntityId, new WeaponReadyPacket(isReady));
        }

        public void WeaponReload(ActionData action)
        {
            // we reload weapon here
            var client = Server.Clients.Find(c => c.Player.EntityId == action.Actor.EntityId);
            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            if (weapon == null)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon); ;
            var ammoClassId = weaponClassInfo.AmmoClassId;
            var foundAmmo = 0U;

            for (var i = 0; i < 50; i++)
            {
                if (client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                // check is empty slot
                if (weaponAmmo == null)
                    return;

                if (weaponAmmo.ItemTemplate.Class == weaponClassInfo.AmmoClassId)
                {
                    // consume ammo
                    var ammoToGrab = Math.Min(weaponClassInfo.ClipSize - foundAmmo - weapon.CurrentAmmo, weaponAmmo.StackSize);
                    foundAmmo = ammoToGrab + weapon.CurrentAmmo;
                    InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, weaponAmmo, ammoToGrab);
                }

                if (foundAmmo == weaponClassInfo.ClipSize)
                    break;
            }

            // update the ammo count
            weapon.CurrentAmmo = foundAmmo;

            // update db
            ItemManager.Instance.UpdateItemCurrentAmmo(weapon);

            // set current action to 0
            client.Player.CurrentAction = 0;

            // send data to client
            client.CellCallMethod(client, client.Player.EntityId, new PerformRecoveryPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, foundAmmo));
        }

        #endregion
    }
}

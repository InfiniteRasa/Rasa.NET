using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Packets.ClientMethod.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Structures;
    using Structures.Char;

    public class CharacterManager
    {
        private static CharacterManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly object _createLock = new();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public const ulong SelectionPodStartEntityId = 100;
        public const byte MaxSelectionPods = 16;

        public static CharacterManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CharacterManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        public CharacterManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.Characters.Any(), client.AccountEntry.Id, client.AccountEntry.CanSkipBootcamp));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var charactersBySlot = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                CharacterEntry character = null;
                if (charactersBySlot.ContainsKey(i))
                {
                    character = charactersBySlot[i];
                }
                SendCharacterInfoProdCreate(client, i, character);
            }

            client.State = ClientState.CharacterSelection;

            // get userOptions
            var optionsList = unitOfWork.UserOptions.Get(client.AccountEntry.Id);

            foreach (var userOption in optionsList)
                client.UserOptions.Add(new UserOptions((UserOption)userOption.OptionId, userOption.Value));
            
            client.CallMethod(SysEntity.ClientMethodId, new UserOptionsPacket(client.UserOptions));
        }

        public void RequestCharacterName(Client client, int gender)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNames.GetFirstName((Gender)gender);
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = name
            });
        }

        public void RequestFamilyName(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNames.GetLastName();
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = name
            });
        }

        public void RequestCloneCharacterToSlot(Client client, RequestCloneCharacterToSlotPacket packet)
        {
            //    var result = packet.Validate();
            //    if (result != CreateCharacterResult.Success)
            //    {
            //        SendCharacterCreateFailed(client, result);
            //        return;
            //    }

            //    CharacterEntry entry;
            //    var clonedCharacter = CharacterTable.GetCharacter(client.AccountEntry.Id, packet.CloneSlotNum);

            //    lock (CreateLock)
            //    {
            //        entry = new CharacterEntry
            //        {
            //            AccountId = client.AccountEntry.Id,
            //            Slot = packet.SlotNum,
            //            Name = packet.CharacterName,
            //            Race = (byte)packet.RaceId,
            //            Class = clonedCharacter.Class,
            //            Scale = packet.Scale,
            //            Gender = packet.Gender,
            //            Experience = clonedCharacter.Experience,
            //            Level = clonedCharacter.Level,
            //            Body = clonedCharacter.Body,
            //            Mind = clonedCharacter.Mind,
            //            Spirit = clonedCharacter.Spirit,
            //            MapContextId = clonedCharacter.MapContextId,
            //            CoordX = clonedCharacter.CoordX,
            //            CoordY = clonedCharacter.CoordY,
            //            CoordZ = clonedCharacter.CoordZ,
            //            Orientation = clonedCharacter.Orientation,
            //        };

            //        if (!CharacterTable.CreateCharacter(entry))
            //        {
            //            SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
            //            return;
            //        }

            //        // Set character appearance
            //        CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Shoes, (uint)Data.EntityClass.ArmorRecruitV01CMNBoots, 2139062144));
            //        CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Torso, (uint)Data.EntityClass.ArmorRecruitV01CMNVest, 2139062144));
            //        CharacterAppearanceTable.AddAppearance(entry.Id, new CharacterAppearanceEntry(entry.Id, (uint)EquipmentData.Legs, (uint)Data.EntityClass.ArmorRecruitV01CMNLegs, 2139062144));

            //        foreach (var data in packet.AppearanceData)
            //        {
            //            data.Value.Class = (Data.EntityClass)StarterItemsTable.GetClassId((uint)data.Value.Class);
            //            CharacterAppearanceTable.AddAppearance(entry.Id, data.Value.GetDatabaseEntry(entry.Id));
            //        }
            //    }

            //    // Give character basic items
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 0, ItemsTable.CreateItem(145, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[17131]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 50, ItemsTable.CreateItem(28, 100, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[28]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 1, ItemsTable.CreateItem(13126, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13126]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 2, ItemsTable.CreateItem(13066, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13066]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 3, ItemsTable.CreateItem(13096, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13096]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 4, ItemsTable.CreateItem(13186, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13186]].ItemClassInfo.MaxHitPoints, 2139062144));
            //    CharacterInventoryTable.AddInvItem(client.AccountEntry.Id, packet.SlotNum, (int)InventoryType.Personal, 5, ItemsTable.CreateItem(13156, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13156]].ItemClassInfo.MaxHitPoints, 2139062144));

            //    // Create default entry in CharacterAbilitiesTable
            //    for (var i = 0; i < 25; i++)
            //        CharacterAbilityDrawerTable.SetCharacterAbility(client.AccountEntry.Id, packet.SlotNum, i, 0, 0);

            //    client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, client.AccountEntry.FamilyName));
            //    ++client.AccountEntry.CharacterCount;

            //    SendCharacterInfo(client, packet.SlotNum, entry, false);

            //    // reduce cloneCredit on cloned character
            //    clonedCharacter.CloneCredits--;
            //    CharacterTable.UpdateCharacterCloneCredits(clonedCharacter.Id, clonedCharacter.CloneCredits);

            //    // update character selection pod
            //    SendCharacterInfo(client, clonedCharacter.Slot, clonedCharacter, false);
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            var result = packet.Validate();
            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            uint characterId;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // TODO to remove this lock, the family name check and update must be redesigned to be thread safe
            lock (_createLock)
            {
                var createdCharacterId = InternalCreate(client, packet, unitOfWork);

                if (createdCharacterId == null)
                {
                    return;
                }

                characterId = createdCharacterId.Value;
            }

            // give basic items
            GiveBasicItems(client, characterId);

            // give first lockbox tab
            if (unitOfWork.CharacterLockboxes.Get(client.AccountEntry.Id) == null)
                unitOfWork.CharacterLockboxes.Add(client.AccountEntry.Id);

			unitOfWork.Complete();
			
            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, packet.FamilyName));

            client.ReloadGameAccountEntry();

            var character = unitOfWork.Characters.Get(characterId);
            SendCharacterInfo(client, packet.SlotNum, character);
        }

        private uint? InternalCreate(Client client, RequestCreateCharacterInSlotPacket packet, ICharUnitOfWork unitOfWork)
        {
            var changeFamilyName = false;
            if (!string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) && packet.FamilyName != client.AccountEntry.FamilyName)
            {
                if (!client.AccountEntry.Characters.Any())
                {
                    changeFamilyName = true;
                }
                else
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterName);
                    return null;
                }
            }

            if ((string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || packet.FamilyName != client.AccountEntry.FamilyName))
            {
                if (!unitOfWork.GameAccounts.CanChangeFamilyName(client.AccountEntry.Id, packet.FamilyName))
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.FamilyNameReserved);
                    return null;
                }
            }

            var characterEntry = unitOfWork.Characters.Create(client.AccountEntry, packet.SlotNum,
                packet.CharacterName,
                (byte)packet.RaceId,
                packet.Scale,
                packet.Gender);
            if (characterEntry == null)
            {
                SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                return null;
            }

            var appearances = CreateCharacterAppearanceEntries(packet);
            unitOfWork.CharacterAppearances.Add(characterEntry, appearances);

            if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
            {
                unitOfWork.GameAccounts.UpdateFamilyName(client.AccountEntry.Id, packet.FamilyName);
            }

            return characterEntry.Id;
        }

        private IEnumerable<CharacterAppearanceEntry> CreateCharacterAppearanceEntries(RequestCreateCharacterInSlotPacket packet)
        {
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Shoes, (uint)EntityClasses.ArmorRecruitV01CMNBoots, 2139062144);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Torso, (uint)EntityClasses.ArmorRecruitV01CMNVest, 2139062144);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Legs, (uint)EntityClasses.ArmorRecruitV01CMNLegs, 2139062144);

            using var worldUnitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var appearancesFromPacket = packet.AppearanceData
                .Select(appearanceData => CreateCharacterAppearanceEntry(appearanceData.Value, worldUnitOfWork))
                .ToList();

            foreach (var characterAppearanceEntry in appearancesFromPacket)
            {
                yield return characterAppearanceEntry;
            }
        }

        private static CharacterAppearanceEntry CreateCharacterAppearanceEntry(AppearanceData appearanceData, IWorldUnitOfWork unitOfWork)
        {
            var databaseEntry = appearanceData.GetDatabaseEntry();
            databaseEntry.Class = unitOfWork.Equipment.GetItemClass(appearanceData.Class);
            return databaseEntry;
        }

        private void GiveBasicItems(Client client, uint characterId)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 0, unitOfWork.Items.CreateItem(new Item(145, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[17131]].ItemClassInfo.MaxHitPoints, 2139062144)));
            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 50, unitOfWork.Items.CreateItem(new Item(28, 100, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[28]].ItemClassInfo.MaxHitPoints, 2139062144)));
            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 1, unitOfWork.Items.CreateItem(new Item(13126, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13126]].ItemClassInfo.MaxHitPoints, 2139062144)));
            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 2, unitOfWork.Items.CreateItem(new Item(13186, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13186]].ItemClassInfo.MaxHitPoints, 2139062144)));
            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 3, unitOfWork.Items.CreateItem(new Item(13156, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13156]].ItemClassInfo.MaxHitPoints, 2139062144)));
            //unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 4, unitOfWork.Items.CreateItem(new Item(13066, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13066]].ItemClassInfo.MaxHitPoints, 2139062144)));
            //unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId, (int)InventoryType.Personal, 5, unitOfWork.Items.CreateItem(new Item(13096, 1, EntityClassManager.Instance.LoadedEntityClasses[ItemManager.Instance.ItemTemplateItemClass[13096]].ItemClassInfo.MaxHitPoints, 2139062144)));
        }

        public void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet)
        {
            try
            {
                var charactersBySlot = client.AccountEntry.GetCharacterBySlot(packet.Slot);
                if (charactersBySlot == null)
                {
                    return;
                }

                using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                {
                    unitOfWork.CharacterAppearances.DeleteForChar(charactersBySlot.Id);
                    // TODO delete ClanMember entry
                    unitOfWork.Characters.Delete(charactersBySlot.Id);
                    unitOfWork.Complete();
                }

                client.ReloadGameAccountEntry();

                client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(client.AccountEntry.Characters.Any()));

                SendCharacterInfo(client, packet.Slot, null);
            }
            catch
            {
                client.CallMethod(SysEntity.ClientMethodId, new DeleteCharacterFailedPacket());
            }
        }

        public void RequestSwitchToCharacterInSlot(Client client, RequestSwitchToCharacterInSlotPacket packet)
        {
            if (packet.SlotNum < 1 || packet.SlotNum > 16)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            client.AccountEntry.SelectedSlot = packet.SlotNum;
            unitOfWork.GameAccounts.UpdateSelectedSlot(client.AccountEntry.Id, packet.SlotNum);

            var character = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id, packet.SlotNum);
            unitOfWork.Characters.UpdateLoginData(character.Id);
            unitOfWork.Complete();

            client.Player = CreateCharacterManifestation(client, character);
            client.Player.MapChannel = MapChannelManager.Instance.FindByContextId(client.Player.MapContextId);
            client.LoadingMap = client.Player.MapContextId;
            MapChannelManager.Instance.PassClientToMapInstance(client);
        }

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterInfoProdCreate(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClasses.CharacterSelectionPod);

            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            newEntityPacket.EntityData.Add(characterInfo);

            client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
        }

        private void SendCharacterInfo(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            client.CallMethod(SelectionPodStartEntityId + slot, characterInfo);
        }

        private CharacterInfoPacket CreateCharacterInfoPacket(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = data == null
                ? new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName)
                : new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data);
            return characterInfo;
        }

        private Manifestation CreateCharacterManifestation(Client client, CharacterEntry character)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var characterAppearances = unitOfWork.CharacterAppearances.GetByCharacterId(character.Id);
            var appearanceData = new Dictionary<EquipmentData, AppearanceData>();
            var lockboxInfo = unitOfWork.CharacterLockboxes.Get(client.AccountEntry.Id);
            var missions = unitOfWork.CharacterMissions.Get(client.AccountEntry.Id, client.AccountEntry.SelectedSlot);
            var missionData = new Dictionary<int, MissionLog>();
            var clan = unitOfWork.Clans.GetClanByCharacterId(character.Id);
            var logos = unitOfWork.CharacterLogoses.GetLogos(character.Id);

            foreach (var appearance in characterAppearances)
                appearanceData.Add((EquipmentData)appearance.Slot, new AppearanceData(appearance));

            var newCharacter = new Manifestation(character, appearanceData)
            {
                ClanId = clan?.Id ?? 0,
                ClanName = clan?.Name,
                GainedWaypoints = unitOfWork.CharacterTeleporters.Get(character.Id),
                LockboxCredits = lockboxInfo?.Credits ?? 0,
                LockboxTabs = lockboxInfo?.PurashedTabs ?? 0,
                Skills = MapChannelManager.Instance.GetPlayerSkills(character.Id),
                Titles = unitOfWork.CharacterTitles.Get(character.Id),
                Abilities = MapChannelManager.Instance.GetPlayerAbilities(character.Id),
                Missions = missionData,
                LoginTime = DateTime.Now,
                Logos = logos
            };

            return newCharacter;
        }

        public void UpdateCharacter(Client client, CharacterUpdate job, object value = null)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            switch (job)
            {
                case CharacterUpdate.Attributes:
                    unitOfWork.Characters.UpdateCharacterAttributes(client.Player.Id, client.Player.SpentBody, client.Player.SpentMind, client.Player.SpentSpirit);
                    break;

                case CharacterUpdate.Class:
                    unitOfWork.Characters.UpdateCharacterClass(client.Player.Id, client.Player.Class);
                    break;

                case CharacterUpdate.CloneCredits:
                    unitOfWork.Characters.UpdateCharacterCloneCredits(client.Player.Id, client.Player.CloneCredits);
                    break;

                case CharacterUpdate.Credits:
                    var ammount = (int)value;

                    if (ammount < 0)
                        client.Player.Credits[CurencyType.Credits] -= Math.Abs(ammount);
                    else
                        client.Player.Credits[CurencyType.Credits] += ammount;

                    // inform owner
                    client.CallMethod(client.Player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, client.Player.Credits[CurencyType.Credits], 0));
                    // update db
                    unitOfWork.Characters.UpdateCharacterCredits(client.Player.Id, client.Player.Credits[CurencyType.Credits]);
                    break;

                case CharacterUpdate.Expirience:
                    unitOfWork.Characters.UpdateCharacterExpirience(client.Player.Id, client.Player.Experience);
                    break;

                case CharacterUpdate.Level:
                    unitOfWork.Characters.UpdateCharacterLevel(client.Player.Id, client.Player.Level);
                    break;

                case CharacterUpdate.Login:
                    var totalTimePlayed = (DateTime.Now - client.Player.LoginTime).Minutes + client.Player.TotalTimePlayed;

                    unitOfWork.Characters.UpdateCharacterLogin(client.Player.Id, (uint)totalTimePlayed, client.Player.NumLogins);
                    break;

                case CharacterUpdate.Logos:
                    client.Player.Logos.Add((uint)value);
                    unitOfWork.CharacterLogoses.SetLogos(client.Player.Id, (uint)value);
                    client.CallMethod(client.Player.EntityId, new LogosStoneAddedPacket((uint)value));
                    break;

                case CharacterUpdate.Position:
                    var data = value as WonkavatePacket;

                    if (data != null)
                    {
                        var character = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id, client.AccountEntry.SelectedSlot);

                        unitOfWork.Characters.UpdateCharacterPosition(character.Id, data.Position.X, data.Position.Y, data.Position.Z, data.Orientation, data.MapContextId);
                    }
                    else
                        unitOfWork.Characters.UpdateCharacterPosition(
                            client.Player.Id,
                            client.Player.Position.X,
                            client.Player.Position.Y,
                            client.Player.Position.Z,
                            client.Player.Rotation,
                            client.Player.MapContextId
                            );

                    break;

                case CharacterUpdate.Prestige:
                    break;

                case CharacterUpdate.Stats:
                    break;

                case CharacterUpdate.ActiveWeapon:
                    client.Player.ActiveWeapon = (byte)value;
                    unitOfWork.Characters.UpdateCharacterActiveWeapon(client.Player.Id, client.Player.ActiveWeapon);
                    break;
                case CharacterUpdate.Teleporter:
                    var teleporter = (CharacterTeleporterEntry)value;

                    unitOfWork.CharacterTeleporters.Add(teleporter);
                    break;
                default:
                    break;
            }
        }
    }
}

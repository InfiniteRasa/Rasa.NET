using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Clan.Client;
    using Packets.Communicator.Client;
    using Packets.Clan.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Misc;
    using Structures;
    using Structures.Char;
    using Repositories.UnitOfWork;

    public class ClanManager
    {
        /*   Clan Packets:
         * - DisplayClanMessage
         * - InviteToClan
         * - SetClanData
         * - SetClanMemberData
         * - ClanMembersRosterBegin
         * - ClanMembersRosterEnd
         * - PlayerJoinedClan
         * - PlayerLeftClan
         * - ClanDisbanded
         * - ClanDeleted
         * - DisplayClanLeaderInfo
         * - DisplayClanMemberInfoHeader
         * - DisplayClanMemberInfo
         * - ClanCreated
         * - GetPvPClanStatus
         *  
         *   Clan Handlers:
         * - GetPvPClanMembershipStatus
         * - CreateClan
         * - ClanInvitationResponse
         * - InviteToClanByName
         * - InviteToClanById
         * - ClanChangeRankTitle
         * - LeaveClan
         * - DisbandClan
         * - KickPlayerFromClan
         * - RemovePlayer
         */

        #region Singleton

        private static ClanManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _currencyManager;

        // Matches game client limits
        private readonly uint _minClanNameLength = 3;
        private readonly uint _maxClanNameLength = 20;
        private readonly byte _clankRankLeader = 3;
        private readonly int _requiredCreditsForClanCreation = 10000;

        // Arbitrary limit right now
        public static readonly uint _maxClanMembers = 100;

        public static ClanManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ClanManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private ClanManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _currencyManager = new ManifestationManager(gameUnitOfWorkFactory);
        }

        #endregion

        #region Caching

        public ConcurrentDictionary<uint, Lazy<ClanEntry>> Clans { get; set; } = new ConcurrentDictionary<uint, Lazy<ClanEntry>>();
        
        public ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> ClanMembers { get; set; } = new ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>>();

        /// <summary>
        /// Open clan invitations, keyed by the invitee's character id - membership is per
        /// character, not per account. One at a time, like the party invitations next door: the
        /// client shows a single invitation dialog, so a second invitation replaces nothing and
        /// is refused.
        ///
        /// There was no record of an invitation at all. ClanInvitationResponse took the character
        /// and the clan straight out of the packet and acted on them, so a modified client could
        /// answer an invitation that was never sent - putting itself, or any other online player,
        /// into any clan on the server.
        ///
        /// Touched only by the packet handlers, which run on the main loop.
        /// </summary>
        private readonly Dictionary<uint, PendingClanInvite> _invites = new Dictionary<uint, PendingClanInvite>();

        private sealed class PendingClanInvite
        {
            public uint ClanId;
            public uint InviterCharacterId;
            public string InviterName;
        }

        #endregion

        internal void ClansInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            List<ClanEntry> clans = unitOfWork.Clans.GetClans();
            foreach(ClanEntry clan in clans)
            {
                Clans.AddOrUpdate(clan.Id, new Lazy<ClanEntry>(clan), (x, y) => new Lazy<ClanEntry>(clan));

                List<ClanMemberEntry> clanMembers = unitOfWork.ClanMembers.GetAllClanMembersByClanId(clan.Id);
                ClanMembers.AddOrUpdate(clan.Id, new Lazy<List<ClanMemberEntry>>(clanMembers), (x, y) => new Lazy<List<ClanMemberEntry>>(clanMembers));
            }
            InitCurrentClanInventories(unitOfWork.Clans.GetClans());
        }

        internal void InitializePlayerClanData(Client client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            ClanEntry clan = unitOfWork.Clans.GetClanByCharacterId(client.Player.Id);

            if (clan != null)
            {
                var clanData = new ClanData(clan);
                var member = unitOfWork.ClanMembers.GetClanMemberByCharacterId(client.Player.Id);

                RegisterClanMember(member.ClanId, member);

                SetClanData(client, clanData);
                SetClanMemberData(client, clanData);

                // The rest of the clan already holds the roster; what changed is this member's line -
                // online now, and on this map. Every login and zone crossing used to rebuild the whole
                // roster for every member online, and send them the clan's own data again besides.
                SendMemberData(MemberDataFor(client, member, true), client.Player.Id);

                // The lockbox window draws whatever it was last told and asks for nothing, so
                // its tab count and history have to be pushed on the way in.
                InventoryManager.Instance.SendClanLockboxState(client);
            }
        }

        internal void InitCurrentClanInventories(List<ClanEntry> clans)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            foreach (ClanEntry clan in clans)
            {
                List<ClanInventoryEntry> getClanInventoryData = unitOfWork.ClanInventories.GetItems(clan.Id);

                foreach (var item in getClanInventoryData)
                {
                    var itemData = unitOfWork.Items.GetItem(item.ItemId);

                    if (itemData == null)
                    {
                        // A lockbox row whose item is gone used to be dereferenced here, at
                        // server start; the row is garbage and is removed.
                        Logger.WriteLog(LogType.Error, $"Clan {clan.Id} lockbox slot {item.SlotId} refers to item {item.ItemId}, which does not exist; row removed.");
                        unitOfWork.ClanInventories.DeleteInvItemByItemId(item.ItemId);
                        continue;
                    }

                    var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                    // continue, not return: one bad template used to skip every later clan's lockbox.
                    if (itemTemplate == null)
                    {
                        Logger.WriteLog(LogType.Error, $"Item {item.ItemId} has unknown template {itemData.ItemTemplateId}; skipped.");
                        continue;
                    }

                    Item newItem = new Item
                    {
                        OwnerSlotId = item.SlotId,
                        ItemTemplate = itemTemplate,
                        StackSize = itemData.StackSize,
                        CurrentHitPoints = itemData.CurrentHitPoints,
                        Color = itemData.Color,
                        Id = item.ItemId,
                        Crafter = itemData.CrafterName
                    };

                    // check if item is weapon
                    if (newItem.ItemTemplate.WeaponInfo != null)
                        newItem.CurrentAmmo = itemData.AmmoCount;

                    EntityManager.Instance.RegisterEntity(newItem.EntityId, EntityType.Item);
                    EntityManager.Instance.RegisterItem(newItem.EntityId, newItem);
                }
            }
        }

        #region Client Packet Handlers
    
        internal void GetPvPClanMembershipStatus(Client client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            ClanEntry clan = unitOfWork.Clans.GetClanByCharacterId(client.Player.Id);

            string clanName = clan?.Name;
            uint pvpTimeoutSeconds = 0;

            CharacterEntry character = unitOfWork.Characters.Get(client.Player.Id);

            if (character != null)
                pvpTimeoutSeconds = PvPCooldownRemainingSeconds(character);

            client.CallMethod(SysEntity.ClientClanManagerId, new GetPvPClanStatusPacket(clanName, pvpTimeoutSeconds));
        }

        /// <summary>How long a character has to wait before joining or founding a PvP clan.</summary>
        private static readonly TimeSpan PvPClanCooldown = TimeSpan.FromDays(7);

        /// <summary>
        /// Seconds of PvP-clan cooldown left, zero when it has run out. The remaining time is
        /// when the cooldown ends minus now; this used to be computed as now minus the
        /// timestamp (the elapsed time, not the remaining), and in CanCreateClan as a negative
        /// TimeSpan cast to uint, which wrapped to about four billion.
        /// </summary>
        private static uint PvPCooldownRemainingSeconds(CharacterEntry character)
        {
            var remaining = character.LastPvPClan + PvPClanCooldown - DateTime.UtcNow;

            return remaining > TimeSpan.Zero ? (uint)Math.Ceiling(remaining.TotalSeconds) : 0;
        }

        /// <summary>
        /// /changeclanname: renames the clan the caller leads.
        ///
        /// The name is held to exactly what creation holds it to - length, not already taken,
        /// and past the censor - because a name that could not be created should not be
        /// reachable by renaming into it either. No fee: creation charges for bringing a clan
        /// into existence, and there is nothing in the client that quotes a price for this.
        ///
        /// Leader only. The client offers the command to anybody who types it, and the roster
        /// every member holds is its own copy, so this is the only place the rank is real.
        /// </summary>
        internal void ChangeClanName(Client client, ChangeClanNamePacket packet)
        {
            if (client?.Player == null)
                return;

            var clanId = client.Player.ClanId;

            if (clanId == 0)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNotInAClan, new Dictionary<string, string>()));
                return;
            }

            var member = GetClanMember(clanId, client.Player.Id);

            if (member == null || member.Rank != _clankRankLeader)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanInsufficientPermissions, new Dictionary<string, string>()));
                return;
            }

            var newName = packet.ClanName?.Trim();

            if (!IsClanNameAcceptable(client, newName))
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var clan = unitOfWork.Clans.GetClanById(clanId);

            if (clan == null)
                return;

            var oldName = clan.Name;

            // Renaming to the name it already has is not worth an announcement, but it is not an
            // error either - the uniqueness check would have refused it as taken, by itself.
            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                return;

            if (!unitOfWork.Clans.UpdateClanName(clanId, newName))
            {
                Logger.WriteLog(LogType.Error, $"ChangeClanName: could not rename clan {clanId} to '{newName}'.");
                return;
            }

            clan.Name = newName;

            // The cached entry is what every later SetClanData is built from, so it has to move
            // with the row or the clan reverts to its old name for anyone who relogs into a
            // process that has not reloaded.
            if (Clans.ContainsKey(clanId))
                Clans[clanId] = new Lazy<ClanEntry>(() => clan);

            // Everyone's client caches the name too, and shows it in the roster, on the clan
            // window and beside members' names. SetClanData is what refreshes it without a relog.
            SetClanDataForOnlineMembers(clanId);

            CallMethodForOnlineMembers(clanId, (ulong)SysEntity.ClientClanManagerId,
                new DisplayClanMessagePacket((int)PlayerMessage.PmClanClannameChanged,
                    new Dictionary<string, string> { { "oldname", oldName }, { "newname", newName } }));

            Logger.WriteLog(LogType.Debug, $"Clan {clanId} renamed from '{oldName}' to '{newName}' by character {client.Player.Id}.");
        }

        /// <summary>
        /// The name rules shared by creating a clan and renaming one, each answering with the
        /// client's own message for that refusal.
        /// </summary>
        private bool IsClanNameAcceptable(Client client, string clanName)
        {
            if (string.IsNullOrWhiteSpace(clanName) || clanName.Length < _minClanNameLength)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameRequired, new Dictionary<string, string>()));
                return false;
            }

            if (clanName.Length > _maxClanNameLength)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameTooLong, new Dictionary<string, string>()));
                return false;
            }

            if (ClanNameExists(clanName))
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanNameNotAvailable,
                    new Dictionary<string, string> { { "clanname", clanName } }));
                return false;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var censor = new Censor(unitOfWork.CensoredWords.GetCensoredWords());

            if (censor.ContainsProfanity(clanName))
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmInappropriateClanName, new Dictionary<string, string>()));
                return false;
            }

            return true;
        }

        internal void CreateClan(Client client, CreateClanPacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // A character is in one clan at most - clan_member.character_id is unique - and
            // nothing checked here. A member sending CreateClan got as far as the membership
            // insert, which threw on the key: by then the clan row existed and the creation fee
            // had been taken, so the player was 10,000 credits poorer, disconnected, and a clan
            // with no members held the name from the next restart on.
            if (client.Player.ClanId != 0 || unitOfWork.Clans.GetClanByCharacterId(client.Player.Id) != null)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanYouAreAlreadyInAClan, new Dictionary<string, string>()));
                return;
            }

            if (!CanCreateClan(client, packet, client.Player.Id))
                return;

            if (!_currencyManager.LossCredits(
                    client, _requiredCreditsForClanCreation))
                return;

            ClanEntry clan = unitOfWork.Clans.CreateClan(packet.ClanName, packet.IsPvP);

            if (clan == null)
            {
                if (!_currencyManager.GainCredits(
                        client, _requiredCreditsForClanCreation))
                    Logger.WriteLog(LogType.Error,
                        $"CreateClan: could not refund creation fee to character {client.Player.Id}.");
                return;
            }

            // Wrap the database data to what the client expects
            var clanData = new ClanData(clan);

            // Create the member data the client expects 
            // This player created the clan and is the leader
            ClanMemberData clanMemberData = CreateClanMemberData(clanData, client, _clankRankLeader);

            // AddOrUpdate the database with the clan creator as a member of this clan. If this
            // fails the clan row goes with it, so a failed creation leaves nothing behind.
            try
            {
                AddMemberToClan(clanMemberData);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"CreateClan: could not add character {client.Player.Id} as leader of new clan {clan.Id} ({packet.ClanName}); removing the clan: {e}");
                unitOfWork.Clans.DeleteClan(clan.Id);
                if (!_currencyManager.GainCredits(
                        client, _requiredCreditsForClanCreation))
                    Logger.WriteLog(LogType.Error,
                        $"CreateClan: could not refund creation fee to character {client.Player.Id}.");
                return;
            }

            // Signals the client to set the default rank titles for a clan
            client.CallMethod(SysEntity.ClientClanManagerId, new ClanCreatedPacket(clanData.Id));

            // Send the data packets to the client
            SetClanData(client, clanData);
            SetClanMemberData(client, clanData);

            client.Player.ClanId = clan.Id;

            // Cache the newly created clan
            RegisterClan(clan);
        }

        internal void KickPlayerFromClan(Client client, KickPlayerFromClanPacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            if (!TryResolveRankChange(client, packet.ClanId, packet.CharacterId, out var member, out var memberToBeKicked))
                return;

            // The leader and the rank below them can kick - but only somebody below themselves.
            // The rank of the one being kicked was never looked at, so an officer could kick the
            // leader and leave the clan with none.
            if (member.Rank < ClanRank.Leader - 1 || memberToBeKicked.Rank >= member.Rank)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            ClanEntry clan = GetClan(member.ClanId);

            if (clan == null)
                return;

            // Read after the checks, not before them, and before the row it reads is deleted.
            ClanMemberData memberToBeKickedData = GetMemberData(memberToBeKicked.CharacterId);

            if (memberToBeKickedData == null)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (clan.IsPvP)
            {
                // Start the 7 day cooldown for the one being kicked. This used to stamp
                // every member of the clan, so a kick put the whole clan on cooldown.
                unitOfWork.Clans.UpdateLastPvPClanTime(memberToBeKicked.CharacterId, DateTime.UtcNow);
            }

            if (!unitOfWork.ClanMembers.DeleteClanMember(memberToBeKicked))
                return;

            UnregisterClanMember(memberToBeKicked);

            // PlayerLeftClan takes the member out of each client's roster by itself
            // (client/clan.py _RemoveClanMemberFromLists); the whole roster used to be rebuilt and
            // sent to everyone first.
            CallMethodForOnlineMembers(member.ClanId, (uint)SysEntity.ClientClanManagerId,
                new PlayerLeftClanPacket(memberToBeKickedData.CharacterId, memberToBeKickedData.CharacterName, memberToBeKickedData.FamilyName, memberToBeKickedData.ClanId, true),
                packet.CharacterId);

            // Notfies the kicked client that they were kicked and clears out the clan data
            // The client expects the characterId to be the entityId for this message
            var memberClient = Server.Clients.Find(c => c.Player.Id == memberToBeKicked.CharacterId);

            if (memberClient != null)
            {
                memberClient.CallMethod(SysEntity.ClientClanManagerId, new PlayerLeftClanPacket(memberClient.Player.EntityId, memberClient.Player.Name, memberClient.Player.FamilyName, packet.ClanId, true));
                memberClient.CallMethod(memberClient.Player.EntityId, new ClanIdPacket(0));
            }
        }

        /// <summary>
        /// /clankick and /kickclan. The clan window kicks by character id; the slash command has
        /// only what the player typed, which in every by-name command here is the family name -
        /// the same thing InviteToClanByName resolves.
        ///
        /// Resolving it is all this adds: the kick itself, and every permission check on it, is
        /// the by-id path, so the two routes cannot drift apart.
        /// </summary>
        internal void KickPlayerFromClanByName(Client client, KickPlayerFromClanByNamePacket packet)
        {
            if (client?.Player == null || packet == null)
                return;

            if (string.IsNullOrWhiteSpace(packet.Name))
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Get(packet.Name);

            if (account == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"{packet.Name} do not exist");
                return;
            }

            var character = unitOfWork.Characters.GetByAccountId(account.Id, account.SelectedSlot);

            if (character == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"{packet.Name} do not exist");
                return;
            }

            // Both ends have to be in the clan before the by-id path runs: it reads the kicker's
            // member row and the target's without checking either, so a stale ClanId on the
            // client - kicked while their window still showed the clan - threw out of the packet
            // handler. The clan window cannot reach that state, which is why it went unnoticed.
            if (GetClanMember(packet.ClanId, client.Player.Id) == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, "You are not in that clan");
                return;
            }

            if (GetClanMember(packet.ClanId, character.Id) == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"{packet.Name} is not in your clan");
                return;
            }

            KickPlayerFromClan(client, new KickPlayerFromClanPacket(character.Id, packet.ClanId));
        }

        internal void ClanInvitationResponse(Client client, ClanInvitationResponsePacket packet)
        {
            if (client?.Player == null)
                return;

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            // A client answers its own invitation and nobody else's. The entity id in this packet
            // used to name whoever the sender chose, and the clan id any clan at all - so the
            // answer was really an instruction to put that character into that clan, which is
            // what it did.
            if (packet.InvitedCharacterEntityId != client.Player.EntityId)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} (character {client.Player.Id}) answered a clan invitation addressed to entity {packet.InvitedCharacterEntityId}; ignored.");
                return;
            }

            // Answered, either way: the invitation is spent whether they take it or not.
            if (!_invites.Remove(client.Player.Id, out var invite))
            {
                RefuseClanAction(client, PlayerMessage.PmClanNotInvited);
                return;
            }

            // We don't do anything right now when the invitation is declined
            if (!packet.Accepted) return;

            // The clan they were invited to, not the one the packet names.
            if (invite.ClanId != packet.ClanId)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} (character {client.Player.Id}) was invited to clan {invite.ClanId} and answered for clan {packet.ClanId}; ignored.");
                RefuseClanAction(client, PlayerMessage.PmClanNotInvited);
                return;
            }

            var clan = GetClan(invite.ClanId);

            if (clan == null)
            {
                // Disbanded while the dialog was open.
                RefuseClanAction(client, PlayerMessage.PmClanAcceptClanDne);
                return;
            }

            // Everything the invitation was checked for when it went out is checked again here,
            // because a dialog sits open for as long as the player leaves it open.
            using (var validation = _gameUnitOfWorkFactory.CreateChar())
            {
                // Re-read rather than trusting Player.ClanId: they may have accepted another
                // invitation or founded a clan in the meantime, and clan_member holds one row per
                // character - a second insert throws out of the handler and costs them the
                // connection.
                if (validation.Clans.GetClanByCharacterId(client.Player.Id) != null)
                {
                    RefuseClanAction(client, PlayerMessage.PmClanAcceptAlreadyInAClan);
                    return;
                }

                // The clan may have filled up since the invitation went out.
                if (validation.ClanMembers.GetAllClanMembersByClanId(clan.Id).Count >= _maxClanMembers)
                {
                    RefuseClanAction(client, PlayerMessage.PmClanSizeLimitViolation);
                    return;
                }

                // The cooldown applies to joining a PvP clan as well as founding one.
                if (clan.IsPvP)
                {
                    var character = validation.Characters.Get(client.Player.Id);

                    if (character != null && PvPCooldownRemainingSeconds(character) > 0)
                    {
                        client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanAcceptInPvpTimeout, new Dictionary<string, string>()));
                        return;
                    }
                }
            }

            var clanData = new ClanData(clan);
            ClanMemberData memberData = CreateClanMemberData(clanData, client);

            // The player accepted so they must be online
            memberData.IsOnline = true;

            // AddOrUpdate the database for the invitee to be in the clan
            AddMemberToClan(memberData);

            // Who let them in, for the operator reading back how somebody came to be in a clan.
            Logger.WriteLog(LogType.Network,
                $"{client.Player.Name} {client.Player.FamilyName} (character {client.Player.Id}) joined clan {clan.Name} ({clan.Id}) on an invitation from {invite.InviterName} (character {invite.InviterCharacterId}).");

            // Membership changed, update all clan members game clients
            // Include a message for the player joined message to update the clan chat
            SetMemberDataForOnlineMembers(clan.Id, client.Player.Id);

            CallMethodForOnlineMembers(clan.Id, (uint)SysEntity.ClientClanManagerId,
                new PlayerJoinedClanPacket(SetClanMemberDataPacket.NameKey, memberData),
                client.Player.Id);

            // AddOrUpdate the joined players clan window
            SetClanData(client, clanData);
            SetClanMemberData(client, clanData);

            client.Player.ClanId = clanData.Id;

            //update clan inventory from db
            InventoryManager.Instance.SetupLocalClanInventory(client);
        }

        internal void CleanupClan(Client client)
        {
            // Rebuilt rather than zeroed slot by slot. A member on the loading screen of their login
            // has no clan lockbox list yet - InitClanInventory builds it at MapLoaded - and a kick or
            // a disband reaching them there indexed the empty list and threw out of the handler half
            // way through, disconnecting whoever sent it with the rows deleted and the cache not.
            client.Player.Inventory.ResetClanInventory();

            client.Player.ClanId = 0;
        }

        internal void InviteToClanByName(Client client, InviteToClanByNamePacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            ClanEntry inviterClan = unitOfWork.Clans.GetClanByCharacterId(client.Player.Id);

            if (!CanInviteToClan(client, inviterClan))
                return;

            List<ClanMemberEntry> members = unitOfWork.ClanMembers.GetAllClanMembersByClanId(inviterClan.Id);
            GameAccountEntry inviteeAccount = unitOfWork.GameAccounts.Get(packet.FamilyName);

            if (inviteeAccount == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"{packet.FamilyName} do not exist");
                return;
            }

            CharacterEntry inviteeCharacter = unitOfWork.Characters.GetByAccountId(inviteeAccount.Id, inviteeAccount.SelectedSlot);

            // An account whose selected slot holds no character: every character deleted, or a
            // fresh account whose family name happens to match.
            if (inviteeCharacter == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"{packet.FamilyName} has no character to invite");
                return;
            }

            var messageArgs = CreatePlayerMessageArgs("playername", $"{inviteeCharacter.Name} {inviteeAccount.FamilyName}");

            if (members.Count >= _maxClanMembers)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanInviteError, messageArgs));
                return;
            }

            if (inviteeAccount != null)
            {                
                ClanEntry existingClan = unitOfWork.Clans.GetClanByCharacterId(inviteeCharacter.Id);

                // Invitee is not already in a clan
                if(existingClan != null)
                {
                    client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanPlayerAlreadyInAClan, messageArgs));
                    return;
                }

                if (inviteeCharacter != null)
                {
                    // Make sure the invitee is online
                    var invitee = Server.Clients.Find(c => c.Player.Id == inviteeCharacter.Id);

                    if (invitee != null)
                    {
                        SendInviteToCharacter(client, invitee.Player.EntityId, inviterClan);
                    }
                }
            }
        }        

        internal void InviteToClanById(Client client, InviteToClanByIdPacket packet)
        {
            var invitee = Server.Clients.Find(c => c.Player.EntityId == packet.CharacterEntityId);

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            if (invitee == null)
                return;
            
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            ClanEntry inviterClan = unitOfWork.Clans.GetClanByCharacterId(client.Player.Id);

            if (!CanInviteToClan(client, inviterClan))
                return;

            ClanEntry existingClan = unitOfWork.Clans.GetClanByCharacterId(invitee.Player.Id);

            var messageArgs = CreatePlayerMessageArgs("playername", $"{invitee.Player.Name} {invitee.Player.FamilyName}");

            // Invitee is not already in a clan
            if (existingClan != null)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanPlayerAlreadyInAClan, messageArgs));
                return;
            }

            List<ClanMemberEntry> members = unitOfWork.ClanMembers.GetAllClanMembersByClanId(inviterClan.Id);

            if(members.Count >= _maxClanMembers)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanInviteError, messageArgs));
                return;
            }
            
            SendInviteToCharacter(client, packet.CharacterEntityId, inviterClan);
        }

        internal void ClanChangeRankTitle(Client client, ClanChangeRankTitlePacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            ClanEntry clan = unitOfWork.Clans.GetClanByCharacterId(client.Player.Id);
            List<ClanMemberEntry> allMembers = GetClanMembers(clan.Id);
            ClanMemberEntry clanLeader = allMembers.FirstOrDefault(x => x.Rank == _clankRankLeader);

            // Only the clan leader can change ranks
            if (clanLeader != null && clanLeader.CharacterId == client.Player.Id)
            {
                if(unitOfWork.Clans.UpdateRankTitleByClanId(clan.Id, packet.Rank, packet.Title))
                {   
                    // Get the clan now that the rank title is updated
                    ClanEntry updatedClan = unitOfWork.Clans.GetClanById(clan.Id);

                    RegisterClan(updatedClan);

                    SetClanDataForOnlineMembers(updatedClan.Id);
                }
            }
            else
            {
                Logger.WriteLog(LogType.Error, $"ClanManager: Character ID {client.Player.Id} attempted to change rank title but is not the leader.");
            }
        }

        internal void LeaveClan(Client client, LeaveClanPacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            ClanMemberEntry member = GetClanMember(packet.ClanId, client.Player.Id);
            ClanEntry clan = GetClan(packet.ClanId);

            if (member == null || clan == null)
            {
                RefuseClanAction(client, PlayerMessage.PmClanNotInAClan);
                return;
            }

            // The leader cannot simply walk out. Nothing promotes anyone behind them, and a clan
            // with nobody at Leader can never be renamed, disbanded, or have its leadership
            // handed on - so the clan, its name and its lockbox are stranded for good. Hand over
            // or disband instead, both of which the clan window already offers a leader.
            if (member.Rank == ClanRank.Leader)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (clan.IsPvP)
            {
                // Start the 7 day cooldown for the one leaving, not for the clan they leave.
                unitOfWork.Clans.UpdateLastPvPClanTime(member.CharacterId, DateTime.UtcNow);
            }

            if (unitOfWork.ClanMembers.DeleteClanMember(member))
            {                
                UnregisterClanMember(member);

                // Notifies other players still in the clan that we left
                CallMethodForOnlineMembers(packet.ClanId, (uint)SysEntity.ClientClanManagerId,
                    new PlayerLeftClanPacket(client.Player.Id, client.Player.Name, client.Player.FamilyName, packet.ClanId, false),
                    client.Player.Id);

                // Notfies the leavers client that we succesfully left and clears out the clan data
                // The client expects the characterId to be the entityId for this message
                client.CallMethod(SysEntity.ClientClanManagerId, new PlayerLeftClanPacket(client.Player.EntityId, client.Player.Name, client.Player.FamilyName, packet.ClanId, false));
                client.CallMethod(client.Player.EntityId, new ClanIdPacket(0));
            }
        }

        internal void DisbandClan(Client client, DisbandClanPacket packet)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            ClanEntry clan = GetClan(packet.ClanId);

            if (clan == null)
                return;

            // GetClanMember answers for this clan only, so a caller who is in another clan - or
            // in none - gets null here rather than a row that would pass the rank test below.
            ClanMemberEntry member = GetClanMember(clan.Id, client.Player.Id);

            if (member == null || member.Rank != ClanRank.Leader)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (clan.IsPvP)
            {
                // Save the time they were last in a PvP clan to start the 7 day cooldown.
                // Prevents PvP clans from disbanding and creating another clan to workaround the cooldown.
                unitOfWork.Clans.UpdateLastPvPClanTimeForMembers(clan.Id, DateTime.UtcNow);
            }

            List<ClanMemberEntry> members = GetClanMembers(clan.Id);

            foreach (ClanMemberEntry m in members)
            {
                var memberClient = Server.Clients.Find(c => c.Player.Id == m.CharacterId);

                if (memberClient != null)
                {
                    // 0 Clears the overhead frame next to the player name
                    memberClient.CallMethod(memberClient.Player.EntityId, new ClanIdPacket(0));

                    // Shows a message in the players chat and updates the clan UI
                    memberClient.CallMethod(SysEntity.ClientClanManagerId, new ClanDisbandedPacket(clan.Id));
                }
            }

            unitOfWork.ClanMembers.DeleteClanMembers(clan.Id);
            unitOfWork.Clans.DeleteClan(packet.ClanId);

            UnregisterClan(clan);
            UnregisterClanMembers(clan.Id);

            //TODO: Clear clan lockbox db inventory?
        }

        internal void RemovePlayer(Client client)
        {
            // An invitation they never answered goes with them, so it cannot be answered by
            // whoever inherits the entity id, and cannot be waiting for them as a stale refusal
            // of the next invitation they are sent.
            _invites.Remove(client.Player.Id);

            var clanId = client.Player.ClanId;
            if (clanId == 0)
                return;

            if (client.State == ClientState.Loading)
                return;

            CleanupClan(client);
            if (ClanMembers.TryGetValue(clanId, out var cachedMembers) && cachedMembers.IsValueCreated)
                cachedMembers.Value?.RemoveAll(member => member.CharacterId == client.Player.Id);

            if (!Server.Clients.Any(other => other != client && other.State != ClientState.Disconnected &&
                    other.Player?.ClanId == clanId))
                return;
            if (!Clans.ContainsKey(clanId) || !ClanMembers.ContainsKey(clanId))
            {
                Logger.WriteLog(LogType.Error,
                    $"Clan {clanId} metadata is unavailable for roster refresh after departure.");
                return;
            }

            try
            {
                SetMemberDataForOnlineMembers(clanId, client.Player.Id);
            }
            catch (Exception error) when (error is DbException || error is DbUpdateException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Unable to refresh clan {clanId} roster after departure: {error.Message}");
            }
        }


        internal void ClanPromotePlayer(Client client, ClanPromotePlayerPacket packet)
        {
            if (!TryResolveRankChange(client, client.Player.ClanId, packet.CharacterId, out var actor, out var member))
                return;

            // Only the leader promotes, and only into the rank below their own. Leadership moves
            // through MakePlayerClanLeader, which hands it over; promoting into it would mint a
            // second leader. This read the target's row and never the caller's, so a rank 0
            // member could promote themselves 0 - 1 - 2 - 3 and own the clan.
            if (actor.Rank != ClanRank.Leader || member.Rank >= ClanRank.Leader - 1)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            UpdateClanMemberRank(member, (byte)(member.Rank + 1));
        }

        internal void ClanDemotePlayer(Client client, ClanDemotePlayerPacket packet)
        {
            if (!TryResolveRankChange(client, client.Player.ClanId, packet.CharacterId, out var actor, out var member))
                return;

            // Same the other way: any member could demote the leader, and a clan with nobody at
            // Leader can never be renamed, disbanded, or have its leadership handed on again.
            if (actor.Rank != ClanRank.Leader || member.Rank <= ClanRank.Member)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            UpdateClanMemberRank(member, (byte)(member.Rank - 1));
        }

        internal void MakePlayerClanLeader(Client client, MakePlayerClanLeaderPacket packet)
        {
            if (!TryResolveRankChange(client, client.Player.ClanId, packet.CharacterId, out var actor, out var member))
                return;

            if (actor.Rank != ClanRank.Leader)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return;
            }

            // The caller's own row is the outgoing leader. This used to scan the roster for
            // whoever held Leader, which for a leader naming themselves handed UpdateClanLeader
            // the same cached row twice: it wrote Leader and then Leader - 1 over the top of it,
            // and the clan came out with nobody at Leader at all.
            UpdateClanLeader(member, actor);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Everything a rank change depends on before any rank is compared: the caller is in a
        /// clan, it is the clan the packet names, the target is in that same clan, and the target
        /// is not the caller. Answers false and tells the client why otherwise.
        ///
        /// None of the rank handlers used to establish any of it. They read the row of whoever
        /// the packet named and acted on it, so the rank a member held decided nothing at all -
        /// promote, demote and the leadership handover were open to every member of the clan,
        /// and kick only ever asked about the caller.
        /// </summary>
        private bool TryResolveRankChange(Client client, uint clanId, uint targetCharacterId,
            out ClanMemberEntry actor, out ClanMemberEntry target)
        {
            actor = null;
            target = null;

            if (client?.Player == null)
                return false;

            // The clan the caller is actually in is the one that counts; the id in the packet
            // only gets to agree with it.
            if (client.Player.ClanId == 0 || clanId != client.Player.ClanId)
            {
                RefuseClanAction(client, PlayerMessage.PmClanNotInAClan);
                return false;
            }

            actor = GetClanMember(client.Player.ClanId, client.Player.Id);

            if (actor == null)
            {
                RefuseClanAction(client, PlayerMessage.PmClanNotInAClan);
                return false;
            }

            target = GetClanMember(client.Player.ClanId, targetCharacterId);

            if (target == null)
            {
                RefuseClanAction(client, PlayerMessage.PmClanPlayerNotInClan);
                return false;
            }

            if (target.CharacterId == actor.CharacterId)
            {
                RefuseClanAction(client, PlayerMessage.PmClanRankChangeFailed);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Whether this client may invite anyone into <paramref name="inviterClan"/>: they have
        /// to be in it, and at the rank the client's own invite button is gated on.
        ///
        /// Both invite handlers used to read the clan of whoever sent the packet and go straight
        /// on to <c>inviterClan.Id</c>, so a player in no clan dereferenced the null; and neither
        /// looked at the inviter's rank, which ClanRank says is checked here.
        /// </summary>
        private bool CanInviteToClan(Client client, ClanEntry inviterClan)
        {
            if (inviterClan == null)
            {
                RefuseClanAction(client, PlayerMessage.PmClanNotInAClan);
                return false;
            }

            ClanMemberEntry inviter = GetClanMember(inviterClan.Id, client.Player.Id);

            if (inviter == null || inviter.Rank < ClanRank.MinRankToInvite)
            {
                RefuseClanAction(client, PlayerMessage.PmClanInsufficientPermissions);
                return false;
            }

            return true;
        }

        /// <summary>Tells the client the clan window why nothing happened.</summary>
        private static void RefuseClanAction(Client client, PlayerMessage reason)
        {
            Logger.WriteLog(LogType.Security,
                $"{client.Player.FamilyName} (character {client.Player.Id}, clan {client.Player.ClanId}) was refused a clan action: {reason}.");

            client.CallMethod(SysEntity.ClientClanManagerId,
                new DisplayClanMessagePacket((int)reason, new Dictionary<string, string>()));
        }

        /// <summary>
        /// A member's line as it goes to everyone but that member. The entity id is left out: it
        /// belongs only in the copy their own client reads (<see cref="ForReader"/>), and in this one
        /// it went into PlayerJoinedClan, so every other member filed the newcomer under their entity
        /// id - a key no later update and no clan window action could ever match.
        /// </summary>
        private ClanMemberData CreateClanMemberData(ClanData clanData, Client client, byte rank = 0, string note = "")
        {
            return new ClanMemberData
            {
                CharacterId = client.Player.Id,
                ContextId = client.Player.MapContextId,
                Level = client.Player.Level,
                CharacterName = client.Player.Name,
                FamilyName = client.Player.FamilyName,
                UserId = client.AccountEntry.Id,
                ClanId = clanData.Id,
                Rank = rank,
                Note = note,
            };
        }

        /// <summary>
        /// The whole roster, to one client - one who has just come into the world, founded the clan
        /// or joined it. ClanMembersRosterBegin clears the client's copy and the lines fill it again.
        ///
        /// One query for all of it. This read each member separately, twice - the whole character with
        /// its appearance and clan, then the whole account with all its characters - each in a unit of
        /// work of its own.
        /// </summary>
        private void SetClanMemberData(Client client, ClanData clanData)
        {
            List<ClanRosterEntry> roster;

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                roster = unitOfWork.ClanMembers.GetRoster(clanData.Id);

            var online = OnlineCharacters();

            client.CallMethod(SysEntity.ClientClanManagerId, new ClanMembersRosterBeginPacket(clanData.Id));

            foreach (var entry in roster)
            {
                online.TryGetValue(entry.CharacterId, out var inWorld);

                client.CallMethod(SysEntity.ClientClanManagerId,
                    new SetClanMemberDataPacket(SetClanMemberDataPacket.NameKey, ForReader(ToMemberData(entry, inWorld), client)));
            }

            client.CallMethod(SysEntity.ClientClanManagerId, new ClanMembersRosterEndPacket(clanData.Id));
        }
        private void SetClanData(Client client, ClanData clanData)
        {
            Manifestation player = client.Player;            
            client.CallMethod(SysEntity.ClientClanManagerId, new SetClanDataPacket(SetClanDataPacket.NameKey, clanData));
            client.CallMethod(player.EntityId, new ClanIdPacket(clanData.Id));
        }

        private void AddMemberToClan(ClanMemberData clanMember)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            unitOfWork.ClanMembers.InsertClanMemberData(clanMember.ClanId, clanMember.CharacterId, clanMember.Rank, clanMember.Note);            

            // AddOrUpdate the cache with the newly added member from the database
            List<ClanMemberEntry> members = unitOfWork.ClanMembers.GetAllClanMembersByClanId(clanMember.ClanId);
            RegisterClanMember(clanMember.ClanId, members.FirstOrDefault(x => x.CharacterId == clanMember.CharacterId));
        }

        /// <summary>
        /// Records the invitation and puts it on the invitee's screen. The record is what
        /// ClanInvitationResponse answers against; without one, an answer is refused.
        /// </summary>
        private void SendInviteToCharacter(Client client, ulong characterEntityId, ClanEntry clan)
        {
            Client inviteeClient = Server.Clients.Find(c => c.Player.EntityId == characterEntityId);

            if (inviteeClient?.Player == null)
                return;

            // One open invitation per character, as the client shows one dialog.
            if (_invites.ContainsKey(inviteeClient.Player.Id))
            {
                client.CallMethod(SysEntity.ClientClanManagerId,
                    new DisplayClanMessagePacket((int)PlayerMessage.PmClanPlayerAlreadyInvitedToYourClan,
                        CreatePlayerMessageArgs("playername", $"{inviteeClient.Player.Name} {inviteeClient.Player.FamilyName}")));
                return;
            }

            _invites[inviteeClient.Player.Id] = new PendingClanInvite
            {
                ClanId = clan.Id,
                InviterCharacterId = client.Player.Id,
                InviterName = client.Player.FamilyName
            };

            var inviteData = new ClanInviteData
            {
                // Sending the full name here because it looks better on the invitation prompt in-game
                // "Name Familyname invited you to join ClanType ClanName"
                InviterFamilyName = $"{client.Player.Name} {client.Player.FamilyName}",
                ClanId = clan.Id,
                ClanName = clan.Name,
                IsPvP = clan.IsPvP,
                InvitedCharacterEntityId = characterEntityId
            };

            inviteeClient.CallMethod(SysEntity.ClientClanManagerId, new InviteToClanPacket(InviteToClanPacket.NameKey, inviteData));
        }

        private bool CanCreateClan(Client client, CreateClanPacket packet, uint characterId)
        {
            // Length, uniqueness and the censor are the same rules a rename is held to, so they
            // live in one place - a name that cannot be created must not be reachable by
            // renaming into it either.
            if (!IsClanNameAcceptable(client, packet.ClanName))
                return false;

            if(client.Player.Credits[CurencyType.Credits] < _requiredCreditsForClanCreation)
            {
                client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmInsufficientFundsToCreateClan, new Dictionary<string, string>()));
                return false;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (packet.IsPvP)
            {
                CharacterEntry character = unitOfWork.Characters.Get(characterId);

                // Verify the creator is not on PvP timeout: PmClanCannotCreateUserInPvpTimeout
                if (character != null && PvPCooldownRemainingSeconds(character) > 0)
                {
                    client.CallMethod(SysEntity.ClientClanManagerId, new DisplayClanMessagePacket((int)PlayerMessage.PmClanCannotCreateUserInPvpTimeout, new Dictionary<string, string>()));
                    return false;
                }
            }

            // The fee is taken by CreateClan once the clan and its leader both exist.
            return true;
        }        

        private bool ClanNameExists(string clanName)
        {
            if (string.IsNullOrEmpty(clanName))
                throw new ArgumentNullException(nameof(clanName));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.Clans.GetClanByName(clanName) != null;
        }

        /// <summary>
        /// Sends one member's line to the other members of the clan who are online.
        ///
        /// This rebuilt and resent the whole roster to every member online instead, two queries per
        /// member per recipient and a packet per member each, on the main loop - for every login,
        /// logout, join, kick and rank change, and twice for a map change. A 100-member clan with 30
        /// online paid about 6,000 queries and 3,000 packets in one tick for each of those. The client files a
        /// SetClanMemberData under the member's id and replaces whatever it had there
        /// (client/clan.py _AddClanMemberToLists), so the member that changed is all it needs.
        /// </summary>
        private void SetMemberDataForOnlineMembers(uint clanId, uint characterId)
        {
            var member = GetMemberData(characterId);

            if (member == null || member.ClanId != clanId)
                return;

            SendMemberData(member, characterId);
        }

        /// <summary>One member's line to every member of their clan who is online, each in the form their own client files it under.</summary>
        private void SendMemberData(ClanMemberData member, uint skipCharacterId = 0)
        {
            CallMethodForOnlineMembers(member.ClanId,
                reader => reader.CallMethod(SysEntity.ClientClanManagerId, new SetClanMemberDataPacket(SetClanMemberDataPacket.NameKey, ForReader(member, reader))),
                skipCharacterId);
        }

        /// <summary>
        /// A member's line for one reader. Their own line carries their manifestation's entity id and
        /// everyone else's the character id (<see cref="ClanMemberData.Write"/>). A copy per reader
        /// rather than one line changed in place: packets are written when they are sent, which is
        /// after every reader's has been queued.
        /// </summary>
        private static ClanMemberData ForReader(ClanMemberData member, Client reader)
        {
            return new ClanMemberData
            {
                UserId = member.UserId,
                CharacterId = member.CharacterId,
                CharacterEntityId = reader.Player.Id == member.CharacterId ? reader.Player.EntityId : 0,
                CharacterName = member.CharacterName,
                FamilyName = member.FamilyName,
                ClanId = member.ClanId,
                Level = member.Level,
                ContextId = member.ContextId,
                Rank = member.Rank,
                IsOnline = member.IsOnline,
                IsAfk = member.IsAfk,
                Note = member.Note
            };
        }

        /// <summary>A member's line from their live character: one who has just come into the world, or is leaving it.</summary>
        private static ClanMemberData MemberDataFor(Client client, ClanMemberEntry member, bool isOnline)
        {
            return new ClanMemberData
            {
                UserId = client.AccountEntry.Id,
                CharacterId = client.Player.Id,
                CharacterName = client.Player.Name,
                FamilyName = client.Player.FamilyName,
                ClanId = member.ClanId,
                Level = client.Player.Level,
                ContextId = client.Player.MapContextId,
                Rank = member.Rank,
                Note = member.Note,
                IsOnline = isOnline
            };
        }

        /// <summary>A member's line as the database has it, live where they are online; null for a character in no clan.</summary>
        private ClanMemberData GetMemberData(uint characterId)
        {
            ClanRosterEntry entry;

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                entry = unitOfWork.ClanMembers.GetRosterEntry(characterId);

            return entry == null ? null : ToMemberData(entry, Server.Clients.Find(c => CountsAsOnline(c) && c.Player.Id == characterId));
        }

        private static ClanMemberData ToMemberData(ClanRosterEntry entry, Client inWorld)
        {
            return new ClanMemberData
            {
                UserId = entry.AccountId,
                CharacterId = entry.CharacterId,
                CharacterName = entry.CharacterName,
                FamilyName = entry.FamilyName,
                ClanId = entry.ClanId,
                // The row is only as new as the character's last save; an online member is read live.
                Level = inWorld?.Player.Level ?? entry.Level,
                ContextId = inWorld?.Player.MapContextId ?? entry.MapContextId,
                Rank = entry.Rank,
                Note = entry.Note,
                IsOnline = inWorld != null
            };
        }

        /// <summary>
        /// Whether a connection's character counts as online to their clan: in the world, or on the
        /// loading screen into it. The roster used to call anyone connected online, and a player back
        /// at character selection still has the character they last played on the connection.
        /// </summary>
        private static bool CountsAsOnline(Client client)
        {
            return client.Player != null
                && client.Player.Id != 0
                && (client.State == ClientState.Ingame || client.State == ClientState.Teleporting || client.State == ClientState.Loading);
        }

        /// <summary>Every character online, by character id, from one pass over the connections.</summary>
        private static Dictionary<uint, Client> OnlineCharacters()
        {
            var online = new Dictionary<uint, Client>();

            foreach (var client in Server.Clients)
                if (CountsAsOnline(client))
                    online.TryAdd(client.Player.Id, client);

            return online;
        }

        private void SetClanDataForOnlineMembers(uint clanId, uint skipCharacterId = 0)
        {
            var clanData = new ClanData(Clans.GetValueOrDefault(clanId).Value);

            CallMethodForOnlineMembers(clanId, (client) => SetClanData(client, clanData), skipCharacterId);
        }

        public void CallMethodForOnlineMembers(uint clanId, ulong entityId, ServerPythonPacket packet, uint skipCharacterId = 0, uint onlyThisCharacterId = 0)
        {
            foreach (ClanMemberEntry member in GetClanMembers(clanId))
            {
                // If the member is online get their cached client
                var memberClient = Server.Clients.Find(c => c.Player.Id == member.CharacterId);

                if (memberClient != null)
                {
                    // Don't send a message to this player
                    if ((skipCharacterId != 0 && skipCharacterId == memberClient.Player.Id) ||
                        onlyThisCharacterId != 0 && onlyThisCharacterId != memberClient.Player.Id)
                        continue;

                    memberClient.CallMethod(entityId, packet);
                }
            }
        }

        public void CallMethodForOnlineMembers(uint clanId, Action<Client> methodToCall, uint skipCharacterId = 0, uint onlyThisCharacterId = 0)
        {
            foreach (ClanMemberEntry member in GetClanMembers(clanId))
            {
                // If the member is online get their cached client
                var memberClient = Server.Clients.Find(c => c.Player.Id == member.CharacterId);

                if (memberClient != null)
                {
                    // Don't send a message to this player
                    if ((skipCharacterId != 0 && skipCharacterId == memberClient.Player.Id) ||
                        onlyThisCharacterId != 0 && onlyThisCharacterId != memberClient.Player.Id)
                        continue;

                    methodToCall.Invoke(memberClient);
                }
            }
        }

        private Dictionary<string, string> CreatePlayerMessageArgs(string key, string value)
        {
            return new Dictionary<string, string>
            {
                { key, value }
            };
        }

        private void UpdateClanMemberRank(ClanMemberEntry member, byte newRank)
        {
            var promoted = newRank > member.Rank;

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                unitOfWork.ClanMembers.UpdateRankByCharacterId(newRank, member.CharacterId);

            // The cached rank is the one every later rank check reads, so it moves with the row
            // before anything else is done.
            member.Rank = newRank;
            RegisterClanMember(member.ClanId, member);

            var memberData = GetMemberData(member.CharacterId);

            if (memberData == null)
                return;

            // The member's line, to everyone online - the member included: their own clan window
            // offers them what their rank allows.
            SendMemberData(memberData);

            var messageArgs = new Dictionary<string, string>
            {
                { "firstname", memberData.CharacterName },
                { "lastname", memberData.FamilyName },
                { "rankname", GetRankTitleForRank(member.ClanId, newRank) },
            };

            CallMethodForOnlineMembers(member.ClanId, (uint)SysEntity.ClientClanManagerId,
                new DisplayClanMessagePacket((int)(promoted ? PlayerMessage.PmClanPlayerPromoted : PlayerMessage.PmClanPlayerDemoted), messageArgs));
        }

        private void UpdateClanLeader(ClanMemberEntry member, ClanMemberEntry leaderMember)
        {
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
            {
                unitOfWork.ClanMembers.UpdateRankByCharacterId(_clankRankLeader, member.CharacterId);
                unitOfWork.ClanMembers.UpdateRankByCharacterId((byte)(_clankRankLeader - 1), leaderMember.CharacterId);
            }

            member.Rank = _clankRankLeader;
            RegisterClanMember(member.ClanId, member);

            leaderMember.Rank = (byte)(_clankRankLeader - 1);
            RegisterClanMember(leaderMember.ClanId, leaderMember);

            var newLeader = GetMemberData(member.CharacterId);
            var oldLeader = GetMemberData(leaderMember.CharacterId);

            // Both lines changed.
            if (oldLeader != null)
                SendMemberData(oldLeader);

            if (newLeader == null)
                return;

            SendMemberData(newLeader);

            var messageArgs = new Dictionary<string, string>
            {
                { "leadername", $"{newLeader.CharacterName} {newLeader.FamilyName}" },
                { "clanname", GetClan(member.ClanId).Name },
            };

            CallMethodForOnlineMembers(member.ClanId, (uint)SysEntity.ClientClanManagerId,
                new DisplayClanMessagePacket((int)PlayerMessage.PmClanNewLeader, messageArgs));
        }

        private string GetRankTitleForRank(uint clanId, uint newRank)
        {
            ClanEntry clan = GetClan(clanId);

            switch(newRank)
            {
                case 0:
                    return clan.RankTitle0;
                case 1:
                    return clan.RankTitle1;
                case 2:
                    return clan.RankTitle2;
                case 3:
                    return clan.RankTitle3;
                default:
                    return clan.RankTitle0;
            }
        }

        #endregion

        #region Caching Helpers

        private void RegisterClan(ClanEntry clan)
        {
            Clans.AddOrUpdate(clan.Id, new Lazy<ClanEntry>(clan), (id, oldClan) => new Lazy<ClanEntry>(clan));
        }

        private void UnregisterClan(ClanEntry clan)
        {
            CallMethodForOnlineMembers(clan.Id, (client) => CleanupClan(client));
            Clans.Remove(clan.Id, out _);
        }

        /// <summary>
        /// A clan by id, from the cache or from the database behind it. The fallback used to be
        /// written as <c>clan.Value ?? …</c>, which dereferences the null it is guarding against:
        /// every one of these lookups takes an id off the wire, and an id for a clan that is not
        /// cached threw rather than answering null.
        /// </summary>
        private ClanEntry GetClan(uint clanId)
        {
            ClanEntry cached = Clans.GetValueOrDefault(clanId)?.Value;

            if (cached != null)
                return cached;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.Clans.GetClanById(clanId);
        }

        private void RegisterClanMember(uint clanId, ClanMemberEntry member)
        {
            List<ClanMemberEntry> members = ClanMembers.GetValueOrDefault(clanId)?.Value;
            ClanMemberEntry existingMember = members?.FirstOrDefault(x => x.CharacterId == member.CharacterId);

            if (existingMember != null)
            {
                members[members.IndexOf(existingMember)] = member;
            }
            else
            {
                if (members == null)
                    members = new List<ClanMemberEntry>();

                members.Add(member);
            }

            ClanMembers.AddOrUpdate(clanId, new Lazy<List<ClanMemberEntry>>(members), (x, y) => new Lazy<List<ClanMemberEntry>>(members));
        }

        private void UnregisterClanMember(ClanMemberEntry member)
        {
            List<ClanMemberEntry> members = ClanMembers.GetValueOrDefault(member?.ClanId ?? 0).Value;
            ClanMemberEntry existingMember = members?.FirstOrDefault(x => x.CharacterId == member?.CharacterId);

            if (existingMember != null)
            {
                CallMethodForOnlineMembers(member.ClanId, (client) => CleanupClan(client), 0, member.CharacterId);
                members.Remove(existingMember);
                ClanMembers.AddOrUpdate(member.ClanId, new Lazy<List<ClanMemberEntry>>(members), (x, y) => new Lazy<List<ClanMemberEntry>>(members));
            }
        }
        
        private void UnregisterClanMembers(uint clanId)
        {
            _ = ClanMembers.Remove(clanId, out _);        
        }

        /// <summary>The clan's roster, from the cache or from the database behind it.</summary>
        private List<ClanMemberEntry> GetClanMembers(uint clanId)
        {
            List<ClanMemberEntry> cached = ClanMembers.GetValueOrDefault(clanId)?.Value;

            if (cached != null)
                return cached;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.ClanMembers.GetAllClanMembersByClanId(clanId);
        }

        /// <summary>
        /// One character's membership of one clan, or null if they are not in it.
        ///
        /// The cache holds every clan's whole roster at startup, but RemovePlayer drops a member
        /// from it as they log out, so by the time a leader gets round to kicking or promoting
        /// somebody who is not online the row is only in the database. Reading through to it
        /// means a rank can be established for an offline member instead of the lookup answering
        /// null - which every caller then dereferenced.
        /// </summary>
        public ClanMemberEntry GetClanMember(uint clanId, uint characterId)
        {
            ClanMemberEntry cached = ClanMembers.GetValueOrDefault(clanId)?.Value?.FirstOrDefault(x => x.CharacterId == characterId);

            if (cached != null)
                return cached;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            ClanMemberEntry stored = unitOfWork.ClanMembers.GetClanMemberByCharacterId(characterId);

            // A character is in one clan at a time, so a row for another clan is not a member of
            // this one.
            return stored != null && stored.ClanId == clanId ? stored : null;
        }

        #endregion
    }
}

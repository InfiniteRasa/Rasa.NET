using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Memory;
    using Packets.LookingForGroup.Client;
    using Packets.LookingForGroup.Server;
    using Structures;

    /// <summary>
    ///     LookingForGroup packets:
    ///     -- WorldMsg
    /// - RequestCreateLookingForGroupAd      => implemented
    /// - RequestLookingForGroupSearch        => implemented
    /// - RemoveLookingForGroupAd             => implemented
    ///
    ///     LookingForGroup handlers (client/lookingforgroupmanager.py):
    /// - LookingForGroupAdPlaced             => implemented
    /// - LookingForGroupAdRemoved            => implemented
    /// - LookingForGroupSearchResults        => implemented
    /// - DisplayLookingForGroupMessage       => implemented
    ///
    /// Ads live only as long as the server process, and each one only while its placer is in
    /// the world. That is how long the client shows it placed: the status indicator goes when
    /// LookingForGroupAdRemoved arrives, or when the game UI is reset on leaving the world for
    /// character selection or the login screen (client/gameui.py Reset posts UI_RESET, and the
    /// status updater clears its indicators on it). A map change resets nothing, so an ad
    /// outlives one.
    /// </summary>
    public class LookingForGroupManager
    {
        // shared/gameconstants.py: MAX_PARTY_SIZE (:261), CHARACTER_LEVEL_MIN/MAX (:57-58).
        private const int MaxPartySize = 6;
        private const int CharacterLevelMin = 1;
        private const int CharacterLevelMax = 50;

        /// <summary>
        /// The results pane builds one widget per row with no paging, so an unbounded
        /// result set would be a client-side stall rather than a useful list. The reply is
        /// also sized as it is built: rows carry a live roster and four id lists, and the
        /// whole answer has to fit one send buffer (PythonSize.PayloadBudget) - it used to
        /// overflow the buffer and disconnect the searcher at around 15 ads.
        /// </summary>
        private const int SearchResultLimit = 50;

        private static LookingForGroupManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// Live ads keyed by placing account id. One ad per account: CreateAd overwrites
        /// g_currentPlacedLFGAdInfo client-side, so a second ad replaces the first.
        /// Follows PartyManager.Parties - a plain dictionary touched from the packet
        /// handlers rather than its own synchronised collection.
        /// </summary>
        private readonly Dictionary<uint, LookingForGroupAd> _ads = new Dictionary<uint, LookingForGroupAd>();

        public static LookingForGroupManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new LookingForGroupManager();
                    }
                }

                return _instance;
            }
        }

        private LookingForGroupManager()
        {
        }

        #region Handlers

        internal void RequestCreateLookingForGroupAd(Client client, RequestCreateLookingForGroupAdPacket packet)
        {
            var adInfo = packet.AdInfo;

            // client/ui/lookingforgroupwindow.py:500 refuses to submit unless
            // _party.IsPartyLeader(), which is true for a partyless player too
            // (client/party.py:369 - the leader id is None when you lead or have no party).
            if (!IsPartyLeader(client))
            {
                DisplayMessage(client, PlayerMessage.PmLfgNotSquadLeader);
                return;
            }

            var squadSize = CurrentSquadSize(client);

            if (squadSize >= MaxPartySize)
            {
                DisplayMessage(client, PlayerMessage.PmLfgAlreadyAtMaxSquadSize);
                return;
            }

            // A requested size at or below what the squad already has would render as zero
            // or negative open slots: the results pane prints squadSize minus the roster
            // length (client/ui/lookingforgroupwindow.py:1096).
            if (adInfo.SquadSize.HasValue && adInfo.SquadSize.Value <= squadSize)
            {
                DisplayMessage(client, PlayerMessage.PmLfgSquadSizeLargerThanRequested);
                return;
            }

            var minLevel = Math.Clamp(adInfo.MinLevel, CharacterLevelMin, CharacterLevelMax);
            var maxLevel = Math.Clamp(adInfo.MaxLevel, CharacterLevelMin, CharacterLevelMax);

            if (maxLevel < minLevel)
            {
                DisplayMessage(client, PlayerMessage.PmLfgLevelRangeInvalid);
                return;
            }

            _ads[client.AccountEntry.Id] = new LookingForGroupAd
            {
                AccountId = client.AccountEntry.Id,
                LeaderEntityId = client.Player.EntityId,
                LeaderName = client.Player.FamilyName,
                PartyId = client.Player.PartyId,
                SquadSize = adInfo.SquadSize,
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                Activities = adInfo.Activities,
                Roles = adInfo.Roles,
                MapIds = adInfo.MapIds,
                ContinentIds = adInfo.ContinentIds,
                PlacedAt = DateTime.UtcNow
            };

            client.CallMethod(SysEntity.ClientLookingForGroupManagerId, new LookingForGroupAdPlacedPacket());
        }

        internal void RemoveLookingForGroupAd(Client client, RemoveLookingForGroupAdPacket packet)
        {
            // Acknowledged whether or not an ad was found. The client's status indicator is
            // what the player is trying to get rid of, and it only clears on this message;
            // staying silent because the server had nothing on file would strand it.
            _ads.Remove(client.AccountEntry.Id);

            client.CallMethod(SysEntity.ClientLookingForGroupManagerId, new LookingForGroupAdRemovedPacket());
        }

        internal void RequestLookingForGroupSearch(Client client, RequestLookingForGroupSearchPacket packet)
        {
            var filter = packet.Filter;
            var results = new List<LookingForGroupAdInfo>();

            // The envelope: tuple + list header. Rows are added while they still fit.
            var size = PythonSize.Of(pw => new LookingForGroupSearchResultsPacket(results).Write(pw));

            foreach (var ad in _ads.Values)
            {
                if (ad.AccountId == client.AccountEntry.Id)
                    continue;

                var leader = FindInWorld(ad.AccountId);

                // The placer is gone but their ad outlived them - a logout path that did not
                // run RemovePlayer. Drop it rather than advertise a squad nobody can join.
                if (leader == null)
                    continue;

                // An invisible GM's ad, to anyone who is not a GM: an ad up is someone online.
                if (Detection.IsGmHiddenFrom(leader.Player, client))
                    continue;

                if (!Matches(ad, filter))
                    continue;

                var row = ToAdInfo(ad, leader);
                var rowSize = PythonSize.Of(row);

                if (size + rowSize + PythonSize.ListHeaderSlack > PythonSize.PayloadBudget)
                    break;

                size += rowSize;
                results.Add(row);

                if (results.Count >= SearchResultLimit)
                    break;
            }

            client.CallMethod(SysEntity.ClientLookingForGroupManagerId,
                new LookingForGroupSearchResultsPacket(results));
        }

        #endregion

        /// <summary>
        /// Drops the player's ad when they leave the world. Called from
        /// MapChannelManager.RemovePlayer alongside the other per-player manager cleanup.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client.AccountEntry == null)
                return;

            // MapChannelManager.ChangeMap takes the player off the old map already Loading - a map
            // link, a summon, a GM teleport - and that is not leaving the world. The ad went with
            // every one of them, silently: nothing is sent when it goes from here, so the placer's
            // client went on showing it placed, and nothing on the client places an ad again, while
            // no search could find it. A dropship ride never came through here and always kept it.
            if (client.State == ClientState.Loading)
                return;

            _ads.Remove(client.AccountEntry.Id);
        }

        /// <summary>
        /// An ad belongs to whoever leads the squad it recruits for, and stops being useful once
        /// that squad is as big as it asked for. PartyManager hands every membership and
        /// leadership change here, for each account it touched; accounts with no ad cost a
        /// dictionary miss. The two outcomes are the messages the client has text for but nothing
        /// ever sent: an ad used to sit there advertising a squad the placer no longer led, or one
        /// that had already filled up.
        /// </summary>
        internal void PartyChanged(uint accountId)
        {
            if (!_ads.TryGetValue(accountId, out var ad))
                return;

            var placer = FindInWorld(accountId);

            // Out of the world: RemovePlayer has the ad, or is about to.
            if (placer == null)
                return;

            if (!IsPartyLeader(placer))
            {
                Drop(placer, PlayerMessage.PmLfgAdRemovedYouAreNoLongerLeader);
                return;
            }

            // "No preference" has no target of its own, so only a full squad ends one.
            var target = ad.SquadSize ?? MaxPartySize;

            if (CurrentSquadSize(placer) >= target)
            {
                Drop(placer, PlayerMessage.PmLfgAdRemovedRequestedSquadSizeReached);
                return;
            }

            ad.PartyId = placer.Player.PartyId;
        }

        /// <summary>
        /// Takes an ad down and says why. LookingForGroupAdRemoved is what clears the client's
        /// status indicator (client/lookingforgroupmanager.py:81); the message alone would leave
        /// the player looking at an indicator for an ad that is gone.
        /// </summary>
        private void Drop(Client placer, PlayerMessage why)
        {
            _ads.Remove(placer.AccountEntry.Id);

            placer.CallMethod(SysEntity.ClientLookingForGroupManagerId, new LookingForGroupAdRemovedPacket());
            DisplayMessage(placer, why);
        }

        /// <summary>
        /// The account's connection while its character is in the world, which includes the
        /// loading screen of a map change and a dropship ride - an ad lasts through both (see
        /// <see cref="RemovePlayer"/>). For that time a search goes on listing the ad and
        /// PartyChanged goes on checking it, rather than taking the placer for gone; the roster
        /// reads a squadmate between maps off the live player as well.
        /// </summary>
        private static Client FindInWorld(uint accountId) =>
            Server.Clients.FirstOrDefault(c => (c.State == ClientState.Ingame
                                                || c.State == ClientState.Teleporting
                                                || c.State == ClientState.Loading)
                                               && c.Player != null
                                               && c.AccountEntry?.Id == accountId);

        private static void DisplayMessage(Client client, PlayerMessage message)
        {
            client.CallMethod(SysEntity.ClientLookingForGroupManagerId,
                new DisplayLookingForGroupMessagePacket(message));
        }

        /// <summary>
        /// An empty criterion means "no preference" - the window renders it as
        /// ID_LFG_NO_PREFERENCE - so it matches everything rather than nothing. A populated
        /// one matches when the ad shares at least one id with it, which is how the player
        /// reads a row of bracketed tags.
        /// </summary>
        private static bool Matches(LookingForGroupAd ad, LookingForGroupAdInfo filter)
        {
            // The level boxes default to the full 1-50 range when left blank, so a range
            // test is always meaningful. Overlap, not containment: a 20-30 ad is a fair
            // result for someone searching 25-40.
            var min = Math.Clamp(filter.MinLevel, CharacterLevelMin, CharacterLevelMax);
            var max = Math.Clamp(filter.MaxLevel, CharacterLevelMin, CharacterLevelMax);

            if (max < min)
                (min, max) = (max, min);

            if (ad.MaxLevel < min || ad.MinLevel > max)
                return false;

            if (!Overlaps(ad.Activities, filter.Activities))
                return false;

            if (!Overlaps(ad.Roles, filter.Roles))
                return false;

            // Maps and continents are one criterion split across two lists: the window puts
            // a continent in continentIds only when none of its maps were ticked
            // (lookingforgroupwindow.py:660), so an ad tagged with a map still answers a
            // continent-level search only if it also carries that continent.
            if (filter.MapIds.Count > 0 || filter.ContinentIds.Count > 0)
                if (!Overlaps(ad.MapIds, filter.MapIds) && !Overlaps(ad.ContinentIds, filter.ContinentIds))
                    return false;

            return true;
        }

        private static bool Overlaps<T>(List<T> adValues, List<T> filterValues)
        {
            if (filterValues.Count == 0)
                return true;

            return adValues.Count == 0 || adValues.Any(filterValues.Contains);
        }

        /// <summary>
        /// Builds the outgoing row. The squad roster is read live rather than from the ad:
        /// members join and leave after an ad is placed, and the open-slot count the player
        /// reads is derived from the roster's length.
        /// </summary>
        private static LookingForGroupAdInfo ToAdInfo(LookingForGroupAd ad, Client leader)
        {
            return new LookingForGroupAdInfo
            {
                UserId = ad.LeaderEntityId,
                UserName = ad.LeaderName,
                SquadSize = ad.SquadSize,
                SquadInfo = SquadRoster(leader),
                MinLevel = ad.MinLevel,
                MaxLevel = ad.MaxLevel,
                Activities = ad.Activities,
                Roles = ad.Roles,
                MapIds = ad.MapIds,
                ContinentIds = ad.ContinentIds
            };
        }

        private static List<PartyMember> SquadRoster(Client leader)
        {
            // A soloing leader is still a squad of one: the details window matches the
            // roster against the ad's userName to mark the leader, and an empty roster
            // would also make the open-slot count read one too high.
            if (leader.Player.PartyId == 0
                || !PartyManager.Instance.Parties.TryGetValue(leader.Player.PartyId, out var party))
                return new List<PartyMember> { Snapshot(leader) };

            var roster = new List<PartyMember>();

            foreach (var member in party.Members)
            {
                // Party.Members caches IsAfk at the moment the member joined - PartyMember's
                // Client constructor hardcodes it to false - so read it off the live player
                // where there is one. The details window greys out AFK names.
                var online = FindInWorld(member.UserId);

                roster.Add(online != null ? Snapshot(online) : member);
            }

            return roster;
        }

        private static PartyMember Snapshot(Client client)
        {
            return new PartyMember(
                client.AccountEntry.Id,
                client.Player.FamilyName,
                client.Player.Class,
                client.Player.Level,
                client.Player.IsAFK);
        }

        private static bool IsPartyLeader(Client client)
        {
            if (client.Player.PartyId == 0)
                return true;

            return PartyManager.Instance.Parties.TryGetValue(client.Player.PartyId, out var party)
                   && party.PartyLeaderId == client.AccountEntry.Id;
        }

        private static int CurrentSquadSize(Client client)
        {
            if (client.Player.PartyId == 0)
                return 1;

            return PartyManager.Instance.Parties.TryGetValue(client.Player.PartyId, out var party)
                ? party.Members.Count
                : 1;
        }
    }
}

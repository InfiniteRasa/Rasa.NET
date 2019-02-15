using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using Structures;

    public class AuctionHouseManager
    {
        /*    AuctionHouse Packets:
         *  - AuctionCreationFailed
         *  - AuctionCreationSuccess
         *  - QuerySuccess
         *  - QueryFailed
         *  - AuctionStatusSuccess
         *  - AuctionStatusFailed       => not used by client
         *  - AuctionBuyoutFailed
         *  - AuctionBuyoutSuccess
         *  - AuctionSold
         *  - CancelAuctionFailed
         *  - CancelAuctionSuccess
         *  - AuctionExpired
         *  
         *  AuctionHouse Handlers:
         *  - RequestAuctionBuyout
         *  - RequestAuctionStatus
         *  - RequestCancelAuction
         *  - RequestCancelAuctioneer
         *  - RequestCreateAuction
         *  - RequestQueryAuctions
         *  
         */

        private static AuctionHouseManager _instance;
        private static readonly object InstanceLock = new object();
        public Dictionary<uint, AuctionItem> AuctionedItems = new Dictionary<uint, AuctionItem>();

        public static AuctionHouseManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new AuctionHouseManager();
                    }
                }

                return _instance;
            }
        }

        private AuctionHouseManager()
        {
        }

        #region Handlers

        public void RequestAuctionBuyout(Client client, RequestAuctionBuyoutPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestAuctionBuyout ");
        }

        public void RequestAuctionStatus(Client client, RequestAuctionStatusPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestAuctionStatus ");
        }

        public void RequestCancelAuction(Client client, RequestCancelAuctionPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestCancelAuction");
        }

        public void RequestCancelAuctioneer(Client client)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestCancelAuctioneer");
        }

        public void RequestCreateAuction(Client client, RequestCreateAuctionPacket packet)
        {
            // if packet.duration is 0 then it's 12h, else it's 1, 2 or 3 day's
            var duration = Math.Max(packet.Duration * 24, 12);

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionCreationSuccessPacket(packet.ItemEntityId));

            // ToDo: validate auction first, move item to auction inventory, pay aution tax etc...
            Logger.WriteLog(LogType.AI, $"ToDo: RequestCreateAuctionPacket");
        }

        public void RequestQueryAuctions(Client client, RequestQueryAuctionsPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestQueryAuctions");
        }

        #endregion

        #region Helper Functions
        public PlayerMessage ValidateAuctionCreation(Client client)
        {
            // testing some auction messages
            var test = new List<PlayerMessage>
            {
                PlayerMessage.PmAuctionNotEnoughCreditsForDeposit,
                PlayerMessage.PmAuctionCouldNotFindItem,
                PlayerMessage.PmAuctionInvalidPriceSet,
                PlayerMessage.PmAuctionItemNeedsRepair,
                PlayerMessage.PmAuctionItemCannotBeAuctioned,
                PlayerMessage.PmAuctionCreationSuccess,
                PlayerMessage.PmAuctionMaxAuctions,
                PlayerMessage.PmAuctionNoBuyoutInboxFull,
                PlayerMessage.PmAuctionNoCreationInboxFull
            };

            foreach (var message in test)
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionCreationFailedPacket(1100, message));

            return PlayerMessage.PmAboutToRespawn;
        }

        #endregion
    }
}

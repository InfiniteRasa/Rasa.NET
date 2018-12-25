namespace Rasa.Managers
{
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;

    public class AuctionHouseManager
    {
        /*    AuctionHouse Packets:
         *  - AuctionCreationFailed
         *  - AuctionCreationSuccess
         *  - QuerySuccess
         *  - QueryFailed
         *  - AuctionStatusSuccess
         *  - AuctionStatusFailed
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
            Logger.WriteLog(LogType.AI, $"ToDo: RequestCreateAuctionPacket");
        }

        public void RequestQueryAuctions(Client client, RequestQueryAuctionsPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"ToDo: RequestQueryAuctions");
        }

        #endregion
    }
}

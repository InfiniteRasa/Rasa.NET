using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Auction
{
    using Context.Char;
    using Structures.Char;

    public class AuctionRepository : IAuctionRepository
    {
        private readonly CharContext _charContext;

        public AuctionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public bool CreateAuction(AuctionEntry auction)
        {
            if (_charContext.CharacterMissionItemEntries.Any(entry => entry.ItemId == auction.ItemId))
                return false;
            // item_id is the key, so a second listing of the same item would throw on save and
            // lose the seller's deposit with it. Checked here instead.
            if (GetAuctionByItemId(auction.ItemId) != null)
            {
                Logger.WriteLog(LogType.Error, $"Item {auction.ItemId} is already up for auction.");
                return false;
            }

            try
            {
                _charContext.AuctionEntries.Add(auction);
                _charContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating auction:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        public AuctionEntry GetAuctionByItemId(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.AuctionEntries);

            return query.FirstOrDefault(a => a.ItemId == itemId);
        }

        public List<AuctionEntry> GetAuctionsBySeller(uint sellerId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.AuctionEntries);

            return query.Where(a => a.SellerId == sellerId).OrderBy(a => a.CreatedAt).ToList();
        }

        public int CountAuctionsBySeller(uint sellerId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.AuctionEntries);

            return query.Count(a => a.SellerId == sellerId);
        }

        public List<AuctionEntry> GetAuctions()
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.AuctionEntries);

            return query.OrderBy(a => a.CreatedAt).ToList();
        }

        public int DeleteAuctionsBySeller(uint sellerId)
        {
            var entries = _charContext.CreateTrackingQuery(_charContext.AuctionEntries)
                .Where(a => a.SellerId == sellerId)
                .ToList();

            // Marked, not saved - unlike DeleteAuction, which is called from paths that have no
            // unit of work to complete. This one is called while a character is being deleted,
            // beside CharacterAppearances.DeleteForChar, and the rows have to go in the same
            // SaveChanges as the character row: committing them separately would take a
            // player's listings down and then leave the character standing if the delete failed.
            _charContext.AuctionEntries.RemoveRange(entries);

            return entries.Count;
        }

        public bool DeleteAuction(uint itemId)
        {
            var query = _charContext.CreateTrackingQuery(_charContext.AuctionEntries);
            var entry = query.FirstOrDefault(a => a.ItemId == itemId);

            if (entry == null)
                return false;

            _charContext.Remove(entry);
            _charContext.SaveChanges();
            return true;
        }
    }
}

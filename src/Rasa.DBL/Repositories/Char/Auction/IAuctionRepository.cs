using System.Collections.Generic;

namespace Rasa.Repositories.Char.Auction
{
    using Structures.Char;

    public interface IAuctionRepository
    {
        /// <summary>Lists an item. Returns false if that item is already listed.</summary>
        bool CreateAuction(AuctionEntry auction);

        /// <summary>The auction for one item, or null when it is not listed.</summary>
        AuctionEntry GetAuctionByItemId(uint itemId);

        /// <summary>Everything one character currently has listed, oldest first.</summary>
        List<AuctionEntry> GetAuctionsBySeller(uint sellerId);

        /// <summary>How many auctions a character has running, without loading them.</summary>
        int CountAuctionsBySeller(uint sellerId);

        /// <summary>Every live auction, for the browse tab and for expiry sweeps.</summary>
        List<AuctionEntry> GetAuctions();

        bool DeleteAuction(uint itemId);

        /// <summary>
        /// Takes down everything one character has listed and says how many rows went, on the
        /// unit of work's next Complete rather than immediately. For a character that is being
        /// deleted: an auction row carries no foreign key, so it would otherwise outlive its
        /// seller and stay in the browse results as a listing nobody can be paid for.
        /// </summary>
        int DeleteAuctionsBySeller(uint sellerId);
    }
}

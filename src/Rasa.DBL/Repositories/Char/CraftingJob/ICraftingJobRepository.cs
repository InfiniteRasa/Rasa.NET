using System.Collections.Generic;

namespace Rasa.Repositories.Char.CraftingJob
{
    using Structures.Char;

    /// <summary>The crafting stations' jobs: crafting_job. Every call but <see cref="DeleteByCharacter"/> saves at once.</summary>
    public interface ICraftingJobRepository
    {
        /// <summary>Adds a job. Returns its row id, which is the job's id from then on; 0 when it could not be added.</summary>
        uint Add(CraftingJobEntry entry);

        /// <summary>Every job there is, for loading at start.</summary>
        List<CraftingJobEntry> GetAll();

        /// <summary>What is still to be taken of a job.</summary>
        bool UpdateCount(uint id, uint count);

        bool Delete(uint id);

        /// <summary>Every job at a station: the station is being deleted. Returns how many went.</summary>
        int DeleteByKraftwerks(uint kraftwerksId);

        /// <summary>
        /// Every job of a character: the character is being deleted. Marked, not saved - the rows
        /// go in the same SaveChanges as the character row (see AuctionRepository.DeleteAuctionsBySeller).
        /// Returns how many were marked.
        /// </summary>
        int DeleteByCharacter(uint characterId);
    }
}

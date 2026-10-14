using System.Collections.Generic;

namespace Rasa.Repositories.Char.Petition
{
    using Structures.Char;

    public interface IPetitionRepository
    {
        /// <summary>
        /// Writes the petition and returns the generated id, or 0 when the row could not be
        /// written. The id is what the client is acknowledged with.
        /// </summary>
        uint AddPetition(PetitionEntry entry);

        /// <summary>The petition with this id, or null when there is none.</summary>
        PetitionEntry GetPetition(uint id);

        /// <summary>
        /// Newest first, capped. A null status means every status - the console defaults to
        /// open ones, because a server that has been up a while has far more closed than open.
        /// </summary>
        List<PetitionEntry> ListPetitions(byte? status, int limit);

        /// <summary>Newest first, capped, for one account only.</summary>
        List<PetitionEntry> ListPetitionsForAccount(uint accountId, int limit);

        /// <summary>
        /// Replaces the body. Used by AddToPetition, which appends rather than replacing - the
        /// caller does the appending so the length rules live with the rest of them.
        /// </summary>
        bool UpdatePetitionBody(uint id, string body);

        /// <summary>
        /// Moves a petition to a new status, and reports whether the row was there to move.
        /// Resolution is stored alongside so the reason travels with the state.
        /// </summary>
        bool SetPetitionStatus(uint id, byte status, string resolution);

        /// <summary>Writes a game master's answer, and reports whether the row was there to take it.</summary>
        bool SetPetitionResolution(uint id, string resolution);

        /// <summary>Whether the player has yet to be told of the petition's answer or close.</summary>
        bool SetNoticePending(uint id, bool pending);

        /// <summary>The account's petitions it has yet to be told about, oldest first.</summary>
        List<PetitionEntry> ListNoticesPending(uint accountId);
    }
}

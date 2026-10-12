using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Repositories.Char;
    using Structures.Char;

    /// <summary>
    /// Which races an account may make a character of.
    ///
    /// The client shows a hybrid it is not sent as enabled locked, with "Unlock this hybrid by
    /// completing certain missions in game.", and has a message for each unlock: "Hybrid Access
    /// Granted: Forean Hybrid" (PmHybridForeanOpen), Brann (PmHybridBrannOpen) and "Bane Hybrid"
    /// for the Thrax (PmHybridBaneOpen). The missions are those whose finishing text grants it:
    ///  - Forean: 1861 Traitor on the Run, Elder Q'uoa in Thoria Das - "I give you access to this
    ///    [the Forean genetic code]. With it, you will be able to create a hybrid Forean clone."
    ///  - Brann: 1851 Remedy, Connant at Baylor Base - "You're going to have Brann DNA coursing
    ///    through your blood stream." The weakest of the three: it never says hybrid.
    ///  - Thrax: 1899 Genome Sweet Genome, for Penumbra in Staal - "I'm giving you the
    ///    authorization to create a cloned Thrax Hybrid."
    /// Kept for the account (GameAccountEntry.HybridUnlocks), as the window it opens is the
    /// account's. Nothing in the client says whether the original kept it per account or per
    /// character.
    ///
    /// GameDataConfig.AlwaysUnlockHybrids, on unless set false, offers every race to everyone and
    /// says nothing as a mission unlocks one; the unlock is still recorded. Off, an account is
    /// offered a human and the hybrids it has unlocked. Either way only the races in
    /// GameDataConfig.EnabledRaces (CharacterManager.EnabledRaces) are ever offered.
    /// </summary>
    public static class HybridUnlocks
    {
        private static readonly Dictionary<uint, (Race Race, PlayerMessage Message)> Missions = new()
        {
            [1861] = (Race.Forean, PlayerMessage.PmHybridForeanOpen),
            [1851] = (Race.Brann, PlayerMessage.PmHybridBrannOpen),
            [1899] = (Race.Thrax, PlayerMessage.PmHybridBaneOpen)
        };

        /// <summary>GameDataConfig.AlwaysUnlockHybrids: every enabled race offered to every account.</summary>
        public static bool AlwaysUnlocked { get; private set; } = true;

        /// <summary>Takes AlwaysUnlockHybrids from the configuration, on load and on every reload; no setting is on.</summary>
        public static void Load(bool? alwaysUnlockHybrids) => AlwaysUnlocked = alwaysUnlockHybrids ?? true;

        /// <summary>The account's bit for a hybrid race (GameAccountEntry.HybridUnlocks); 0 for a human.</summary>
        public static byte Bit(Race race) => race is Race.Forean or Race.Brann or Race.Thrax ? (byte)(1 << ((int)race - 2)) : (byte)0;

        /// <summary>The hybrid race completing this mission unlocks, if it unlocks one.</summary>
        public static bool TryGetRace(uint missionId, out Race race)
        {
            race = Missions.TryGetValue(missionId, out var unlock) ? unlock.Race : default;
            return race != default;
        }

        /// <summary>Whether this account has unlocked the race by its mission; a human always.</summary>
        public static bool HasUnlocked(GameAccountEntry account, Race race) =>
            race == Race.Human || account != null && (account.HybridUnlocks & Bit(race)) != 0;

        /// <summary>The races the character creation window lets this account pick.</summary>
        public static IReadOnlyList<Race> OfferedTo(GameAccountEntry account) =>
            AlwaysUnlocked
                ? CharacterManager.EnabledRaces
                : CharacterManager.EnabledRaces.Where(race => HasUnlocked(account, race)).ToList();

        /// <summary>Whether this account may make a character of this race.</summary>
        public static bool IsOfferedTo(GameAccountEntry account, Race race) =>
            CharacterManager.IsRaceEnabled(race) && (AlwaysUnlocked || HasUnlocked(account, race));

        /// <summary>
        /// Records the hybrid this mission unlocks for the account, in the reward's own
        /// transaction; the race, if the account did not have it. Run again on a retried
        /// transaction it gives the same answer, as nothing is kept until it commits.
        /// </summary>
        public static Race? Record(ICharUnitOfWork unitOfWork, GameAccountEntry account, uint missionId)
        {
            if (account == null || !TryGetRace(missionId, out var race))
                return null;

            return unitOfWork.GameAccounts.AddHybridUnlocks(account.Id, Bit(race)) != 0 ? race : null;
        }

        /// <summary>
        /// Once the reward has committed: the logged-in account has the race from now on, and is
        /// told so if that changes what it is offered.
        /// </summary>
        public static void Granted(Client client, Race? race)
        {
            if (race is not { } unlocked || client?.AccountEntry == null)
                return;

            var wasOffered = IsOfferedTo(client.AccountEntry, unlocked);
            client.AccountEntry.HybridUnlocks |= Bit(unlocked);

            if (wasOffered || !IsOfferedTo(client.AccountEntry, unlocked))
                return;

            var message = Missions.Values.Single(unlock => unlock.Race == unlocked).Message;
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }
    }
}

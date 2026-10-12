namespace Rasa.Data
{
    /// <summary>
    /// The four clan ranks and what each one may do, from the client's own
    /// <c>shared.gameconstants</c>: <c>CLAN_RANK_1</c> through <c>CLAN_RANK_4</c> are 0 to 3, and
    /// <c>CLAN_RANK_LEADER</c> is the last of them.
    ///
    /// These have to match, because the client decides what to *offer* from the same numbers -
    /// it greys out the invite button below <c>CLAN_MIN_RANK_TO_INVITE</c> and refuses to send on
    /// the leaders channel below <c>CLAN_MIN_RANK_TO_SPEAK_IN_LEADERS_CHANNEL</c>. A server that
    /// drew the line somewhere else would either refuse things the window had already offered, or
    /// allow things it never shows.
    ///
    /// They are the client's *own* view of the sender's rank, though, which is as current as the
    /// last roster it was sent. Every one of these is checked here as well.
    /// </summary>
    public static class ClanRank
    {
        /// <summary>The rank a member joins at, and the lowest that exists.</summary>
        public const byte Member = 0;

        /// <summary>CLAN_RANK_4, and CLAN_RANK_LEADER: one per clan.</summary>
        public const byte Leader = 3;

        /// <summary>CLAN_MIN_RANK_TO_INVITE.</summary>
        public const byte MinRankToInvite = 1;

        /// <summary>CLAN_MIN_RANK_TO_SPEAK_IN_LEADERS_CHANNEL.</summary>
        public const byte MinRankToSpeakInLeadersChannel = 2;

        /// <summary>
        /// CLAN_RANK_TO_WITHDRAW_FROM_LOCKBOX. Buying a lockbox tab is held to this as well: it
        /// spends the clan's prestige, which is a withdrawal in everything but name, and the
        /// client gates the withdraw button on it while leaving the purchase button open.
        /// </summary>
        public const byte MinRankToWithdrawFromLockbox = 2;

        /// <summary>CLAN_RANK_TO_CHALLENGE, for feuds and wargames. Unused until 8.8.</summary>
        public const byte MinRankToChallenge = 3;

        /// <summary>
        /// The rank from which the clan window offers Remove Member, Promote and Demote
        /// (socialwindow.py, its CLAN_RANK_3 options): the leader and the rank below them. Each
        /// is offered on a member below the caller - a kick or a demotion on anyone at least one
        /// rank down, a promotion on anyone at least two down, since nobody is promoted into the
        /// caller's own rank - and a promotion only up to <see cref="MaxPromotedRank"/>: the rank
        /// below Leader, which is reached by Make Leader alone.
        /// </summary>
        public const byte MinRankToManageMembers = 2;

        /// <summary>The highest rank Promote reaches (CLAN_RANK_3); Leader is handed over, never promoted into.</summary>
        public const byte MaxPromotedRank = 2;

        /// <summary>Whether a member of rank <paramref name="actor"/> may promote one of rank <paramref name="target"/>, as the client offers it.</summary>
        public static bool MayPromote(byte actor, byte target) =>
            actor >= MinRankToManageMembers && actor - target >= 2 && target + 1 <= MaxPromotedRank;

        /// <summary>Whether a member of rank <paramref name="actor"/> may demote one of rank <paramref name="target"/>, as the client offers it.</summary>
        public static bool MayDemote(byte actor, byte target) =>
            actor >= MinRankToManageMembers && actor > target && target > Member;
    }
}

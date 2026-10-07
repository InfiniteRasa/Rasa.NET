using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;

    /// <summary>
    /// What a kill is worth to those who made it, shared with the squad: its experience and its
    /// adrenaline are split evenly between the player with the kill and every member of their
    /// squad who shares in it - on the killer's map, within PartyManager.LootShareRange (200 m)
    /// of the corpse. That is PartyManager.SharersOf, the rule a kill's loot, its boss titles
    /// and a garrison's prestige already go by; a player in no squad shares with nobody and is
    /// paid the whole of it.
    ///
    /// The split is ours: the client has no part in it and nothing we have says how the
    /// original server divided a kill. What it is:
    ///  - even, whatever the levels of the squad: the amount / those it is split between,
    ///    rounded down, and never less than 1 for a kill that was worth anything. What does
    ///    not divide is lost;
    ///  - between those who can be paid, so nobody's part goes to waste:
    ///     - experience, between the sharers below the level cap. One at the cap gains none
    ///       (ManifestationManager.GainExperience) and takes no share from the rest; a killer
    ///       at the cap leaves the whole kill to their squad;
    ///     - adrenaline, between the sharers who are alive. A dead one is given none
    ///       (ManifestationManager.GainAdrenaline) and takes no share. A member whose bar is
    ///       already full is counted: full is a moment, not a state;
    ///  - of each award by itself. A finishing move pays the kill twice
    ///    (CreatureManager.HandleCreatureKill), and both are split. The second is "by Crit
    ///    Killing" to the player with the kill - or "by Team Crit Killing" if somebody else
    ///    opened the window, as it was - and "by Team Crit Killing" to the rest of the squad:
    ///    their team finished it.
    ///
    /// Adrenaline is a share of each player's own bar (ManifestationManager.AdrenalineForKill:
    /// AdrenalinePerKillPercent of it, scaled by their Regen), so what is split is each
    /// sharer's own figure for the kill, doubled for a finish, and not the killer's.
    ///
    /// No group bonus is added: XPInfo's groupMod stays 1. Each sharer's part is multiplied by
    /// their own kill streak (KillStreaks), which their line says, and by nothing else.
    /// </summary>
    public static class KillShares
    {
        /// <summary>Who shares in a kill: the squad of the player with it, near the corpse. A test's to replace.</summary>
        public static Func<Client, Vector3, List<Client>> SharersOf { get; set; } =
            (killer, corpse) => PartyManager.Instance.SharersOf(killer, corpse);

        /// <summary>What each of sharers players is paid of an amount split between them.</summary>
        public static uint ShareOf(uint amount, int sharers)
        {
            if (amount == 0)
                return 0;

            return sharers <= 1 ? amount : Math.Max(1u, amount / (uint)sharers);
        }

        /// <summary>
        /// The players a kill at corpse is shared with: the killer, first, and those of their
        /// squad who share in it, each once.
        /// </summary>
        public static List<Client> SharersFor(Client killer, Vector3 corpse)
        {
            var sharers = new List<Client>();

            if (killer?.Player == null)
                return sharers;

            sharers.Add(killer);

            List<Client> squad = null;

            try
            {
                squad = SharersOf?.Invoke(killer, corpse);
            }
            catch (Exception e)
            {
                // The kill is still the killer's: a squad that could not be read costs them nothing.
                Logger.WriteLog(LogType.Error, $"The squad sharing a kill of character {killer.Player.Id} could not be read: {e}");
            }

            if (squad != null)
                foreach (var member in squad)
                    if (member?.Player != null && !sharers.Contains(member))
                        sharers.Add(member);

            return sharers;
        }

        /// <summary>Whether a kill's experience can be paid to this player: they are below the level cap.</summary>
        public static bool GainsExperience(Client client) =>
            client?.Player != null && client.Player.Level >= 1 && client.Player.Level < ManifestationManager.MaxPlayerLevel;

        /// <summary>Whether a kill's adrenaline can be given to this player: they are alive.</summary>
        public static bool GainsAdrenaline(Client client) =>
            client?.Player != null && client.Player.State != CharacterState.Dead;

        /// <summary>
        /// Pays one award of a kill's experience to those of the sharers who can gain it, in
        /// equal parts, and returns the part. critKill is the killer's: the others are told of
        /// a finish as their team's.
        /// </summary>
        public static uint AwardExperience(ManifestationManager manifestations, IReadOnlyList<Client> sharers, Client killer, uint experience, CritKill critKill = CritKill.None)
        {
            if (manifestations == null || sharers == null)
                return 0;

            var paid = sharers.Where(GainsExperience).ToList();

            if (paid.Count == 0)
                return 0;

            var share = ShareOf(experience, paid.Count);

            if (share == 0)
                return 0;

            // Each their part, with what their own item modules add to it: "3% experience from
            // kills." (ItemModuleBonuses) - and that, times their own kill streak (KillStreaks).
            // The part returned is the part before either.
            foreach (var sharer in paid)
            {
                var streak = KillStreaks.MultiplierOf(sharer);
                var part = ItemModuleBonuses.WithExperience(sharer.Player, share);

                manifestations.GainExperience(sharer, (uint)Math.Min(uint.MaxValue, (ulong)part * (ulong)streak),
                    sharer == killer || critKill == CritKill.None ? critKill : CritKill.Team, streak);
            }

            return share;
        }

        /// <summary>
        /// Gives a kill's adrenaline to those of the sharers who are alive, each their own
        /// figure for a kill - twice it for a finishing move, in one award, so the bar shows
        /// one number - divided by how many of them there are.
        /// </summary>
        public static void AwardAdrenaline(ManifestationManager manifestations, IReadOnlyList<Client> sharers, bool finished)
        {
            if (manifestations == null || sharers == null)
                return;

            var paid = sharers.Where(GainsAdrenaline).ToList();

            foreach (var sharer in paid)
            {
                var worth = manifestations.AdrenalineForKill(sharer);

                if (worth <= 0)
                    continue;

                manifestations.GainAdrenaline(sharer, (int)ShareOf((uint)(finished ? worth * 2 : worth), paid.Count));
            }
        }
    }
}

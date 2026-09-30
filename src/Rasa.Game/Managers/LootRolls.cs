using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Party.Server;
    using Structures;

    /// <summary>
    /// Squad loot rolls: the squad's loot threshold at work, and PartyMemberRoll to show it.
    ///
    /// The client has a threshold for the leader to set (Junk to Epic, next to the loot method)
    /// and prints roll results it is sent, but has no need, greed or pass buttons: the rolling is
    /// the server's. Its messages - "Start rolling on X", "(greed rolls)", "There's a tie, start
    /// rolling again", "X has looting rights to Y" for a need nobody else shares - come in the same
    /// late block of ids as "Loot threshold changed", so the threshold is what they answer to:
    /// an item at or above it is rolled for among the squad sharing the corpse, whatever the loot
    /// method, and below it the method decides as before. Nothing in the data says what "need"
    /// is, how big the die is or what an unset threshold means; the choices here are:
    ///
    /// - Need: an item the member could equip - they have the skill it wants, at any level, and
    ///   it is not for another race (InventoryManager.ValidateItemEquip less the level and
    ///   attribute checks, which a member grows into). The needers roll; if nobody needs it
    ///   everyone rolls greed. A lone needer has it with no dice.
    /// - Rolls are 1-100; a tie at the top is thrown again by those who tied.
    /// - A squad that never set a threshold rolls from Uncommon up.
    /// - Mission items are never rolled. The Dice Roll method, which the client cannot choose,
    ///   rolls every item.
    ///
    /// The winner has the item to themselves for the life of the corpse (LootItem.ReservedFor),
    /// and is added to its looters - under Rotation that is how they get to the corpse at all.
    /// </summary>
    public static class LootRolls
    {
        public const int DieSides = 100;

        /// <summary>Ties thrown again at most this often; after that the first of those tied has it.</summary>
        public const int MaxRounds = 20;

        public const LootQuality DefaultThreshold = LootQuality.Uncommon;

        private static readonly Random Die = new Random();

        /// <summary>One throw of the die, 1..DieSides.</summary>
        public static int Throw()
        {
            lock (Die)
                return Die.Next(1, DieSides + 1);
        }

        /// <summary>The squad's threshold as a quality, or the default for one never set.</summary>
        public static LootQuality ThresholdOf(Party party)
        {
            var threshold = (LootQuality)(int)party.LootThreshold;

            return Enum.IsDefined(typeof(LootQuality), threshold) && threshold != LootQuality.Mission ? threshold : DefaultThreshold;
        }

        /// <summary>Whether an item on a squad's corpse is rolled for.</summary>
        public static bool IsRolled(Party party, Item item)
        {
            if (party == null || item?.ItemTemplate == null || party.LootMethod == PartyLootMethod.Individual)
                return false;

            var quality = (LootQuality)item.ItemTemplate.QualityId;

            if (quality == LootQuality.Mission)
                return false;

            if (party.LootMethod == PartyLootMethod.DiceRoll)
                return true;

            return quality.Rank() >= ThresholdOf(party).Rank();
        }

        /// <summary>Whether the member could equip the item: the skill it asks for, and their race if it names one.</summary>
        public static bool Needs(Manifestation player, Item item)
        {
            var template = item?.ItemTemplate;

            if (player == null || template?.EquipableInfo == null)
                return false;

            if (template.ItemInfo != null && template.ItemInfo.RaceReq != 0 && template.ItemInfo.RaceReq != (int)player.Race)
                return false;

            var skillId = template.EquipableInfo.SkillId;

            return skillId == 0 || player.Skills.ContainsKey((SkillId)skillId);
        }

        public class Result
        {
            public uint WinnerUserId { get; set; }
            public bool Greed { get; set; }

            /// <summary>Each throw, round by round: account id to roll.</summary>
            public List<List<(uint UserId, int Roll)>> Rounds { get; } = new();
        }

        /// <summary>
        /// Rolls one item among the rollers (account id, whether they need it): the needers if
        /// there are any, everyone as greed if not, a tie at the top thrown again by those tied.
        /// </summary>
        public static Result Roll(IReadOnlyList<(uint UserId, bool Needs)> rollers, Func<int> die)
        {
            var result = new Result();

            if (rollers == null || rollers.Count == 0)
                return result;

            var needers = rollers.Where(r => r.Needs).Select(r => r.UserId).ToList();

            result.Greed = needers.Count == 0;

            var inRound = result.Greed ? rollers.Select(r => r.UserId).ToList() : needers;

            for (var round = 0; round < MaxRounds; round++)
            {
                var throws = inRound.Select(u => (UserId: u, Roll: die())).ToList();
                result.Rounds.Add(throws);

                var top = throws.Max(t => t.Roll);
                var tied = throws.Where(t => t.Roll == top).Select(t => t.UserId).ToList();

                result.WinnerUserId = tied[0];

                if (tied.Count == 1)
                    break;

                inRound = tied;
            }

            return result;
        }

        /// <summary>
        /// Rolls the corpse's items that are at or over the squad's threshold among the members
        /// sharing it (eligible: in the world on the killer's map, within reach - as
        /// PartyManager.LootersFor counts them), reserves each for its winner, adds the winner to
        /// the corpse's looters, and tells every one of them how it went. Nothing is rolled among
        /// fewer than two. Returns the winners who were not already looters, to be shown the corpse.
        /// </summary>
        public static List<Client> Distribute(LootDispenser loot, Party party, List<Client> eligible, Func<int> die = null)
        {
            var added = new List<Client>();

            if (loot == null || party == null || eligible == null || eligible.Count < 2)
                return added;

            die ??= Throw;

            foreach (var lootItem in loot.LootItems)
            {
                if (lootItem.Taken || lootItem.ReservedFor != 0 || !IsRolled(party, lootItem.Item))
                    continue;

                var rollers = eligible.Select(c => (c.AccountEntry.Id, Needs(c.Player, lootItem.Item))).ToList();
                var result = Roll(rollers, die);
                var winner = eligible.Find(c => c.AccountEntry.Id == result.WinnerUserId);

                if (winner == null)
                    continue;

                lootItem.ReservedFor = winner.Player.EntityId;
                lootItem.ActorId = winner.Player.EntityId;
                lootItem.PartyId = 0;

                if (loot.Looters.Add(winner.Player.EntityId) && !added.Contains(winner))
                    added.Add(winner);

                var packet = new PartyMemberRollPacket(lootItem.ItemClassId, result.WinnerUserId, result.Rounds, result.Greed);

                foreach (var member in eligible)
                    member.CallMethod(SysEntity.ClientPartyManagerId, packet);
            }

            return added;
        }
    }
}

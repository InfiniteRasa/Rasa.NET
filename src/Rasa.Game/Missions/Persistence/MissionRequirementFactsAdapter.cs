using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Runtime;
using Rasa.Repositories.Char;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Game.Missions.Persistence
{
    internal sealed class MissionRequirementFactsAdapter
    {
        internal static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
        {
            "character.level-is-even", "account.starting-experience-entitled", "character.starting-experience-completed",
            "character.starting-experience-active"
        };

        internal static MissionRequirementFacts WithLevel(MissionRequirementFacts facts, uint level) =>
            WithCustom(facts with { Level = level }, "character.level-is-even", level % 2 == 0);

        internal static MissionRequirementFacts WithCustom(MissionRequirementFacts facts, string key, bool value)
        {
            if (!facts.Custom.ContainsKey(key))
                return facts;
            var custom = facts.Custom.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            custom[key] = value;
            return facts with { Custom = custom };
        }

        internal static MissionRequirementFacts WithFlag(MissionRequirementFacts facts, uint flagId, uint? value,
            bool completedStartingExperienceState)
        {
            var flags = facts.Flags.ToDictionary(entry => entry.Key, entry => entry.Value);
            if (value.HasValue)
                flags[flagId] = value.Value;
            else
                flags.Remove(flagId);
            facts = facts with { Flags = flags };
            return flagId == CharacterFlagIds.BootcampComplete
                ? WithCustom(facts, "character.starting-experience-completed", completedStartingExperienceState || value == 1)
                : facts;
        }

        internal MissionRequirementFacts Read(Manifestation player, IReadOnlyCollection<string> requested, ICharUnitOfWork unit = null) =>
            Read(player, requested, unit, out _);

        internal MissionRequirementFacts Read(Manifestation player, IReadOnlyCollection<string> requested, ICharUnitOfWork unit,
            out bool completedStartingExperienceState)
        {
            var custom = new Dictionary<string, bool>(StringComparer.Ordinal);
            var character = unit?.Characters.Get(player.Id);
            if (unit != null && character == null)
                throw new MissionRuleException($"No authoritative character facts are available for {player.Id}.");
            completedStartingExperienceState = unit != null && requested.Contains("character.starting-experience-completed") &&
                HasCompletedStartingExperienceState(unit, player.Id);
            foreach (var fact in requested)
            {
                if (!Supported.Contains(fact))
                    throw new MissionRuleException($"No Game fact provider is registered for {fact}.");
                custom[fact] = fact switch
                {
                    "character.level-is-even" => (character?.Level ?? player.Level) % 2 == 0,
                    "account.starting-experience-entitled" => ReadAccountEntitlement(player, character, unit),
                    "character.starting-experience-completed" => unit == null
                        ? player.StartingExperienceCompleted
                        : completedStartingExperienceState || unit.CharacterFlags.HasValue(player.Id, CharacterFlagIds.BootcampComplete),
                    "character.starting-experience-active" => unit == null
                        ? throw new MissionRuleException("Active starting-experience offers require durable source facts.")
                        : unit.CharacterStartingExperience.ReadState(player.Id) == CharacterStartingExperienceState.Bootcamp,
                    _ => throw new MissionRuleException($"Unknown fact {fact}.")
                };
            }
            var history = unit?.CharacterMissions.Runtime.History(player.Id);
            return new MissionRequirementFacts(character?.Level ?? player.Level,
                unit == null ? player.Missions.ToDictionary(entry => entry.Key, entry => entry.Value.State) :
                    unit.CharacterMissions.Get(player.Id).ToDictionary(entry => entry.MissionId, entry => (MissionState)entry.MissionState),
                unit == null ? player.MissionHistory :
                    history.GroupBy(entry => entry.MissionId).ToDictionary(group => group.Key,
                        group => (MissionState)group.OrderByDescending(entry => entry.AssignmentGeneration)
                            .ThenByDescending(entry => entry.CompletedAtUtc).ThenByDescending(entry => entry.AssignmentId).First().Outcome),
                unit == null ? player.PlayerFlags : unit.CharacterFlags.Get(player.Id), custom,
                unit == null ? player.MissionSuccessHistory :
                    history.Where(entry => entry.Rewarded || entry.Outcome is 1 or 4).Select(entry => entry.MissionId).ToHashSet(),
                unit == null ? player.MissionRewardTimes.Keys.ToHashSet() :
                    history.Where(entry => entry.Rewarded).Select(entry => entry.MissionId).ToHashSet());
        }

        private static bool ReadAccountEntitlement(Manifestation player, CharacterEntry character, ICharUnitOfWork unit)
        {
            var account = unit != null
                ? unit.GameAccounts.Get(character.AccountId)
                : player.MapChannel?.ClientList?.FirstOrDefault(client => ReferenceEquals(client.Player, player))?.AccountEntry;
            if (account == null)
                throw new MissionRuleException($"No authoritative account entitlement fact is available for character {player.Id}.");
            return account.CanSkipBootcamp;
        }

        internal static bool HasCompletedStartingExperience(ICharUnitOfWork unit, uint characterId) =>
            HasCompletedStartingExperienceState(unit, characterId) ||
            unit.CharacterFlags.HasValue(characterId, CharacterFlagIds.BootcampComplete);

        private static bool HasCompletedStartingExperienceState(ICharUnitOfWork unit, uint characterId) =>
            unit.CharacterStartingExperience.ReadState(characterId) is
                CharacterStartingExperienceState.Completed or CharacterStartingExperienceState.Skipped;
    }
}

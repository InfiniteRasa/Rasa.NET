using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
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
            "character.starting-experience-active", "character.soldier-family", "character.specialist-family"
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
                    "character.soldier-family" => CharacterClassTree.Is(
                        (CharacterClass)(character?.Class ?? player.Class), CharacterClass.Soldier),
                    "character.specialist-family" => CharacterClassTree.Is(
                        (CharacterClass)(character?.Class ?? player.Class), CharacterClass.Specialist),
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

        internal static MissionRequirementFacts WithAssignmentItems(MissionRequirementFacts facts, Manifestation player,
            MissionRequirement requirement, ICharUnitOfWork unit, Func<uint, Mission> resolve)
        {
            var requested = MissionRequirementEvaluator.AssignmentItemRequirements(requirement);
            if (requested.Count == 0)
                return facts with { AssignmentItems = new Dictionary<AssignmentItemRequirement, uint>() };
            if (unit == null || resolve == null)
                throw new MissionRuleException("Assignment inventory eligibility requires authoritative character and content sources.");
            var quantities = new Dictionary<AssignmentItemRequirement, uint>();
            foreach (var item in requested)
                quantities[item] = AssignmentQuantity(player, item, unit, resolve);
            return facts with { AssignmentItems = quantities };
        }

        private static uint AssignmentQuantity(Manifestation player, AssignmentItemRequirement requirement,
            ICharUnitOfWork unit, Func<uint, Mission> resolve)
        {
            var definition = resolve(requirement.MissionId);
            if (definition?.IsOperational != true ||
                !definition.Items.TryGetValue(requirement.ItemKey, out var binding) ||
                binding.Scope != MissionItemScope.AssignmentIssued)
                throw new MissionRuleException("Assignment inventory requirement names no operational issued-item binding.");
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(player.Id, requirement.MissionId);
            if (assignment?.MissionState != (uint)MissionState.Active ||
                assignment.ContentRevision != definition.ContentRevision ||
                unit.CharacterMissionItems.GetQuarantine(player.Id, assignment.AssignmentId) != null)
                return 0;
            if (requirement.SourceOfferMissionId is uint sourceMission)
            {
                var offer = unit.MissionOffers.Get(player.Id, sourceMission);
                var recipient = unit.CharacterMissions.GetByCharacterAndMission(player.Id, sourceMission);
                var source = offer?.SourceInstanceId == null ? null :
                    unit.CharacterMissions.Runtime.Scene(offer.SourceInstanceId);
                if (offer?.State != MissionOfferState.Consumed ||
                    offer.SourceKind != Rasa.Missions.Definitions.MissionOfferSourceKind.Scene ||
                    recipient?.AssignmentId != offer.ConsumedAssignmentId ||
                    recipient.Generation != offer.ConsumedAssignmentGeneration ||
                    offer.SourceAssignmentId != assignment.AssignmentId ||
                    offer.SourceAssignmentGeneration != assignment.Generation ||
                    source?.AssignmentId != assignment.AssignmentId ||
                    source.MissionId != assignment.MissionId || source.Generation != offer.SourceGeneration ||
                    source.Status is not ("Running" or "Waiting"))
                    return 0;
            }
            var character = unit.Characters.Get(player.Id)
                ?? throw new MissionRuleException("Assignment inventory owner is unavailable.");
            var owners = unit.CharacterMissionItems.GetOwned(player.Id).Where(owner =>
                owner.MissionId == assignment.MissionId && owner.AssignmentId == assignment.AssignmentId &&
                owner.Generation == assignment.Generation && owner.ItemKey == requirement.ItemKey).ToArray();
            var slots = unit.CharacterInventories.GetItems(character.AccountId).Where(slot =>
                    slot.CharacterId == player.Id && slot.InventoryType == (uint)InventoryType.Personal)
                .GroupBy(slot => slot.ItemId).ToDictionary(group => group.Key, group => group.Count());
            var items = unit.Items.GetItems(owners.Select(owner => owner.ItemId).ToArray()).ToDictionary(item => item.ItemId);
            ulong quantity = 0;
            foreach (var owner in owners)
            {
                if (slots.GetValueOrDefault(owner.ItemId) != 1 || !items.TryGetValue(owner.ItemId, out var item) ||
                    item.ItemTemplateId != binding.ItemTemplateId || item.StackSize != owner.Quantity)
                    return 0;
                quantity += owner.Quantity;
            }
            return checked((uint)quantity);
        }

        internal static bool HasCompletedStartingExperience(ICharUnitOfWork unit, uint characterId) =>
            HasCompletedStartingExperienceState(unit, characterId) ||
            unit.CharacterFlags.HasValue(characterId, CharacterFlagIds.BootcampComplete);

        private static bool HasCompletedStartingExperienceState(ICharUnitOfWork unit, uint characterId) =>
            unit.CharacterStartingExperience.ReadState(characterId) is
                CharacterStartingExperienceState.Completed or CharacterStartingExperienceState.Skipped;
    }
}

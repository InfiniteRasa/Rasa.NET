namespace Rasa.Structures
{
    internal sealed record MissionLootSource(uint MissionId, uint ObjectiveId, uint RewardId)
    {
        internal string ClaimKey(uint itemTemplateId) =>
            $"reward-loot:{RewardId}:{itemTemplateId}";
    }
}

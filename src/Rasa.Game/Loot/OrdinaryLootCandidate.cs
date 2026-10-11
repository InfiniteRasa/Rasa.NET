namespace Rasa.Loot
{
    internal sealed record OrdinaryLootCandidate(
        uint TemplateId, int Weight, uint MinimumQuantity, uint MaximumQuantity);
}

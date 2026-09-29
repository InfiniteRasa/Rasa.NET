namespace Rasa.Data
{
    /// <summary>shared/gameconstants.py LOOT_METHOD_*. A new squad is Individual (0), which is
    /// what the client assumes until it is told otherwise; the squad window offers Free For All and
    /// Rotation. Random and Dice Roll have names in the client but no way to pick them.</summary>
    public enum PartyLootMethod
    {
        Individual  = 0,
        FreeForAll  = 1,
        Rotation    = 2,
        Random      = 3,
        DiceRoll    = 4
    }
}

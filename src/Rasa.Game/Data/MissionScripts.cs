namespace Rasa.Data
{
    public enum MissionScriptCommand
    {
        Dispenser               = 1,    // value1: creatureDbId of giver NPC
        Collector               = 2,    // value1: creatureDbId of collector NPC
        RewardItem              = 3,    // value1: templateId of item, value2: item quantity
        RewardXp                = 4,    // value1: xp
        RewardCredits           = 5,    // value1: credits
        RewardPrestige          = 6,    // value1: prestige
        MissionInfoCategoryId   = 7,    // value1: categoryId (missioncategorylanguage.py)
        MissionInfoLevel        = 8,    // value1: mission level (not to be confused with required level)
        NoOp                    = 9,    // does nothing (placeholder)
        CompleteObjective       = 10,   // value1: creatureDbId of talk-to-me NPC, value2: objectiveId value3: playerFlagId
        ObjectiveAnnounce       = 11,   // value1: objectiveId
        ObjectiveCounter        = 12,   // value1: objectiveId value2: counterId value3: maxCount
        ObjectiveComplete       = 13	// value1: objectiveId
    }
}

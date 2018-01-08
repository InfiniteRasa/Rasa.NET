namespace Rasa.Data
{
    public enum MissionScriptCommand
    {
        Dispenser               = 1,    // value1: typeId of giver NPC
        CompleteObjective       = 2,    // value1: typeId of talk-to-me NPC, value2: objectiveId value3: playerFlagId
        Collector               = 3,    // value1: typeId of collector NPC
        CollectorRewardItem     = 4,    // value1: templateId of item, value2: item count
        CollectorRewardXp       = 5,    // value1: xp
        CollectorRewardCredits  = 6,    // value1: credits
        CollectorRewardPrestige = 7,    // value1: prestige
        MissionInfoCategoryId   = 8,    // value1: categoryId (missioncategorylanguage.py)
        MissionInfoLevel        = 9,    // value1: mission level (not to be confused with required level)
        NoOp                    = 10,   // does nothing (placeholder)
        ObjectiveAnnounce       = 11,   // value1: objectiveId
        ObjectiveCounter        = 12,   // value1: objectiveId value2: counterId value3: maxCount
        ObjectiveComplete       = 13	// value1: objectiveId
    }
}

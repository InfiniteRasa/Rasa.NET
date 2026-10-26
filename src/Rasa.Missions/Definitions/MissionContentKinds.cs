namespace Rasa.Structures.World
{
    public enum MissionContentRequirement : byte
    {
        Required = 1,
        Optional = 2
    }

    public enum MissionAbandonmentPolicy : byte
    {
        Allowed = 1,
        Prohibited = 2
    }

    public enum MissionActionKind : byte
    {
        RevealObjective = 1,
        ActivateObjective = 2,
        CompleteObjective = 3,
        GrantReward = 4,
        StartScenario = 5,
        ActivateSpawnGroup = 6,
        ShowIndicator = 7,
        SetPlayerFlag = 8,
        ShowAmbientConversation = 9,
        IssueMissionItem = 10,
        ConsumeMissionItem = 11,
        RemoveMissionItems = 12,
        FailRelatedMission = 13,

        /// <summary>
        /// item_intent TakeDownFieldIntent: the force field of a world_destructible row goes down
        /// for good on the player's map channel - in a squad instance, that copy alone
        /// (PlacedDestructibles.TakeDown).
        /// </summary>
        TakeDownField = 14
    }
}

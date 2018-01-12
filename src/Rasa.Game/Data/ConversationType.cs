namespace Rasa.Data
{
    public enum ConversationType
    {
        Greeting            = 0,
        ForceTopic          = 1,
        MissionDispense     = 2,
        MissionComplete     = 3,
        MissionReminder     = 4,
        ObjectiveAmbient    = 5,
        ObjectiveComplete   = 6,
        MissionReward       = 7,
        ObjectiveChoice     = 8,
        EndConversation     = 9,
        Training            = 10,
        Vending             = 11,
        ImportantGreering   = 12,
        Clan                = 13,
        Auctioneer          = 14,
        ForcedByScript      = 15
    }

    public enum ConversationStatus
    {
        None                = 0,
        Unavailable         = 1,
        Available           = 2,
        ObjectivComplete    = 3,
        MissionComplete     = 4,
        Reward              = 5,
        MissionReminder     = 6,
        ObjectivChoice      = 7,
        ObjectivAMB         = 8,
        Vending             = 9,
        Train               = 10,
        Greeting            = 11,
        ImportantGreeting   = 12,
        Clan                = 13,
        Auctioneer          = 14,
        Escort              = 15,
        CritDeath           = 16,
        WargameHostile      = 17,
        WargameFriendly     = 18,
        WargameNutral       = 19,
        WargameHostileSafe  = 20,
        WargameFriendlySafe = 21,
        WargameNeutralSafe  = 22,
        Idle                = 23
    }
}

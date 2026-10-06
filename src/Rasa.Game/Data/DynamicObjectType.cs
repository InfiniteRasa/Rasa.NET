namespace Rasa.Data
{
    public enum DynamicObjectType
    {
        ControlPoint        = 1,
        LocalTeleporter     = 2,
        Lockbox             = 3,
        Waypoint            = 4,
        Wormhole            = 5,
        MapTrigger          = 6,
        DropshipTeleporter  = 7,
        Logos               = 8,
        Kraftwerks          = 9,
        DropshipPad         = 10,   // the hovering dropship and beam over a transport pad
        Hortimonculus       = 11,   // the plant Hortimonculus grows from a corpse
        Emitter             = 12,   // an FXPackageEmitter playing an FX package (EmitterManager)
        ForceField          = 13,   // a force field a GM has placed (ForceFields)
        PracticeDummy       = 14,   // a Bootcamp practice target (PracticeTargetManager)
        DropshipBeacon      = 15,   // a Dropship Extraction Beacon's personal dropship (DropshipBeacons)
        TeamTeleporter      = 16,   // the pad at a team's teleporter in a battleground's staging area (Battlegrounds)
        PersonalWaypoint    = 17,   // a Personal Waypoint a player has put down (PersonalWaypoints)
        Scenery             = 18    // a piece of a map the server puts there: a mesh, no use and no target (SecretPassages)
    }
}

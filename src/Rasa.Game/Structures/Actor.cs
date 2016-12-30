namespace Rasa.Structures
{
    public class Actor
    {
        public uint EntityId { get; set; }
        public uint EntityClassId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }
        public int MapContextId { get; set; }
        public bool IsRunning { get; set; }
        public bool InCombatMode { get; set; }
        //public char State { get; set; }
        // action data
        //public ActorCurrentAction CurrentAction { get; set; }
        public ActorStats Stats = new ActorStats();
        //public GameEffect ActiveEffects { get; set; }
        public MapCellLocation CellLocation { get; set; } = new MapCellLocation();
        // sometimes we only have access to the actor, the owner variable allows us to access the client anyway (only if actor is a player manifestation)
        //public MapChannelClient Owner { get; set; }
    }
}

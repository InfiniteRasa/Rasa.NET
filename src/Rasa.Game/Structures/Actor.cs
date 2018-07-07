using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Managers;

    public class Actor
    {
        public Actor()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }
        public uint EntityId { get; set; }
        public int EntityClassId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public Position Position { get; set; }
        public Quaternion Rotation { get; set; }
        public int MapContextId { get; set; }
        public bool IsRunning { get; set; }
        public bool InCombatMode { get; set; }
        public ActorState State { get; set; }
        // action data
        public int CurrentAction { get; set; }
        public Dictionary<Attributes, ActorAttributes> Attributes = new Dictionary<Attributes, ActorAttributes>();
        public Dictionary<int, GameEffect> ActiveEffects { get; set; } = new Dictionary<int, GameEffect>();
        public MapCellLocation CellLocation { get; set; } = new MapCellLocation();
        // sometimes we only have access to the actor, the owner variable allows us to access the client anyway (only if actor is a player manifestation)
        //public MapChannelClient Owner { get; set; }
    }
}

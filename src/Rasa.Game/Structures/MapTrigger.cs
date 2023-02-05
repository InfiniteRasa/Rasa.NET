using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Game;
    using Interfaces;
    public class MapTrigger : IHasPosition
    {
        internal uint TriggerId { get; set; }
        internal string TriggerName { get; set; }
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        internal uint MapContextId { get; set; }
        internal List<Client> TrigeredBy = new List<Client>();

        public MapTrigger(uint id, string name, Vector3 positon, double rotation, uint contextId)
        {
            TriggerId = id;
            TriggerName = name;
            Position = positon;
            Rotation = rotation;
            MapContextId = contextId;
        }
    }
}

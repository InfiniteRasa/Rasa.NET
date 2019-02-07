using System.Numerics;

namespace Rasa.Structures
{
    public class BaseBehaviorBaseNode
    {
        public Vector3 Position { get; set; }
        public uint MapContextid { get; set; }
        public uint Index { get; set; } //-- postition of pathnode
    }
}
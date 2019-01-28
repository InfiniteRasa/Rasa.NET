namespace Rasa.Structures
{
    public class BaseBehaviorBaseNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public uint MapContextid { get; set; }
        public uint Index { get; set; } //-- postition of pathnode
    }
}
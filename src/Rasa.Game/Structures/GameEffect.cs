namespace Rasa.Structures
{
    public class GameEffect
    {
        // owner
        public int TypeId { get; set; } // effect class
        public int EffectId { get; set; } // effect id
        public uint EffectLevel { get; set; }
        public int Duration { get; set; }
        public int EffectTime { get; set; }
    }
}

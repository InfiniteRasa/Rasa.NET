namespace Rasa.Structures
{
    public class GameEffect
    {
        public int TypeId { get; set; } // effect class
        public int EffectId { get; set; } // effect id
        public int EffectLevel { get; set; }
        public int Duration { get; set; }
        public int EffectTime { get; set; }
        // list
        public GameEffect Previous { get; set; }
        public GameEffect Next { get; set; }
    }
}

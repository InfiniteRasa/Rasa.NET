namespace Rasa.Structures
{
    using Data;

    public class MissionScriptLine
    {
        public int MissionId { get; set; }
        public MissionScriptCommand Command { get; set; }
        public int State { get; set; }
        public int Flags { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        // calculated during preprocessing
        public int StorageIndex { get; set; } // indicates the index for this state
    }
}

namespace Rasa.Structures
{
    public class ModuleInfo
    {
        public int EffectId { get; set; }
        public int SetLevel { get; set; }
        public int FlatValue { get; set; }
        public int LinearValue { get; set; }
        public int ExpValue { get; set; }
        public int Arg1 { get; set; }
        public int Arg2 { get; set; }
        public int Arg3 { get; set; }
        public int Arg4 { get; set; }

        public ModuleInfo(int effectId, int setLevel, int flatValue, int linearValue, int expValue, int arg1, int arg2, int arg3, int arg4)
        {
            EffectId = effectId;
            SetLevel = setLevel;
            FlatValue = flatValue;
            LinearValue = linearValue;
            ExpValue = expValue;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = Arg4;
        }
    }
}

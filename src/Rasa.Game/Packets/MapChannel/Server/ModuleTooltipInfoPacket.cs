namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ModuleTooltipInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ModuleTooltipInfo;

        public int ModuleId { get; set; }
        public int ModuleLevel { get; set; }
        public ModuleInfo ModuleInfo { get; set; }

        public ModuleTooltipInfoPacket(ItemModule module)
        {
            ModuleId = module.ModuleId;
            ModuleLevel = module.ModuleLevel;
            ModuleInfo = module.ModuleInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        // ToDo find data types (not working curently)
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);           // 3 arguments
            pw.WriteInt(ModuleId);      // moduleId
            pw.WriteInt(ModuleLevel);   // moduleLevel
            pw.WriteTuple(9);       // moduleInfo
            pw.WriteInt(2);         // effectId
            pw.WriteInt(5);         // setLevel
            pw.WriteInt(1);     // flatValue
            pw.WriteInt(1);     // linearValue
            pw.WriteInt(1);     // expValue
            pw.WriteInt(1);  // arg1
            pw.WriteInt(1);  // arg2
            pw.WriteInt(1);  // arg3
            pw.WriteInt(1);  // arg4
            /*pw.WriteInt(ModuleInfo.EffectId);
            pw.WriteInt(ModuleInfo.SetLevel);
            pw.WriteInt(ModuleInfo.FlatValue);
            pw.WriteInt(ModuleInfo.LinearValue);
            pw.WriteInt(ModuleInfo.ExpValue);
            pw.WriteInt(ModuleInfo.Arg1);
            pw.WriteInt(ModuleInfo.Arg2);
            pw.WriteInt(ModuleInfo.Arg3);
            pw.WriteInt(ModuleInfo.Arg4);*/
        }
    }
}
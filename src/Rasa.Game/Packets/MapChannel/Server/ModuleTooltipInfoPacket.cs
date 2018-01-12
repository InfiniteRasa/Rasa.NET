namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ModuleTooltipInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ModuleTooltipInfo;

        public int ModuleId { get; set; }
        public int ListOfItems { get; set; }
        public ModuleInfo ModuleInfo { get; set; }

        public ModuleTooltipInfoPacket(int moduleId, ModuleInfo moduleInfo)
        {
            ModuleId = moduleId;
            ModuleInfo = moduleInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        // ToDo find data types (not working curently)
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);       // 3 arguments
            pw.WriteInt(ModuleId);  // moduleId
            pw.WriteInt(2);         // moduleLevel
            pw.WriteTuple(9);       // moduleInfo
            pw.WriteInt(2);         // effectId
            pw.WriteInt(5);         // setLevel
            pw.WriteDouble(3D);     // flatValue
            pw.WriteDouble(4D);     // linearValue
            pw.WriteDouble(5D);     // expValue
            pw.WriteString("111");  // arg1
            pw.WriteString("111");  // arg2
            pw.WriteString("111");  // arg3
            pw.WriteString("111");  // arg4
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
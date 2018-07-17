using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ResistanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResistanceData;

        public List<ResistDataDict> ResistDataDict { get; set; }

        /*public ResistanceDataPacket(List<ResistDataDict> resistDataDict)
        {
            ResistDataDict = ResistDataDict;
        }*/

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ResistDataDict.Count);
            foreach (var resistance in ResistDataDict)
            {
                pw.WriteInt((int)resistance.ResistanceType);
                pw.WriteInt(resistance.ResistanceAmmount);
            }
        }
    }

    public class ResistDataDict
    {
        public DamageType ResistanceType { get; set; }
        public int ResistanceAmmount { get; set; }
    }
}
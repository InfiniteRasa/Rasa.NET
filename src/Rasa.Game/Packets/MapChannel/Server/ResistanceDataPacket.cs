using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ResistanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResistanceData;

        public List<ResistanceData> ResistanceData { get; set; }

        public ResistanceDataPacket(List<ResistanceData> resistanceData)
        {
            ResistanceData = resistanceData;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ResistanceData.Count);
            foreach (var resistance in ResistanceData)
            {
                pw.WriteInt((int)resistance.ResistanceType);
                pw.WriteInt(resistance.ResistanceAmmount);
            }
        }
    }
}
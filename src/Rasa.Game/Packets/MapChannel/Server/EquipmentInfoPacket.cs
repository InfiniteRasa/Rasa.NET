using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        //public Dictionary<int, int> equipmentInfo { get; set; }
        public uint WeaponDrawer { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(1);
            pw.WriteTuple(2);
            pw.WriteInt(13);
            pw.WriteInt((int)WeaponDrawer);
        }
    }
}

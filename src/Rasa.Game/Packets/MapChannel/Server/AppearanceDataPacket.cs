namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class AppearanceDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public int AbilityId { get; set; }
        public int PumpLevel { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(21);
            for (var i = 0; i < 21; i++)
            {

                pw.WriteInt(i); // slotId
                pw.WriteTuple(1);
                pw.WriteInt(0); // class id
            }
        }
    }
}

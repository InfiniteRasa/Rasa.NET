namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AttributeInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttributeInfo;

        // ToDO add all stats
        public int BodyCurent { get; set; }
        public int BodyCurrentMax { get; set; }
        public int BbodyNormalMax { get; set; }


        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(10);   // let's he how much
            // Body
            pw.WriteInt(1);
            pw.WriteTuple(5);         
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Mind
            pw.WriteInt(2);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Spirit
            pw.WriteInt(3);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Health
            pw.WriteInt(4);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Chi - Chi is adrenaline?
            pw.WriteInt(5);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Power (test)
            pw.WriteInt(6);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Aware (test)
            pw.WriteInt(7);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Armor
            pw.WriteInt(8);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Speed (test)
            pw.WriteInt(9);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            // Regen
            pw.WriteInt(10);
            pw.WriteTuple(5);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
        }
    }
}

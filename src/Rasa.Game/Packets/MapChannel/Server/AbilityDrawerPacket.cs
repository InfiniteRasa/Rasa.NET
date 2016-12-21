namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AbilityDrawerPacket :PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbilityDrawer;

        public int AbilityId { get; set; }
        public int PumpLevel { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(25);
            for (var i = 0; i < 25; i++)
            {
                
                pw.WriteInt(i);             // slotId
                pw.WriteTuple(3);
                pw.WriteInt(AbilityId);     // abilityId
                pw.WriteInt(PumpLevel);    // abilityLevel
                pw.WriteNoneStruct();       // itemId ( unknown purpose ) <<= c++  krssrb =>> if you drag 'n' drop usable iteme from inventory
            }
        }
    }
}

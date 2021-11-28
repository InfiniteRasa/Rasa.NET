namespace Rasa.Packets.Party.Both
{
    using Data;
    using Memory;

    public class ChangePartyLootMethodPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangePartyLootMethod;
        
        internal PartyLootMethod PartyLootMethod { get; set; }
        
        public ChangePartyLootMethodPacket()
        {
        }

        public ChangePartyLootMethodPacket(PartyLootMethod method)
        {
            PartyLootMethod = method;
        }
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            PartyLootMethod = (PartyLootMethod)pr.ReadUInt();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)PartyLootMethod);
        }
    }
}

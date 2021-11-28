namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class SetCurrentPartyIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetCurrentPartyId;

        internal uint PartyId { get; set; }
        internal bool WasKicked { get; set; }

        internal SetCurrentPartyIdPacket(uint partyId, bool wasKicked = false)
        {
            PartyId = partyId;
            WasKicked = wasKicked;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            if (PartyId != 0)
                pw.WriteUInt(PartyId);
            else
                pw.WriteNoneStruct();
            pw.WriteBool(WasKicked);
        }
    }
}

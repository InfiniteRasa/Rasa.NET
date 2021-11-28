namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class KickUserFromPartyByIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.KickUserFromPartyById;

        internal ulong MemberId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            MemberId = pr.ReadULong();
        }
    }
}

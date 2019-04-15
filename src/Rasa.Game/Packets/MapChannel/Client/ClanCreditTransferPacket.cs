namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ClanCreditTransferPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanCreditTransfer;

        public uint CreditsType { get; set; }
        public long Ammount { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CreditsType = pr.ReadUInt();

            if (pr.PeekType() == PythonType.Long)
                Ammount = pr.ReadLong();
            else
                Ammount = pr.ReadInt();
        }
    }
}

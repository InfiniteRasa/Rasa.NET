namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestMoveItemToClanLockboxPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.RequestMoveItemToClanLockbox;

        public uint CharacterId { get; set; }
        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.ReadUInt();
            ClanId = pr.ReadUInt();
        }
    }
}

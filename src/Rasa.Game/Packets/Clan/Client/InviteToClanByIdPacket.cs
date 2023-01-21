namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class InviteToClanByIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.InviteToClanById;

        public ulong CharacterEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterEntityId = pr.ReadULong();                
        }
    }
}

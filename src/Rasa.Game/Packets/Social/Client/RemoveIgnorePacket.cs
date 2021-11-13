namespace Rasa.Packets.Social.Client
{
    using Data;
    using Memory;

    public class RemoveIgnorePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.RemoveIgnore;

        public uint AccountId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            AccountId = pr.ReadUInt();
        }
    }
}

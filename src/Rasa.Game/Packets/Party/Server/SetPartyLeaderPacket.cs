namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class SetPartyLeaderPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetPartyLeader;

        internal uint UserId { get; set; }

        internal SetPartyLeaderPacket(uint userId)
        {
            UserId = userId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(UserId);
        }
    }
}

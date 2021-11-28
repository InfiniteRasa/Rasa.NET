namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class SquadRequestSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SquadRequestSuccess;

        internal string InviteeName { get; set; }
        
        internal SquadRequestSuccessPacket(string inviteeName)
        {
            InviteeName = inviteeName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(InviteeName);
        }
    }
}

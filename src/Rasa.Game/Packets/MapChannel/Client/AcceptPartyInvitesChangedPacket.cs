using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    public class AcceptPartyInvitesChangedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AcceptPartyInvitesChanged;
        
        public override void Read(PythonReader pr)
        {
            Console.WriteLine("AcceptPartyInvitesChanged \n{0}", pr.ToString());
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}

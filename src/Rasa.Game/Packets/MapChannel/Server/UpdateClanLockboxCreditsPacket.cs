using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateClanLockboxCreditsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateClanLockboxCredits;

        public List<uint> ListCredits = new List<uint>();

        public UpdateClanLockboxCreditsPacket(uint credits, uint prestige)
        {
            ListCredits.Add(credits);
            ListCredits.Add(prestige);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(2);
            uint creditsType = 1;
            foreach (var creditsAmount in ListCredits)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(creditsType);
                pw.WriteUInt(creditsAmount);
                creditsType++;
            }
        }
    }
}

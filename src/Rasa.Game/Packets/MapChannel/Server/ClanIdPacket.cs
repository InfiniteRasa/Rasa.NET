namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ClanIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanId;

        public uint ClanId { get; }

        public ClanIdPacket(uint clanId)
        {
            ClanId = clanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ClanId);
        }
    }
}

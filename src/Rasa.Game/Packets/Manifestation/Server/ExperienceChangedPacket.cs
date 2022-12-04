namespace Rasa.Packets.Manifestation.Server
{
    using Data;
    using Memory;
    using Rasa.Structures;

    public class ExperienceChangedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ExperienceChanged;

        public XPInfo XPInfo { get; set; }

        public ExperienceChangedPacket(XPInfo xpInfo)
        {
            XPInfo = xpInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteStruct(XPInfo);
        }
    }
}

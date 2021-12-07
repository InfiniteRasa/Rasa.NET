namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PreTeleportPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreTeleport;

        public TeleportType TeleportType { get; set; }

        public PreTeleportPacket(TeleportType teleportType)
        {
            TeleportType = teleportType;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)TeleportType);
        }
    }
}

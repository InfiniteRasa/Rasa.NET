namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Emitter.Recv_EmitterInfo(on, *state): the emitter's state and whether it is playing. For
    /// an FXPackageEmitter the state is one value, the FX package's string table id
    /// (FXPackageEmitter.SetEmitterState). Sent in the entity's creation data, and again when
    /// the package changes - the client stops the old package before it starts the new one.
    /// </summary>
    public class EmitterInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EmitterInfo;

        public bool On { get; }
        public uint PackageId { get; }

        public EmitterInfoPacket(bool on, uint packageId)
        {
            On = on;
            PackageId = packageId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteBool(On);
            pw.WriteUInt(PackageId);
        }
    }
}

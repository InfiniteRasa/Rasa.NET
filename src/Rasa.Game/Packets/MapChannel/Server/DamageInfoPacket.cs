namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DamageInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DamageInfo;

        public bool CanBeDamaged { get; }
        public bool CanBeRepaired { get; }
        public uint TotalHitPoints { get; }
        public uint CurrentHitPoints { get; }

        public DamageInfoPacket(bool canBeDamaged, bool canBeRepaired, uint totalHitPoints, uint currentHitPoints)
        {
            CanBeDamaged = canBeDamaged;
            CanBeRepaired = canBeRepaired;
            TotalHitPoints = totalHitPoints;
            CurrentHitPoints = currentHitPoints;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteBool(CanBeDamaged);
            pw.WriteBool(CanBeRepaired);
            pw.WriteUInt(TotalHitPoints);
            pw.WriteUInt(CurrentHitPoints);
        }
    }
}

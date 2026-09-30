namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Usable.Recv_DamageInfo(canBeDamaged, canBeRepaired, totHitPoints, curHitPoints): the hit
    /// points of a usable that has them (a DestroyableStatelessSwitch such as the Hortimonculus
    /// plant), which its overhead bar shows.
    /// </summary>
    public class UsableDamageInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DamageInfo;

        public bool CanBeDamaged { get; }
        public bool CanBeRepaired { get; }
        public int TotalHitPoints { get; }
        public int CurrentHitPoints { get; }

        public UsableDamageInfoPacket(bool canBeDamaged, bool canBeRepaired, int totalHitPoints, int currentHitPoints)
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
            pw.WriteInt(TotalHitPoints);
            pw.WriteInt(CurrentHitPoints);
        }
    }
}

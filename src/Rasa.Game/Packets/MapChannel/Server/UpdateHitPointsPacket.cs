namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>Usable.Recv_UpdateHitPoints(curHitPoints): a usable's hit points have changed.</summary>
    public class UpdateHitPointsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateHitPoints;

        public int CurrentHitPoints { get; }

        public UpdateHitPointsPacket(int currentHitPoints)
        {
            CurrentHitPoints = currentHitPoints;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(CurrentHitPoints);
        }
    }
}

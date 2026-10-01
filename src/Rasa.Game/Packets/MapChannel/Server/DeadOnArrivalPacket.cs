namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Actor.Recv_DeadOnArrival(canRevive): the actor a client has just been given is dead -
    /// shown lying there, without the death effects that a death blow plays.
    /// </summary>
    public class DeadOnArrivalPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DeadOnArrival;

        public bool CanRevive { get; set; }

        public DeadOnArrivalPacket(bool canRevive = false)
        {
            CanRevive = canRevive;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(CanRevive ? 1 : 0);
        }
    }
}

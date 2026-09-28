namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/clientmethod.py Recv_StartTrackingScriptableClientEvent(eventId): from now on the client
    /// sends ScriptableClientEvent((eventId,)) each time the player does that
    /// (ScriptableClientEvent). The mission log, weapon drawer and ability drawer send it at once if
    /// the player has already done it. The client keeps the list until character select.
    /// </summary>
    public class StartTrackingScriptableClientEventPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StartTrackingScriptableClientEvent;

        public ScriptableClientEvent EventId { get; set; }

        public StartTrackingScriptableClientEventPacket(ScriptableClientEvent eventId)
        {
            EventId = eventId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)EventId);
        }
    }
}

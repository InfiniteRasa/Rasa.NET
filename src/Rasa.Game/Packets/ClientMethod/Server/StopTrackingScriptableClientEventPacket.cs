namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/clientmethod.py Recv_StopTrackingScriptableClientEvent(eventId): the client stops sending
    /// ScriptableClientEvent for that event.
    /// </summary>
    public class StopTrackingScriptableClientEventPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StopTrackingScriptableClientEvent;

        public ScriptableClientEvent EventId { get; set; }

        public StopTrackingScriptableClientEventPacket(ScriptableClientEvent eventId)
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

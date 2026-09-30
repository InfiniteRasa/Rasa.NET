namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A tracked player action happened: <c>(eventId,)</c>, from client/clientmethod.py
    /// OnScriptableClientEvent, only for events the server started tracking. Sent again every time.
    /// </summary>
    public class ScriptableClientEventPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ScriptableClientEvent;

        public uint EventId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            if (pr.PeekType() == PythonType.Int)
                EventId = pr.ReadUInt();
            else
                pr.SkipValue();
        }
    }
}

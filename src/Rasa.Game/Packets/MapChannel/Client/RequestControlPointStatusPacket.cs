namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// RequestControlPointStatus (817): client/controlpointmanager.py RequestControlPointStatus(),
    /// "Query for the challenge board data", with no arguments. Sent only by the challenge board
    /// window's Show, which the 1.16.5.0 client never reaches (ControlPointStatusPacket).
    /// Answered with ControlPointStatus (Battlegrounds.RequestControlPointStatus).
    /// </summary>
    public class RequestControlPointStatusPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestControlPointStatus;

        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>RequestRevive(reviverId): the revive window's answer to an offer (revivewindow.py).</summary>
    public class RequestRevivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestRevive;

        public ulong ReviverId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            // The id it was sent, which the client hands back as a Python long; an int if small.
            ReviverId = pr.PeekType() == PythonType.Int ? pr.ReadUInt() : pr.ReadULong();
        }
    }
}

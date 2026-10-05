namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>RefuseRevive(reviverId): the revive window's answer to an offer (revivewindow.py).</summary>
    public class RefuseRevivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RefuseRevive;

        public ulong ReviverId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            // The id it was sent, which the client hands back as a Python long; an int if small.
            ReviverId = pr.PeekType() == PythonType.Int ? pr.ReadUInt() : pr.ReadULong();
        }
    }
}

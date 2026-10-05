namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// ReviveMe(graveyardId): Actor.OnRequestRevive, from the hospital window with the hospital
    /// picked (or the nearest, when its timer runs out) and from the death window's "Go To
    /// Hospital" with none.
    /// </summary>
    public class ReviveMePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReviveMe;

        public int? GraveyardId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GraveyardId = pr.ReadNullableInt();
        }
    }
}

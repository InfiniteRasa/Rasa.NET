namespace Rasa.Packets.Wargame.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// WargameChallengeResponse (606), client/wargame.py SendWargameChallengeResponse:
    /// <c>(accepted, pmMsg)</c> - the duel dialog's Accept (1, None) or Decline (0,
    /// PM_WARGAME_REFUSED). It names no wargame: a player holds one challenge to answer at a time.
    /// </summary>
    public class WargameChallengeResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeResponse;

        public bool Accepted { get; set; }
        public int? Message { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();
            Accepted = pr.ReadBool();

            if (count > 1)
                Message = pr.PeekType() == PythonType.Int || pr.PeekType() == PythonType.Structs ? pr.ReadNullableInt() : SkipOther(pr);
        }

        private static int? SkipOther(PythonReader pr)
        {
            pr.SkipValue();
            return null;
        }
    }
}

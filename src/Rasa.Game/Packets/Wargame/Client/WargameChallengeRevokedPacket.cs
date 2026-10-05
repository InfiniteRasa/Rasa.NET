namespace Rasa.Packets.Wargame.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// WargameChallengeRevoked (638), client/wargame.py OnRevokeWargameDuelChallenge: <c>()</c> -
    /// the challenger's Revoke. It names no wargame: a player holds one challenge of their own at a
    /// time.
    /// </summary>
    public class WargameChallengeRevokedPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeRevoked;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}

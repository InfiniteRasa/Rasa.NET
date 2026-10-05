namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>SurrenderWargame (674), communicator.SurrenderWargame: <c>()</c> - /surrender, giving up a duel.</summary>
    public class SurrenderWargamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SurrenderWargame;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}

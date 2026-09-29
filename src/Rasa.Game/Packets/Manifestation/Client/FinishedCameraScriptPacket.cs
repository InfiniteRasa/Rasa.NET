namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A camera script over: <c>(scriptId,)</c>, from inputstate/cameracut.py's OnExitState, whether
    /// the script ran out or the player cut it short.
    /// </summary>
    public class FinishedCameraScriptPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FinishedCameraScript;

        public uint ScriptId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            if (pr.PeekType() == PythonType.Int)
                ScriptId = pr.ReadUInt();
            else
                pr.SkipValue();
        }
    }
}

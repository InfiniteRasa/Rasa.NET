namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/clientmethod.py Recv_RunCameraScript(scriptId): plays one of the map's camera
    /// scripts. Only from the game input state: the client goes into inputstate/cameracut, hides
    /// the HUD, takes the controls away and runs the script until it ends or the player presses
    /// space or escape, and then sends FinishedCameraScript((scriptId,)).
    /// </summary>
    public class RunCameraScriptPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RunCameraScript;

        public uint ScriptId { get; set; }

        public RunCameraScriptPacket(uint scriptId)
        {
            ScriptId = scriptId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ScriptId);
        }
    }
}

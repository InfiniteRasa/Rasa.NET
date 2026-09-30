using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Actor.Recv_StateCorrection(stateList): every state in the list transitioned to, whatever
    /// the actor thinks it is in. Recv_StateChange, the everyday one, passes over movement states
    /// and IDLE, and NORMAL on a dead actor; this does not, which is what makes it the way to put
    /// a client right - a posture it took for itself and the server refused, or an actor brought
    /// back from DEAD.
    /// </summary>
    public class StateCorrectionPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StateCorrection;

        public List<CharacterState> StateIds { get; set; }

        public StateCorrectionPacket(List<CharacterState> stateIds)
        {
            StateIds = stateIds;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(StateIds.Count);

            foreach (var stateId in StateIds)
                pw.WriteUInt((uint)stateId);
        }
    }
}

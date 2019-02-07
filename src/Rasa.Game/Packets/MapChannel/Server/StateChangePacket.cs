using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class StateChangePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StateChange;

        public List<ActorState> StateIds { get; set; }

        public StateChangePacket(List<ActorState> stateIds)
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

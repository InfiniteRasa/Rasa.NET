using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Use (client/augmentations/usable.py Recv_Use(actorId, curStateId, windupTimeMs, *args)): the
    /// object goes from the state it is in to the one given, with the transition's effect.
    ///
    /// Only the clan control point (clancontrolpoint.py) reads an argument after the three - the
    /// clan that owns it from now, "the clan that affected the usable state change is the owner"
    /// - and it must have it: its OnBeforeUse(actorId, owningClanId) takes exactly the one. The
    /// state is then made over, a transition from clan controlled to itself among them, with
    /// the effect of that owner (ControlPoints.SetHolder).
    /// </summary>
    public class UsePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Use;

        public ulong PlayerEntityId { get; set; }
        public UseObjectState CurState { get; set; }
        public int WindupTimeMs { get; set; }
        public List<int> Args = new List<int>();

        /// <summary>The clan that owns a clan control point from now; null for any other object, which is sent no such argument.</summary>
        public int? OwningClanId { get; set; }

        public UsePacket(ulong playerEntityId, UseObjectState curState, int windupTimeMs, int? owningClanId = null)
        {
            PlayerEntityId = playerEntityId;
            CurState = curState;
            WindupTimeMs = windupTimeMs;
            OwningClanId = owningClanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(OwningClanId == null ? 3 : 4);
            pw.WriteULong(PlayerEntityId);
            pw.WriteUInt((uint)CurState);
            pw.WriteInt(WindupTimeMs);

            if (OwningClanId != null)
                pw.WriteInt(OwningClanId.Value);
        }
    }
}

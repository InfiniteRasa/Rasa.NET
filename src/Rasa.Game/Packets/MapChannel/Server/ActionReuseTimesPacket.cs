using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_ActionReuseTimes(reuseTimeList) on the player's own manifestation: "called during
    /// player init to setup the persisted reuse times of actions", reuseTimeList being
    /// [(actionId, reuseTimeRemaining in milliseconds)]. The client calls SetActionReuseTime for
    /// each, which is what its reticle, ability drawer and inventory draw their cooldown sweeps
    /// from. Its timers live on the actor, and the actor is made afresh on every map
    /// (gamemap.py RemoveAllPhysicalEntities), so this goes out on every arrival.
    /// </summary>
    public class ActionReuseTimesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionReuseTimes;

        public List<(ActionId ActionId, int RemainingMs)> ReuseTimes { get; }

        public ActionReuseTimesPacket(List<(ActionId ActionId, int RemainingMs)> reuseTimes)
        {
            ReuseTimes = reuseTimes;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(ReuseTimes.Count);

            foreach (var (actionId, remainingMs) in ReuseTimes)
            {
                pw.WriteTuple(2);
                pw.WriteUInt((uint)actionId);
                pw.WriteInt(remainingMs);
            }
        }
    }
}

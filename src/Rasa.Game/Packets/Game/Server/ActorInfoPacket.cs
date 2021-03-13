using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ActorInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorInfo;
        
        public IReadOnlyCollection<uint> StateIds { get; }

        public double Rotation { get; }

        public ulong TrackingTargetEntityId { get; }

        public double MovementModifier { get; }

        public uint DesiredPostureId { get; }

        public bool IsHoldingCombatMode { get; }

        public ActorInfoPacket(Actor actor)
        {
            StateIds = new List<uint>();
            Rotation = actor.Rotation;
            TrackingTargetEntityId = 0;
            MovementModifier = 1;
            DesiredPostureId = (uint)actor.GetPosture();
            IsHoldingCombatMode = false;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);

            pw.WriteList(StateIds.Count);
            foreach (var stateId in StateIds)
            {
                pw.WriteUInt(stateId);
            }

            pw.WriteDouble(Rotation);
            pw.WriteULong(TrackingTargetEntityId);
            pw.WriteDouble(MovementModifier);
            pw.WriteUInt(DesiredPostureId);
            pw.WriteBool(IsHoldingCombatMode);
        }
    }
}
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

        public double Yaw { get; }

        public ulong TrackingTargetEntityId { get; }

        public double MovementModifier { get; }

        public CharacterState DesiredPostureId { get; }

        public bool IsHoldingCombatMode { get; }

        public ActorInfoPacket(Actor actor)
        {
            StateIds = new List<uint>();
            Yaw = actor.Rotation;
            TrackingTargetEntityId = 0;
            MovementModifier = 1;
            DesiredPostureId = actor.IsCrouching ? CharacterState.Crouched : CharacterState.Standing;
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

            pw.WriteDouble(Yaw);
            pw.WriteULong(TrackingTargetEntityId);
            pw.WriteDouble(MovementModifier);
            pw.WriteUInt((uint)DesiredPostureId);
            pw.WriteBool(IsHoldingCombatMode);
        }
    }
}
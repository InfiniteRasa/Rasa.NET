using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorInfo;

        public List<CharacterState> StateIds = new List<CharacterState>();
        public ulong TrackingTarget { get; set; }
        public float Yaw { get; set; }
        public double MovementMode { get; set; }
        public CharacterStateType DesiredPostureId { get; set; }
        public bool CombatMode { get; set; }


        public Dictionary<CharacterState, CharacterStateType> Tester = new Dictionary<CharacterState, CharacterStateType> {
            { CharacterState.Standing, CharacterStateType.Posture },
            { CharacterState.Sitting, CharacterStateType.Posture },
            { CharacterState.LyingDown,CharacterStateType.Posture},
            { CharacterState.Swimming, CharacterStateType.Posture},
            { CharacterState.Dead, CharacterStateType.Control},
            { CharacterState.Stopped, CharacterStateType.Movement},
            { CharacterState.Slow, CharacterStateType.Movement},
            { CharacterState.Fast, CharacterStateType.Movement},
            { CharacterState.Flying, CharacterStateType.Posture},
            { CharacterState.Flailing, CharacterStateType.Posture},
            { CharacterState.Normal, CharacterStateType.Control},
            { CharacterState.UnControlled, CharacterStateType.Control},
            { CharacterState.Stunned, CharacterStateType.Control},
            { CharacterState.Crouched, CharacterStateType.Posture},
            { CharacterState.AtPeace, CharacterStateType.Combat},
            { CharacterState.CombatEngaged, CharacterStateType.Combat},
            { CharacterState.Idle, CharacterStateType.Action},
            { CharacterState.Recovery, CharacterStateType.Action},
            { CharacterState.Windup, CharacterStateType.Action},
            { CharacterState.NoTool, CharacterStateType.Tool},
            { CharacterState.ToolReady, CharacterStateType.Tool},
            { CharacterState.Special, CharacterStateType.Movement},
            { CharacterState.Dying, CharacterStateType.Control}
        };

        public ActorInfoPacket(CharacterState characterState, ulong trackingTarget, float yaw, double movementMode, bool combatMode)
        {
            StateIds.Add(characterState);
            TrackingTarget = TrackingTarget;
            Yaw = yaw;
            MovementMode = movementMode;
            DesiredPostureId = Tester[characterState];
            CombatMode = combatMode;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteList(StateIds.Count);
            foreach(var state in StateIds)
                pw.WriteInt((int)state);            // stateIds
            pw.WriteDouble(Yaw);                    // yaw
            pw.WriteULong(TrackingTarget);          // trackingTarget
            pw.WriteDouble(MovementMode);           // movementMod
            pw.WriteInt((int)DesiredPostureId);     // desiredPostureId
            pw.WriteBool(CombatMode);               // isHoldingCombatMode
        }
    }
}
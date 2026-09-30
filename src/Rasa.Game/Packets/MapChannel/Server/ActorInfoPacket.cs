using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Managers;
    using Memory;
    using Structures;

    public class ActorInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorInfo;

        public List<CharacterState> StateIds = new List<CharacterState>();
        public ulong TrackingTarget { get; set; }
        public double Yaw { get; set; }
        public double MovementMode { get; set; }
        /// <summary>
        /// The posture the actor wants to be in, as a state id (client Recv_ActorInfo passes it to
        /// GetStateFromId and, when it is Crouched, SetDesiredPosture). It used to be the posture's
        /// state *type* (1..6), which the client read as a state id: an idle actor's 3 as LyingDown,
        /// a dead one's 5 as Dead.
        /// </summary>
        public CharacterState DesiredPostureId { get; set; }
        public bool CombatMode { get; set; }

        public ActorInfoPacket(Actor actor)
        {
            StateIds.Add(actor.State);

            // A creature with its weapon out (CreatureWeaponDraw): drawn for whoever meets it now.
            if (actor is Creature { WeaponDrawn: true })
                StateIds.Add(CharacterState.ToolReady);
            // What its movement is locked on, not what it has selected; nothing if that has gone.
            TrackingTarget = TrackingTargets.Current(actor);
            Yaw = actor.Rotation;
            MovementMode = actor.MovementSpeed;
            DesiredPostureId = actor.IsCrouching ? CharacterState.Crouched : CharacterState.Standing;
            CombatMode = actor.InCombatMode;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteList(StateIds.Count);
            foreach (var state in StateIds)
                pw.WriteInt((int)state);            // stateIds
            pw.WriteDouble(Yaw);                    // yaw
            pw.WriteULong(TrackingTarget);          // trackingTarget
            pw.WriteDouble(MovementMode);           // movementMod
            pw.WriteInt((int)DesiredPostureId);     // desiredPostureId
            pw.WriteBool(CombatMode);               // isHoldingCombatMode
        }
    }
}

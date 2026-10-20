using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Managers;
    using Memory;
    using Structures;

    /// <summary>
    /// Use (client/augmentations/usable.py Recv_Use(actorId, curStateId, windupTimeMs, *args)): the
    /// object goes from the state it is in to the one given, with the transition's effect.
    ///
    /// Only the clan control point (clancontrolpoint.py) reads an argument after the three - the
    /// clan that owns it from now, "the clan that affected the usable state change is the owner"
    /// - and it must have it: its OnBeforeUse(actorId, owningClanId) takes exactly the one. The
    /// state is then made over, a transition from clan controlled to itself among them, with
    /// the effect of that owner (ControlPoints.SetHolder).
    ///
    /// Two classes of usable take no windup: WORMHOLE (wormhole.py, the Temporary Wormhole's
    /// UsableAbilityTemporaryWormhole and the dropship beacon's UsableAbilityTemporaryDropship_Reward)
    /// and SHRINE (shrine.py, UsableShrinePlayer) override Recv_Use as (actorId, curStateId, *args)
    /// and hand the whole of *args on as Usable.Recv_Use's windupTimeMs. A third argument arrives
    /// there as a tuple, (0,), which the object keeps as its windup override: truthy, so
    /// GetUsableWindupTime returns it when that client next uses the object, and the use breaks
    /// on float((0,)) in entitymanager.AddMethodCall - a TypeError, and the player cannot use
    /// it. Sent with two arguments, *args is (), no override, and the use action's own windup
    /// stands. <see cref="For"/> picks the shape by the object's class.
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

        /// <summary>
        /// Whether the windup goes with it: false for the classes whose Recv_Use takes none
        /// (<see cref="TakesWindup"/>), which are sent (actorId, curStateId) alone.
        /// </summary>
        public bool WithWindup { get; set; } = true;

        public UsePacket(ulong playerEntityId, UseObjectState curState, int windupTimeMs, int? owningClanId = null)
        {
            PlayerEntityId = playerEntityId;
            CurState = curState;
            WindupTimeMs = windupTimeMs;
            OwningClanId = owningClanId;
        }

        /// <summary>A Use for this object, in the shape its class's Recv_Use takes.</summary>
        public static UsePacket For(DynamicObject obj, ulong playerEntityId, UseObjectState curState, int windupTimeMs) =>
            new UsePacket(playerEntityId, curState, windupTimeMs)
            {
                WithWindup = TakesWindup(EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(obj.EntityClassId, out var entityClass) ? entityClass : null)
            };

        /// <summary>False for a WORMHOLE or SHRINE class, whose Recv_Use is (actorId, curStateId, *args).</summary>
        public static bool TakesWindup(EntityClass entityClass) =>
            entityClass?.Augmentations == null ||
            !entityClass.Augmentations.Any(augmentation => augmentation == AugmentationType.Wormhole || augmentation == AugmentationType.Shrine);

        public override void Write(PythonWriter pw)
        {
            if (!WithWindup)
            {
                pw.WriteTuple(2);
                pw.WriteULong(PlayerEntityId);
                pw.WriteUInt((uint)CurState);
                return;
            }

            pw.WriteTuple(OwningClanId == null ? 3 : 4);
            pw.WriteULong(PlayerEntityId);
            pw.WriteUInt((uint)CurState);
            pw.WriteInt(WindupTimeMs);

            if (OwningClanId != null)
                pw.WriteInt(OwningClanId.Value);
        }
    }
}

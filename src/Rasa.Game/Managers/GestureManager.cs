using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Protocol;
    using Packets;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Slash-command gestures (/wave, /dance, ...): client/actions/gesture.py, and the weapon hand
    /// signals (/eyes, /quiet, /crouch): client/actions/gestureweapon.py, the same action with the
    /// weapon kept out, under ActionId.GestureWeapon.
    ///
    /// The gesturing client plays its own gesture without waiting for the server - windup,
    /// then after windupDelayMs a local recovery - and only sends RequestGesture so others
    /// can see it. Everyone else needs, on the gesturer's entity:
    ///
    ///  - PerformWindup(2, argId, targetId): Gesture.Windup plays the windup animation and
    ///    posts the "X waves to Y" message for players within 30 m (kGestureEmoteDistSquared);
    ///  - PerformRecovery(2, argId, hits, misses, missdata, hitdata) after windupDelayMs:
    ///    TargetedAction.DoAction takes the four lists, and recovery returns the actor to idle.
    ///    Without it the actor stays in windup;
    ///  - for looping gestures, a GESTURE_EFFECT attached before that recovery. Gesture.
    ///    LocalDoAction announces it, and GestureEffect holds the looping animation until it
    ///    is detached. The gesturer's own client needs this effect too, and asks for it to be
    ///    detached with RequestDetachGameEffect when the player moves, acts or crouches.
    ///
    /// Windup and recovery skip the gesturer: their client has already played both.
    /// </summary>
    public class GestureManager
    {
        private static GestureManager _instance;
        private static readonly object InstanceLock = new object();

        public static GestureManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new GestureManager();
                    }
                }

                return _instance;
            }
        }

        private GestureManager()
        {
        }

        internal void RequestGesture(Client client, RequestGesturePacket packet)
        {
            var actor = client.Player;
            var mapChannel = actor?.MapChannel;

            if (mapChannel == null || actor.State == CharacterState.Dead)
                return;

            if (!Gestures.TryGet(packet.GestureId, out var gesture))
            {
                Logger.WriteLog(LogType.Debug, $"{actor.FamilyName} requested unknown gesture {packet.GestureId}");
                return;
            }

            // A reward emote wants its player flag, as the client checks it (Gestures.TryGetEmoteFlag).
            if (Gestures.TryGetEmoteFlag(packet.GestureId, out var playerFlagId) &&
                !CharacterFlagProjection.ToNativeIds(actor.PlayerFlags).Contains(playerFlagId))
            {
                Logger.WriteLog(LogType.Security,
                    $"{actor.FamilyName} requested gesture {packet.GestureId} without its emote player flag {playerFlagId}");
                return;
            }

            Start(mapChannel, actor, ActionId.Gesture, packet.GestureId, packet.TargetEntityId, gesture);
        }

        /// <summary>
        /// RequestGestureWeapon: /eyes, /quiet, /crouch (the "get down" signal), action 474. The
        /// same windup and recovery as a gesture, under GESTURE_WEAPON: other clients resolve 474
        /// to GestureWeapon too, so they keep the weapon in the signaller's hand and show the
        /// "X signals..." messages themselves. The emote player flag each needs is checked here as
        /// the client checks it (HasPlayerFlag over the flags PlayerFlags sent it), so a client
        /// that skips its own check does not get the signal played for everyone else.
        /// </summary>
        internal void RequestGestureWeapon(Client client, RequestGestureWeaponPacket packet)
        {
            var actor = client.Player;
            var mapChannel = actor?.MapChannel;

            if (mapChannel == null || actor.State == CharacterState.Dead)
                return;

            if (!Gestures.TryGetWeapon(packet.GestureId, out var gesture, out var playerFlagId))
            {
                Logger.WriteLog(LogType.Debug, $"{actor.FamilyName} requested unknown weapon gesture {packet.GestureId}");
                return;
            }

            if (!CharacterFlagProjection.ToNativeIds(actor.PlayerFlags).Contains(playerFlagId))
            {
                Logger.WriteLog(LogType.Security,
                    $"{actor.FamilyName} requested weapon gesture {packet.GestureId} without its emote player flag {playerFlagId}");
                return;
            }

            Start(mapChannel, actor, ActionId.GestureWeapon, packet.GestureId, packet.TargetEntityId, gesture);
        }

        private static void Start(MapChannel mapChannel, Manifestation actor, ActionId actionId, uint argId, ulong targetId, GestureInfo gesture)
        {
            // A new gesture replaces the last one. Nothing is sent for the old one: the new
            // PerformWindup cancels whatever action other clients are showing for this actor.
            EndGesture(mapChannel, actor);

            SendToOthers(mapChannel, actor, new PerformWindupPacket(PerformType.ThreeArgs, actionId, argId, targetId));

            // The recovery goes out from ActorActionManager once the windup has run, and
            // RequestActionInterrupt (the player moving mid-windup) marks this entry interrupted.
            mapChannel.PerformRecovery.Add(new ActionData(actor, actionId, argId, targetId, gesture.WindupMs));
        }

        /// <summary>Called from ActorActionManager.PerformRecovery when the windup has run.</summary>
        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            var actor = action.Actor;

            if (action.IsInrerrupted)
            {
                SendToOthers(mapChannel, actor, new ActionInterruptPacket(actor.EntityId, action.ActionId, action.ActionArgId));
                return;
            }

            GestureInfo gesture;
            if (action.ActionId == ActionId.GestureWeapon
                    ? !Gestures.TryGetWeapon(action.ActionArgId, out gesture, out _)
                    : !Gestures.TryGet(action.ActionArgId, out gesture))
                return;

            if (gesture.Looping)
                AttachGestureEffect(mapChannel, actor, action.ActionArgId, action.TargetId);

            var args = new MissileArgs();

            if (action.TargetId != 0)
                args.HitEntities.Add(action.TargetId);

            SendToOthers(mapChannel, actor, new PerformRecoveryPacket(PerformType.ListOfArgs, action.ActionId, action.ActionArgId, args));
        }

        /// <summary>
        /// RequestDetachGameEffect: the player wants an effect off. Gestures and effects flagged
        /// AllowDetach (sprint, and buffs in general) go; debuffs end on the server's terms.
        /// </summary>
        internal void RequestDetachGameEffect(Client client, RequestDetachGameEffectPacket packet)
        {
            var actor = client.Player;
            var mapChannel = actor?.MapChannel;

            if (mapChannel == null || !actor.ActiveEffects.TryGetValue(packet.EffectId, out var effect))
                return;

            // A gesture, or an effect that says it can be taken off by request - a toggle like
            // sprint, which the client turns off by asking for exactly this, or a buff the player
            // right-clicks away. Debuffs end on the server's terms.
            if (effect.TypeId != Gestures.EffectTypeId && !effect.AllowDetach)
            {
                Logger.WriteLog(LogType.Security, $"{actor.FamilyName} asked to detach effect {packet.EffectId} of type {effect.TypeId}, which is not theirs to remove");
                return;
            }

            GameEffectManager.Instance.DettachEffect(mapChannel, actor, effect);
        }

        /// <summary>Drops a pending gesture recovery and detaches any looping gesture.</summary>
        private static void EndGesture(MapChannel mapChannel, Actor actor)
        {
            mapChannel.PerformRecovery.RemoveAll(a => a.Actor == actor &&
                (a.ActionId == ActionId.Gesture || a.ActionId == ActionId.GestureWeapon));

            var looping = actor.ActiveEffects.Values.Where(e => e.TypeId == Gestures.EffectTypeId).ToList();

            foreach (var effect in looping)
                GameEffectManager.Instance.DettachEffect(mapChannel, actor, effect);
        }

        private static void AttachGestureEffect(MapChannel mapChannel, Actor actor, uint actionArgId, ulong targetId)
        {
            mapChannel.CurrentEffectId++;

            var effect = new GameEffect
            {
                TypeId = Gestures.EffectTypeId,
                EffectId = mapChannel.CurrentEffectId,
                EffectLevel = actionArgId,
                SourceId = actor.EntityId,
                // Held until detached.
                ExpiresTick = long.MaxValue,
                AllowDetach = true
            };

            GameEffectManager.Instance.AddToList(actor, effect);

            // Everyone, the gesturer included: their client holds the pose from this effect too.
            CellManager.Instance.CellCallMethod(mapChannel, actor, new GestureEffectAttachedPacket(effect.EffectId, actionArgId, actor.EntityId, targetId));
        }

        /// <summary>CellManager.CellCallMethod without the actor's own client.</summary>
        private static void SendToOthers(MapChannel mapChannel, Actor actor, PythonPacket packet)
        {
            foreach (var cell in CellManager.CellsIn(mapChannel, actor.Cells))
                foreach (var client in cell.ClientList)
                    if (client.Player != actor)
                        client.CallMethod(actor.EntityId, packet);
        }
    }
}

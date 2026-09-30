namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// An actor's tracking target: the entity its movement is locked on, for the engine
    /// (gameclient_world.SetTrackingTarget). A player's client sets one on its own body whenever
    /// it tracks something - Follow from another player's menu (1.2 m), walking up to a usable
    /// or an NPC to use it (1.5 m), a click to move that lands on a targetable body - and tells
    /// the server with SetTrackingTarget; it clears it with ClearTrackingTarget when that ends.
    ///
    /// The server keeps it on the actor, tells everyone else who can see the actor
    /// (Recv_SetTrackingTarget), and gives it to whoever meets the actor later in ActorInfo. The
    /// player's own client is not told what it already did. Creatures have none of their own;
    /// .track sets one on any actor, to see what the engine does with it.
    /// </summary>
    public static class TrackingTargets
    {
        /// <summary>Whether <paramref name="targetId"/> can be tracked by the actor: none, or another entity the server knows.</summary>
        public static bool IsValid(Actor actor, ulong targetId)
        {
            return targetId == 0
                || (targetId != actor.EntityId && EntityManager.Instance.GetEntityType(targetId) != 0);
        }

        /// <summary>What the actor tracks now: 0 when that entity has gone since.</summary>
        public static ulong Current(Actor actor)
        {
            var id = actor.TrackingTargetEntityId;

            return id != 0 && EntityManager.Instance.GetEntityType(id) != 0 ? id : 0;
        }

        /// <summary>A player's own SetTrackingTarget or ClearTrackingTarget (0).</summary>
        public static void FromClient(Client client, ulong targetId)
        {
            var player = client.Player;

            if (player == null)
                return;

            if (!IsValid(player, targetId))
            {
                Logger.WriteLog(LogType.Debug, $"{player.FamilyName} asked to track {targetId}, which is not an entity here; ignored.");
                return;
            }

            if (player.TrackingTargetEntityId == targetId)
                return;

            player.TrackingTargetEntityId = targetId;

            if (client.State == ClientState.Ingame && player.MapChannel != null)
                client.CellIgnoreSelfCallMethod(client, new TrackingTargetPacket(targetId));
        }

        /// <summary>Sets an actor's tracking target and tells everyone who can see it, the actor's own client included.</summary>
        public static void Set(MapChannel mapChannel, Actor actor, ulong targetId)
        {
            actor.TrackingTargetEntityId = targetId;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new TrackingTargetPacket(targetId));
        }
    }
}

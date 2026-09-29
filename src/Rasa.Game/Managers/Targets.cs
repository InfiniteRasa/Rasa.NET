namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Who an actor has targeted, told to the clients that draw it (Recv_TargetId): a player's
    /// selection, which their combat stance aims the upper body and weapon at, and a creature's
    /// fight, which aims the guns of the turrets, Stalkers, Striders and Juggernauts.
    ///
    /// - A player's own client sets the target on itself as it sends SetTargetId or
    ///   ClearTargetId; everyone else in range is told here.
    /// - A creature's is the target of its fight (Controller.ActionFighting) while it fights,
    ///   none otherwise; BehaviorManager calls Sync when a fight starts, changes, stops or leashes.
    /// - Whoever meets the actor later gets it with the rest of its data.
    /// Only an entity the server knows is ever sent.
    /// </summary>
    public static class Targets
    {
        /// <summary>The actor's target as the clients should see it: 0 for none, or once that entity has gone.</summary>
        public static ulong Current(Actor actor)
        {
            ulong id = actor switch
            {
                Creature creature => creature.Controller != null
                                     && creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
                                     && creature.State != CharacterState.Dead
                                     && creature.State != CharacterState.Dying
                    ? creature.Controller.ActionFighting.TargetEntityId
                    : 0,
                _ => actor.Target
            };

            return id != 0 && id != actor.EntityId && EntityManager.Instance.GetEntityType(id) != 0 ? id : 0;
        }

        /// <summary>A player's selection changed (SetTargetId, or ClearTargetId as 0): everyone else in range is told.</summary>
        public static void PlayerChanged(Client client, ulong oldTargetId)
        {
            var player = client.Player;

            if (player?.MapChannel == null || client.State != ClientState.Ingame)
                return;

            var shown = Current(player);

            if (shown == (oldTargetId != 0 && EntityManager.Instance.GetEntityType(oldTargetId) != 0 ? oldTargetId : 0))
                return;

            client.CellIgnoreSelfCallMethod(client, new TargetIdPacket(shown));
        }

        /// <summary>A creature's fight began, changed or ended: tells everyone in range if what they were shown is no longer it.</summary>
        public static void Sync(MapChannel mapChannel, Creature creature)
        {
            if (creature == null)
                return;

            var target = Current(creature);

            if (target == creature.ShownTargetId)
                return;

            creature.ShownTargetId = target;

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new TargetIdPacket(target));
        }
    }
}

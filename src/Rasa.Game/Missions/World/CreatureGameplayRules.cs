using System;

namespace Rasa.Game.Missions.World
{
    using global::Rasa.Missions.Definitions;
    using global::Rasa.Missions.Scenes;
    using Managers;
    using Structures;

    internal sealed record ActorGameplayBinding(MapChannel Map, SpawnPool SpawnPool, uint TemplateId,
        ActorHandle Handle, ActorGameplayPolicy Policy, Func<bool> OwnsActor);

    internal static class CreatureGameplayRules
    {
        internal static void BindRole(Creature creature, MapChannel map, ActorHandle handle,
            ActorGameplayPolicy policy, Func<bool> ownsActor)
        {
            if (policy == null || creature.State == Data.CharacterState.Dead)
            {
                ClearRole(creature, handle);
                return;
            }
            if (policy.ValidationError() is string error)
                throw new GameplayRejectionException($"Actor {handle.Role} in run {handle.RunId}: {error}.");
            if (creature.GameplayBinding is { } current && current.Map == map &&
                current.SpawnPool == creature.SpawnPool && current.TemplateId == creature.DbId && current.Handle == handle &&
                current.Policy.EquivalentTo(policy) && current.OwnsActor())
                return;
            ClearRole(creature);
            creature.CombatParticipant = null;
            creature.GameplayBinding = new ActorGameplayBinding(map, creature.SpawnPool, creature.DbId, handle,
                policy.Snapshot(), ownsActor);
        }

        internal static void ClearRole(Creature creature, ActorHandle handle = null)
        {
            if (creature?.GameplayBinding is { } binding && (handle == null || binding.Handle == handle))
            {
                creature.GameplayBinding = null;
                creature.CombatParticipant = null;
            }
        }

        private static bool TryRolePolicy(Creature creature, out ActorGameplayPolicy policy)
        {
            policy = null;
            if (creature?.GameplayBinding is not { } binding)
                return false;
            if (creature.State != Data.CharacterState.Dead && binding.Map.MissionEpoch == binding.Handle.MapEpoch &&
                creature.DbId == binding.TemplateId &&
                ReferenceEquals(creature.RuntimeMapChannel, binding.Map) &&
                ReferenceEquals(binding.SpawnPool?.RuntimeMapChannel, binding.Map) &&
                ReferenceEquals(creature.SpawnPool, binding.SpawnPool) &&
                MapInstanceScope.TryGetCreature(binding.Map, creature.EntityId, out var current) &&
                ReferenceEquals(current, creature) && binding.OwnsActor())
            {
                policy = binding.Policy;
                return true;
            }
            ClearRole(creature, binding.Handle);
            return false;
        }

        internal static ActorGameplayPolicy Policy(Creature creature)
        {
            if (TryRolePolicy(creature, out var policy))
                return policy;
            var map = creature?.RuntimeMapChannel ?? creature?.SpawnPool?.RuntimeMapChannel;
            return map?.IsPrivateInstance == true && MapInstanceScope.Contains(map, creature)
                ? MissionApplication.Instance.ActorPolicies.Get(map.MapInfo.MapContextId, creature.DbId)
                : ActorGameplayPolicy.Ordinary;
        }

        internal static bool RewardsKills(Creature creature) => creature != null &&
            (TryRolePolicy(creature, out var policy) ? policy.RewardScenarioKills :
                creature.SpawnPool?.ScenarioKey == null || Policy(creature).RewardScenarioKills);

        internal static bool IsDefender(Creature creature) => Policy(creature).Defends;
        internal static bool IsInvulnerable(Creature creature) => Policy(creature).Invulnerable;
        internal static bool TracksParticipation(Creature creature) => Policy(creature).TrackParticipation;
    }
}

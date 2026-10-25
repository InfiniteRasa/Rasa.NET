using System.Linq;
using Rasa.Data;
using Rasa.Game;
using Rasa.Missions;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>Assignment-owned NPCs and the public NPCs they replace, phased per character.</summary>
    internal static class MissionNpcVisibility
    {
        private static (uint Mission, bool Protected) Replacement(Creature creature, MissionApplication missions = null) =>
            creature?.SpawnPool is { SceneRunId: null } pool
                ? (missions ?? MissionApplication.Instance)?.Scenes.NpcReplacement(pool.DbId) ?? default : default;

        internal static bool IsScoped(Creature creature) =>
            creature?.SpawnPool?.SceneOwnerOnly == true || Replacement(creature).Mission != 0;

        internal static bool IsProtectedPublicActor(Creature creature) => Replacement(creature).Protected;

        internal static bool IsShownTo(Client client, Creature creature, MissionApplication missions = null)
        {
            if (creature?.SpawnPool?.SceneOwnerOnly == true &&
                creature.SpawnPool.ScenarioOwnerCharacterId != client?.Player?.Id)
                return false;
            var mission = Replacement(creature, missions).Mission;
            if (mission == 0 || client?.Player == null)
                return true;
            var player = client.Player;
            if (player.MissionSuccessHistory.Contains(mission))
                return false;
            return !player.Missions.TryGetValue(mission, out var active) ||
                active.State != MissionState.Active && active.State != MissionState.Completed;
        }

        internal static void Refresh(Client client, MissionApplication missions = null)
        {
            var map = client?.Player?.MapChannel;
            if (map == null) return;
            foreach (var id in client.VisibleMissionNpcs.ToArray())
            {
                var creature = EntityManager.Instance.GetCreature(id);
                if (creature != null && IsShownTo(client, creature, missions)) continue;
                client.VisibleMissionNpcs.Remove(id);
                client.FarContacts.Remove(id);
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(id));
            }
            foreach (var creature in CellManager.CellsIn(map, client.Player.Cells)
                .SelectMany(cell => cell.CreatureList).Distinct().ToArray())
                if (IsScoped(creature) && IsShownTo(client, creature, missions) &&
                    !client.VisibleMissionNpcs.Contains(creature.EntityId))
                    CreatureManager.Instance.CreateCreatureOnClient(client, creature);
        }
    }
}

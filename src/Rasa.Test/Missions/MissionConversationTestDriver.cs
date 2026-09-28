using System.Collections.Generic;
using System.Linq;

namespace Rasa.Test.Missions
{
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    internal static class MissionConversationTestDriver
    {
        // Gameplay fixtures position the character before opening the real handler. Admission
        // tests call the raw methods so this setup cannot hide unopened, stale or distant input.
        internal static bool OpenNpcConversation(this MissionApplication missions, Client client, ulong entityId)
        {
            if (client?.Player == null)
                return false;
            lock (client.SyncRoot)
            {
                if (EntityManager.Instance.Creatures.TryGetValue(entityId, out var npc) &&
                    ReferenceEquals(npc.RuntimeMapChannel, client.Player.MapChannel))
                    client.SetWorldPosition(npc.Position, client.Player.Rotation);
                else if (EntityManager.Instance.TryGetObject(entityId, out var obj) &&
                    ReferenceEquals(obj.RuntimeMapChannel, client.Player.MapChannel))
                    client.SetWorldPosition(obj.Position, client.Player.Rotation);

                var pending = Drain(client);
                try
                {
                    new NpcManager(null, missions).RequestNpcConverse(client,
                        new RequestNPCConversePacket { EntityId = entityId });
                    var opened = Drain(client);
                    var conversation = opened.Any(packet =>
                        packet.Message is CallMethodMessage { Packet: ConversePacket });
                    pending.AddRange(opened.Where(packet =>
                        packet.Message is not CallMethodMessage { Packet: ConversePacket }));
                    return conversation;
                }
                finally
                {
                    foreach (var packet in pending)
                        client.SendMessage(packet.Message, packet.Compress, packet.Channel);
                }
            }
        }

        internal static bool AcceptOfferedMission(this MissionApplication missions, Client client,
            ulong npcEntityId, uint missionId)
        {
            missions.OpenNpcConversation(client, npcEntityId);
            return missions.TryAcceptNpcMission(client, npcEntityId, missionId);
        }

        internal static bool CompleteOfferedObjective(this MissionApplication missions, Client client,
            ulong npcEntityId, uint missionId, uint objectiveId, uint playerFlagId)
        {
            missions.OpenNpcConversation(client, npcEntityId);
            return missions.TryCompleteNpcObjective(client, npcEntityId, missionId, objectiveId, playerFlagId);
        }

        internal static bool CompleteOfferedMission(this MissionApplication missions, Client client,
            ulong npcEntityId, uint missionId, int? selectionIndex, int? rating = null)
        {
            missions.OpenNpcConversation(client, npcEntityId);
            return missions.TryCompleteNpcMission(client, npcEntityId, missionId, selectionIndex, rating);
        }

        internal static bool RewardOfferedMission(this MissionApplication missions, Client client,
            ulong npcEntityId, uint missionId, int? selectionIndex, int? rating)
        {
            missions.OpenNpcConversation(client, npcEntityId);
            return missions.TryRewardNpcMission(client, npcEntityId, missionId, selectionIndex, rating);
        }

        private static List<ProtocolPacket> Drain(Client client)
        {
            var packets = new List<ProtocolPacket>();
            while (client.DequeueOutgoingPacket() is ProtocolPacket packet)
                packets.Add(packet);
            return packets;
        }
    }
}

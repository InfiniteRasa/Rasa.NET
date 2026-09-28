using System;
using Rasa.Game.Missions.Integration;

namespace Rasa.Game.Missions
{
    internal sealed record MissionSessionIdentity(Guid SessionId, Guid PlayerEpoch, uint CharacterId,
        ulong PlayerEntityId, uint AccountId, Guid MapEpoch)
    {
        internal static MissionSessionIdentity Capture(Client client) =>
            MissionInteractionPolicy.IsActivePlayer(client)
                ? new(client.MissionSessionId, client.Player.MissionLocationEpoch, client.Player.Id,
                    client.Player.EntityId, client.AccountEntry.Id, client.Player.MapChannel.MissionEpoch)
                : null;

        internal bool IsCurrent(Client client) =>
            MissionInteractionPolicy.IsActivePlayer(client) && client.MissionSessionId == SessionId &&
            client.Player.MissionLocationEpoch == PlayerEpoch && client.Player.Id == CharacterId &&
            client.Player.EntityId == PlayerEntityId && client.AccountEntry.Id == AccountId &&
            client.Player.MapChannel.MissionEpoch == MapEpoch;
    }
}

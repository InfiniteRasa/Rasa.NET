using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Game.Missions.Protocol
{
    internal sealed class CharacterFlagProjection
    {
        private Manifestation _owner;
        private uint _characterId;
        private bool _dirty;

        internal static uint[] ToNativeIds(IReadOnlyDictionary<uint, uint> flags) =>
            flags.Where(flag => CharacterFlagIds.IsMissionFlag(flag.Key) && flag.Value != 0)
                .Select(flag => flag.Key)
                .OrderBy(id => id)
                .ToArray();

        internal void ApplyCommitted(Client client, IReadOnlyDictionary<uint, uint> flags)
        {
            lock (client.SyncRoot)
            {
                var player = TrackOwner(client);
                var previous = ToNativeIds(player.PlayerFlags);
                player.PlayerFlags = new Dictionary<uint, uint>(flags);
                _dirty |= !previous.SequenceEqual(ToNativeIds(player.PlayerFlags));
            }
        }

        internal bool PublishPending(Client client, Action<PlayerFlagsPacket> publish)
        {
            lock (client.SyncRoot)
            {
                var player = TrackOwner(client);
                if (!_dirty || client.State != ClientState.Ingame || client.PendingTransfer != null ||
                    player.Disconected || player.RemoveFromMap)
                    return false;

                var packet = new PlayerFlagsPacket(ToNativeIds(player.PlayerFlags));
                publish(packet);
                Acknowledge(client, player, packet);
                return true;
            }
        }

        internal void Acknowledge(Client client, Manifestation player, PlayerFlagsPacket packet)
        {
            lock (client.SyncRoot)
                if (ReferenceEquals(TrackOwner(client), player) &&
                    packet.PlayerFlagIds.SequenceEqual(ToNativeIds(player.PlayerFlags)))
                    _dirty = false;
        }

        private Manifestation TrackOwner(Client client)
        {
            var player = client.Player;
            // A connection can select another character or replace the same character's manifestation.
            if (!ReferenceEquals(_owner, player) || _characterId != player.Id)
            {
                _owner = player;
                _characterId = player.Id;
                _dirty = false;
            }
            return player;
        }
    }
}

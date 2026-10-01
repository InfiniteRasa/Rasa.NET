using System.Collections.Generic;

namespace Rasa.Managers
{
    using Game;
    using Packets.Wargame.Server;
    using Structures;

    /// <summary>
    /// What every kind of wargame shares: the sides a player is on, as the client's WargameData
    /// carries them (<c>{wargameId: side}</c>, Actor.Recv_WargameData), and the showing of them.
    /// Clan feuds (ClanFeuds) and duels (Duels) each give their own entries; their ids do not
    /// overlap. Pvp reads the sides from here to tell enemies apart.
    /// </summary>
    public static class Wargames
    {
        /// <summary>The player's WargameData: every feud their clan is in and their duel.</summary>
        public static Dictionary<uint, bool> DataOf(Manifestation player)
        {
            var data = ClanFeuds.Instance.WargameDataOf(player);

            foreach (var duel in Duels.Instance.WargameDataOf(player))
                data[duel.Key] = duel.Value;

            return data;
        }

        /// <summary>
        /// The player's WargameData to everyone who can see them, themselves included, and who is
        /// now an enemy: HOSTILE across a wargame, FRIENDLY again after it (Pvp).
        /// </summary>
        public static void Show(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return;

            var packet = new WargameDataPacket(DataOf(player));

            if (player.MapChannel == null)
                client.CallMethod(player.EntityId, packet);
            else
                CellManager.Instance.CellCallMethod(player.MapChannel, player, packet);

            Pvp.RefreshCategories(client);
        }
    }
}

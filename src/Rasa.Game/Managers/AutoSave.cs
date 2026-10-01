using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// Saves every player in the world every few minutes (GameConfig.AutoSaveMinutes), so that a
    /// crash, a power cut or a killed process costs at most that much.
    ///
    /// A character used to be written only when it left the world (MapChannelManager.RemovePlayer)
    /// or changed maps: whatever a player had done since they logged in - where they walked to,
    /// the time played, and now the health, death penalties and cooldowns kept across a relog
    /// (RelogVitals, ActionReuse) - was lost with the process.
    ///
    /// Each save is what leaving the world saves, but for the leaving: position and map, run and
    /// crouch state, time played, health, armour, power, Rez Trauma, the no-healing, and the
    /// cooldowns. A player's first save is staggered across the first interval by entity id, so
    /// a server full of players who all logged in after a restart does not save them all in one
    /// tick, and no map saves more than <see cref="MaxPerMapPerPass"/> in one pass.
    ///
    /// Only a player standing in the world is saved: not on a loading screen or mid-transfer,
    /// not leaving, not dead (a dead player's last save stays; logging in again puts them back
    /// where they were alive).
    /// </summary>
    public static class AutoSave
    {
        public const int DefaultMinutes = 5;

        /// <summary>How often each player is saved; 0 or less turns autosaving off. Set from GameConfig.</summary>
        public static int IntervalMinutes { get; set; } = DefaultMinutes;

        /// <summary>The map worker's pass runs this often (MapChannelManager's "AutoSave" timer).</summary>
        public const long PassIntervalMs = 5000;

        /// <summary>The most saves one map makes in a pass; the rest are a pass later.</summary>
        public const int MaxPerMapPerPass = 10;

        /// <summary>What a save does; replaced in tests.</summary>
        public static Action<Client> SaveAction { get; set; } = Save;

        /// <summary>One pass over a map: saves the players whose time has come. Returns how many.</summary>
        public static int Worker(MapChannel mapChannel, long nowTick)
        {
            if (mapChannel?.ClientList == null || IntervalMinutes <= 0)
                return 0;

            var interval = IntervalMinutes * 60_000L;
            var saved = 0;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                if (!Eligible(client))
                    continue;

                var player = client.Player;

                // First seen: due somewhere in the coming interval, not all at once.
                if (player.NextAutoSaveTick == 0)
                {
                    player.NextAutoSaveTick = nowTick + 1 + (long)(player.EntityId % (ulong)interval);
                    continue;
                }

                if (nowTick < player.NextAutoSaveTick)
                    continue;

                if (saved >= MaxPerMapPerPass)
                    break;

                player.NextAutoSaveTick = nowTick + interval;
                saved++;

                try
                {
                    SaveAction(client);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Autosave of {player.FamilyName} failed: {e.Message}");
                }
            }

            return saved;
        }

        public static bool Eligible(Client client)
        {
            var player = client?.Player;

            return player != null && player.Id != 0
                && client.State == ClientState.Ingame && client.PendingTransfer == null
                && !player.Disconected && !player.RemoveFromMap
                && player.State != CharacterState.Dead && player.State != CharacterState.Dying;
        }

        /// <summary>Everything leaving the world would save, for a player who stays in it.</summary>
        public static void Save(Client client)
        {
            try
            {
                // Position, rotation, map, run and crouch state.
                client.SaveCharacter();

                // Time played: worked out from the login time, so saving it again later is right.
                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Login, null);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Autosave of {client.Player?.FamilyName}'s position failed: {e.Message}");
            }

            // Each catches and logs its own failure.
            RelogVitals.Save(client);
            ActionReuse.Save(client);
        }
    }
}

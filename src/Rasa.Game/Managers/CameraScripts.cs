using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Structures;

    /// <summary>
    /// Map camera scripts (CameraScriptTable): RunCameraScript plays one on a client, which hands
    /// the camera and the controls over to it until it ends or is cut short, and answers with
    /// FinishedCameraScript. In between the player can do nothing, so nothing starts on them:
    /// creatures do not notice them and let them go if they were fighting them (BehaviorManager,
    /// Threat), as they do a cloaked player.
    ///
    /// The client only runs a script from the game input state and says nothing when it does not,
    /// so the server does not wait on the answer forever: the player counts as watching for the
    /// script's length and <see cref="SlackMs"/> more, at most <see cref="MaxWatchMs"/>, or until
    /// FinishedCameraScript comes back. Ours, both.
    ///
    /// Nothing in the data says what set a script off - that was the live server's mission and
    /// instance scripting - so for now only a GM does, with .camerascript.
    /// </summary>
    public static class CameraScripts
    {
        public const long SlackMs = 10_000;
        public const long MaxWatchMs = 30 * 60 * 1000;

        /// <summary>How long a player counts as watching a script of this length.</summary>
        public static long WatchMs(long lengthMs) => Math.Min(Math.Max(0, lengthMs), MaxWatchMs) + SlackMs;

        /// <summary>Plays the script on the client; whether it was sent. The script must be one of the player's map's.</summary>
        public static bool Run(Client client, uint scriptId)
        {
            var player = client?.Player;
            var mapName = player?.MapChannel?.MapInfo?.MapName;

            if (mapName == null)
                return false;

            var script = CameraScriptTable.Find(mapName, scriptId);

            if (script == null)
                return false;

            player.CameraScriptId = scriptId;
            player.CameraScriptUntil = Environment.TickCount64 + WatchMs(script.LengthMs);

            client.CallMethod(SysEntity.ClientMethodId, new RunCameraScriptPacket(scriptId));

            return true;
        }

        /// <summary>FinishedCameraScript: the player has the controls back.</summary>
        public static void Finished(Client client, uint scriptId)
        {
            var player = client?.Player;

            if (player == null)
                return;

            if (player.CameraScriptId != 0 && player.CameraScriptId != scriptId)
                Logger.WriteLog(LogType.Debug, $"FinishedCameraScript: {player.Name} finished script {scriptId} while {player.CameraScriptId} was running");

            player.CameraScriptId = 0;
            player.CameraScriptUntil = 0;
        }

        /// <summary>Whether the player is watching a camera script now.</summary>
        public static bool IsWatching(Manifestation player) =>
            player != null && player.CameraScriptId != 0 && Environment.TickCount64 < player.CameraScriptUntil;
    }
}

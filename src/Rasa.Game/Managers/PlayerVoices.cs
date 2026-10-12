using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Structures;

    /// <summary>
    /// The player's own pain and critical-hit voice lines.
    ///
    /// The client has a voice set for each, per sex, in generated.client.audiosetdata:
    /// VOICE_AVATAR_MALE_PN (24) and VOICE_AVATAR_FEMALE_PN (466), four "gethit" clips each, and
    /// VOICE_AVATAR_MALE_RC (142) and VOICE_AVATAR_FEMALE_RC (137), the two other "gethit" clips
    /// each. Nothing in the client's Python plays them - Manifestation.PlayVoiceOverForPlayerMsg
    /// voices only the failure messages - so it is the server's to say when. The one message
    /// that plays an audio set of the server's choosing is PlayTutorialAudio
    /// (tutorialwindow._PlayAudio → backgroundaudiomgr.PlayAudioSet), and it plays it to that
    /// player alone and at no position, as the client plays its own failure lines. Nobody else
    /// hears it.
    ///
    /// Ours, where nothing says:
    ///  - a hit that lands (on armour or health) and leaves the player standing is pain if it
    ///    took at least <see cref="PainPercent"/> of their maximum health, no more often than
    ///    every <see cref="PainGapMs"/>. A tick of an effect on them - a burn, a poison - and
    ///    lava are not hits, and neither is a fall; a creature's cloud or a propellant pool,
    ///    whose ticks are each a hit of the cloud's or pool's, is;
    ///  - a critical hit on them is its own line, said whatever the pain gap and no more often
    ///    than every <see cref="CritGapMs"/>, and it starts the pain gap over;
    ///  - what lands between two map ticks is said once, at the end of the tick, the critical
    ///    hit over the pain: the line of a hit and the line of its crit would cut each other
    ///    off, since the client plays one at a time;
    ///  - nothing is said while a mission's or a tutorial's voice-over is playing, which a line
    ///    would cut short (<see cref="VoiceOverHoldMs"/> after one is sent), nor once the hit
    ///    has brought them down.
    /// </summary>
    public static class PlayerVoices
    {
        public const uint MalePain = 24;          // VOICE_AVATAR_MALE_PN
        public const uint FemalePain = 466;       // VOICE_AVATAR_FEMALE_PN
        public const uint MaleCritical = 142;     // VOICE_AVATAR_MALE_RC
        public const uint FemaleCritical = 137;   // VOICE_AVATAR_FEMALE_RC

        /// <summary>The least a hit must take, in hundredths of the player's maximum health, to be cried at. Ours.</summary>
        public const int PainPercent = 5;

        /// <summary>The least time between two pain lines. Ours.</summary>
        public const int PainGapMs = 5000;

        /// <summary>The least time between two critical-hit lines. Ours.</summary>
        public const int CritGapMs = 2000;

        /// <summary>
        /// How long after a mission's or tutorial's voice-over is sent nothing is said over it.
        /// The server does not know how long a clip is; this is ours.
        /// </summary>
        public const int VoiceOverHoldMs = 20000;

        /// <summary>The clock, in milliseconds. Tests set it.</summary>
        internal static Func<long> Now = () => Environment.TickCount64;

        public static uint PainSet(Manifestation player) => player.Gender == 0 ? MalePain : FemalePain;

        public static uint CriticalSet(Manifestation player) => player.Gender == 0 ? MaleCritical : FemaleCritical;

        /// <summary>
        /// A hit has landed on the player: what it took (armour and health), and whether it was
        /// critical. Said at the end of the map's tick (<see cref="Worker"/>).
        /// </summary>
        public static void Hurt(Manifestation player, int taken, bool critical = false)
        {
            if (player == null || taken <= 0)
                return;

            lock (player.Voice)
            {
                player.Voice.Taken += taken;
                player.Voice.Critical |= critical;
            }
        }

        /// <summary>A critical hit has landed on the player (CritEffects.OnCritical).</summary>
        public static void Critical(Manifestation player)
        {
            if (player == null)
                return;

            lock (player.Voice)
                player.Voice.Critical = true;
        }

        /// <summary>A mission's or tutorial's voice-over has been sent to the player: nothing of theirs is said over it.</summary>
        public static void VoiceOverSent(Manifestation player, bool stopped)
        {
            if (player == null)
                return;

            lock (player.Voice)
                player.Voice.VoiceOverUntil = stopped ? 0 : Now() + VoiceOverHoldMs;
        }

        /// <summary>The end of the map's tick: each player hurt since the last says the line for it, if they may.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null)
                    continue;

                var line = LineFor(player);

                if (line != 0)
                    client.CallMethod(SysEntity.ClientMethodId, new PlayTutorialAudioPacket(line));
            }
        }

        /// <summary>The voice set the player is to hear now, 0 for none; what was pending is taken either way.</summary>
        internal static uint LineFor(Manifestation player)
        {
            var voice = player.Voice;
            int taken;
            bool critical;
            var now = Now();

            lock (voice)
            {
                taken = voice.Taken;
                critical = voice.Critical;
                voice.Taken = 0;
                voice.Critical = false;

                if (taken <= 0 && !critical)
                    return 0;

                if (player.State == CharacterState.Dead || player.State == CharacterState.Dying
                    || !player.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0
                    || now < voice.VoiceOverUntil)
                    return 0;

                if (critical && now >= voice.NextCriticalAt)
                {
                    voice.NextCriticalAt = now + CritGapMs;
                    voice.NextPainAt = now + PainGapMs;

                    return CriticalSet(player);
                }

                if (taken > 0 && now >= voice.NextPainAt && taken * 100L >= (long)PainPercent * Math.Max(1, health.CurrentMax))
                {
                    voice.NextPainAt = now + PainGapMs;

                    return PainSet(player);
                }
            }

            return 0;
        }
    }

    /// <summary>What PlayerVoices keeps about one player.</summary>
    public sealed class PlayerVoiceState
    {
        /// <summary>What hits have taken since the last map tick.</summary>
        internal int Taken;

        /// <summary>A critical hit has landed since the last map tick.</summary>
        internal bool Critical;

        internal long NextPainAt;
        internal long NextCriticalAt;
        internal long VoiceOverUntil;
    }
}

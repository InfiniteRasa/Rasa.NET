using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// Timing for one gesture action argument (ActionId.Gesture, argId).
    /// </summary>
    public readonly struct GestureInfo
    {
        /// <summary>windupDelayMs: how long the windup animation runs before recovery.</summary>
        public int WindupMs { get; }

        /// <summary>
        /// recoveryDelayMs &lt; 0. The client holds the pose in a GESTURE_EFFECT game effect
        /// (client/actions/gesture.py GestureEffect) until the player moves, crouches or
        /// starts another action, instead of returning to idle.
        /// </summary>
        public bool Looping { get; }

        public GestureInfo(int windupMs, bool looping)
        {
            WindupMs = windupMs;
            Looping = looping;
        }
    }

    /// <summary>
    /// Every (2, argId) row of generated/client/actiondata.py actionArguments, the table the
    /// client times its own gestures from. Recovery delay is 0 or -1 for every row, so only
    /// the windup and whether it loops are kept. Comments name the slash commands mapped to
    /// the row in generated/client/slashcommand.py; rows without one are played from items
    /// or mission scripts.
    /// </summary>
    public static class Gestures
    {
        /// <summary>generated/client/gameeffectdata.py GESTURE_EFFECT.</summary>
        public const int EffectTypeId = 129;

        private static readonly Dictionary<uint, GestureInfo> Table = new()
        {
            [1] = new GestureInfo(2666, false),  // /bow
            [2] = new GestureInfo(5994, false),  // /clap
            [4] = new GestureInfo(3663, false),  // /wave
            [6] = new GestureInfo(2500, false),  // /shakefist
            [7] = new GestureInfo(2266, false),  // /hi
            [8] = new GestureInfo(3996, false),  // /cheer
            [9] = new GestureInfo(2830, false),  // /stop
            [10] = new GestureInfo(2566, false),  // /lead
            [11] = new GestureInfo(1933, false),  // /come
            [12] = new GestureInfo(5000, false),  // /charge
            [13] = new GestureInfo(1833, false),  // /back
            [14] = new GestureInfo(6166, false),  // /cry
            [15] = new GestureInfo(4150, false),  // /idiot
            [16] = new GestureInfo(3164, false),  // /kiss
            [17] = new GestureInfo(9000, false),  // /laugh, /lol
            [18] = new GestureInfo(2400, false),  // /point
            [19] = new GestureInfo(4600, false),  // /dismiss
            [20] = new GestureInfo(3830, false),  // /orderpushups
            [21] = new GestureInfo(2000, false),  // /throat
            [22] = new GestureInfo(2997, false),  // /yes
            [23] = new GestureInfo(3164, false),  // /no
            [24] = new GestureInfo(2166, false),  // /shrug
            [25] = new GestureInfo(3000, false),  // /salute
            [26] = new GestureInfo(0, true),  // /dance
            [27] = new GestureInfo(10323, false),  // /beg
            [28] = new GestureInfo(5000, false),  // /golfclap
            [30] = new GestureInfo(6327, false),  // /flirt
            [31] = new GestureInfo(9158, false),  // /pushups
            [33] = new GestureInfo(4733, false),  // /yawn
            [34] = new GestureInfo(6400, false),  // /jumpingjacks
            [35] = new GestureInfo(4666, false),  // /shootme
            [36] = new GestureInfo(11322, false),  // /airguitar9000
            [37] = new GestureInfo(9833, false),  // /moonwalk
            [38] = new GestureInfo(3000, false),  // /taunt
            [39] = new GestureInfo(12000, false),  // /headbow
            [40] = new GestureInfo(9324, false),
            [41] = new GestureInfo(9333, false),  // /propose
            [42] = new GestureInfo(2000, false),  // /raisedfist
            [43] = new GestureInfo(4000, false),  // /hug
            [44] = new GestureInfo(6993, false),  // /toast
            [45] = new GestureInfo(4333, false),  // /trickortreat
            [46] = new GestureInfo(12000, false),  // /momentofsilence
            [47] = new GestureInfo(6000, false),  // /logosphi
            [48] = new GestureInfo(4828, false),  // /logosfist
            [49] = new GestureInfo(0, true),  // /rave
            [50] = new GestureInfo(2833, false),  // /thumbs
            [51] = new GestureInfo(2166, false),  // /jumpforjoy
            [52] = new GestureInfo(8666, false),  // /defeat
            [53] = new GestureInfo(4662, false),  // /stomp
            [55] = new GestureInfo(4662, false),
            [56] = new GestureInfo(3000, false),
            [57] = new GestureInfo(6000, false),  // /logoslove
            [58] = new GestureInfo(6000, false),  // /logosiloveyou
            [59] = new GestureInfo(6000, false),  // /logosihateyou
            [60] = new GestureInfo(3000, false),  // /quarterbow
            [61] = new GestureInfo(9324, false),
            [64] = new GestureInfo(4833, false),  // /usecomputer
            [67] = new GestureInfo(0, true),  // /ballet
            [68] = new GestureInfo(1800, true),  // /poledance
            [72] = new GestureInfo(9324, false),
            [73] = new GestureInfo(9324, false),
            [74] = new GestureInfo(9324, false),
            [75] = new GestureInfo(0, true),  // /robot
            [76] = new GestureInfo(2500, true),  // /submission
            [77] = new GestureInfo(6000, false),  // /logosangry
            [78] = new GestureInfo(6000, false),  // /logosgood
            [79] = new GestureInfo(6000, false),  // /logosevil
            [80] = new GestureInfo(6000, false),  // /logoshappy
            [81] = new GestureInfo(6000, false),  // /logossad
            [82] = new GestureInfo(6000, false),  // /logosplanet
            [83] = new GestureInfo(2166, true),  // /sit
            [84] = new GestureInfo(2500, false),  // /cutthroat
            [85] = new GestureInfo(0, true),  // /ymca
            [86] = new GestureInfo(2000, true),
            [87] = new GestureInfo(3000, true),  // /liedown
            [88] = new GestureInfo(6000, false),  // /logosgreet
            [89] = new GestureInfo(6000, false),  // /logosstop
            [90] = new GestureInfo(2166, true),  // /situps
            [91] = new GestureInfo(1900, true),  // /windmill
            [92] = new GestureInfo(3000, true),  // /warmth
            [93] = new GestureInfo(7700, false),  // /fixit
            [94] = new GestureInfo(9200, false),  // /scan
            [95] = new GestureInfo(2966, true),  // /read
            [96] = new GestureInfo(6000, false),  // /logospcgameruk
            [97] = new GestureInfo(2000, true),  // /taichi
            [98] = new GestureInfo(0, true),  // /breakdance
            [99] = new GestureInfo(14666, false),  // /drunk
        };

        public static bool TryGet(uint argId, out GestureInfo info) => Table.TryGetValue(argId, out info);

        /// <summary>
        /// The (474, argId) rows - GESTURE_WEAPON, client/actions/gestureweapon.py: military hand
        /// signals made with the weapon kept in hand (hideWeapon False), requested with
        /// RequestGestureWeapon. None loops. Each needs its emote player flag (actiondata
        /// playerFlagReqs), which the client checks before it sends.
        /// </summary>
        private static readonly Dictionary<uint, (GestureInfo Info, uint PlayerFlagId)> WeaponTable = new()
        {
            [63] = (new GestureInfo(3000, false), 765),  // /eyes: "You signal: X spotted!"
            [65] = (new GestureInfo(3000, false), 766),  // /quiet: "You signal X to be quiet."
            [66] = (new GestureInfo(2666, false), 764),  // /crouch: "You face X and signal: get down!"
        };

        public static bool TryGetWeapon(uint argId, out GestureInfo info, out uint playerFlagId)
        {
            var found = WeaponTable.TryGetValue(argId, out var row);
            info = row.Info;
            playerFlagId = row.PlayerFlagId;
            return found;
        }
    }
}

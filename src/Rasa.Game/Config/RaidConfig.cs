namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Raids: the raids on the open world's control points (Managers.BaneRaids) - the
    /// Bane's on the points the AFS hold, and the AFS's on the points the Bane hold. Every value has a
    /// default, so a file without the section has them on. Read again when the file changes: a raid
    /// under way goes on by what it was begun with, and the next one by the new values.
    /// </summary>
    public class RaidConfig
    {
        /// <summary>
        /// Whether the Bane raid the points the AFS hold: waves of troops by dropship that come to take
        /// the point. Off, a point changes hands only as before - a player's capture, its garrison all
        /// killed, a game master.
        /// </summary>
        public bool Raids { get; set; } = true;

        /// <summary>How long after one raid on a point the next comes, in minutes: drawn between the two.</summary>
        public int RaidMinutesMin { get; set; } = 30;
        public int RaidMinutesMax { get; set; } = 55;

        /// <summary>How long after the server starts the first raid on a point may come, in minutes: drawn between the two.</summary>
        public int RaidFirstMinutesMin { get; set; } = 6;
        public int RaidFirstMinutesMax { get; set; } = 12;

        /// <summary>
        /// How long after the AFS take a point the Bane come back for it, in minutes: "there will be a
        /// delay before Bane troops are dropshipped in outside the base".
        /// </summary>
        public int RaidAfterCaptureMinutes { get; set; } = 8;

        /// <summary>
        /// How near a player must be to a point for a raid on it to begin, in metres across the ground;
        /// 0: anywhere on its map. A raid under way goes on whoever stays.
        /// </summary>
        public int RaidRange { get; set; } = 300;

        /// <summary>How many waves a Bane raid sends: drawn between the two ("they usually come in 3-4 waves").</summary>
        public int RaidWavesMin { get; set; } = 3;
        public int RaidWavesMax { get; set; } = 4;

        /// <summary>How many troops a ship of a Bane raid brings.</summary>
        public int RaidSquadSize { get; set; } = 4;

        /// <summary>How long the Bane wait between a wave being worn down and the next ships coming, in seconds.</summary>
        public int RaidWaveGapSeconds { get; set; } = 20;

        /// <summary>
        /// How long a raider must stand at the point, unhurt, to take it, in seconds: "they will take it
        /// over within 10 seconds unless they are interrupted by attacking them".
        /// </summary>
        public int RaidClaimSeconds { get; set; } = 10;

        /// <summary>
        /// How many hours the AFS must have held a point for each more troop a Bane raid's ships bring, up
        /// to three more: a point "got increasingly difficult to defend the longer it was held". 0: none.
        /// </summary>
        public int RaidRampHours { get; set; } = 6;

        /// <summary>
        /// Whether only a player who is playing calls a raid to a point: one who can be hurt (not under
        /// <c>.immune</c>) and is not AFK. A game master standing about a point with <c>.immune all</c>
        /// calls nothing. Off: anyone near does.
        /// </summary>
        public bool RaidCallersMustBePlaying { get; set; } = true;

        /// <summary>
        /// How near a raided point its defenders are called to the fight, in metres: whoever of the side
        /// that holds it stands that near it and is not fighting already goes for the nearest raider
        /// within that range of it, whatever their own aggro range. 0: they fight what they see.
        /// </summary>
        public int RaidDefendRange { get; set; } = 40;

        /// <summary>How many of a raided point's defenders, the nearest, turn on a raider claiming it.</summary>
        public int RaidDefendersOnClaimer { get; set; } = 2;

        /// <summary>
        /// Whether the AFS raid the points the Bane hold, as the Bane raid theirs but with less hope - by
        /// the user's wish, "retaken by similar Allied raids but at lower success rates than the Bane":
        /// one ship a wave, a countdown before the first ("AFS Reinforcements En Route"), their soldiers
        /// going for the Bane's healers first, and a claim that takes longer.
        /// </summary>
        public bool AfsRaids { get; set; } = true;

        /// <summary>How long after one AFS raid on a point the next comes, in minutes: drawn between the two.</summary>
        public int AfsRaidMinutesMin { get; set; } = 30;
        public int AfsRaidMinutesMax { get; set; } = 60;

        /// <summary>How long after the server starts the first AFS raid on a point the Bane hold may come, in minutes.</summary>
        public int AfsRaidFirstMinutesMin { get; set; } = 10;
        public int AfsRaidFirstMinutesMax { get; set; } = 20;

        /// <summary>How long after the Bane take a point the AFS first come back for it, in minutes.</summary>
        public int AfsRaidAfterCaptureMinutes { get; set; } = 30;

        /// <summary>How many waves an AFS raid sends: drawn between the two.</summary>
        public int AfsRaidWavesMin { get; set; } = 3;
        public int AfsRaidWavesMax { get; set; } = 3;

        /// <summary>
        /// How long after one AFS wave the next comes, in seconds, whatever is left of it: the AFS land
        /// their waves as one assault. 0: as the Bane's do, each as the last is worn down.
        /// </summary>
        public int AfsRaidWaveEverySeconds { get; set; } = 40;

        /// <summary>How many soldiers a ship of an AFS raid brings.</summary>
        public int AfsRaidSquadSize { get; set; } = 8;

        /// <summary>
        /// How many levels under the middle level of the point's Bane the AFS's soldiers come (negative:
        /// over it): brought to that level as they are set down (CreatureSummons.Rescale), whatever level
        /// their rows are of. The Bane rows of a point are tougher than the AFS's soldier rows of its
        /// level: at the same level the AFS lost every raid on a point with Caretakers in its garrison.
        /// </summary>
        public int AfsRaidLevelsBelow { get; set; } = -3;

        /// <summary>How long a soldier must stand at the point, unhurt, to take it back, in seconds.</summary>
        public int AfsRaidClaimSeconds { get; set; } = 15;

        /// <summary>
        /// How far round the point's object no Bane may stand for a soldier to begin a claim, in metres;
        /// 0: none (a soldier claims as a Bane raider does). With a garrison that raises its dead
        /// (Caretakers, Technicians) the ground was never clear and no claim was ever begun.
        /// </summary>
        public int AfsRaidClearRange { get; set; } = 0;

        /// <summary>
        /// How long before their first ships the AFS's coming is told, in seconds: the client's "AFS
        /// Reinforcements En Route" messages run from five minutes (PlayerMessage 1369) to ten seconds
        /// (1376), and each of them that falls within this is shown as its time comes.
        /// </summary>
        public int AfsRaidWarningSeconds { get; set; } = 120;

        /// <summary>
        /// Whether, while players are at a point the Bane hold and no raid is on, AFS ships bring a few of
        /// the point's own soldiers every three minutes to fight beside them: "some AFS Dropships swooping
        /// in to deposit NPC troops ... to assist with the assault". Needs Reinforcements.AfsDropships.
        /// </summary>
        public bool AfsSupport { get; set; } = true;
    }
}

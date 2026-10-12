using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>The kinds of battle cry, as the client's battlecrypackages numbers them.</summary>
    public enum BattlecryType
    {
        Aggro = 1,
        KilledTarget = 2,
        ReceivedDamage = 3,
        ReceivedCriticalDamage = 4,
        Help = 5,
        CloseToDeath = 6,
        EnterCombat = 7,
        ExitCombat = 8,
        StartPatrol = 9,
        StopPatrol = 10,
        ResumePatrol = 11
    }

    /// <summary>
    /// Battle cries: what a creature shouts as it fights, and an NPC as it walks its beat.
    ///
    /// The client plays one when it is told to. Creature.Recv_BattlecryNotification(packageId,
    /// typeId, seed) (client/augmentations/creature.py) looks (packageId, typeId) up in
    /// generated/client/battlecrypackages for an audio set and plays it on the creature, the
    /// seed choosing which of the set's sounds, so that everyone hears the same one. It does
    /// nothing with User.UserInterface.EnableBattleCries off, or for a pair the table has not
    /// got. Nothing else in the client starts one: which creature has which package, and when it
    /// cries, were the 2009 server's.
    ///
    /// What the client does say is what a package is and what a cry is for. Its 21 packages
    /// (<see cref="Packages"/>) are six human voices and two Brann, with all eleven cries; the
    /// Thrax soldier, officer and technician, with the eight of a fight; a few creatures with an
    /// alert or two (Lightbender, Linker, Caretaker, Treelurker, Treemite); a drill sergeant
    /// with two patrol barks; and three placeholders. The cries are named by their audio sets -
    /// "Battlecry English Male Rugd Aggro", "... Killed Target", "... Recieved Dmg", "... Rcvd
    /// Crit Dmg", "... Help", "... Close 2 Death", "... Enter Combat", "... Exit Combat",
    /// "... Start Patrol", "... Stop Patrol", "... Resume Patrol" (<see cref="BattlecryType"/>).
    ///
    /// The package is data: creature_battlecry gives one to an entity class or to one creature
    /// row, the row before the class (<see cref="PackageOf"/>). A creature with neither is
    /// silent.
    ///
    /// When each is cried is ours - nothing of it survives - and is all here:
    ///
    ///  - a fight starting: Enter Combat for a creature that has just been hurt - the fight came
    ///    to it - and Aggro for one that has not, having picked it. A package with only one of
    ///    the two cries that one.
    ///  - in a fight: Close to Death the first time its health is at or under
    ///    <see cref="CloseToDeathPercent"/>, Help the first time at or under
    ///    <see cref="HelpPercent"/>, each once a fight; otherwise Received Damage on
    ///    <see cref="HitChancePercent"/> in a hundred of the thinks it is found hurt in.
    ///  - a critical hit on it (CritEffects.OnCritical): Received Critical Damage.
    ///  - its blow kills a player or a creature: Killed Target.
    ///  - a fight over with it alive - its target dead, or leashed: Exit Combat.
    ///  - on a beat (Patrols): Stop Patrol as it comes to stand at a step with a pause, Start
    ///    Patrol as it walks on from one, Resume Patrol as it takes the beat up again after
    ///    something took it off.
    ///
    /// The six English human voices (8, 9, 16 to 19) are no class's in the client, and are
    /// given here instead (<see cref="HumanVoice"/>): a creature of a human body class
    /// (<see cref="HumanClasses"/>) with no package of its own or of its class takes one of
    /// its sex's three at random - an armed one for as long as it lives, and a mission escort
    /// for good, kept for its creature row in creature_battlecry so that it is that voice
    /// whenever and to whomever it comes. An unarmed one that escorts nobody - a vendor, a
    /// mission giver - stays silent.
    ///
    /// A creature cries no more often than every <see cref="MinGapMs"/>; the patrol cries are
    /// outside that, the walk between two stops being their gap. Help and Close to Death wait
    /// for the gap and are said when it is over, if the fight is still on: a fight shorter
    /// than the gap is its opening cry and no more. There is no Help mechanic - nobody comes -
    /// it is the cry alone.
    ///
    /// The fight and the beat are read from the creature each think (<see cref="Observe"/>),
    /// not told by each place that changes them: BehaviorManager starts and ends a fight in a
    /// dozen places. So those cries come a think after what they are about.
    /// </summary>
    public static class Battlecries
    {
        /// <summary>The least time between two cries of one creature, patrol cries aside. Ours.</summary>
        public const int MinGapMs = 20000;

        /// <summary>The chance in a hundred that a creature found hurt in a fight cries Received Damage. Ours.</summary>
        public const int HitChancePercent = 10;

        /// <summary>Health, in hundredths of its maximum, at or under which a creature in a fight cries Help, once a fight. Ours.</summary>
        public const int HelpPercent = 50;

        /// <summary>Health at or under which it cries Close to Death, once a fight. Ours.</summary>
        public const int CloseToDeathPercent = 25;

        /// <summary>The seed sent is under this. The client hands it to the audio request (SetRandomSeed).</summary>
        public const int SeedRange = 32768;

        /// <summary>
        /// The client's battlecrypackages, 1.16.5.0: the cries each package has an audio set
        /// for. A cry a package has not got is not sent - the client would play nothing.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, int[]> Packages = new Dictionary<int, int[]>
        {
            [2] = new[] { 3, 9 },                                   // Voice Avatar Male: a placeholder
            [3] = new[] { 1 },                                      // Creature Treelurker Alert
            [4] = new[] { 9, 10 },                                  // Bark Drill Seargent Start Patrol, Stop Patrol
            [5] = new[] { 1 },                                      // Item Mining Laser Resolve: a placeholder
            [6] = new[] { 1 },                                      // World Bane Meat Door Close: a placeholder
            [7] = new[] { 1 },                                      // Creature Treemite Alert
            [8] = All,                                              // Battlecry English Male Rugd
            [9] = All,                                              // Battlecry English Female Cute
            [10] = new[] { 1, 7 },                                  // Creature Lightbender Alert
            [11] = Fight,                                           // Creature Thrax Officer
            [12] = Fight,                                           // Creature Thrax Soldier
            [13] = Fight,                                           // Creature Thrax Technician
            [14] = new[] { 1, 7 },                                  // Creature Bane Linker Alert
            [15] = new[] { 1, 3, 4, 7 },                            // Creature Caretaker Alert, Get Hit
            [16] = All,                                             // Battlecry English Male Cute
            [17] = All,                                             // Battlecry English Male Int
            [18] = All,                                             // Battlecry English Female Int
            [19] = All,                                             // Battlecry English Female Rugd
            [20] = new[] { 1, 7 },                                  // Creature Bane Linker Alert, as 14
            [21] = All,                                             // Battlecry Brann Female
            [22] = All                                              // Battlecry Brann Male
        };

        private static int[] All => new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        /// <summary>The English male voices: Rugd (8), Cute (16), Int (17).</summary>
        public static readonly int[] MaleVoices = { 8, 16, 17 };

        /// <summary>The English female voices: Cute (9), Int (18), Rugd (19).</summary>
        public static readonly int[] FemaleVoices = { 9, 18, 19 };

        /// <summary>
        /// The human body classes, each with its sex's voices: the base bodies, the NPC, Redshirt
        /// and vendor swapsets and their bosses, the armed Redshirts, the soldiers and the
        /// holograms. Children, the Corman and the Z_TEST classes are left out, and so is
        /// NPC_Human_Military_Surplus, whose name says no sex.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, int[]> HumanClasses = Voiced(
            female: new uint[]
            {
                691,                                                    // HumanBaseFemale
                3848, 10609, 26009, 26011,                              // NPC_Human_Swapset_Female (_Boss), NPC_Human_MiniSwapset_Female (_Boss)
                3981, 30136, 26019, 26018,                              // Redshirt_Human_Swapset_Female (_Boss), Redshirt_Human_MiniSwapset_Female (_Boss)
                20972,                                                  // Vendor_Human_Female
                21897, 21899, 21901, 21903, 21905, 21907, 21909, 21911, // Redshirt_Human_T2/T3_<weapon>_Female
                21913, 21915, 21917, 21919,
                29424, 29432,                                           // Redshirt_Human_Soldier_Light_Female, _Heavy_Female
                25613, 26484                                            // Holographic_Human_Female_Rocket, NPC_Holographic_Human_Female
            },
            male: new uint[]
            {
                692,                                                    // HumanBaseMale
                3846, 10610, 26014, 26013,                              // NPC_Human_Swapset_Male (_Boss), NPC_Human_MiniSwapset_Male (_Boss)
                3982, 30137, 26016, 26017,                              // Redshirt_Human_Swapset_Male (_Boss), Redshirt_Human_MiniSwapset_Male (_Boss)
                20975,                                                  // Vendor_Human_Male
                21898, 21900, 21902, 21904, 21906, 21908, 21910, 21912, // Redshirt_Human_T2/T3_<weapon>_Male
                21914, 21916, 21918, 21920,
                29423, 29433, 29765,                                    // Redshirt_Human_Soldier_Light_Male, _Heavy_Male, _Light_Male_Crouch
                25614, 26485                                            // Holographic_Human_Male_Rifle, NPC_Holographic_Human_Male
            });

        private static Dictionary<uint, int[]> Voiced(uint[] female, uint[] male)
        {
            var voiced = new Dictionary<uint, int[]>();

            foreach (var classId in female)
                voiced.Add(classId, FemaleVoices);

            foreach (var classId in male)
                voiced.Add(classId, MaleVoices);

            return voiced;
        }

        /// <summary>
        /// Keeps an escort's voice for its creature row in creature_battlecry (CreatureManager
        /// sets it to write the row off the map's thread). Tests set it.
        /// </summary>
        internal static Action<uint, int> Keep = (creatureId, packageId) => { };

        private static readonly object KeptLock = new object();
        private static int[] Fight => new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>A roll under its argument, from 0. Tests set it.</summary>
        internal static Func<int, int> Roll = max => Random.Shared.Next(max);

        /// <summary>The clock the gap is kept by, in milliseconds. Tests set it.</summary>
        internal static Func<long> Now = () => Environment.TickCount64;

        private static Dictionary<uint, int> _byClass = new Dictionary<uint, int>();
        private static Dictionary<uint, int> _byCreature = new Dictionary<uint, int>();

        /// <summary>Goes up with every Load: a creature's package from before it is looked up again.</summary>
        private static int _loaded;

        /// <summary>
        /// Takes the assignments (creature_battlecry). A row for a package the client has not
        /// got, or of a scope that is neither a class nor a creature row, is passed over and
        /// logged. Returns how many were taken.
        /// </summary>
        public static int Load(IEnumerable<CreatureBattlecryEntry> rows)
        {
            var byClass = new Dictionary<uint, int>();
            var byCreature = new Dictionary<uint, int>();

            foreach (var row in rows ?? Enumerable.Empty<CreatureBattlecryEntry>())
            {
                if (row.PackageId > int.MaxValue || !Packages.ContainsKey((int)row.PackageId))
                {
                    Logger.WriteLog(LogType.Error, $"creature_battlecry ({row.Scope}, {row.TargetId}) names package {row.PackageId}, which the client has not got.");
                    continue;
                }

                switch (row.Scope)
                {
                    case CreatureBattlecryEntry.ScopeClass:
                        byClass[row.TargetId] = (int)row.PackageId;
                        break;
                    case CreatureBattlecryEntry.ScopeCreature:
                        byCreature[row.TargetId] = (int)row.PackageId;
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"creature_battlecry ({row.Scope}, {row.TargetId}) has a scope that is neither a class ({CreatureBattlecryEntry.ScopeClass}) nor a creature ({CreatureBattlecryEntry.ScopeCreature}).");
                        break;
                }
            }

            lock (KeptLock)
            {
                _byClass = byClass;
                _byCreature = byCreature;
                _loaded++;
            }

            return byClass.Count + byCreature.Count;
        }

        /// <summary>
        /// The package the creature cries with now: what <see cref="PackageOf"/> gives it, or the
        /// human voice it has been given (<see cref="HumanVoice"/>); 0 for none. What .battlecry
        /// and .npcinfo say.
        /// </summary>
        public static int VoiceOf(Creature creature) => StateOf(creature)?.PackageId ?? 0;

        /// <summary>
        /// A human body's voice, when nothing in creature_battlecry gives it one: its creature
        /// row's kept escort voice if it has one; else, for a mission escort or an armed one, one
        /// of its sex's voices, rolled once for this creature - and an escort's is kept for its
        /// row (<see cref="Keep"/>), the first one kept standing for every other. 0 for any other
        /// creature, and for an unarmed human escorting nobody.
        /// </summary>
        private static int HumanVoice(Creature creature, BattlecryState state)
        {
            if (!HumanClasses.TryGetValue((uint)creature.EntityClass, out var voices))
                return 0;

            if (creature.DbId != 0 && _byCreature.TryGetValue(creature.DbId, out var kept))
                return kept;

            var escort = BehaviorManager.IsMissionEscort(creature);

            if (!escort && creature.Actions.Count == 0)
                return 0;

            if (state.Voice == 0)
                state.Voice = voices[Math.Clamp(Roll(voices.Length), 0, voices.Length - 1)];

            return escort && creature.DbId != 0 ? KeepVoice(creature.DbId, state.Voice) : state.Voice;
        }

        /// <summary>An escort's voice kept for its creature row; the one already kept, if another copy of it got there first.</summary>
        private static int KeepVoice(uint creatureId, int packageId)
        {
            lock (KeptLock)
            {
                if (_byCreature.TryGetValue(creatureId, out var kept))
                    return kept;

                // A new map rather than a change to the one the map threads are reading.
                _byCreature = new Dictionary<uint, int>(_byCreature) { [creatureId] = packageId };
            }

            Keep(creatureId, packageId);

            return packageId;
        }

        /// <summary>The creature's package: its own row's, or else its class's; 0 for none.</summary>
        public static int PackageOf(Creature creature)
        {
            if (creature == null)
                return 0;

            if (creature.DbId != 0 && _byCreature.TryGetValue(creature.DbId, out var own))
                return own;

            return _byClass.TryGetValue((uint)creature.EntityClass, out var ofClass) ? ofClass : 0;
        }

        /// <summary>Whether the package has an audio set for the cry.</summary>
        public static bool Has(int packageId, BattlecryType type) =>
            Packages.TryGetValue(packageId, out var types) && Array.IndexOf(types, (int)type) >= 0;

        /// <summary>
        /// A living creature's think: what has changed about it since the last one, and the cry
        /// for it. The first think of a creature only takes note of it.
        /// </summary>
        public static void Observe(MapChannel mapChannel, Creature creature)
        {
            var state = StateOf(creature);

            if (state == null)
                return;

            var controller = creature.Controller;
            var fighting = controller.CurrentAction == BehaviorManager.BehaviorActionFighting;
            var patrolling = controller.CurrentAction == BehaviorManager.BehaviorActionPatrol && creature.Patrol != null && creature.Patrol.Count > 0;
            var standing = patrolling && StandsAtStep(creature);

            var health = 0;
            var healthMax = 0;

            if (creature.Attributes.TryGetValue(Attributes.Health, out var bar))
            {
                health = bar.Current;
                healthMax = bar.CurrentMax;
            }

            if (state.Seen)
            {
                var hurt = health < state.Health;

                if (fighting && !state.Fighting)
                {
                    state.SaidHelp = false;
                    state.SaidCloseToDeath = false;
                    state.OffBeat |= state.Patrolled;

                    var first = hurt ? BattlecryType.EnterCombat : BattlecryType.Aggro;
                    var other = hurt ? BattlecryType.Aggro : BattlecryType.EnterCombat;

                    Cry(mapChannel, creature, state, Has(state.PackageId, first) ? first : other);
                }
                else if (!fighting && state.Fighting)
                {
                    Cry(mapChannel, creature, state, BattlecryType.ExitCombat);
                }
                else if (fighting)
                {
                    var percent = healthMax > 0 ? health * 100L / healthMax : 100;

                    if (percent <= CloseToDeathPercent && !state.SaidCloseToDeath)
                        state.SaidCloseToDeath = Said(mapChannel, creature, state, BattlecryType.CloseToDeath);
                    else if (percent <= HelpPercent && !state.SaidHelp)
                        state.SaidHelp = Said(mapChannel, creature, state, BattlecryType.Help);
                    else if (hurt && Has(state.PackageId, BattlecryType.ReceivedDamage) && Roll(100) < HitChancePercent)
                        Cry(mapChannel, creature, state, BattlecryType.ReceivedDamage);
                }

                if (patrolling)
                {
                    if (!state.Patrolling && state.OffBeat)
                    {
                        state.OffBeat = false;
                        Cry(mapChannel, creature, state, BattlecryType.ResumePatrol);
                    }
                    else if (standing && !state.Standing)
                    {
                        Cry(mapChannel, creature, state, BattlecryType.StopPatrol);
                    }
                    else if (!standing && state.Standing && state.Patrolling)
                    {
                        Cry(mapChannel, creature, state, BattlecryType.StartPatrol);
                    }
                }
                else if (state.Patrolling)
                {
                    // Off it for whatever reason - a fight, a leash, a shove: taking it up again is a resuming.
                    state.OffBeat = true;
                }
            }

            state.Seen = true;
            state.Fighting = fighting;
            state.Patrolling = patrolling;
            state.Patrolled |= patrolling;
            state.Standing = standing;
            state.Health = health;
        }

        /// <summary>A critical hit has landed on the creature (CritEffects.OnCritical).</summary>
        public static void Crit(MapChannel mapChannel, Creature creature)
        {
            var state = StateOf(creature);

            if (state != null)
                Cry(mapChannel, creature, state, BattlecryType.ReceivedCriticalDamage);
        }

        /// <summary>The creature's blow has killed someone: a player (PlayerDeath) or a creature (CreatureManager.HandleCreatureKill).</summary>
        public static void KilledTarget(MapChannel mapChannel, Creature creature)
        {
            if (creature == null || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            var state = StateOf(creature);

            if (state != null)
                Cry(mapChannel, creature, state, BattlecryType.KilledTarget);
        }

        /// <summary>
        /// Sends the cry whatever the creature's own package and whenever it last cried: a GM
        /// hearing what a package sounds like on a creature (.battlecry). False for a pair the
        /// client has no audio set for.
        /// </summary>
        public static bool Play(MapChannel mapChannel, Creature creature, int packageId, BattlecryType type)
        {
            if (mapChannel == null || creature == null || !Has(packageId, type))
                return false;

            Send(mapChannel, creature, packageId, type);

            return true;
        }

        /// <summary>The cry, or true as well for one its package has not got: it is not to be tried again.</summary>
        private static bool Said(MapChannel mapChannel, Creature creature, BattlecryState state, BattlecryType type) =>
            !Has(state.PackageId, type) || Cry(mapChannel, creature, state, type);

        private static bool Cry(MapChannel mapChannel, Creature creature, BattlecryState state, BattlecryType type)
        {
            if (mapChannel == null || !Has(state.PackageId, type))
                return false;

            var patrol = type is BattlecryType.StartPatrol or BattlecryType.StopPatrol or BattlecryType.ResumePatrol;
            var now = Now();

            if (!patrol)
            {
                if (state.Cried && now - state.CriedAt < MinGapMs)
                    return false;

                state.Cried = true;
                state.CriedAt = now;
            }

            Send(mapChannel, creature, state.PackageId, type);

            return true;
        }

        private static void Send(MapChannel mapChannel, Creature creature, int packageId, BattlecryType type) =>
            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new BattlecryNotificationPacket(packageId, (int)type, Roll(SeedRange)));

        /// <summary>On a step of its beat, turned to it, and standing out a pause: a halt, not a corner.</summary>
        private static bool StandsAtStep(Creature creature)
        {
            var patrol = creature.Controller.ActionPatrol;

            return patrol.Arrived && patrol.Faced && patrol.Step >= 0 && patrol.Step < creature.Patrol.Count
                && creature.Patrol[patrol.Step].PauseMs > 0;
        }

        /// <summary>What is kept about the creature, made on its first think; null for one with no package.</summary>
        private static BattlecryState StateOf(Creature creature)
        {
            if (creature == null)
                return null;

            var state = creature.Cries ??= new BattlecryState { Loaded = -1 };

            // Loaded again since: what it has seen of the creature stands, its package is asked for anew.
            if (state.Loaded != _loaded)
            {
                state.Assigned = PackageOf(creature);
                state.Loaded = _loaded;
            }

            // A human body with nothing given it is looked at each think: it may have been made
            // an escort, or its row been given an escort's voice, since the last.
            state.PackageId = state.Assigned != 0 ? state.Assigned : HumanVoice(creature, state);

            return state.PackageId == 0 ? null : state;
        }
    }

    /// <summary>What Battlecries keeps about one creature between thinks.</summary>
    public sealed class BattlecryState
    {
        /// <summary>Its package now; 0 for a creature that has none, which is then looked at no further.</summary>
        internal int PackageId;

        /// <summary>The package creature_battlecry gives it (Battlecries.PackageOf), as of Loaded; 0 for none.</summary>
        internal int Assigned;

        /// <summary>The human voice rolled for it (Battlecries.HumanVoice); 0 until one is.</summary>
        internal int Voice;

        /// <summary>The Battlecries.Load it was made under.</summary>
        internal int Loaded;

        /// <summary>It has been through a think: the fields below say how it was then.</summary>
        internal bool Seen;

        internal bool Fighting;
        internal bool Patrolling;
        internal bool Standing;
        internal int Health;

        /// <summary>It has been on its beat at some time.</summary>
        internal bool Patrolled;

        /// <summary>It has been taken off its beat since it was last on it.</summary>
        internal bool OffBeat;

        internal bool SaidHelp;
        internal bool SaidCloseToDeath;

        internal bool Cried;
        internal long CriedAt;
    }
}

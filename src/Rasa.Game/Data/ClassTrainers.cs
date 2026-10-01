using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// What a trainer creature trains, and where its dialog starts.
    ///
    /// <see cref="Trains"/> is the class this one advances a character into, or
    /// <see cref="CharacterClass.None"/> for a generic trainer that takes any class its visitor
    /// is due.
    ///
    /// <see cref="DialogGroup"/> is the first of four consecutive npctrainerdialoglanguage ids.
    /// The table is 320 lines in 80 groups of four, and every group is ordered the same way:
    /// the offer, the wrong path, already trained, not high enough yet. So the line to send is
    /// the group plus <see cref="TrainerDialog"/>.
    /// </summary>
    public readonly struct TrainerInfo
    {
        public CharacterClass Trains { get; }
        public int DialogGroup { get; }

        public TrainerInfo(CharacterClass trains, int dialogGroup)
        {
            Trains = trains;
            DialogGroup = dialogGroup;
        }
    }

    /// <summary>Which of a dialog group's four lines fits the character in front of the trainer.</summary>
    public enum TrainerDialog
    {
        Offer = 0,
        WrongPath = 1,
        AlreadyTrained = 2,
        TooLow = 3
    }

    /// <summary>
    /// The creatures that train a character into a new class.
    ///
    /// Nothing in the world data marks one. The entity class they are built on is
    /// Vendor_Human_Male, a generic vendor body shared with the shop NPCs; their name ids are one
    /// per trainer and one of them is also used by a non-trainer; and the Vendor augmentation
    /// they carry from that class says shop, not trainer. The client's augmentation list has no
    /// Trainer entry at all. So the ids are named here, the way the clan master is recognised by
    /// its name id in CreatureManager, and this belongs in a column on the creature table when it
    /// next takes a migration.
    ///
    /// The per-class entries come from the client saying so twice over. uimapmarkertext holds 37
    /// trainer texts of which 27 were never placed, and 19 of those name one class at one place.
    /// npctrainerdialoglanguage's 80 groups name a class and a place 24 times. The two share no
    /// ids and agree: six classes at Alia Das and Twin Pillars, twelve at Fort Defiance and
    /// Torden Mires. Add_class_trainers seeds them at 501001.
    /// </summary>
    public static class ClassTrainers
    {
        private static readonly Dictionary<uint, TrainerInfo> Trainers = new Dictionary<uint, TrainerInfo>
        {
            // Alia Das, map 1220
            { 501001, new TrainerInfo(CharacterClass.Soldier, 5) },
            { 501002, new TrainerInfo(CharacterClass.Specialist, 13) },
            { 501003, new TrainerInfo(CharacterClass.Commando, 105) },
            { 501004, new TrainerInfo(CharacterClass.Ranger, 113) },
            { 501005, new TrainerInfo(CharacterClass.Sapper, 121) },
            { 501006, new TrainerInfo(CharacterClass.Biotechnician, 97) },
            { 510006, new TrainerInfo(CharacterClass.None, 337) },

            // Twin Pillars, map 1220
            { 501007, new TrainerInfo(CharacterClass.Soldier, 9) },
            { 501008, new TrainerInfo(CharacterClass.Specialist, 17) },
            { 501009, new TrainerInfo(CharacterClass.Commando, 109) },
            { 501010, new TrainerInfo(CharacterClass.Ranger, 117) },
            { 501011, new TrainerInfo(CharacterClass.Sapper, 125) },
            { 501012, new TrainerInfo(CharacterClass.Biotechnician, 101) },

            // Fort Defiance, map 1497
            { 501013, new TrainerInfo(CharacterClass.Commando, 213) },
            { 501014, new TrainerInfo(CharacterClass.Ranger, 241) },
            { 501015, new TrainerInfo(CharacterClass.Sapper, 245) },
            { 501016, new TrainerInfo(CharacterClass.Biotechnician, 209) },
            { 501017, new TrainerInfo(CharacterClass.Grenadier, 229) },
            { 501018, new TrainerInfo(CharacterClass.Guardian, 233) },
            { 501019, new TrainerInfo(CharacterClass.Sniper, 249) },
            { 501020, new TrainerInfo(CharacterClass.Spy, 253) },
            { 501021, new TrainerInfo(CharacterClass.Demolitionist, 217) },
            { 501022, new TrainerInfo(CharacterClass.Engineer, 221) },
            { 501023, new TrainerInfo(CharacterClass.Medic, 237) },
            { 501024, new TrainerInfo(CharacterClass.Exobiologist, 225) },

            // Torden Mires, map 1759
            { 501025, new TrainerInfo(CharacterClass.Commando, 29) },
            { 501026, new TrainerInfo(CharacterClass.Ranger, 81) },
            { 501027, new TrainerInfo(CharacterClass.Sapper, 85) },
            { 501028, new TrainerInfo(CharacterClass.Biotechnician, 33) },
            { 501029, new TrainerInfo(CharacterClass.Grenadier, 61) },
            { 501030, new TrainerInfo(CharacterClass.Guardian, 65) },
            { 501031, new TrainerInfo(CharacterClass.Sniper, 89) },
            { 501032, new TrainerInfo(CharacterClass.Spy, 93) },
            { 501033, new TrainerInfo(CharacterClass.Demolitionist, 37) },
            { 501034, new TrainerInfo(CharacterClass.Engineer, 41) },
            { 501035, new TrainerInfo(CharacterClass.Medic, 73) },
            { 501036, new TrainerInfo(CharacterClass.Exobiologist, 45) },

            // Ashen Desert, map 1734
            { 501037, new TrainerInfo(CharacterClass.Soldier, 1) },
            { 501038, new TrainerInfo(CharacterClass.Specialist, 25) },
            // The seventeen on TRAINER_ALL_GENERAL pins: no class of their own, and
            // dialog group 337 - "I'm here to train you, regardless of your specialty."
            { 500001, new TrainerInfo(CharacterClass.None, 337) },
            { 500002, new TrainerInfo(CharacterClass.None, 337) },
            { 500012, new TrainerInfo(CharacterClass.None, 337) },
            { 500046, new TrainerInfo(CharacterClass.None, 337) },
            { 500080, new TrainerInfo(CharacterClass.None, 337) },
            { 500093, new TrainerInfo(CharacterClass.None, 337) },
            { 500112, new TrainerInfo(CharacterClass.None, 337) },
            { 500113, new TrainerInfo(CharacterClass.None, 337) },
            { 500114, new TrainerInfo(CharacterClass.None, 337) },
            { 500146, new TrainerInfo(CharacterClass.None, 337) },
            { 500180, new TrainerInfo(CharacterClass.None, 337) },
            { 500229, new TrainerInfo(CharacterClass.None, 337) },
            { 500248, new TrainerInfo(CharacterClass.None, 337) },
            { 500274, new TrainerInfo(CharacterClass.None, 337) },
            { 500300, new TrainerInfo(CharacterClass.None, 337) },
            { 500318, new TrainerInfo(CharacterClass.None, 337) },
            { 500324, new TrainerInfo(CharacterClass.None, 337) },
        };

        public static int Count => Trainers.Count;

        public static bool Trains(uint creatureDbId)
        {
            return Trainers.ContainsKey(creatureDbId);
        }

        public static bool TryGet(uint creatureDbId, out TrainerInfo info)
        {
            return Trainers.TryGetValue(creatureDbId, out info);
        }

        /// <summary>
        /// Which line a trainer of <paramref name="trains"/> has for this character.
        ///
        /// A generic trainer (None) has no class of its own to measure against, so it asks the
        /// same question about the character's whole tree: anything open now is an offer,
        /// nothing open but a tier still ahead is too low, and no tier left is done.
        /// </summary>
        public static TrainerDialog DialogFor(CharacterClass trains, CharacterClass playerClass, int level)
        {
            if (trains == CharacterClass.None)
            {
                if (CharacterClassTree.AdvancementsFor(playerClass, level).Count > 0)
                    return TrainerDialog.Offer;

                return CharacterClassTree.AdvancementsFor(playerClass, int.MaxValue).Count > 0
                    ? TrainerDialog.TooLow
                    : TrainerDialog.AlreadyTrained;
            }

            if (CharacterClassTree.Is(playerClass, trains))
                return TrainerDialog.AlreadyTrained;

            if (!CharacterClassTree.CanAdvanceTo(playerClass, trains))
                return TrainerDialog.WrongPath;

            return level >= CharacterClassTree.LevelFor(trains)
                ? TrainerDialog.Offer
                : TrainerDialog.TooLow;
        }
    }
}

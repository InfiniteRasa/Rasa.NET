namespace Rasa.Data
{
    /// <summary>
    /// generated/client/constant/creatureflag - the client's own table, names and all. The flags
    /// travel in CreatureInfo as a plain list of ids, and the client reads them with
    /// <c>target.HasFlag(creatureflag.BIOLOGICAL)</c> and the like.
    ///
    /// Three of them decide what a tool may be used on. harvest.py refuses a BIOLOGICAL creature
    /// to Salvage and a MECHANICAL one to Tissue Extraction, and healdisc.py will only heal a
    /// creature that is BIOLOGICAL or MACHINA. The three are one each: a Machina is MACHINA,
    /// not MECHANICAL as well. The SPECIES_* entries name the forty-six families the game
    /// recognises.
    ///
    /// Each class's flags are the monster flag editor's (CreatureFlagEdits). The server acts on
    /// the substances, the species, the experience modifiers, POWERLEVEL_0_AMBIENT, _6_BOSS and
    /// _7_SUPERBOSS, the damage and control resistances, immunities and vulnerabilities,
    /// IMMOBILIZE_IMMUNITY, REAR_POSITION_BONUS_IMMUNITY, NO_CRIT_KILL, CAN_WALK_BACKWARDS and
    /// the ALT_MESH flags; the client draws the target frame's borders from the UI_INDICATOR_
    /// ones. The rest - kingdom, sentience, origin, IS_BANE and its like, the other power,
    /// armour and chitin levels, CAN_CROUCH, CAN_BE_REVIVED - go to the client in CreatureInfo
    /// and nothing on the server reads them yet.
    /// </summary>
    public enum CreatureFlag
    {
        /// <summary>KINGDOM_ANIMAL</summary>
        KingdomAnimal = 1,

        /// <summary>ORIGIN_ARIEKI</summary>
        OriginArieki = 2,

        /// <summary>ARMOR_LEVEL_0_NONE</summary>
        ArmorLevel0None = 3,

        /// <summary>IS_BANE</summary>
        IsBane = 4,

        /// <summary>BIOLOGICAL</summary>
        Biological = 5,

        /// <summary>XP_MODIFIER_4_DOUBLE</summary>
        XpModifier4Double = 6,

        /// <summary>ORIGIN_EARTH</summary>
        OriginEarth = 7,

        /// <summary>ORIGIN_FOREAS</summary>
        OriginForeas = 8,

        /// <summary>IS_HUMAN</summary>
        IsHuman = 9,

        /// <summary>MACHINA</summary>
        Machina = 10,

        /// <summary>MECHANICAL</summary>
        Mechanical = 11,

        /// <summary>ORIGIN_MYCON</summary>
        OriginMycon = 12,

        /// <summary>XP_MODIFIER_0_NONE</summary>
        XpModifier0None = 13,

        /// <summary>SENTIENCE_1_AWARE</summary>
        Sentience1Aware = 14,

        /// <summary>XP_MODIFIER_3_NORMAL</summary>
        XpModifier3Normal = 15,

        /// <summary>KINGDOM_PLANT</summary>
        KingdomPlant = 16,

        /// <summary>XP_MODIFIER_5_QUADRUPLE</summary>
        XpModifier5Quadruple = 17,

        /// <summary>XP_MODIFIER_1_QUARTER</summary>
        XpModifier1Quarter = 18,

        /// <summary>SENTIENCE_2_INTELLIGENT</summary>
        Sentience2Intelligent = 19,

        /// <summary>IS_SUMMONED</summary>
        IsSummoned = 20,

        /// <summary>IS_HUMANOID</summary>
        IsHumanoid = 21,

        /// <summary>POWERLEVEL_0_AMBIENT</summary>
        Powerlevel0Ambient = 22,

        /// <summary>POWERLEVEL_1_ANNOYANCE</summary>
        Powerlevel1Annoyance = 23,

        /// <summary>POWERLEVEL_2_MINION</summary>
        Powerlevel2Minion = 24,

        /// <summary>POWERLEVEL_3_THUG</summary>
        Powerlevel3Thug = 25,

        /// <summary>POWERLEVEL_4_LIEUTENANT</summary>
        Powerlevel4Lieutenant = 26,

        /// <summary>POWERLEVEL_5_PLAYER</summary>
        Powerlevel5Player = 27,

        /// <summary>POWERLEVEL_6_BOSS</summary>
        Powerlevel6Boss = 28,

        /// <summary>POWERLEVEL_7_SUPERBOSS</summary>
        Powerlevel7Superboss = 29,

        /// <summary>ARMOR_LEVEL_1_MINION</summary>
        ArmorLevel1Minion = 30,

        /// <summary>ARMOR_LEVEL_2_LIEUTENANT</summary>
        ArmorLevel2Lieutenant = 31,

        /// <summary>ARMOR_LEVEL_3_PLAYER</summary>
        ArmorLevel3Player = 32,

        /// <summary>ARMOR_LEVEL_4_BOSS</summary>
        ArmorLevel4Boss = 33,

        /// <summary>XP_MODIFIER_2_HALF</summary>
        XpModifier2Half = 34,

        /// <summary>KINGDOM_FUNGUS</summary>
        KingdomFungus = 35,

        /// <summary>SENTIENCE_0_INANIMATE</summary>
        Sentience0Inanimate = 36,

        /// <summary>ORIGIN_ARTIFACT</summary>
        OriginArtifact = 37,

        /// <summary>ORIGIN_ELOH</summary>
        OriginEloh = 38,

        /// <summary>ORIGIN_NEPH</summary>
        OriginNeph = 39,

        /// <summary>ORIGIN_THRAX</summary>
        OriginThrax = 40,

        /// <summary>ORIGIN_OTHER</summary>
        OriginOther = 41,

        /// <summary>NO_BURIAL</summary>
        NoBurial = 42,

        /// <summary>ALT_MESH</summary>
        AltMesh = 43,

        /// <summary>ALIGN_TO_NORMAL</summary>
        AlignToNormal = 44,

        /// <summary>IMMOBILIZE_IMMUNITY</summary>
        ImmobilizeImmunity = 45,

        /// <summary>REAR_POSITION_BONUS_IMMUNITY</summary>
        RearPositionBonusImmunity = 46,

        /// <summary>CHITIN_ARMOR_1_MINION</summary>
        ChitinArmor1Minion = 47,

        /// <summary>CHITIN_ARMOR_2_LIEUTENANT</summary>
        ChitinArmor2Lieutenant = 48,

        /// <summary>CHITIN_ARMOR_3_PLAYER</summary>
        ChitinArmor3Player = 49,

        /// <summary>CHITIN_ARMOR_4_BOSS</summary>
        ChitinArmor4Boss = 50,

        /// <summary>NO_CRIT_KILL</summary>
        NoCritKill = 51,

        /// <summary>CAN_CROUCH</summary>
        CanCrouch = 52,

        /// <summary>CAN_WALK_BACKWARDS</summary>
        CanWalkBackwards = 53,

        /// <summary>CAN_BE_REVIVED</summary>
        CanBeRevived = 54,

        /// <summary>UI_INDICATOR_OPERATION</summary>
        UiIndicatorOperation = 55,

        /// <summary>UI_INDICATOR_BOSS</summary>
        UiIndicatorBoss = 56,

        /// <summary>CAN_USE_CONTROL_POINT</summary>
        CanUseControlPoint = 57,

        /// <summary>XP_MODIFIER_3A_ONE_AND_A_HALF</summary>
        XpModifier3aOneAndAHalf = 58,

        /// <summary>XP_MODIFIER_4A_TRIPLE</summary>
        XpModifier4aTriple = 59,

        /// <summary>SPECIES_KAEL</summary>
        SpeciesKael = 60,

        /// <summary>SPECIES_LIGHTBENDER</summary>
        SpeciesLightbender = 61,

        /// <summary>SPECIES_THRAX</summary>
        SpeciesThrax = 62,

        /// <summary>SPECIES_CARETAKER</summary>
        SpeciesCaretaker = 63,

        /// <summary>SPECIES_STALKER</summary>
        SpeciesStalker = 64,

        /// <summary>SPECIES_STRIDER</summary>
        SpeciesStrider = 65,

        /// <summary>SPECIES_TECHNICIAN</summary>
        SpeciesTechnician = 66,

        /// <summary>SPECIES_LINKER</summary>
        SpeciesLinker = 67,

        /// <summary>SPECIES_MIASMA</summary>
        SpeciesMiasma = 68,

        /// <summary>SPECIES_MOX</summary>
        SpeciesMox = 69,

        /// <summary>SPECIES_ARIEKI_BOT</summary>
        SpeciesAriekiBot = 70,

        /// <summary>SPECIES_AMOEBOID</summary>
        SpeciesAmoeboid = 71,

        /// <summary>SPECIES_FITHIK</summary>
        SpeciesFithik = 72,

        /// <summary>SPECIES_MACHINA</summary>
        SpeciesMachina = 73,

        /// <summary>SPECIES_XANX</summary>
        SpeciesXanx = 74,

        /// <summary>SPECIES_TREEBACK</summary>
        SpeciesTreeback = 75,

        /// <summary>SPECIES_MAGMONIX</summary>
        SpeciesMagmonix = 76,

        /// <summary>SPECIES_SHIELD_DRONE</summary>
        SpeciesShieldDrone = 77,

        /// <summary>SPECIES_SENTINEL</summary>
        SpeciesSentinel = 78,

        /// <summary>SPECIES_HUNTER</summary>
        SpeciesHunter = 79,

        /// <summary>SPECIES_HOWLER</summary>
        SpeciesHowler = 80,

        /// <summary>SPECIES_NITROGLAZER</summary>
        SpeciesNitroglazer = 81,

        /// <summary>SPECIES_ATTA</summary>
        SpeciesAtta = 82,

        /// <summary>SPECIES_BEAM_MANTA</summary>
        SpeciesBeamManta = 83,

        /// <summary>SPECIES_BARB_TICK</summary>
        SpeciesBarbTick = 84,

        /// <summary>SPECIES_FLARE_GASHER</summary>
        SpeciesFlareGasher = 85,

        /// <summary>SPECIES_LASHER</summary>
        SpeciesLasher = 86,

        /// <summary>SPECIES_TREELURKER</summary>
        SpeciesTreelurker = 87,

        /// <summary>SPECIES_WARNET</summary>
        SpeciesWarnet = 88,

        /// <summary>SPECIES_FILCHER</summary>
        SpeciesFilcher = 89,

        /// <summary>SPECIES_BOARGAR</summary>
        SpeciesBoargar = 90,

        /// <summary>SPECIES_GRUBBER</summary>
        SpeciesGrubber = 91,

        /// <summary>SPECIES_HUMAN</summary>
        SpeciesHuman = 92,

        /// <summary>SPECIES_PREDATOR</summary>
        SpeciesPredator = 93,

        /// <summary>SPECIES_JUGGERNAUT</summary>
        SpeciesJuggernaut = 94,

        /// <summary>SPECIES_TURRET</summary>
        SpeciesTurret = 95,

        /// <summary>SPECIES_BRANN</summary>
        SpeciesBrann = 96,

        /// <summary>SPECIES_FOREAN</summary>
        SpeciesForean = 97,

        /// <summary>SPECIES_LAVAR</summary>
        SpeciesLavar = 98,

        /// <summary>SPECIES_TREEMITE</summary>
        SpeciesTreemite = 99,

        /// <summary>SPECIES_GRANITOUR</summary>
        SpeciesGranitour = 100,

        /// <summary>SPECIES_MAW</summary>
        SpeciesMaw = 101,

        /// <summary>SPECIES_NECROMITE</summary>
        SpeciesNecromite = 102,

        /// <summary>ALT_MESH_DELAYED_3500</summary>
        AltMeshDelayed3500 = 103,

        /// <summary>SPECIES_SEEKER</summary>
        SpeciesSeeker = 104,

        /// <summary>SPECIES_RAVAGER</summary>
        SpeciesRavager = 105,

        /// <summary>SPECIES_AFS_VEHICLE</summary>
        SpeciesAfsVehicle = 106,

        /// <summary>UI_INDICATOR_EPIC</summary>
        UiIndicatorEpic = 107,

        /// <summary>UI_INDICATOR_EPIC_BOSS</summary>
        UiIndicatorEpicBoss = 108,

        /// <summary>RESIST_PHYSICAL</summary>
        ResistPhysical = 109,

        /// <summary>RESIST_FIRE</summary>
        ResistFire = 110,

        /// <summary>RESIST_ICE</summary>
        ResistIce = 111,

        /// <summary>RESIST_VIRULENT</summary>
        ResistVirulent = 112,

        /// <summary>RESIST_EMP</summary>
        ResistEmp = 113,

        /// <summary>RESIST_LIGHT</summary>
        ResistLight = 114,

        /// <summary>RESIST_SONIC</summary>
        ResistSonic = 115,

        /// <summary>RESIST_KNOCKBACK</summary>
        ResistKnockback = 116,

        /// <summary>RESIST_STUN</summary>
        ResistStun = 117,

        /// <summary>RESIST_SLEEP</summary>
        ResistSleep = 118,

        /// <summary>RESIST_ENERGY</summary>
        ResistEnergy = 119,

        /// <summary>RESIST_SNARE</summary>
        ResistSnare = 120,

        /// <summary>RESIST_ROOT</summary>
        ResistRoot = 121,

        /// <summary>RESIST_CONFUSION</summary>
        ResistConfusion = 122,

        /// <summary>RESIST_FEAR</summary>
        ResistFear = 123,

        /// <summary>RESIST_BLIND</summary>
        ResistBlind = 124,

        /// <summary>IMMUNE_PHYSICAL</summary>
        ImmunePhysical = 125,

        /// <summary>IMMUNE_FIRE</summary>
        ImmuneFire = 126,

        /// <summary>IMMUNE_ICE</summary>
        ImmuneIce = 127,

        /// <summary>IMMUNE_VIRULENT</summary>
        ImmuneVirulent = 128,

        /// <summary>IMMUNE_EMP</summary>
        ImmuneEmp = 129,

        /// <summary>IMMUNE_LIGHT</summary>
        ImmuneLight = 130,

        /// <summary>IMMUNE_SONIC</summary>
        ImmuneSonic = 131,

        /// <summary>IMMUNE_KNOCKBACK</summary>
        ImmuneKnockback = 132,

        /// <summary>IMMUNE_STUN</summary>
        ImmuneStun = 133,

        /// <summary>IMMUNE_SLEEP</summary>
        ImmuneSleep = 134,

        /// <summary>IMMUNE_ENERGY</summary>
        ImmuneEnergy = 135,

        /// <summary>IMMUNE_SNARE</summary>
        ImmuneSnare = 136,

        /// <summary>IMMUNE_ROOT</summary>
        ImmuneRoot = 137,

        /// <summary>IMMUNE_CONFUSION</summary>
        ImmuneConfusion = 138,

        /// <summary>IMMUNE_FEAR</summary>
        ImmuneFear = 139,

        /// <summary>IMMUNE_BLIND</summary>
        ImmuneBlind = 140,

        /// <summary>VULNERABLE_PHYSICAL</summary>
        VulnerablePhysical = 141,

        /// <summary>VULNERABLE_FIRE</summary>
        VulnerableFire = 142,

        /// <summary>VULNERABLE_ICE</summary>
        VulnerableIce = 143,

        /// <summary>VULNERABLE_VIRULENT</summary>
        VulnerableVirulent = 144,

        /// <summary>VULNERABLE_EMP</summary>
        VulnerableEmp = 145,

        /// <summary>VULNERABLE_LIGHT</summary>
        VulnerableLight = 146,

        /// <summary>VULNERABLE_SONIC</summary>
        VulnerableSonic = 147,

        /// <summary>VULNERABLE_ENERGY</summary>
        VulnerableEnergy = 148
    }
}

using System;

namespace Rasa.Data
{
    /// <summary>
    /// What the armour skills do at each pump, per piece of that armour worn, as the client
    /// describes them. Like the weapon skills (WeaponSkills) there is no table of these in the
    /// client data; each pump's tooltip (skilldata.skillLevel → uielement "Bio Armor 3: Expert" /
    /// "Health Regeneration: +6% Per Piece Worn | Radius: 15m") is the statement of them, and
    /// this is that text as numbers.
    ///
    /// A piece counts when it is in one of the five armour slots (helmet, torso, gloves, legs,
    /// boots) and its template requires the skill - every armour template requires exactly one
    /// of these seven, which is what makes it that kind of armour.
    ///
    ///  - Motor Assist: movement +2/4/6/8/10% per piece.
    ///  - Reflective: reflects +3/6/9/12/15% per piece of the damage taken back at the attacker;
    ///    the wearer still takes all of it.
    ///  - Hazmat: resistance +1..+5 per piece to electric, virulent, incendiary and cryogenic.
    ///  - Graviton: armour regeneration +3/6/9/12/15% per piece; knockback and stun resistance
    ///    the same, for when those exist.
    ///  - Stealth: creature detection range -2/4/6/8/10% per piece; radar signature -4..-20%
    ///    per piece, which is a PvP radar matter and not applied.
    ///  - Mech: power regeneration +2..10% per piece for the wearer and the squad within
    ///    5/10/15/20/25 m.
    ///  - Bio: health regeneration the same, same radii.
    ///
    /// The client shows each as a status effect from armorskilleffect.py, whose tooltips take
    /// the totals; the effect ids are here with them.
    /// </summary>
    public static class ArmorSkills
    {
        // skilldata ids.
        public const int MotorAssist = 19;
        public const int Reflective = 21;
        public const int Hazmat = 30;
        public const int Graviton = 39;
        public const int Stealth = 48;
        public const int Mech = 57;
        public const int Bio = 66;

        // gameeffectdata ids of the effects the client has classes for.
        /// <summary>BIO_ARMOR_SKILL_TARGET, "Bio Armor Aura": "Improves health regen by %(regenMod)s%%".</summary>
        public const int BioAuraTypeId = 10000007;
        /// <summary>MECH_ARMOR_SKILL_TARGET, "Mech Armor Aura": "Power regen %(regenMod)s%% of normal".</summary>
        public const int MechAuraTypeId = 10000061;
        /// <summary>MEDIUM_ARMOR_SKILL: no icon; its Recv_AnnounceReflect plays a reflection.</summary>
        public const int ReflectiveHiddenTypeId = 10000010;
        /// <summary>MEDIUM_ARMOR_SKILL_VISIBLE, "Reflective Armor": "Damage Reflection: %(reflectAmt)s%%".</summary>
        public const int ReflectiveVisibleTypeId = 10000028;
        /// <summary>HAZMAT_ARMOR_SKILL_VISIBLE, "Hazmat Armor": "Resist Virulent: +%(resistMod)s ...".</summary>
        public const int HazmatVisibleTypeId = 10000024;
        /// <summary>HEAVY_ARMOR_SKILL_VISIBLE, "Graviton Armor": "Knockback / Stun Resist: %(resistMod)s%% | Armor Recharge: %(regenMod)s%%".</summary>
        public const int GravitonVisibleTypeId = 10000025;
        /// <summary>LIGHT_ARMOR_SKILL_VISIBLE, "Motor Assist Armor": "Improves speed while running to %(movementMod)s%%".</summary>
        public const int MotorAssistVisibleTypeId = 10000026;
        /// <summary>STEALTH_ARMOR_SKILL_VISIBLE, "Stealth Armor": "Stealth Modifier: %(perceptionMod)s%%".</summary>
        public const int StealthVisibleTypeId = 10000029;

        /// <summary>The five armour slots a piece is counted in.</summary>
        public static readonly EquipmentData[] ArmorSlots =
        {
            EquipmentData.Helmet, EquipmentData.Torso, EquipmentData.Gloves, EquipmentData.Legs, EquipmentData.Shoes
        };

        /// <summary>The damage types Hazmat Armor resists: electric, virulent, incendiary, cryogenic.</summary>
        public static readonly DamageType[] HazmatTypes = { DamageType.Electrical, DamageType.Virulent, DamageType.Fire, DamageType.Ice };

        private static int Pump(int pump) => Math.Max(0, Math.Min(5, pump));

        /// <summary>Per piece: +2% movement a pump.</summary>
        public static int MovementPercent(int pump, int pieces) => 2 * Pump(pump) * pieces;

        /// <summary>Per piece: +3% of the damage taken reflected a pump.</summary>
        public static int ReflectPercent(int pump, int pieces) => 3 * Pump(pump) * pieces;

        /// <summary>Per piece: +1 resistance a pump.</summary>
        public static int HazmatResist(int pump, int pieces) => Pump(pump) * pieces;

        /// <summary>Per piece: +3% armour regeneration a pump (and knockback / stun resistance).</summary>
        public static int GravitonPercent(int pump, int pieces) => 3 * Pump(pump) * pieces;

        /// <summary>Per piece: -2% creature detection range a pump.</summary>
        public static int DetectionCutPercent(int pump, int pieces) => 2 * Pump(pump) * pieces;

        /// <summary>Per piece: +2% health (Bio) or power (Mech) regeneration a pump.</summary>
        public static int RegenPercent(int pump, int pieces) => 2 * Pump(pump) * pieces;

        /// <summary>Bio and Mech aura radius: 5 m a pump.</summary>
        public static float AuraRadius(int pump) => 5f * Pump(pump);
    }
}

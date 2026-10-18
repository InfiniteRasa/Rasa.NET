using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Corporal Van Valkenberg, who meets the player at the Proving Grounds' extraction in Calling
    /// for Reinforcements (missions 1995 and 2005, creature 510209), as footage of the live game
    /// shows him (Bootcamp_van_valkenberg).
    ///
    ///  - His look: the Stealth 6 armour (Armor_T3_Stealth_V06_RAR, the level 15 to 19 pieces) as
    ///    painted, helmet included: a navy suit under pale plates at the shoulders and shins, with
    ///    the lit blue panel on the chest.
    ///    Until now he wore the olive officer's uniform of the Proving Grounds' other quest NPCs
    ///    (BootcampBaseNpcs.OfficerOlive). Not Clothing_Armor_Stealth_6, whose helm the client has
    ///    in the legs slot (equipableClassEquipmentSlot 16).
    ///  - His line: greeting 1648, "Don't worry, soldier. We've got this all under control for
    ///    now. Just head on to the dropship pad to transport out of here." The mission's own
    ///    conversation comes first while the check-in is open (NpcManager.OpenConversation), so
    ///    the line is what he says once the player has checked in and the only thing left is the
    ///    dropship pad.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each slot changes only while it is as it was,
    /// the greeting goes in only where he has none, and Down takes out only what Up put in.
    /// </summary>
    public static class BootcampVanValkenberg
    {
        public const uint VanValkenberg = BootcampWorldContentSeedData.CorporalVanValkenbergCreatureId;

        /// <summary>npcgreetinglanguage 1648.</summary>
        public const uint Greeting = 1648;

        public const uint Unhued = AliaDasThirdLook.Unhued;
        public const uint StealthHelmet = 23540, StealthBoots = 23524, StealthGloves = 23532, StealthLegs = 23548, StealthVest = 23556;

        public const uint OfficerBoots = 4021, OfficerLegs = 4022, OfficerTunic = 4023;
        public const uint Pistol = 6271, Hair = 9781, HairColor = 4278655809, Face = 24008;

        public static readonly Slot[] Was =
        {
            new(2, OfficerBoots, BootcampBaseNpcs.OfficerOlive), new(13, Pistol, 1), new(14, Hair, HairColor),
            new(15, OfficerTunic, BootcampBaseNpcs.OfficerOlive), new(16, OfficerLegs, BootcampBaseNpcs.OfficerOlive), new(17, Face, SkinLight)
        };

        public static readonly Slot[] Is =
        {
            new(1, StealthHelmet, Unhued), new(2, StealthBoots, Unhued), new(3, StealthGloves, Unhued), new(13, Pistol, 1),
            new(14, Hair, HairColor), new(15, StealthVest, Unhued), new(16, StealthLegs, Unhued), new(17, Face, SkinLight)
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var sql in Change(VanValkenberg, Was, Is))
                yield return sql;

            yield return $"INSERT INTO npc_greeting (id, greeting_id, important) SELECT {VanValkenberg}, {Greeting}, 0 " +
                $"FROM creature c WHERE c.id = {VanValkenberg} AND NOT EXISTS (SELECT 1 FROM npc_greeting x WHERE x.id = {VanValkenberg});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM npc_greeting WHERE id = {VanValkenberg} AND greeting_id = {Greeting} AND important = 0;";

            foreach (var sql in Change(VanValkenberg, Is, Was))
                yield return sql;
        }
    }
}

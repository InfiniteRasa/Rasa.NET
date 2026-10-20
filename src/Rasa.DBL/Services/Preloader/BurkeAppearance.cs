using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Lieutenant Burke (creature 96, "AFS Officer Burke"), whom "Orders From High Command" (701)
    /// sends the player to in Monarch Grove (spawnpool 171), had no creature_appearance rows. His
    /// class, NPC_Corman_Swapset_Male (6339), is a bare body that takes its face, hair and clothes
    /// from those rows, so he stood with no head and nothing on (Burke_appearance). He was the only
    /// placed NPC on one of the swapset bodies with none.
    ///
    /// What he wore in the live game is not known: neither the client nor the seed this table
    /// came from has it. He is given what the other AFS lieutenants and officers on the same body
    /// wear in the seed - Lt Wood's (121) six rows, as Lt Col Cimoch (118) and Lt Cmdr Parsons (119)
    /// have them: the officer's helmet, gloves, boots, torso and legs, and the Caucasian long face.
    ///
    /// Guarded: a slot is filled only if Burke has nothing in it (a look put on him with
    /// .creatureappearance is kept), and Down takes away only a row still as it was put in. Plain
    /// SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class BurkeAppearance
    {
        public const uint Burke = 96;

        /// <summary>Slot, item class and colour: Lt Wood's (creature 121).</summary>
        public static readonly (uint Slot, uint ClassId, uint Color)[] Rows =
        {
            (1, 26677, 4286886614),  // NPC_Clothing_Officer_3_Helmet
            (2, 4021, 4294934528),   // NPC_Clothing_Officer_1_Boots
            (3, 26673, 22120),       // NPC_Clothing_Officer_3_Gloves
            (15, 4023, 4294934528),  // NPC_Clothing_Officer_1_Torso
            (16, 4022, 4294934528),  // NPC_Clothing_Officer_1_Legs
            (17, 24008, 4286690539), // AvatarSwap_Face_Caucasian_Long_1
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var (slot, classId, color) in Rows)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT {Burke}, {slot}, {classId}, {color} " +
                    $"FROM creature c WHERE c.id = {Burke} AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = {Burke} AND a.slot_id = {slot});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (slot, classId, color) in Rows)
                yield return $"DELETE FROM creature_appearance WHERE id = {Burke} AND slot_id = {slot} AND Class_id = {classId} AND color = {color};";
        }
    }
}

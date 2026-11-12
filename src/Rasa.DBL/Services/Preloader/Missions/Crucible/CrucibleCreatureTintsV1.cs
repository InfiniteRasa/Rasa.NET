using System.Collections.Generic;
using System.Linq;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible set 14: the body tints of the Crucible creatures (creature_tint, CreatureTintEntry, as
    /// ValverdeCreatureTints): hue and hue2 both the colour given, alpha 255, or a row with no tint, the model's stock
    /// colours. A creature not listed keeps the random tint (Creature.TintAtSpawn). The plan is
    /// tools/mission-crossref/mob-tints-plan-crucible.csv (52 rows: the 32 creature rows of the Crucible maps and 20 new rows
    /// of sets 1-13), the research NOTES-mob-skins.md section 9. The new rows left out, and why: <see cref="LeftOut"/>.
    ///
    /// Layers (NOTES-mob-skins.md, the layered design): (1) named, boss and officer rows with a colour of their own,
    /// (2) family and rank (level band) colour of the normals, (3) stock rows (no tint), (4) no row: the random tint, as before.
    /// Each row keeps its sources (Why) and what is guessed (Estimate), as the plan has them. Source codes: NOTES-mob-skins.md
    /// sections 6.2, 7 and 9.8 (TRp = TaRapedia "Thrax Soldier"; YT-* = the YouTube videos of 9.8.2; TX = the stock client
    /// texture and its alpha mask; SH = the shader name in the client .geo).
    ///
    /// Every RGB value is an ESTIMATE: no retail capture of a creature's tints exists.
    ///
    /// Data only, plain SQL valid on SQLite and MySQL: Up inserts only rows that are not there (CrucibleSql.Guard),
    /// Down deletes only rows that still have the values Up wrote; each is safe to run twice.
    /// </summary>
    public static class CrucibleCreatureTintsV1
    {
        /// <summary>One creature row's tint (hue = hue2, or Stock) with the class and level the plan read from the World DB, its map and its sources.</summary>
        public sealed record Row(uint CreatureId, string Name, uint ClassId, uint Level, uint? Hue, string Zone, string Why, string Estimate);

        /// <summary>A row with no tint: the model's stock colours.</summary>
        private static readonly uint? Stock = null;

        /// <summary>A colour as Color.Hue packs it: (A &lt;&lt; 24) | (B &lt;&lt; 16) | (G &lt;&lt; 8) | R, alpha 255 (as ValverdeCreatureTints.Rgb).</summary>
        public static uint Rgb(byte red, byte green, byte blue) => ValverdeCreatureTints.Rgb(red, green, blue);

        public static readonly IReadOnlyList<Row> Rows = new Row[]
        {
            // Layer 1: named, boss and officer rows.
            new(520013, "Executioner Derge", 10504, 47, Rgb(255, 255, 255), "Crucible",
                "named boss; mission 1241 text calls him \"a Thrax Commander\": officer white/grey (TRp, Overseer Quarm); Valverde officer value, seen as light silver in the real client 2026-11-06 (section 8.2); no image of Derge",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(520014, "Thrax Commander", 10504, 47, Rgb(255, 255, 255), "Crucible",
                "officer white/grey (TRp; TW-Ovs conflicts); Valverde Thrax Commander value reused (section 8.2); band L47 has no image",
                "ESTIMATE: value; colour from the rank rule"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(678007, "Ember", 24088, 47, Rgb(130, 40, 0), "Magma Caverns",
                "named boss (BOSS: unique colour); no image; ember orange-red from the name, on the whole-body mask; value kept as bright as Orax (130,10,0) because low values show no colour (section 8.2)",
                "ESTIMATE: colour and value (no image)"),  // Creature_Magmonix_Boss, code spawn (MagmaCaverns.cs)

            // Layer 2: family and rank colour of the normals. Thrax red at L42-45.
            new(531018, "Thrax Soldier Mark IX", 20757, 42, Rgb(110, 8, 0), "Crucible/Staal Junkyard",
                "Thrax red: TRp says red is the highest rank; Valverde value reused (FT, DV, KT; section 8.2 PASS); Crucible images (section 9.8): crimson Thrax at a L43 player's spot (YT-SB WLkPcBrGc70 1:00-1:11, 2008-01-14, level not readable), crimson Thrax L36-37 (YT-LC 5YliAWPu-rc 3:06-3:41), red corpses L38-40 (YT-FI lFBFJHJW8TQ 5:40); a L43 Thunderhead Thrax Rifleman is teal (YT-TH), other name",
                "ESTIMATE: value; colour from the Crucible images at L36-43 (no L42 nameplate readable)"),  // Bane_Thrax_Soldier_Pistol_NoBeaminBirth
            new(531019, "Bane_Thrax_Grenadier", 9354, 42, Rgb(110, 8, 0), "Crucible/Staal Junkyard",
                "Thrax red: TRp highest rank red; Valverde grenadier value reused (TW-Gren rust/dark red); band L42 has no grenadier image; Crucible Thrax soldiers and technicians L36-43 are crimson (section 9.8: YT-SB, YT-LC, YT-FI)",
                "ESTIMATE: value; colour from the rank rule and the Crucible family images"),  // Bane_Thrax_Grenadier
            new(531020, "Thrax Technician Major", 7043, 43, Rgb(110, 8, 0), "Crucible",
                "Thrax red: TRp highest rank red; Valverde technician value reused (TT, TW-Tech); \"Thrax Technician\" L42-43 is orange-red/rust in Thunderhead (YT-TH b-nBe7m2G6Y 2:48-3:03, retail, section 9.8); Thrax Technician Chief Warrant Officer L37 red in the Crucible (YT-LC 5YliAWPu-rc 3:16)",
                "ESTIMATE: value; colour from images of the same name at L42-43 (other zone) and L37 (Crucible)"),  // Bane_Thrax_Technician
            new(678013, "Rebel Converted Thrax", 9354, 45, Rgb(110, 8, 0), "Magma Caverns",
                "Thrax on the Rebel side (faction 3); family rank rule: TRp highest rank red, Valverde grenadier value reused; no image (a converted Thrax can have its own colour, as the green Disturbed Thrax)",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Grenadier
            new(678016, "Rebel Pacified Thrax", 20757, 44, Rgb(110, 8, 0), "Magma Caverns",
                "Thrax on the Rebel side (faction 3); family rank rule: TRp highest rank red, Valverde soldier value reused; no image (a pacified Thrax can have its own colour)",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Pistol_NoBeaminBirth

            // Layer 3: stock colours, no tint.
            new(531021, "Caretaker Surgeon", 9244, 43, Stock, "Crucible",
                "stock colours: Valverde Caretaker value reused (TW-Caretaker grey-silver; beta images); band L43 has no image",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Bane_Caretaker
            new(531022, "Fithik Guardian", 4313, 42, Stock, "Crucible",
                "new family, no image; stock dark brown-grey plates, red-orange glow, violet wings (TX, mask 37%); client Fithik rank names are gem and colour names, so retail had a rank tint ESTIMATE, colour unknown",
                "ESTIMATE: stock (no image) (Low)"),  // Bane_Fithik_Winged
            new(540030, "Flaregasher", 7338, 43, Stock, "Crucible/Magma Caverns/Staal Junkyard",
                "new family; stock rust-brown hide with red-orange stripes (TX, whole body); retail image: 4 Flaregashers close up in the Crucible near Chaukas (YT-SF2 MqWk5QW3sOw 1:09-1:13, 2008-10-15): body hue as the lit ground, no saturated colour (section 9.8); YT-SF1 K9i12Dw5IMc agrees; the boss Cracked Tooth is \"a bloody red\" (TaRapedia), so the normal is not red",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Creature_Flaregasher
            new(551013, "Bane Turret", 7482, 47, Stock, "Crucible",
                "stock colours (SH: PalDiffNormalGlossEnvVertColor has no Hue); Valverde value reused",
                "no: a tint has no effect"),  // Emplacement_Bane_Turret_Standard
            new(592014, "Bane Mortar", 7482, 45, Stock, "Crucible",
                "stock colours (SH: PalDiffNormalGlossEnvVertColor has no Hue); Valverde value reused",
                "no: a tint has no effect"),  // Emplacement_Bane_Turret_Standard
            new(531023, "Warden Bot Claviger", 6961, 43, Stock, "Warden bot facility",
                "new family, no image; stock grey metal with brown and orange parts (TX, mask 33%)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Prison_Bot_Warden
            new(678002, "Rebel Leader Salyard", 3846, 47, Stock, "Magma Caverns",
                "human NPC, neutral (faction 3): no tint; the body texture has the skin in the mask (hum_m_nude_v00_df 69%), so a random tint colours the skin; clothes come from creature_appearance",
                "ESTIMATE: stock (no image) (Medium)"),  // NPC_Human_Swapset_Male
            new(678003, "Lieutenant Sudevi", 3848, 46, Stock, "Magma Caverns",
                "human NPC, neutral (faction 3): no tint; skin in the mask (hum_f_nude_v00_df 100%); clothes come from creature_appearance",
                "ESTIMATE: stock (no image) (Medium)"),  // NPC_Human_Swapset_Female
            new(678004, "Lieutenant Nelby", 3846, 46, Stock, "Magma Caverns",
                "human NPC, neutral (faction 3): no tint; skin in the mask (hum_m_nude_v00_df 69%); clothes come from creature_appearance",
                "ESTIMATE: stock (no image) (Medium)"),  // NPC_Human_Swapset_Male
            new(678005, "Penumbra Assault Leader Mezen", 3846, 47, Stock, "Magma Caverns",
                "human NPC, neutral (faction 3): no tint; skin in the mask (hum_m_nude_v00_df 69%); clothes come from creature_appearance",
                "ESTIMATE: stock (no image) (Medium)"),  // NPC_Human_Swapset_Male
            new(678006, "Lieutenant Dama", 3848, 46, Stock, "Magma Caverns",
                "human NPC, neutral (faction 3): no tint; skin in the mask (hum_f_nude_v00_df 100%); clothes come from creature_appearance",
                "ESTIMATE: stock (no image) (Medium)"),  // NPC_Human_Swapset_Female
            new(678010, "Rebel Guerrilla", 29423, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX npc_redshirt_hum_mf_v01_df, mask 28%); no image of retail Rebel or Penumbra soldiers",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678011, "Rebel Assassin", 29423, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678012, "Rebel Marine", 29433, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Heavy_Male
            new(678014, "Rebel Defender", 29433, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Heavy_Male
            new(678015, "Rebel Field Medic", 29423, 44, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678020, "Penumbra Black Ops", 29423, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678021, "Penumbra Shock Trooper", 29433, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Heavy_Male
            new(678022, "Penumbra Spy", 29423, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678023, "Penumbra Spec Ops", 29433, 45, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Heavy_Male
            new(678024, "Penumbra Field Medic", 29423, 44, Stock, "Magma Caverns",
                "human soldier, neutral (faction 3): stock grey-blue armour plates (TX, mask 28%); no image",
                "ESTIMATE: stock (no image) (Low-Medium)"),  // Redshirt_Human_Soldier_Light_Male
            new(678032, "Quicksilver Magmonix", 6338, 45, Stock, "Magma Caverns",
                "new family, no image; stock purple-grey hide with orange lava glow (TX, whole body); the name says quicksilver (silver), no tint chosen until an image shows it",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Magmonix
            new(678030, "Beam Manta Soarer", 6441, 44, Stock, "Magma Caverns",
                "new family, no image; stock dark maroon wings, grey-brown scales (TX, whole body); the boss Plains Devil Lord is \"a vivid red\" (TaRapedia), so the normal is not vivid red ESTIMATE",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Beam_Manta, code spawn (MagmaCaverns.cs)
            new(678031, "Platinum Barb Tick", 7236, 44, Stock, "Magma Caverns",
                "new family, no image; stock pale bone/cream shell with blue spots (TX, whole body); the client rank names are metals and Platinum is pale silver, close to the stock",
                "ESTIMATE: stock matches the name (Low)"),  // Creature_Barb_Tick, code spawn (MagmaCaverns.cs)

            // The new creature rows of Crucible sets 1-13 (770xxx; plan NOTES-mob-skins.md 9.4 and 9.8.4, 9.9).
            // Layer 1: named Thrax officers and bosses.
            new(770023, "Preceptor Molitor", 10504, 43, Rgb(255, 255, 255), "Crucible",
                "set 5 row (copy of Derge 520013); officer white/grey (TRp; MSN 1206/1589 \"Thrax Officers\"); TaRapedia File:Preceptor Molitor.jpg is in \"Thrax Soldier screenshots\" (2008-01-03, not downloaded, section 9.8.1); Valverde officer value, seen as light silver in the real client 2026-11-06 (section 8.2); TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(770031, "Archfiend Krubb", 10504, 45, Rgb(255, 255, 255), "Crucible",
                "set 7 row; officer white/grey (TRp; MSN 1206/1589 \"Thrax Officers\"); TaRapedia: Krubb \"looks like Preceptor Molitar\" (section 9.8.1); Valverde officer value (section 8.2); TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(770032, "Coercer Mungrel", 10502, 45, Rgb(255, 255, 255), "Crucible",
                "set 7 row; TaRapedia: \"a Thrax Technician boss\" (section 9.8.1); officer white/grey for the four Thrax officers of MSN 1206/1589 (section 9.8.4 point 7); Valverde officer value (section 8.2); TX thrax_technician_v01 masked 45%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Technician_Boss
            new(770033, "Defiler Sedge", 21882, 45, Rgb(255, 255, 255), "Crucible",
                "set 7 row; TaRapedia: \"a Thrax Grenadier boss\" (section 9.8.1); officer white/grey for the four Thrax officers of MSN 1206/1589 (section 9.8.4 point 7); Valverde officer value (section 8.2); TX thrax_lieutenant_v01 masked 43%; the Valverde boss Orax of the same class is crimson (TW-Orax), so a boss colour is possible",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Grenadier_Boss
            new(770042, "Maxxum Ugoretz", 10504, 43, Rgb(255, 255, 255), "Crucible",
                "set 8b row (copy of Derge 520013); MSN 1216 \"the Bane commander\" of Khalago Base: officer white/grey if Thrax (section 9.4, TRp); Valverde officer value (section 8.2); TaRapedia has no file (section 9.8.1); TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(770061, "Skeev (talks)", 28589, 40, Rgb(0, 60, 20), "Crucible (Khulago laboratory)",
                "set 13 row, neutral (faction 3); dark green-black: TaRapedia File:Thrax_Overseer_Skeev.jpg (TW-Ovs, section 9.4); TX thrax_overseer_skeev_v01 whole body (alpha 100%, measured 2026-11-08); a low value on a whole-body mask can draw the model dark or black (section 8.2, Oakril), so check in the real client",
                "ESTIMATE: RGB value read from the image (no retail capture)"),  // NPC_Bane_Miniboss_Overseer_Skeev
            new(770062, "Skeev (fights)", 28589, 40, Rgb(0, 60, 20), "Crucible (Khulago laboratory)",
                "set 13 row, hostile; the same look as 770061 (one Skeev, two rows); dark green-black: TaRapedia File:Thrax_Overseer_Skeev.jpg (TW-Ovs, section 9.4); TX whole body (alpha 100%); check in the real client (section 8.2)",
                "ESTIMATE: RGB value read from the image (no retail capture)"),  // NPC_Bane_Miniboss_Overseer_Skeev

            // Layer 2: the Thrax Technician family red.
            new(770036, "Thrax Technician", 7043, 40, Rgb(110, 8, 0), "Staal Junkyard",
                "set 7 row (copy of 531020 at level 40); Thrax red: TRp highest rank red; the 531020 family value (section 9.4 reuse); Thrax Technician Chief Warrant Officer L37 red in the Crucible (YT-LC 5YliAWPu-rc 3:16); Thrax Technician Lieutenant L40 and red Thrax corpses in the Crucible (YT-FI lFBFJHJW8TQ 5:12, 5:40); TX thrax_technician_v01 masked 45%",
                "ESTIMATE: value; colour from the Crucible images of the family at L37-40"),  // Bane_Thrax_Technician

            // Layer 3: stock colours, no tint.
            new(770005, "Roscoe", 7338, 35, Stock, "Staal Centrum",
                "set 2 row, Vargo's pet Flaregasher, hostile (faction 0, CrucibleGrupaChiefsNpcsV1.Hostile); the Flaregasher family stock, as 540030: retail image of Flaregashers in the Crucible with no saturated colour (YT-SF2 MqWk5QW3sOw 1:09-1:13, section 9.8); TX whole body",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Creature_Flaregasher
            new(770007, "Warden Bot Screw", 6961, 35, Stock, "Crucible",
                "set 3 row; the Warden Bot family stock, as 531023 (section 9.4); stock grey metal with brown and orange parts (TX, mask 33%); TaRapedia Warden Bot.jpg exists, not downloaded (section 9.8.1)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Prison_Bot_Warden
            new(770008, "Lasher", 7477, 40, Stock, "Crucible",
                "set 3 row; stock (section 9.4); stock dark grey-brown with bone-cream (TX, whole body); TaRapedia Lasher.jpg exists, not downloaded (section 9.8.1); the Lasher boss Esoobac is \"purple colored\" (section 3), so the boss has its own colour, not the normal",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Lasher
            new(770015, "Reconstructor Bot Laborer", 6960, 35, Stock, "Crucible",
                "set 4 row; stock (section 9.4); stock dark gunmetal with green lights (TX, mask 26%); TaRapedia Reconstructor Bot.jpg exists, not downloaded (section 9.8.1)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Prison_Bot_Reconstructor
            new(770038, "Ultra Reconstructor", 10314, 42, Stock, "Staal Junkyard",
                "set 7 row; stock (section 9.4); the same mesh and texture as the Reconstructor Bot (TX bot_reconstructor_v01, mask 26%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Prison_Bot_Reconstructor_Boss
            new(770041, "Juggernaut", 7722, 42, Stock, "Crucible",
                "set 8b row; stock, the Valverde Juggernaut value (TTR, TW-Jugg; section 9.4); TX bane_juggernaut_v01 masked 26%, the same mesh as the Valverde boss 24555",
                "ESTIMATE: stock matches the image (Medium)"),  // Vehicle_Bane_Juggernaut
            new(770014, "Bane Smart Mine", 7528, 40, Stock, "Crucible",
                "set 4 row; copy of the Thrax Necromite 570003; no image; stock, as the other new families; the shader has Hue (PalDiffNormalGlossEnvGlowHue.fx, creature_thrax_necromite_v01_df alpha 100%, measured 2026-11-08), so the row keeps the stock colours instead of the random tint",
                "ESTIMATE: stock (no image) (Low)"),  // Ability_Bane_Necromite

            // Layer 3: stock colours, no tint, on models with no Hue shader (as ValverdeCreatureTints: Shield Drones, turrets).
            new(770022, "Shield Drone", 7233, 35, Stock, "Crucible",
                "set 5 row; no Hue shader (SH); section 9.4 plans stock; as the Valverde Shield Drones (531087, 531094, 531050)",
                "no: a tint has no effect"),  // Bane_Shield_Drone
            new(770047, "Incurable Brute", 23191, 40, Stock, "Incurables Ward",
                "set 11a row; no Hue shader (SH: TRHair.fx only); section 9.4 plans stock",
                "no: a tint has no effect"),  // Redshirt_Brann_Incurable_Male
            new(770048, "Incurable Slayer", 25611, 40, Stock, "Incurables Ward",
                "set 11a row; no Hue shader (SH: TRHair.fx only); section 9.4 plans stock",
                "no: a tint has no effect"),  // Redshirt_Brann_Incurable_Female
            new(770049, "Jaikev", 25547, 42, Stock, "Incurables Ward",
                "set 11a row; no Hue shader (SH: TRHair.fx only); section 9.4 plans stock",
                "no: a tint has no effect"),  // Redshirt_Brann_Incurable_Male_Boss
            new(770050, "Parvatee", 25617, 42, Stock, "Incurables Ward",
                "set 11a row; no Hue shader (SH: TRHair.fx only); section 9.4 plans stock",
                "no: a tint has no effect"),  // Redshirt_Brann_Incurable_Female_Boss
        };

        /// <summary>
        /// The new Crucible creature rows of sets 1-13 that get no row, and why (NOTES-mob-skins.md 9.9). They keep the random tint.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, string> LeftOut = new Dictionary<uint, string>
        {
            [770034] = "Kalamit (Creature_Lasher_Boss 10331): no image; a boss colour of its own is unknown",
            [770035] = "Lamontix (Bane_Thrax_Soldier_Rifle_Boss 10504): no image; MSN 1538 \"the Bane named Lamontix\", colour unknown",
        };

        public static IEnumerable<string> Up() => Rows.Select(row => CrucibleSql.Guard(
            $"INSERT INTO {CreatureTintEntry.TableName} (id, hue, hue2) VALUES ({row.CreatureId}, {Sql(row.Hue)}, {Sql(row.Hue)});"));

        public static IEnumerable<string> Down() => Rows.Select(row =>
            $"DELETE FROM {CreatureTintEntry.TableName} WHERE id = {row.CreatureId} AND " +
            (row.Hue is { } hue ? $"hue = {hue} AND hue2 = {hue};" : "hue IS NULL AND hue2 IS NULL;"));

        private static string Sql(uint? value) => value?.ToString() ?? "NULL";
    }
}

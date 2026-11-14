using System.Collections.Generic;
using System.Linq;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Ligo Ashen Desert set 21: the body tints of the Ashen creatures (creature_tint, CreatureTintEntry, as
    /// CrucibleCreatureTintsV1): hue and hue2 both the colour given, alpha 255, or a row with no tint, the model's stock colours.
    /// A creature not listed keeps the random tint (Creature.TintAtSpawn). The rows: the 21 creature rows of the four Ashen maps
    /// (Ashen Desert 1734, Bane Conscription Facility 1988, Indra Caverns 2055, Avernus Outpost 2110; NOTES-mob-skins.md 10.2)
    /// and the new hostile rows of Ashen sets 1-20 (790xxx; 10.4, 10.8). The new rows left out, and why: <see cref="LeftOut"/>.
    ///
    /// Layers (NOTES-mob-skins.md, the layered design): (1) named, boss and officer rows with a colour of their own,
    /// (2) family and rank (level band) colour of the normals, (3) stock rows (no tint), (4) no row: the random tint, as before.
    /// Each row keeps its sources (Why) and what is guessed (Estimate). Source codes: NOTES-mob-skins.md sections 6.2, 7, 9.8
    /// and 10.8 (TRp = TaRapedia "Thrax Soldier"; TW-* = a TaRapedia image or page; YT-* = the YouTube videos of 9.8.2 and 10.8;
    /// TX = the stock client texture and its alpha mask; SH = the shader name in the client .geo).
    ///
    /// Every RGB value is an ESTIMATE: no retail capture of a creature's tints exists.
    ///
    /// Data only, plain SQL valid on SQLite and MySQL: Up inserts only rows that are not there (CrucibleSql.Guard, the Ashen
    /// batch's guard), Down deletes only rows that still have the values Up wrote; each is safe to run twice.
    /// </summary>
    public static class AshenCreatureTintsV1
    {
        /// <summary>One creature row's tint (hue = hue2, or Stock) with the class and level of its World DB row, its map and its sources.</summary>
        public sealed record Row(uint CreatureId, string Name, uint ClassId, uint Level, uint? Hue, string Zone, string Why, string Estimate);

        /// <summary>A row with no tint: the model's stock colours.</summary>
        private static readonly uint? Stock = null;

        /// <summary>A colour as Color.Hue packs it: (A &lt;&lt; 24) | (B &lt;&lt; 16) | (G &lt;&lt; 8) | R, alpha 255 (as ValverdeCreatureTints.Rgb).</summary>
        public static uint Rgb(byte red, byte green, byte blue) => ValverdeCreatureTints.Rgb(red, green, blue);

        private const string NoEffect = "no: a tint has no effect";

        public static readonly IReadOnlyList<Row> Rows = new Row[]
        {
            // The 21 rows of the Ashen maps (NOTES-mob-skins.md 10.2).
            // Layer 1: named Overseers and the Commander, officer white/grey.
            new(520003, "Overseer Himbri", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "named Overseer; MSN 1580 \"Thrax Overseers\"; officer white/grey (TRp, Overseer Quarm); Valverde and Crucible officer value, seen as light silver in the real client 2026-11-06 (section 8.2); TaRapedia page has no image (10.8); TW-Ovs (Rankash, Skeev, Torqua) conflicts; TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(520004, "Overseer Mitecht", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "named Overseer; TaRapedia: \"a boss Thrax Grenadier\" (10.8, no image); MSN 1580; officer white/grey (TRp), as the Crucible boss Defiler Sedge (9.8.4 point 7); Valverde officer value (section 8.2); TW-Ovs conflicts",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(520005, "Overseer Naxveik", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "named Overseer; TaRapedia: \"Boss Thrax Technician\", MSN 1355 (10.8, no image); MSN 1580; officer white/grey (TRp), as the Crucible boss Coercer Mungrel (9.8.4 point 7); Valverde officer value (section 8.2); TW-Ovs conflicts",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(520006, "Overseer Ulovik", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "named Overseer; TaRapedia: \"Boss Thrax Technician\", MSN 1303 (10.8, no image); MSN 1580; officer white/grey (TRp), as Coercer Mungrel (9.8.4 point 7); Valverde officer value (section 8.2); TW-Ovs conflicts",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(520007, "Thrax Overseer Exing", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "named Overseer; TaRapedia: \"Boss Thrax Technician (?)\", MSN 1244 (10.8, no image); MSN 1580; officer white/grey (TRp); Valverde officer value (section 8.2); the name form \"Thrax Overseer X\" is the one of Skeev and Rankash, who have own colours (TW-Ovs), so own colour possible",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(531012, "Thrax Commander", 10504, 42, Rgb(255, 255, 255), "Ashen Desert/Indra Caverns",
                "officer white/grey (TRp; TW-Ovs conflicts); Valverde and Crucible Thrax Commander value reused (section 8.2, 9.2); band L42 has no image",
                "ESTIMATE: value; colour from the rank rule"),  // Bane_Thrax_Soldier_Rifle_Boss

            // Layer 2: Thrax red at L39-40.
            new(531008, "Thrax Soldier Mark VI", 20757, 39, Rgb(110, 8, 0), "Ashen Desert/Conscription Facility/Indra Caverns",
                "Thrax red: TRp says red is the highest rank; Valverde and Crucible value reused (FT, DV, KT; section 8.2 PASS); crimson Thrax Infantry L36-40 in the Crucible (YT-LC 5YliAWPu-rc 3:06-3:41, YT-FI lFBFJHJW8TQ 4:28-5:44, section 9.8.2); in the Ashen Desert, White Oasis Post: \"Thrax Infantry Captain\" L42 dark red-brown with crimson patches (YT-WO WADeltlMewA 0:08, 2009-02-05; YT-WO2 -SZx4vgk0g8 3:56, rust/red-brown; 10.8), as the Crucible crimson; TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from Ashen images of the family at L41-42 (other name)"),  // Bane_Thrax_Soldier_Pistol_NoBeaminBirth
            new(531009, "Thrax Ravager", 9354, 39, Rgb(110, 8, 0), "Ashen Desert/Conscription Facility/Indra Caverns",
                "Thrax red: TRp highest rank red; Valverde and Crucible grenadier value reused (TW-Gren rust/dark red); the Ligo Thrax L36-40 are crimson (YT-LC, YT-FI, section 9.8.2), Ashen Thrax Infantry L41-42 at White Oasis dark red-brown/crimson (YT-WO, YT-WO2; 10.8); no Ravager image; TX thrax_lieutenant_v01 masked 43%",
                "ESTIMATE: value; colour from the rank rule and the Ligo family images"),  // Bane_Thrax_Grenadier
            new(531010, "Thrax Technician Lieutenant", 7043, 40, Rgb(110, 8, 0), "Ashen Desert/Conscription Facility/Indra Caverns",
                "Thrax red: TRp highest rank red; Valverde and Crucible technician value reused (TT, TW-Tech); \"Thrax Technician Lieutenant\" L40, the same name and level, among red Thrax in the Crucible (YT-FI lFBFJHJW8TQ 5:12, 5:40); Ashen Thrax Infantry L41-42 at White Oasis dark red-brown/crimson (YT-WO, YT-WO2; 10.8); TX thrax_technician_v01 masked 45%",
                "ESTIMATE: value; colour from an image of the same name and level (other zone)"),  // Bane_Thrax_Technician

            // Layer 3: stock colours, no tint.
            new(520002, "Atta Imperial Guard", 22627, 44, Stock, "Conscription Facility",
                "named Atta soldier boss; no image (10.8); stock olive-green and yellow-brown carapace, orange-red stripes (TX arieki_atta_soldier_v01, mask 21%); the plan (10.2, 10.7 q2) keeps stock",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard_Boss
            new(531011, "Caretaker Doctor", 9244, 40, Stock, "Ashen Desert/Conscription Facility/Indra Caverns",
                "stock colours: Valverde and Crucible Caretaker value reused (TW-Caretaker grey-silver; beta images); YT-FI Caretakers L40 are too small; TX bane_caretaker_v01 masked 25%",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Bane_Caretaker
            new(531014, "Linker Master", 9751, 40, Stock, "Ashen Desert/Conscription Facility/Indra Caverns",
                "new family; stock gunmetal armour plates with red lights, pink-red flesh (TX bane_linker_armor_v01, mask 15%); the client ladder (Initiate ... Grand Master) gives no colour; TaRapedia Linker.jpg exists (2007-12-20, not downloaded, 10.8)",
                "ESTIMATE: stock (no image) (Low)"),  // Bane_Linker
            new(531015, "Atta Soldier Trooper", 7081, 40, Stock, "Avernus Outpost",
                "new family; stock olive-green and yellow-brown carapace, orange-red stripes (TX arieki_atta_soldier_v01, mask 21%); the client ladder (Conscript ... Commando) gives no colour; TaRapedia Atta Soldier.jpg exists (2007-08-29, beta, not downloaded, 10.8)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard
            new(531016, "Atta Harvester Acquirer", 7078, 39, Stock, "Avernus Outpost",
                "new family; stock ochre/tan plates, red-orange underside (TX arieki_atta_harvester_v01, whole body 100%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Harvester_Standard
            new(531017, "Atta grub", 29089, 39, Stock, "Avernus Outpost",
                "new family, no client name (name_id 0 on every grub row); stock grey-brown and pink-red flesh (TX arieki_atta_grub_v01, whole body 100%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Grub_Standard
            new(540018, "Fithik Guardian", 4313, 39, Stock, "Ashen Desert",
                "the Crucible Fithik Guardian 531022 value (stock, section 9.3); stock dark brown-grey plates, red-orange glow, violet wings (TX thrax_fithik_v01, mask 37%); the client Fithik rank names are gem and colour names, \"Fithik Guardian\" has none",
                "ESTIMATE: stock (no image) (Low)"),  // Bane_Fithik_Winged
            new(540019, "Malonic Treemite", 6040, 39, Stock, "Ashen Desert",
                "stock brown/tan shell, red-orange (TX treemite_v01, mask 11%); TW-Treemite (beta): tan/beige shell, orange-red legs = stock; the acid ladder names are not colours",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Creature_Treemite
            new(540020, "Xanx Burrower", 7510, 39, Stock, "Ashen Desert",
                "the Valverde Xanx Hunter 540010 value (stock); TW-Xanx (beta) orange-tan abdomen, red-brown legs = stock (TX creature_xanx_v02_df, mask 18%)",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Creature_Xanx_Standard

            // Layer 3: stock colours, no tint, on models with no Hue shader.
            new(531013, "Shield Drone Aggressor", 7233, 40, Stock, "Ashen Desert/Indra Caverns",
                "no Hue shader (SH: PalDiff_NorHeight_Gloss_Env_Glow_Parallax); as the Valverde and Crucible Shield Drones",
                NoEffect),  // Bane_Shield_Drone
            new(551010, "Bane Turret", 7482, 44, Stock, "Ashen Desert",
                "stock colours (SH: PalDiffNormalGlossEnvVertColor has no Hue); Valverde and Crucible value reused",
                NoEffect),  // Emplacement_Bane_Turret_Standard
            new(592011, "Bane Mortar", 7482, 42, Stock, "Ashen Desert",
                "stock colours (SH: PalDiffNormalGlossEnvVertColor has no Hue); Valverde and Crucible value reused",
                NoEffect),  // Emplacement_Bane_Turret_Standard

            // The new hostile rows of Ashen sets 1-20 (790xxx; NOTES-mob-skins.md 10.4, 10.8).
            // Layer 1: named Thrax Overseers, officer white/grey.
            new(790523, "Overseer Jirex", 10504, 44, Rgb(255, 255, 255), "Ashen Desert",
                "set 14 row (MSN 1286); TaRapedia: \"Boss Thrax Soldier\", near a pillbox north-east of Ashoka (10.8, no image); MSN 1580 names the 6 Overseers with Naxveik, Exing, Mitecht, Himbri, Ulovik; officer white/grey (TRp), as 520003-520007; Valverde officer value (section 8.2); TX thrax_soldier_v03 masked 40%",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss
            new(790563, "Thrax Overseer Kuutk", 10504, 44, Rgb(255, 255, 255), "Indra Caverns",
                "set 15 row (MSN 1395); no TaRapedia page (10.8); officer white/grey (TRp), as the Ashen Overseers; Valverde officer value (section 8.2); the name form \"Thrax Overseer X\" is the one of Skeev and Rankash, who have own colours (TW-Ovs), so own colour possible (10.7 q1)",
                "ESTIMATE: value; colour from the rank rule (no image)"),  // Bane_Thrax_Soldier_Rifle_Boss

            // Layer 2: the Thrax soldier family red.
            new(790657, "Thrax Tunneler", 20757, 43, Rgb(110, 8, 0), "Avernus Outpost",
                "set 18 row (MSN 1537); the Thrax soldier class and texture of 531008 (TX thrax_soldier_v03, masked 40%); Thrax red: TRp highest rank red; the 531008 family value; crimson Ligo Thrax L36-40 (YT-LC, YT-FI, section 9.8.2); Ashen Thrax Infantry Captain L42 at White Oasis dark red-brown/crimson (YT-WO, YT-WO2; 10.8); a L43 player's crimson Thrax in the Crucible (YT-SB WLkPcBrGc70); no Tunneler image",
                "ESTIMATE: value; colour from the rank rule and the Ligo family images"),  // Bane_Thrax_Soldier_Pistol_NoBeaminBirth

            // Layer 3: stock colours, no tint.
            new(790043, "Ashen Atta Soldier", 7081, 40, Stock, "Ashen Desert",
                "set 2 row (MSN 1242); the Atta soldier family stock, as 531015 (TX arieki_atta_soldier_v01, mask 21%); no image; \"Ashen\" is a place name (6.4), an own colour is possible (10.7 q4)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard
            new(790044, "Ashen Atta Worker", 7078, 40, Stock, "Ashen Desert",
                "set 2 row (MSN 1242, 1287 \"Ashen Atta Harvesters\"); the Atta harvester family stock, as 531016 (TX arieki_atta_harvester_v01, whole body 100%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Harvester_Standard
            new(790045, "Mutated Atta Soldier", 7081, 41, Stock, "Ashen Desert",
                "set 2 row (MSN 1243); the Atta soldier family stock, as 531015 (TX mask 21%); no image; an own colour is possible (as the green Disturbed Thrax, section 3; 10.7 q4)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard
            new(790646, "Avernus Invader", 7081, 41, Stock, "Avernus Outpost",
                "set 17 row (MSN 1473); the Atta soldier family stock, as 531015 (TX arieki_atta_soldier_v01, mask 21%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard
            new(790662, "Atta Soldier (Enraged Atta)", 7081, 43, Stock, "Conscription Facility",
                "set 20 row (MSN 1551 \"Enraged Atta\"); the client name is \"Atta Soldier\" (name 8094); the Atta soldier family stock, as 531015 (TX mask 21%); no image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard
            new(790360, "Sally", 22627, 44, Stock, "Ashen Desert",
                "set 10 row (MSN 1287 \"the Ashen Atta Soldier called Sally\"); the class and texture of the Atta Imperial Guard 520002 (stock, TX mask 21%); no image (10.8); 10.4 plans stock until an image",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Soldier_Standard_Boss
            new(790664, "Atta Hive Queen", 24087, 49, Stock, "Conscription Facility",
                "set 20 row (MSN 1501, 1551); stock dark red-brown (TX creature_atta_queen_df_v01, whole body 100%); TaRapedia Atta Queen.jpg exists (2007-12-20, not downloaded, 10.8); 10.4 plans stock",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Atta_Queen
            new(790666, "Enslaver Xahn", 11134, 45, Stock, "Conscription Facility",
                "set 20 row (MSN 1499, 1501); stock dark brown-grey armour, red eyes (TX creature_neph_enslaver_v01, whole body 100%); no image (10.8); 10.4 plans stock until an image",
                "ESTIMATE: stock (no image) (Low)"),  // Boss_Enslaver
            new(790080, "Desert Predator", 3902, 42, Stock, "Ashen Desert",
                "set 3 row (MSN 1248; a creature row of its own, so the tint needs its own row, set 3 auditor F3); stock (TX vehicle_bane_predator_v01, whole body 100%); TaRapedia Predator.jpg, Predator-2.jpg exist (2007-08-31, beta, not downloaded; PR1 blue/violet, PR2 pale silver near stock, section 6.2); \"Desert\" is a place name (6.4)",
                "ESTIMATE: stock (no image) (Low)"),  // Vehicle_Bane_Predator
            new(790121, "Class XI Desert Stalker", 3781, 40, Stock, "Ashen Desert",
                "set 4 row (MSN 1272); stock (TX creature_thrax_stalker_head/legs_v01, whole body 100%), as the Stalker rows 540002, 750021; YT3-FW (6.2) shows a mauve \"Stalker\" L38 in the Conscription Facility under purple-red light, not judged a tint (10.7 q5); ST2 Class VIII pale cream",
                "ESTIMATE: stock (no image) (Low)"),  // Vehicle_Bane_Stalker
            new(790240, "Nitroglazer", 7234, 40, Stock, "Ashen Desert",
                "set 7 row (MSN 1264); TW-Nitro: TaRapedia Nitroglazer.jpg (2007-12-20, retail; a Wayback copy of the wiki's 500x500 crop, 10.8): gunmetal/slate shell, red parts, teal lights = the stock texture (TX creature_bane_nitroglazer_df_v01, mask 34%); level and place of the image unknown",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Bane_Nitroglazer
            new(790241, "Hominis Machina Mk XI", 3868, 40, Stock, "Ashen Desert",
                "set 7 row (MSN 1264); stock pale flesh, gunmetal, brown (TX creature_thrax_hominus_v01, mask 29%); no image (TaRapedia Hominis Machina.jpg has no file, 10.8); a Mk rank tint is possible (section 4)",
                "ESTIMATE: stock (no image) (Low)"),  // Bane_Hominis_Machina
            new(790362, "Lasher", 7477, 40, Stock, "Ashen Desert",
                "set 10 row (MSN 1289); the Lasher family stock, as the Crucible Lasher 770008 (TX creature_lasher_df_v01, whole body 100%); TaRapedia Lasher.jpg exists (not downloaded)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Lasher
            new(790367, "Beam Manta Pilot", 6441, 40, Stock, "Ashen Desert",
                "set 10 row (MSN 1291); the Beam Manta family stock, as the Crucible 678030 (TX creature_beam_manta_df_v01, whole body 100%); the boss Plains Devil Lord is \"large reddish\" (TaRapedia), so the normal is not red; TaRapedia Beam Manta*.jpg exist (not downloaded)",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Beam_Manta
            new(790373, "Granitour", 10164, 42, Stock, "Ashen Desert",
                "set 10 row (MSN 1293); new family; stock dark brown-black plates, red-orange flesh (TX creature_granitour_v01_df, whole body 100%, SH PalDiffNormalGlossEnvDecHue); TaRapedia Granitour.jpg exists (2007-12-20, not downloaded), the page has no colour",
                "ESTIMATE: stock (no image) (Low)"),  // Creature_Granitour_Young
            new(790654, "Caretaker Doctor", 9244, 43, Stock, "Avernus Outpost",
                "set 18 row (MSN 1537); the Caretaker family stock, as 531011 and the Crucible 531021 (TW-Caretaker grey-silver, beta; TX mask 25%)",
                "ESTIMATE: stock matches the image (Low-Medium)"),  // Bane_Caretaker
            new(790655, "Fithik Guardian", 4313, 43, Stock, "Avernus Outpost",
                "set 18 row (MSN 1537); the Fithik Guardian value of 540018 and the Crucible 531022 (stock, section 9.3; TX mask 37%)",
                "ESTIMATE: stock (no image) (Low)"),  // Bane_Fithik_Winged
            new(790656, "Mutated Miasma", 6170, 43, Stock, "Avernus Outpost",
                "set 18 row (MSN 1537; the row comment says \"Altered Miasma\", the client name 9037 is \"Mutated Miasma\"); the Miasma family stock, as the Valverde Mutated Miasma 760226 and Miasma 540004 (TX creature_miasma_df_v01, whole body 100%); TaRapedia \"Altered Miasma\": \"a slightly darker appearance\" than regular Miasmas (10.8), but a low tint on a whole-body mask can draw a model black (section 8.2, Oakril), so no darkening tint",
                "ESTIMATE: stock (the darker look not drawn) (Low)"),  // Creature_Miasma

            // Layer 3: the heavy mortar copies of 592011 (sets 3 and 5), no Hue shader (set 3 auditor F3: a tint on 592011 does not reach them).
            new(790081, "Bane Heavy Mortar 1", 7482, 42, Stock, "Ashen Desert", "set 3 row (MSN 1249), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790082, "Bane Heavy Mortar 2", 7482, 42, Stock, "Ashen Desert", "set 3 row (MSN 1249), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790083, "Bane Heavy Mortar 3", 7482, 42, Stock, "Ashen Desert", "set 3 row (MSN 1249), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790084, "Bane Heavy Mortar 4", 7482, 42, Stock, "Ashen Desert", "set 3 row (MSN 1249), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790160, "Bane Heavy Mortar 1", 7482, 42, Stock, "Ashen Desert", "set 5 row (MSN 1352), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790161, "Bane Heavy Mortar 2", 7482, 42, Stock, "Ashen Desert", "set 5 row (MSN 1352), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790162, "Bane Heavy Mortar 3", 7482, 42, Stock, "Ashen Desert", "set 5 row (MSN 1352), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
            new(790163, "Bane Heavy Mortar 4", 7482, 42, Stock, "Ashen Desert", "set 5 row (MSN 1352), a copy of 592011; no Hue shader (SH: PalDiffNormalGlossEnvVertColor), as 592011", NoEffect),
        };

        /// <summary>
        /// The new hostile Ashen creature rows of sets 1-20 that get no row, and why (NOTES-mob-skins.md 10.8). They keep the random tint.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, string> LeftOut = new Dictionary<uint, string>
        {
            [790365] = "Molly (Creature_Lasher_Boss 10331): no image; a boss colour of its own is unknown (the Lasher boss Esoobac is \"purple colored\"), as Kalamit in the Crucible",
            [790371] = "Trudy (Creature_Beam_Manta_Boss 10317): no image; a boss colour of its own is unknown (the manta boss Plains Devil Lord is \"large reddish\")",
            [790376] = "Wendy (Creature_Granitour_Adult_Boss 25620): no image; a boss colour of its own is unknown",
            [790602] = "Overlord Kretas (Bane_Thrax_Soldier_Rifle_Boss 10504): no image, no TaRapedia page; an Overlord, above the Overseers, colour unknown, as Lamontix in the Crucible",
        };

        public static IEnumerable<string> Up() => Rows.Select(row => CrucibleSql.Guard(
            $"INSERT INTO {CreatureTintEntry.TableName} (id, hue, hue2) VALUES ({row.CreatureId}, {Sql(row.Hue)}, {Sql(row.Hue)});"));

        public static IEnumerable<string> Down() => Rows.Select(row =>
            $"DELETE FROM {CreatureTintEntry.TableName} WHERE id = {row.CreatureId} AND " +
            (row.Hue is { } hue ? $"hue = {hue} AND hue2 = {hue};" : "hue IS NULL AND hue2 IS NULL;"));

        private static string Sql(uint? value) => value?.ToString() ?? "NULL";
    }
}

using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// The hospitals' graveyard ids. Two numberings meet at a hospital: the teleporter table's
    /// row id, which is its waypoint id and what Recv_GraveyardGained names it by
    /// (waypointlanguage), and the graveyard id that Recv_PlayerDead's list and ReviveMe carry,
    /// which the hospital window names it by (graveyardlanguage). Nothing in the client joins the
    /// two, so this does, by name: a teleporter's waypointlanguage name or description matched to
    /// the graveyardlanguage name (exact, or near - "Hospital: Fort Haroun" to "Fort Haroun
    /// Hospital Bay"), and by hand where the names are the developers' placeholders
    /// ("PALISADES_ELOHTEMPLES_HOSPITAL_01" to "Temple of the Proud Patriarch"). A hospital not
    /// here is given one of the client's generic names (<see cref="GenericIds"/>) that nothing
    /// else on its map uses; an id is only ever one hospital on any map.
    /// </summary>
    public static class HospitalGraveyards
    {
        /// <summary>Teleporter (waypoint) id to graveyard id.</summary>
        public static readonly Dictionary<uint, uint> ByTeleporter = new Dictionary<uint, uint>
        {
            [93] = 202,         // Hospital: Foreas Base: Foreas Base Hospital
            [94] = 201,         // Hospital: Delta Outpost: Delta Outpost Hospital
            [96] = 200,         // Hospital: Foxtrot Outpost: Foxtrot Outpost Hospital
            [97] = 203,         // Hospital: Thoria Das: Thoria Das Hospital
            [99] = 2,           // Denzil's Caldera Outpost Hospital: Outpost Hospital
            [103] = 3,          // Hospital: Alia Das: Alia Das Hospital
            [104] = 32,         // Hospital: Wilderness LZ: AFS Drop Zone
            [105] = 6,          // Hospital: Twin Pillars: Twin Pillars Hospital
            [106] = 5,          // Hospital: Ranja Gorge: Ranja Gorge Hospital
            [108] = 20,         // Hospital: Daghda's: Daghda's Urn Hospital
            [112] = 219,        // Hospital: Cumbria Research: Cumbria Hospital
            [114] = 217,        // Hospital: Staging Point: Staging Point Hospital
            [115] = 223,        // Hospital: Treeback Ridge: Treeback Ridge Hospital
            [123] = 44,         // Hospital: Falcon Hold Medic Tent: Falcon Hold Medic Tent
            [124] = 45,         // Hospital: Paludos: Paludos Hospital
            [126] = 114,        // Hospital: Retread City Medic: Retread Medical
            [127] = 50,         // Hospital: Fort Defiance: Fort Defiance Hospital
            [129] = 65,         // Hospital: Wedge Rock: Wedge Rock Hospital
            [130] = 48,         // Hospital: Warnet Caverns Entrance: Warnet Caverns Entrance
            [131] = 49,         // Hospital: Warnet Caverns Research Area: Research Area
            [132] = 64,         // Hospital: Camp Resistance: Camp Resistance Hospital
            [145] = 61,         // Hospital: Plains Post Medical Tent: Plains Post Medical Tent
            [146] = 63,         // Hospital: Nyxroq Post Med Tent: Nyxroq Post Med Tent
            [152] = 90,         // Hospital: Eir Crater Field: Eir Crater Field Hospital
            [153] = 89,         // Hospital: Irendas Colony: Irendas Colony Hospital
            [154] = 91,         // Hospital: Mt. Hellas Outpost Hospital: Mt. Hellas Outpost Hospital
            [157] = 68,         // Hospital: AFS Medical Center in the Snake Pit: Snake Pit Med Center
            [168] = 73,         // Hhospital: Eastern Pools Listening Post First Aid Station: East Listening Post
            [170] = 74,         // Hospital: Retread Camp First Aid Station: Retread First Aid
            [172] = 196,        // Hospital: Viddia Syndicate Camp Hospital: Viddia Syndicate Camp Hospital
            [173] = 75,         // Hospital: Thunderhead Base Hospital: Thunderhead Base Hospital
            [177] = 83,         // Hospital: Substation E-104 Field Hospital: Substation E-104 Field Hospital
            [183] = 170,        // Hospital: Fort Virgil: Fort Virgil Hospital
            [184] = 118,        // Hospital: Camp Cato Medic Post: Camp Cato Medic Post
            [185] = 169,        // Hospital: Fortuna Village Field Hospital: Fortuna Field Hospital
            [205] = 2,          // Hospital: Outpost Intrepid: Outpost Hospital
            [207] = 102,        // Hospital: Ashoka Settlement: Ashoka Settlement Hospital
            [208] = 101,        // Hospital: Shadow's Edge Post: Shadow's Edge Hospital
            [210] = 172,        // Hospital: Antaeus Hollow Medic Station: Antaeus Hollow Medic Station
            [218] = 110,        // Hospital: Imperial Valley (Control Point): Imperial Valley Hospital (Control Point)
            [220] = 240,        // Hospital: Hydro Plant (Control Point): Hospital: Hydro Plant (Control Point)
            [222] = 241,        // Hospital: Purgas (Control Point): Hospital: Purgas (Control Point)
            [224] = 220,        // Hospital: River-base Krymm (Control Point): Hospital: River-base Krymm (Control Point)\r\n
            [226] = 221,        // Hospital: Fort Dew (Control Point): Hospital: Fort Dew (Control Point)
            [228] = 129,        // Hospital: Irendas Support Facility (CP): Hospital: Irendas Support Facility (Control Point)
            [231] = 124,        // Hospital: Geyser Chimney Basin (CP): Hospital: Geyser Chimney Basin (Control Point)\r\n
            [232] = 127,        // Hospital: Lightning Fields (CP): Hospital: Lightning Fields (Control Point)
            [234] = 131,        // Hospital: Badlands (Control Point): Hospital: Badlands (Control Point)
            [236] = 137,        // Hospital: Ortho (Control Point): Hospital: Ortho (Control Point)
            [240] = 121,        // Hospital: Orsa (Control Point): Hospital: Orsa (Control Point)
            [246] = 211,        // Hospital: Healing Shaman: Healing Shaman
            [271] = 130,        // Hospital: Junkyard First Aid Station: Junkyard First Aid
            [277] = 162,        // Hospital: Charon's Crossing (Control Point): Hospital: Charon's Crossing (Control Point)
            [279] = 160,        // Hospital: Dybukkar Forward Camp (Control Point): Hospital: Dybukkar Forward Camp (Control Point)
            [281] = 144,        // Hospital: Northeast AFS (Control Point): Hospital: Northeast AFS (Control Point)
            [284] = 145,        // Hospital: Northwest AFS (Control Point): Hospital: Northwest AFS (Control Point)
            [286] = 146,        // Hospital: Southeast Bane (Control Point): Hospital: Southeast Bane (Control Point)
            [288] = 147,        // Hospital: Southwest Bane (Control Point): Hospital: Southwest Bane (Control Point)
            [290] = 148,        // Hospital: Retread Outpost (Control Point): Hospital: Retread Outpost (Control Point)
            [292] = 149,        // Hospital: Bane Comm Center (Control Point): Hospital: Bane Comm Center (Control Point)
            [294] = 150,        // Hospital: Bane Guard Station (Control Point): Hospital: Bane Guard Station (Control Point)
            [296] = 156,        // Hospital: Research Point (Control Point): Hospital: Research Point (Control Point)
            [298] = 158,        // Hospital: Bane Assertion Camp (Control Point): Hospital: Bane Assertion Camp (Control Point)
            [300] = 179,        // Hospital: Virgil's Resonator (Control Point): Hospital: Virgil's Resonator (Control Point)
            [302] = 180,        // Hospital: Mal Dys Upper Resonator (Control Point): Hospital: Mal Dys Upper Resonator (Control Point)
            [304] = 181,        // Hospital: Mal Dys Lower Resonator (Control Point): Hospital: Mal Dys Lower Resonator (Control Point)
            [308] = 184,        // Hospital: Cuthah Scout Post (Control Point): Hospital: Cuthah Scout Post (Control Point)
            [310] = 191,        // Hospital: Dead Zone Power Station (Control Point): Hospital: Dead Zone Power Station (Control Point)
            [312] = 192,        // Hospital: Cuthah Ammo Depot (Control Point): Hospital: Cuthah Ammo Depot (Control Point)
            [314] = 159,        // Hospital: Fort Intrepid (Control Point): Hospital: Fort Intrepid (Control Point)
            [316] = 161,        // Hospital: Prometheus Outpost (Control Point): Hospital: Prometheus Outpost (Control Point)
            [318] = 166,        // Hospital: White Oasis Post (Control Point): Hospital: White Oasis Post (Control Point)
            [322] = 163,        // Hospital: Conscription Garrison (Control Point): Hospital: Conscription Garrison (Control Point)\r\n
            [324] = 167,        // Hospital: Western Rim (Control Point): Hospital: Western Rim (Control Point)
            [326] = 168,        // Hospital: Eastern Rim (Control Point): Hospital: Eastern Rim (Control Point)
            [328] = 185,        // Hospital: Outpost Aurora (Control Point): Hospital: Outpost Aurora (Control Point)
            [330] = 186,        // Hospital: Fault Lever (Control Point): Hospital: Fault Lever (Control Point)
            [344] = 151,        // Quasso Station Entrance: Quasso Station Entrance
            [345] = 152,        // Ojasa Hive Entrance: Ojasa Atta Hive Entrance
            [351] = 153,        // Live Target Pens Entrance: LIve Target Pens Entrance
            [352] = 57,         // Hospital: Guard station: Bane Guard Post
            [360] = 239,        // Hospital: Level 01: Entrance Hall: Level 01: Entrance Hall
            [364] = 164,        // Hospital: Chaukas Entrance: Chaukas Entrance
            [365] = 165,        // Gangus Outpost Hospital: Gangus Outpost Hospital
            [371] = 187,        // Hospital: Tantalus Base: Tantalus Base Infirmary
            [374] = 189,        // Hospital: Tampeii Settlement Medical Station: Tampeii Settlement Healer
            [377] = 190,        // Hospital: Archeron Outpost Infirmary: Archeron Post Medical Tent
            [378] = 188,        // Hospital: Icarus Post: Icarus Post Medical Tent
            [383] = 125,        // Hospital: Kardash Atta Colony: Medical Station: Kardash Atta Colony
            [387] = 86,         // Hospital: Temporal Chamber: Temporal Chamber
            [388] = 41,         // Hospital: Devil's Den: Devil's Den Entrance
            [394] = 141,        // Hospital: Outpost Condor: Fort Condor Hospital Tent
            [395] = 126,        // Hospital: Fort Haroun: Fort Haroun Hospital Bay
            [396] = 128,        // Hospital: Baylor Base: Baylor Base Hospital Bay
            [397] = 38,         // Field Hospital: Timora Mines: Field Medic, Mine Entrance
            [398] = 198,        // Hospital: Level 01: Entrance: Level 01: Entrance
            [399] = 199,        // Hospital: Level 02: Barricade: Level 02: Barricade
            [404] = 276,        // Hospital for Team Blue: Blue Base Hospital
            [405] = 275,        // Hospital for Team Red: Red Base Hospital
            [409] = 213,        // Hospital: Dia Uyona: Hospital: Dia Uyona
            [414] = 214,        // Hospital: Velon Hollow: Hospital: Velon Hollow
            [426] = 193,        // Hospital: Comm Tower Entrance: Entrance
            [429] = 193,        // Hospital: Incurables Ward Entrance: Entrance
            [430] = 193,        // Hospital: Velon Hollow Entrance: Entrance
            [431] = 210,        // Hospital: Fault Lever Entrance: Fault Lever Entrance
            [433] = 193,        // Hospital: Junkyard Entrance: Entrance
            [442] = 193,        // Hospital: Bane Conscription Facility Entrance: Entrance
            [446] = 193,        // Hospital: Fluxite Mines Entrance: Entrance
            [450] = 193,        // Hospital: Logos Research Facility Entrance: Entrance
            [456] = 193,        // Hospital: Torcastra Prison Entrance: Entrance
            [458] = 230,        // Hospital: Omega Entrance Medic Post: Entrance Medic Post
            [459] = 231,        // Hospital: Omega Network Operations Medic Unit: Network Operations Medic Unit
            [460] = 39,         // Hospital: Minos Caverns Field Medic: Minos Caverns Field Medic
            [479] = 193,        // Hospital: Eloh Vale Entrance: Entrance
            [480] = 243,        // Hospital: CELLAR Arena Medic: A.F.S. Challenge Arena Medic
            [485] = 252,        // Hospital: Field Medic: Rebel Camp: Field Medic: Rebel Camp
            [499] = 261,        // Hospital: Dybukkar Medical Team: Dybukkar Medical Team
            [516] = 268,        // Hospital: Raksha Robotics (Control Point): Hospital: Raksha Robotics (Control Point)
            [517] = 270,        // Hospital: Medic: 23rd Street Station: Medic: 23rd Street Station
            [518] = 271,        // Hospital: A.F.S. Outpost Lexington: Hospital: A.F.S. Outpost Lexington
            [519] = 272,        // Hospital: Field Medic: Empire Sector: Field Medic: Empire Sector
            [526] = 273,        // Hospital: Control Point Echo: West Control Point Hospital
            [529] = 193,        // Hospital: Epic Caves of Donn Entrance: Entrance
            [533] = 274,        // Hospital: Control Point Whiskey: East Control Point Hospital
            [535] = 193,        // Hospital: Entrance Gauntlet: Entrance
            [577] = 10000001,   // Control Point Eastern Rim: CP Hospital
            [579] = 232,        // Hospital: Turpis Refinery: Turpis Medical Tent
            [580] = 193,        // Hospital: Live Target Pens Hospital Entrance: Entrance
            [581] = 194,        // Hospital: MARSHES_BSD_HOSPITAL_SURVIVORS: Survivor's Camp
            [586] = 46,         // Hospital: RETREAD_CAVES_HOSPITAL: Retread Camp Hospital
            [591] = 154,        // Hospital: LTP_GUARDSTATION_HOSPITAL: Live Target Pens Guard Station
            [592] = 80,         // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_01: Temple of the Proud Patriarch
            [593] = 81,         // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_02: Temple of the Raging Patriarch
            [594] = 82,         // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_03: Temple of Bowed Patriarch
            [596] = 251,        // Hospital: MAGMA_PENUMBRA_HOSPITAL: Field Medic: Penumbra Landing Zone
            [598] = 20000001,   // Hospital: BOOTCAMP_REFUGEE_HOSPITAL: Refugee Base Medic
            [599] = 104,        // Hospital: CRUCIBLE_HOSPITAL_AWOLCAMP: Awol Camp Hospital
            [601] = 279,        // Hospital: ELOH_VALE_HOSPITAL_PYRAMID: Hospital: Forean Pyramid
            [606] = 218,        // Hospital: MIRES_TAHRENDRA_HOSPITAL: Tahrendra Base Field Medic
            [609] = 135,        // Hospital: CUTHAH_HOSPITAL_RATNEST: Cuthah Base Entrance Field Medic
            [610] = 236,        // Hospital: CUTHAH_HOSPITAL_TOP: AFS Field Medic
            [611] = 209,        // Hospital: HOWLINGMAWOP_CUTHAH_SUBLEVELMEDIC: Sublevel Medic Station
        };

        /// <summary>
        /// Graveyard ids whose name says nothing of where they are, for a hospital with no name
        /// of its own in the client: "AFS Field Medic" ten times over, then "Medic Station",
        /// "Medical Station" and "Field Medic".
        /// </summary>
        public static readonly uint[] GenericIds = { 36, 42, 52, 95, 111, 132, 142, 195, 215, 236, 53, 97, 237, 112, 178, 100 };

        /// <summary>
        /// The last waypoint id the client has a name for (waypointlanguage), short of its test
        /// ids from <see cref="FirstClientTestWaypoint"/>. The teleporter rows after it are ours:
        /// the client prints "ERROR- 23- Missing translation for waypointlanguage ID 598" in
        /// place of the name of one of those.
        /// </summary>
        public const uint LastClientWaypoint = 533;

        public const uint FirstClientTestWaypoint = 10000000;

        /// <summary>"AFS Field Medic" in waypointlanguage: what a hospital the client has no name for is gained as.</summary>
        public const uint GenericWaypoint = 120;

        /// <summary>
        /// Hospitals placed under an id of ours whose name the client has under one of its own:
        /// teleporter id to the waypointlanguage id of that name. The client's id is a row of the
        /// teleporter table with a name and no place ("Refugee Base Medic", 500). Matched by the
        /// name the hospital window gives it (<see cref="ByTeleporter"/>, graveyardlanguage), which
        /// is the same name in each case but the last, where the camp's waypoint is the nearest
        /// there is.
        /// </summary>
        public static readonly Dictionary<uint, uint> WaypointNames = new Dictionary<uint, uint>
        {
            [581] = 385,        // Hospital: MARSHES_BSD_HOSPITAL_SURVIVORS: Survivor's Camp
            [592] = 176,        // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_01: Temple of the Proud Patriarch
            [593] = 342,        // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_02: Temple of the Raging Patriarch
            [594] = 175,        // Hospital: PALISADES_ELOHTEMPLES_HOSPITAL_03: Temple of the Bowed Patriarch
            [596] = 486,        // Hospital: MAGMA_PENUMBRA_HOSPITAL: Field Medic: Penumbra Landing Zone
            [598] = 500,        // Hospital: BOOTCAMP_REFUGEE_HOSPITAL: Refugee Base Medic
            [601] = 171,        // Hospital: ELOH_VALE_HOSPITAL_PYRAMID: Hospital: Forean Pyramid
            [609] = 274,        // Hospital: CUTHAH_HOSPITAL_RATNEST: Cuthah Base Entrance Field Medic
            [610] = 272,        // Hospital: CUTHAH_HOSPITAL_TOP: AFS Field Medic
            [611] = 407,        // Hospital: HOWLINGMAWOP_CUTHAH_SUBLEVELMEDIC: Sublevel Medic Station
            [599] = 192,        // Hospital: CRUCIBLE_HOSPITAL_AWOLCAMP: Awol Camp (the hospital window: Awol Camp Hospital)
        };

        /// <summary>Whether the client has a name for this waypoint id.</summary>
        public static bool ClientNames(uint waypointId)
        {
            return waypointId <= LastClientWaypoint || waypointId >= FirstClientTestWaypoint;
        }

        /// <summary>
        /// The waypoint id GraveyardGained names a hospital by: its own where the client has a
        /// name for it, the id its name is under (<see cref="WaypointNames"/>), or else
        /// <see cref="GenericWaypoint"/>.
        /// </summary>
        public static uint GainedAs(uint teleporterId)
        {
            if (WaypointNames.TryGetValue(teleporterId, out var named))
                return named;

            return ClientNames(teleporterId) ? teleporterId : GenericWaypoint;
        }
    }
}

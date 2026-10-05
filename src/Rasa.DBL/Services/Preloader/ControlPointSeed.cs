using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The control points of the fifteen open zones and what belongs to each (Add_control_points):
    /// the rows of control_point and control_point_link, inserted as SQL.
    ///
    /// A point's id, name and position are the client's: one row per CONTROL_POINT marker in its
    /// uimapmarker table, numbered in map and marker order. The Landing Zone's is where the
    /// server's one test point stood. The Descent's five are Phi Resonators (class 26486); the
    /// rest are the Eloh control point (3814).
    ///
    /// The links are reconstructed from what was already seeded around each marker:
    ///  - the Bane's garrison (kind 1): the spawn pools Spawn_pools_off_safe_ground set to mode 1,
    ///    each to the nearest point within 130 m;
    ///  - the AFS's garrison (kind 2): the pools of friendly creatures named for the point or
    ///    labelled "(Control Point)" within 130 m, and any other friendly pool within 60 m;
    ///  - its hospital and waypoint (kinds 3 and 4): the teleporter rows found the same way;
    ///  - its bosses (kind 5): the Thrax Commander of a garrison that has one.
    ///
    /// The Bane start with the six points the client names for them - Bane Comm Center, Bane
    /// Guard Station, Bane Assertion Camp, Southeast Bane, Southwest Bane and Bane Conscription
    /// Garrison - and the AFS with the rest, as the world stood before there was ownership.
    /// </summary>
    public static class ControlPointSeed
    {
        public const uint FirstId = 1;
        public const uint LastId = 41;

        private static readonly string[] PointColumns =
        {
            "id", "map_context_id", "name", "class_id", "pos_x", "pos_y", "pos_z", "rotation", "marker_entity_id", "default_owner"
        };

        private static readonly string[] LinkColumns = { "control_point_id", "kind", "object_id" };

        public static readonly object[][] Points =
        {
            new object[] { 1u, 1148u, "Purgas", 3814u, -665.53, 123.428, -629.322, 0.0, 132770324754926UL, (byte)1 },
            new object[] { 2u, 1148u, "Hydro Plant", 3814u, -243.5, 57.992, 43.5, 0.0, 132770324755375UL, (byte)1 },
            new object[] { 3u, 1220u, "Imperial Valley", 3814u, -357.084, 174.609, -528.608, 0.0, 133079561961325UL, (byte)1 },
            new object[] { 4u, 1220u, "Landing Zone", 3814u, 197.66, 162.27, -54.08, 3.05, 133079561962676UL, (byte)1 },
            new object[] { 5u, 1244u, "River-base Krimm", 3814u, 215.978, 111.01, 341.967, 0.0, 133182640964378UL, (byte)1 },
            new object[] { 6u, 1244u, "Fort Dew", 3814u, -183.362, 169.001, -720.211, 0.0, 133182640964479UL, (byte)1 },
            new object[] { 7u, 1304u, "Retread Outpost", 3814u, 201.46, 867.5, 332.048, 0.0, 133436044038285UL, (byte)1 },
            new object[] { 8u, 1304u, "Bane Comm Center", 3814u, -460.235, 680.438, -404.708, 0.0, 133436044038377UL, (byte)0 },
            new object[] { 9u, 1304u, "Bane Guard Station", 3814u, 558.634, 670.116, -104.631, 0.0, 133436044038474UL, (byte)0 },
            new object[] { 10u, 1454u, "Research Point", 3814u, -321.1, 217.066, -312.847, 0.0, 134084584059990UL, (byte)1 },
            new object[] { 11u, 1454u, "Bane Assertion Camp", 3814u, 104.669, 219.084, -325.566, 0.0, 134084584060091UL, (byte)0 },
            new object[] { 12u, 1497u, "Northeast AFS", 3814u, 115.311, 370.844, 418.15, 0.0, 134269267734376UL, (byte)1 },
            new object[] { 13u, 1497u, "Northwest AFS", 3814u, -384.718, 375.768, 430.858, 0.0, 134269267734472UL, (byte)1 },
            new object[] { 14u, 1497u, "Southeast Bane", 3814u, 92.025, 370.841, -132.706, 0.0, 134269267734567UL, (byte)0 },
            new object[] { 15u, 1497u, "Southwest Bane", 3814u, -328.112, 375.702, -105.556, 0.0, 134269267734661UL, (byte)0 },
            new object[] { 16u, 1734u, "Bane Conscription Garrison", 3814u, 429.175, 273.099, -59.088, 0.0, 134419591472985UL, (byte)0 },
            new object[] { 17u, 1734u, "White Oasis Post", 3814u, -511.082, 283.43, 164.351, 0.0, 134419591473154UL, (byte)1 },
            new object[] { 18u, 1759u, "Orsa", 3814u, -191.938, 207.409, -21.564, 0.0, 134419591474017UL, (byte)1 },
            new object[] { 19u, 1759u, "Iapyx", 3814u, 571.207, 263.025, -423.268, 0.0, 134419591474135UL, (byte)1 },
            new object[] { 20u, 1761u, "Badlands", 3814u, 392.787, 279.6, 373.026, 0.0, 134419591485602UL, (byte)1 },
            new object[] { 21u, 1761u, "Ortho", 3814u, 391.689, 260.412, 33.037, 0.0, 134419591485684UL, (byte)1 },
            new object[] { 22u, 1764u, "Geyser Chimney Basin", 3814u, 182.486, 433.596, 249.111, 0.0, 134419591472554UL, (byte)1 },
            new object[] { 23u, 1764u, "Lightning Fields", 3814u, -135.449, 432.294, 516.976, 0.0, 134419591472699UL, (byte)1 },
            new object[] { 24u, 1764u, "Irendas Support Facility", 3814u, 786.273, 430.875, 587.662, 0.0, 134419591472821UL, (byte)1 },
            new object[] { 25u, 1764u, "Abyssal Zone", 3814u, 38.018, 419.0, -466.059, 0.0, 134419591477458UL, (byte)1 },
            new object[] { 26u, 1911u, "Western Rim", 3814u, -529.476, 384.633, 249.935, 0.0, 134419591472423UL, (byte)1 },
            new object[] { 27u, 1911u, "Eastern Rim", 3814u, 677.953, 370.565, 201.213, 0.0, 134419591472521UL, (byte)1 },
            new object[] { 28u, 1911u, "Outpost Aurora", 3814u, 735.462, 384.425, -207.972, 0.0, 134419591473988UL, (byte)1 },
            new object[] { 29u, 1911u, "Fault Lever", 3814u, 1.63, 324.48, -65.413, 0.0, 134419591474105UL, (byte)1 },
            new object[] { 30u, 1993u, "Fort Intrepid", 3814u, 757.066, 171.121, 549.128, 0.0, 134419591475317UL, (byte)1 },
            new object[] { 31u, 1993u, "Prometheus Outpost", 3814u, 400.142, 170.353, 387.916, 0.0, 134419591475414UL, (byte)1 },
            new object[] { 32u, 2028u, "Dybukkar Forward Camp", 3814u, 181.27, 558.685, 462.914, 0.0, 134419591469558UL, (byte)1 },
            new object[] { 33u, 2028u, "Charon's Crossing", 3814u, -37.081, 534.826, -144.379, 0.0, 134419591469783UL, (byte)1 },
            new object[] { 34u, 2047u, "Phi Resonator 2", 26486u, 123.0, 609.899, 712.5, 0.0, 134419591466213UL, (byte)1 },
            new object[] { 35u, 2047u, "Virgil's Resonator", 26486u, -531.032, 727.866, 68.211, 0.0, 134419591468428UL, (byte)1 },
            new object[] { 36u, 2047u, "Mal Dys Upper Resonator", 26486u, 524.054, 472.695, 316.935, 0.0, 134419591468498UL, (byte)1 },
            new object[] { 37u, 2047u, "Mal Dys Lower Resonator", 26486u, 275.48, 337.757, 68.259, 0.0, 134419591468785UL, (byte)1 },
            new object[] { 38u, 2047u, "Drill Resonator", 26486u, 118.221, 244.74, 222.15, 0.0, 134419591468853UL, (byte)1 },
            new object[] { 39u, 2051u, "Cuthah Scout Post", 3814u, -1251.885, 256.61, -394.525, 0.0, 134419591475364UL, (byte)1 },
            new object[] { 40u, 2051u, "Dead Zone Power Station", 3814u, -479.658, 200.174, -364.655, 0.0, 134419591475767UL, (byte)1 },
            new object[] { 41u, 2051u, "Cuthah Ammo Depot", 3814u, 1282.52, 242.359, 316.12, 0.0, 134419591475868UL, (byte)1 },
        };

        /// <summary>(control_point_id, kind, object_id), by point.</summary>
        public static readonly object[][] Links =
        {
            // 1 Purgas
            new object[] { 1u, (byte)1, 530001u }, new object[] { 1u, (byte)1, 530027u }, new object[] { 1u, (byte)2, 500144u }, new object[] { 1u, (byte)2, 500157u },
            new object[] { 1u, (byte)3, 222u }, new object[] { 1u, (byte)4, 221u }, new object[] { 1u, (byte)5, 530005u },
            // 2 Hydro Plant
            new object[] { 2u, (byte)2, 500143u }, new object[] { 2u, (byte)2, 500150u }, new object[] { 2u, (byte)2, 500154u }, new object[] { 2u, (byte)2, 500156u },
            new object[] { 2u, (byte)2, 500161u }, new object[] { 2u, (byte)2, 510113u }, new object[] { 2u, (byte)2, 510114u }, new object[] { 2u, (byte)3, 220u },
            new object[] { 2u, (byte)4, 219u },
            // 3 Imperial Valley
            new object[] { 3u, (byte)1, 580001u }, new object[] { 3u, (byte)2, 32u }, new object[] { 3u, (byte)2, 84u }, new object[] { 3u, (byte)2, 95u },
            new object[] { 3u, (byte)2, 201u }, new object[] { 3u, (byte)2, 500003u }, new object[] { 3u, (byte)3, 218u }, new object[] { 3u, (byte)4, 156u },
            // 4 Landing Zone
            new object[] { 4u, (byte)2, 33u }, new object[] { 4u, (byte)2, 106u }, new object[] { 4u, (byte)2, 107u }, new object[] { 4u, (byte)2, 113u },
            new object[] { 4u, (byte)2, 114u }, new object[] { 4u, (byte)2, 117u }, new object[] { 4u, (byte)2, 120u }, new object[] { 4u, (byte)2, 172u },
            new object[] { 4u, (byte)2, 181u }, new object[] { 4u, (byte)2, 182u }, new object[] { 4u, (byte)2, 188u }, new object[] { 4u, (byte)2, 208u },
            new object[] { 4u, (byte)3, 104u },
            // 5 River-base Krimm
            new object[] { 5u, (byte)1, 531462u }, new object[] { 5u, (byte)1, 531463u }, new object[] { 5u, (byte)1, 531466u }, new object[] { 5u, (byte)1, 531469u },
            new object[] { 5u, (byte)1, 531470u }, new object[] { 5u, (byte)1, 531472u }, new object[] { 5u, (byte)1, 531477u }, new object[] { 5u, (byte)1, 531478u },
            new object[] { 5u, (byte)2, 500177u }, new object[] { 5u, (byte)2, 500186u }, new object[] { 5u, (byte)2, 510129u }, new object[] { 5u, (byte)2, 510134u },
            new object[] { 5u, (byte)2, 510135u }, new object[] { 5u, (byte)3, 224u }, new object[] { 5u, (byte)4, 223u },
            // 6 Fort Dew
            new object[] { 6u, (byte)2, 500176u }, new object[] { 6u, (byte)2, 500184u }, new object[] { 6u, (byte)2, 510122u }, new object[] { 6u, (byte)2, 510130u },
            new object[] { 6u, (byte)3, 226u }, new object[] { 6u, (byte)4, 225u },
            // 7 Retread Outpost
            new object[] { 7u, (byte)2, 500297u }, new object[] { 7u, (byte)2, 500306u }, new object[] { 7u, (byte)3, 290u }, new object[] { 7u, (byte)4, 289u },
            // 8 Bane Comm Center
            new object[] { 8u, (byte)1, 531661u }, new object[] { 8u, (byte)2, 500295u }, new object[] { 8u, (byte)2, 500304u }, new object[] { 8u, (byte)3, 292u },
            new object[] { 8u, (byte)4, 291u }, new object[] { 8u, (byte)5, 531093u },
            // 9 Bane Guard Station
            new object[] { 9u, (byte)1, 531664u }, new object[] { 9u, (byte)2, 500296u }, new object[] { 9u, (byte)2, 500305u }, new object[] { 9u, (byte)2, 500308u },
            new object[] { 9u, (byte)3, 294u }, new object[] { 9u, (byte)4, 293u },
            // 10 Research Point
            new object[] { 10u, (byte)2, 500246u }, new object[] { 10u, (byte)2, 500251u }, new object[] { 10u, (byte)2, 500255u }, new object[] { 10u, (byte)2, 500258u },
            new object[] { 10u, (byte)3, 296u }, new object[] { 10u, (byte)4, 295u },
            // 11 Bane Assertion Camp
            new object[] { 11u, (byte)1, 531284u }, new object[] { 11u, (byte)1, 531287u }, new object[] { 11u, (byte)2, 500244u }, new object[] { 11u, (byte)2, 500254u },
            new object[] { 11u, (byte)3, 298u }, new object[] { 11u, (byte)4, 297u },
            // 12 Northeast AFS
            new object[] { 12u, (byte)2, 500269u }, new object[] { 12u, (byte)2, 500279u }, new object[] { 12u, (byte)3, 281u }, new object[] { 12u, (byte)4, 280u },
            // 13 Northwest AFS
            new object[] { 13u, (byte)2, 500270u }, new object[] { 13u, (byte)2, 500280u }, new object[] { 13u, (byte)2, 500286u }, new object[] { 13u, (byte)3, 284u },
            new object[] { 13u, (byte)4, 282u },
            // 14 Southeast Bane
            new object[] { 14u, (byte)1, 531608u }, new object[] { 14u, (byte)2, 500271u }, new object[] { 14u, (byte)2, 500281u }, new object[] { 14u, (byte)3, 286u },
            new object[] { 14u, (byte)4, 285u },
            // 15 Southwest Bane
            new object[] { 15u, (byte)1, 531609u }, new object[] { 15u, (byte)2, 500272u }, new object[] { 15u, (byte)2, 500282u }, new object[] { 15u, (byte)3, 288u },
            new object[] { 15u, (byte)4, 287u },
            // 16 Bane Conscription Garrison
            new object[] { 16u, (byte)1, 531033u }, new object[] { 16u, (byte)2, 500010u }, new object[] { 16u, (byte)2, 500015u }, new object[] { 16u, (byte)3, 322u },
            new object[] { 16u, (byte)4, 319u },
            // 17 White Oasis Post
            new object[] { 17u, (byte)2, 500011u }, new object[] { 17u, (byte)2, 500016u }, new object[] { 17u, (byte)2, 510007u }, new object[] { 17u, (byte)2, 510012u },
            new object[] { 17u, (byte)3, 318u }, new object[] { 17u, (byte)4, 317u },
            // 18 Orsa
            new object[] { 18u, (byte)1, 531381u }, new object[] { 18u, (byte)1, 531385u }, new object[] { 18u, (byte)1, 531395u }, new object[] { 18u, (byte)2, 500091u },
            new object[] { 18u, (byte)2, 500099u }, new object[] { 18u, (byte)3, 240u }, new object[] { 18u, (byte)4, 239u },
            // 19 Iapyx
            new object[] { 19u, (byte)2, 500090u }, new object[] { 19u, (byte)2, 500098u }, new object[] { 19u, (byte)2, 500100u }, new object[] { 19u, (byte)4, 237u },
            // 20 Badlands
            new object[] { 20u, (byte)1, 531235u }, new object[] { 20u, (byte)1, 531238u }, new object[] { 20u, (byte)1, 531247u }, new object[] { 20u, (byte)2, 500077u },
            new object[] { 20u, (byte)2, 500081u }, new object[] { 20u, (byte)3, 234u }, new object[] { 20u, (byte)4, 233u },
            // 21 Ortho
            new object[] { 21u, (byte)2, 500079u }, new object[] { 21u, (byte)2, 500082u }, new object[] { 21u, (byte)2, 500083u }, new object[] { 21u, (byte)2, 510063u },
            new object[] { 21u, (byte)2, 510065u }, new object[] { 21u, (byte)2, 510070u }, new object[] { 21u, (byte)2, 510072u }, new object[] { 21u, (byte)2, 510073u },
            new object[] { 21u, (byte)2, 510074u }, new object[] { 21u, (byte)2, 510077u }, new object[] { 21u, (byte)3, 236u }, new object[] { 21u, (byte)4, 235u },
            // 22 Geyser Chimney Basin
            new object[] { 22u, (byte)2, 500108u }, new object[] { 22u, (byte)2, 500117u }, new object[] { 22u, (byte)3, 231u }, new object[] { 22u, (byte)4, 229u },
            // 23 Lightning Fields
            new object[] { 23u, (byte)2, 500110u }, new object[] { 23u, (byte)2, 500119u }, new object[] { 23u, (byte)3, 232u }, new object[] { 23u, (byte)4, 230u },
            // 24 Irendas Support Facility
            new object[] { 24u, (byte)1, 531543u }, new object[] { 24u, (byte)2, 500109u }, new object[] { 24u, (byte)2, 500118u }, new object[] { 24u, (byte)3, 228u },
            new object[] { 24u, (byte)4, 227u }, new object[] { 24u, (byte)5, 531078u },
            // 26 Western Rim
            new object[] { 26u, (byte)1, 531695u }, new object[] { 26u, (byte)1, 531717u }, new object[] { 26u, (byte)2, 500043u }, new object[] { 26u, (byte)2, 500055u },
            new object[] { 26u, (byte)3, 324u }, new object[] { 26u, (byte)4, 323u },
            // 27 Eastern Rim
            new object[] { 27u, (byte)1, 531700u }, new object[] { 27u, (byte)1, 531716u }, new object[] { 27u, (byte)1, 531720u }, new object[] { 27u, (byte)1, 531726u },
            new object[] { 27u, (byte)2, 500040u }, new object[] { 27u, (byte)2, 500052u }, new object[] { 27u, (byte)2, 510043u }, new object[] { 27u, (byte)3, 326u },
            new object[] { 27u, (byte)3, 577u },
            // 28 Outpost Aurora
            new object[] { 28u, (byte)2, 500042u }, new object[] { 28u, (byte)2, 500050u }, new object[] { 28u, (byte)2, 500054u }, new object[] { 28u, (byte)3, 328u },
            new object[] { 28u, (byte)4, 327u },
            // 29 Fault Lever
            new object[] { 29u, (byte)1, 531714u }, new object[] { 29u, (byte)2, 500041u }, new object[] { 29u, (byte)2, 500053u }, new object[] { 29u, (byte)3, 330u },
            new object[] { 29u, (byte)4, 329u },
            // 30 Fort Intrepid
            new object[] { 30u, (byte)1, 531098u }, new object[] { 30u, (byte)2, 500024u }, new object[] { 30u, (byte)2, 500028u }, new object[] { 30u, (byte)3, 314u },
            new object[] { 30u, (byte)4, 313u },
            // 31 Prometheus Outpost
            new object[] { 31u, (byte)2, 500025u }, new object[] { 31u, (byte)2, 500027u }, new object[] { 31u, (byte)2, 500029u }, new object[] { 31u, (byte)2, 500031u },
            new object[] { 31u, (byte)2, 510021u }, new object[] { 31u, (byte)2, 510029u }, new object[] { 31u, (byte)2, 510031u }, new object[] { 31u, (byte)3, 316u },
            new object[] { 31u, (byte)4, 315u },
            // 32 Dybukkar Forward Camp
            new object[] { 32u, (byte)1, 531003u }, new object[] { 32u, (byte)1, 531004u }, new object[] { 32u, (byte)2, 500063u }, new object[] { 32u, (byte)2, 500073u },
            new object[] { 32u, (byte)3, 279u }, new object[] { 32u, (byte)4, 278u },
            // 33 Charon's Crossing
            new object[] { 33u, (byte)2, 500062u }, new object[] { 33u, (byte)2, 500072u }, new object[] { 33u, (byte)2, 510051u }, new object[] { 33u, (byte)3, 277u },
            new object[] { 33u, (byte)4, 276u },
            // 35 Virgil's Resonator
            new object[] { 35u, (byte)1, 531164u }, new object[] { 35u, (byte)2, 500228u }, new object[] { 35u, (byte)2, 500235u }, new object[] { 35u, (byte)2, 500239u },
            new object[] { 35u, (byte)3, 300u }, new object[] { 35u, (byte)4, 299u },
            // 36 Mal Dys Upper Resonator
            new object[] { 36u, (byte)1, 531137u }, new object[] { 36u, (byte)1, 531143u }, new object[] { 36u, (byte)1, 531151u }, new object[] { 36u, (byte)1, 531152u },
            new object[] { 36u, (byte)1, 531161u }, new object[] { 36u, (byte)1, 531168u }, new object[] { 36u, (byte)2, 500227u }, new object[] { 36u, (byte)2, 500238u },
            new object[] { 36u, (byte)3, 302u }, new object[] { 36u, (byte)4, 301u },
            // 37 Mal Dys Lower Resonator
            new object[] { 37u, (byte)1, 531136u }, new object[] { 37u, (byte)1, 531146u }, new object[] { 37u, (byte)2, 500226u }, new object[] { 37u, (byte)2, 500237u },
            new object[] { 37u, (byte)3, 304u }, new object[] { 37u, (byte)4, 303u },
            // 38 Drill Resonator
            new object[] { 38u, (byte)2, 500225u }, new object[] { 38u, (byte)2, 500233u }, new object[] { 38u, (byte)4, 305u },
            // 39 Cuthah Scout Post
            new object[] { 39u, (byte)1, 531187u }, new object[] { 39u, (byte)1, 531202u }, new object[] { 39u, (byte)1, 531204u }, new object[] { 39u, (byte)2, 500206u },
            new object[] { 39u, (byte)2, 500211u }, new object[] { 39u, (byte)2, 500213u }, new object[] { 39u, (byte)3, 308u },
            // 40 Dead Zone Power Station
            new object[] { 40u, (byte)2, 500207u }, new object[] { 40u, (byte)2, 500214u }, new object[] { 40u, (byte)3, 310u }, new object[] { 40u, (byte)4, 309u },
            // 41 Cuthah Ammo Depot
            new object[] { 41u, (byte)1, 531171u }, new object[] { 41u, (byte)2, 500205u }, new object[] { 41u, (byte)2, 500212u }, new object[] { 41u, (byte)3, 312u },
            new object[] { 41u, (byte)4, 311u }, new object[] { 41u, (byte)5, 531033u },
        };

        /// <summary>The inserts of both tables, in order; each is plain SQL either provider takes.</summary>
        public static IEnumerable<string> InsertStatements
        {
            get
            {
                foreach (var statement in Inserts(ControlPointEntry.TableName, PointColumns, Points))
                    yield return statement;

                foreach (var statement in Inserts(ControlPointLinkEntry.TableName, LinkColumns, Links))
                    yield return statement;
            }
        }

        /// <summary>How many control points the seed has.</summary>
        public static int PointCount => Points.Length;

        /// <summary>How many links the seed has.</summary>
        public static int LinkCount => Links.Length;

        private static IEnumerable<string> Inserts(string table, string[] columns, object[][] rows)
        {
            const int perStatement = 50;

            for (var i = 0; i < rows.Length; i += perStatement)
                yield return $"insert into {table} ({string.Join(", ", columns)}) values "
                    + string.Join(", ", rows.Skip(i).Take(perStatement).Select(row => "(" + string.Join(", ", row.Select(Literal)) + ")"))
                    + ";";
        }

        private static string Literal(object value)
        {
            return value switch
            {
                string text => "'" + text.Replace("'", "''") + "'",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}

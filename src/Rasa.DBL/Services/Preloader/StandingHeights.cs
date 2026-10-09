namespace Rasa.Services.Preloader
{
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// The height of every creature that can go nowhere and is alone on a point pool, put at the
    /// floor under it, and fifteen of them moved out of the furniture they were entered in.
    ///
    /// Such a creature - a vendor, a hospital's medic, a mission's contact; neither a walking
    /// speed nor a running one - stands exactly on its pool's point (SpawnPoolManager.SpawnPoint),
    /// so the point's height has to be the floor's. 650 pools hold one, on 69 maps. Each was
    /// measured against the client's own data: the terrain heightmap, and the collision mesh of
    /// every entity of the map at its place, read as the navmesh builder reads them
    /// (Rasa.ClientData). The floor under a point is the surface facing up with nothing over it
    /// for 1.7 m; 634 of the points have one. The measure gives the five surfaces
    /// BootcampGroundingTests has under Youngblood's base, and the hospital floor
    /// WildernessTwinPillarsTests has under George Corman, to the millimetre; and of the 80
    /// pools entered by hand (SpawnpoolPreloader, BootcampPostedNpcSpawnpoolPreloader) that have
    /// a floor, 76 are within 0.1 m of it, 0.03 m on average, and three of the other four are
    /// at the height of the navmesh that was there before 4 October.
    ///
    /// As they stood - on the navmesh's height where it was within 3 m, on their own where it was
    /// not - 321 of the 634 were 0.15 m or more over their floor, 128 were 0.3 m or more, 38
    /// half a metre or more, and 9 more than 3 m: medics, vendors and temple entrances whose
    /// height is a map marker's, hung over the spot it marks. The navmesh is 0.17 m over the
    /// floor at the median, and beside anything an agent can step onto it slopes up to its top.
    ///
    ///  - Heights: 185 pools whose height was 0.15 m or more from the floor, given the floor's.
    ///    163 come down - 12 by more than 3 m, 27 by 1 to 3 m - and 22 go up, out of the ground
    ///    or a platform, where the navmesh had been holding them. The 442 within 0.15 m are left
    ///    as they are.
    ///  - Moves: 15 pools whose spot is inside something or on its edge, put on the nearest
    ///    spot - 0.25 to 1.41 m off - that has the floor clear for 0.4 m around, walkable ground
    ///    within a metre and no other pool within 1.5 m. Seven are of the rings of trainers
    ///    that ClassTrainerSpawnpoolPreloader lays round a marker, through whatever is there.
    ///
    /// Left as they are: Lt. Saviours (198), entered standing on a stretcher; the two Military
    /// Surplus crates (500106, 500267), which are on crates; and five the client has nothing
    /// under for 20 m either way (500005, 500219, 500259, 500261, 500312).
    ///
    /// Every statement tests the position it replaces, so a row that has been changed since is
    /// left alone and a second run does nothing; Down puts each back.
    /// </summary>
    public static class StandingHeights
    {
        /// <summary>A pool, the height it had, and its floor's.</summary>
        private static readonly (uint Pool, double Was, double Floor)[] Heights =
        {
            // adv_arieki_ligo_ashendesert (1734)
            (501037, 297.0f, 296.75),     // Soldier Trainer: Ashen Desert: 0.25 m down
            (510007, 282.11f, 282.29),    // Captain Daris Linier, White Oasis Post: 0.18 m up
            (510008, 280.64f, 280.45),    // Captain Flynn Harris, Ashoka Settlement: 0.19 m down
            (510009, 316.85f, 316.37),    // Captain Marcus Emmons, Stonewall Ridge: 0.48 m down
            (510010, 296.59f, 296.39),    // Colonel Mason Linier, Shadow's Edge Post: 0.20 m down
            (510011, 296.58f, 296.27),    // Ida Hayner, Shadow's Edge Post: 0.31 m down
            (510012, 282.39f, 281.76),    // Lieutenant Dania Fontecilla, White Oasis Post: 0.63 m down
            (510013, 289.75f, 289.56),    // Lieutenant Jan Blood, Boneyard Outpost: 0.19 m down
            (510014, 280.53f, 280.31),    // Mechanic Raji Lasat, Ashoka Settlement: 0.22 m down
            // adv_arieki_ligo_ashendesert_baneconscriptfacility (1988)
            (500019, 156.2f, 153.38),     // AFS Field Medic: 2.82 m down
            (500020, 155.9f, 153.36),     // AFS Field Medic: 2.54 m down
            // adv_arieki_ligo_burningsteps (1993)
            (500028, 168.9f, 168.5),      // Token Banker: Fort Intrepid (Control Point): 0.40 m down
            (510015, 188.43f, 187.78),    // Awol Captain Cheung, Staal: 0.65 m down
            (510016, 189.15f, 188.52),    // Awol Lieutenant Deirdre, Staal: 0.63 m down
            (510019, 152.3f, 152.0),      // Captain DeLisi, Outpost Intrepid: 0.30 m down
            (510020, 152.68f, 152.5),     // Captain Kelly, Outpost Intrepid: 0.18 m down
            (510021, 168.82f, 169.77),    // Captain Tiersky, Prometheus Outpost: 0.95 m up
            (510022, 171.06f, 170.86),    // Corporal Fiorella, Outpost Intrepid: 0.20 m down
            (510023, 152.39f, 152.0),     // Intelligence Officer D'Ambrose, Outpost Intrepid: 0.39 m down
            (510024, 188.77f, 187.85),    // Kappa Grupa Lohen, Staal: 0.92 m down
            (510025, 188.27f, 187.77),    // Labbna Grupa Rigs, Staal: 0.50 m down
            (510026, 188.01f, 187.77),    // Labbna Sigal Haggus, Staal: 0.24 m down
            (510027, 188.05f, 187.77),    // Larai Grupa Vitto, Staal: 0.28 m down
            (510028, 188.15f, 187.77),    // Larai Sigal Tempa, Staal: 0.38 m down
            (510029, 169.07f, 169.77),    // Larai Zupa Madias, Prometheus Outpost: 0.70 m up
            (510030, 152.26f, 152.0),     // Lieutenant Sherer, Outpost Intrepid: 0.26 m down
            (510031, 168.63f, 169.77),    // Major Keplinger, Prometheus Outpost: 1.14 m up
            (510033, 180.84f, 180.5),     // Merchant Vargo, Staal Centrum: 0.34 m down
            (510034, 167.66f, 167.32),    // Private Drake, Outpost Intrepid: 0.34 m down
            (510035, 188.41f, 187.77),    // Special Agent Capriulo, Staal: 0.64 m down
            (510036, 188.68f, 187.77),    // Viddea Scientist Eugin, Staal: 0.91 m down
            (510037, 188.85f, 187.77),    // Viddia Grupa Donal, Staal: 1.08 m down
            // adv_arieki_ligo_burningsteps_magmacaverns (1977)
            (500035, 132.0f, 131.74),     // Field Medic: Rebel Camp: 0.26 m down
            // adv_arieki_ligo_thunderhead (1911)
            (510042, 380.6f, 380.45),     // Enforcer Bonree, Larai Outpost: 0.15 m down
            (510044, 386.35f, 385.62),    // Field Sgt. Eiserloh, Vashtar Pass: 0.73 m down
            (510045, 337.4f, 337.21),     // Scientist Irial, Viddia Outpost: 0.19 m down
            (510047, 380.6f, 380.45),     // Sigal Cisra, Larai Outpost: 0.15 m down
            (510048, 337.49f, 337.21),    // Sigal Swona, Viddia Outpost: 0.28 m down
            // adv_arieki_torden_abyss (2028)
            (510050, 532.27f, 532.03),    // Agent Maxwell, Tantalus Base: 0.24 m down
            (510051, 532.19f, 531.86),    // Corporal Cottman, Charon's Crossing: 0.33 m down
            (510052, 532.29f, 532.03),    // General Perry, Tantalus Base: 0.26 m down
            (510053, 532.27f, 532.03),    // Labbna Trader Joss, Tantalus Base: 0.24 m down
            (510054, 529.88f, 528.58),    // Larai Zupa Monlo, Tampeii Settlement: 1.30 m down
            (510056, 542.26f, 541.97),    // Private Heard, Icarus Outpost: 0.29 m down
            (510057, 536.13f, 535.87),    // Professor Whitney, Icarus Post: 0.26 m down
            (510058, 575.65f, 574.73),    // Recon Officer Granger, Brimstone Falls: 0.92 m down
            (510059, 529.61f, 529.37),    // Researcher Dalton, Tampeii Settlement: 0.24 m down
            (510060, 532.24f, 532.03),    // The Director, Tantalus Base: 0.21 m down
            // adv_arieki_torden_incline (1761)
            (510063, 259.51f, 258.87),    // Colonel Deiley, Ortho: 0.64 m down
            (510064, 223.03f, 222.64),    // Coordinator Maila, Mires Post: 0.39 m down
            (510065, 260.03f, 259.13),    // Coordinator Melsi, Ortho: 0.90 m down
            (510066, 234.67f, 233.73),    // Field Commander Foletto, Nyxroq Trench: 0.94 m down
            (510068, 234.96f, 233.73),    // Lieutenant Epp, Nyxroq Trench: 1.23 m down
            (510069, 235.16f, 234.94),    // Major Nicholson, Nyxroq Post: 0.22 m down
            (510070, 259.8f, 258.7),      // Medic Rahish, Ortho Post: 1.10 m down
            (510071, 237.56f, 238.56),    // Private Parsons, Plains Post: 1.00 m up
            (510072, 259.58f, 258.69),    // Protector Starl, Ortho: 0.89 m down
            (510073, 259.19f, 258.8),     // Retread Otto, Ortho Post: 0.39 m down
            (510074, 259.05f, 258.82),    // Salvage Master Orto, Ortho Post: 0.23 m down
            (510075, 162.51f, 162.0),     // Science Officer Brandeis, Pax Oasis: 0.51 m down
            (510076, 277.42f, 276.03),    // Science Officer Collier, Raintree Post: 1.39 m down
            (510077, 259.71f, 259.13),    // Supply Sergeant Otto, Ortho Post: 0.58 m down
            // adv_arieki_torden_incline_ojasaattahive (1865)
            (500085, 72.7f, 72.42),       // Ojasa Atta Hive Entrance: 0.28 m down
            // adv_arieki_torden_mires (1759)
            (501025, 228.5f, 226.86),     // Commando Trainer: Torden Mires: 1.64 m down
            (510078, 224.55f, 224.34),    // Dr. Robertson, Fort Haroun: 0.21 m down
            (510079, 224.25f, 224.0),     // Dr. Torpor, Fort Haroun: 0.25 m down
            (510080, 224.4f, 224.0),      // General Mondragon, Fort Haroun: 0.40 m down
            // adv_arieki_torden_mires_banefluxitemines (2115)
            (500101, 147.7f, 144.61),     // Fluxite Mines Field Medic: 3.09 m down
            (500102, 148.3f, 144.61),     // Fluxite Mines Field Medic: 3.69 m down
            // adv_arieki_torden_mires_tahrendrabase (2125)
            (500104, 119.5f, 116.5),      // Tahrendra Base Field Medic: 3.00 m down
            // adv_arieki_torden_plains (1764)
            (510084, 438.91f, 438.62),    // Colonel Franks, Mt. Hellas Outpost: 0.29 m down
            (510091, 412.67f, 412.49),    // Lieutenant Liu, Eir Crater: 0.18 m down
            // adv_arieki_torden_plains_attacolony (1773)
            (510096, 314.58f, 313.45),    // Lieutenant Donners, Kardash Atta Colony: 1.13 m down
            // adv_bootcamp (1985)
            (510205, 119.4, 119.58),      // Corporal Hartmann: 0.18 m up
            (510206, 120.059, 119.7),     // Corporal DeSimone: 0.36 m down
            // adv_foreas_concordia_divide (1148)
            (500153, 85.6f, 85.25),       // Medical Vendor: Foxtrot Outpost: 0.35 m down
            (500158, 81.1f, 81.34),       // Weapon Vendor: Delta Outpost: 0.24 m up
            (500162, 167.0f, 166.78),     // Weapon Vendor: Thoria Das: 0.22 m down
            (510098, 116.14f, 115.97),    // Agent Cicely, Foreas Base: 0.17 m down
            (510099, 117.53f, 116.54),    // Agent Franz, Foreas Base: 0.99 m down
            (510100, 116.84f, 116.54),    // Captain Rankin, Foreas Base: 0.30 m down
            (510102, 85.2f, 85.03),       // Dr. Kibner, Foxtrot Outpost: 0.17 m down
            (510104, 170.93f, 170.01),    // Elder Q'uoa, Thoria Das: 0.92 m down
            (510105, 116.5f, 116.28),     // Field Lt. McMurray, Foreas Base: 0.22 m down
            (510108, 141.51f, 141.1),     // Lieutenant Seguine, Nidu Dav: 0.41 m down
            (510110, 117.9f, 118.53),     // Recon Officer Stevens, Foreas Base: 0.63 m up
            (510111, 85.87f, 85.03),      // Sergeant Hawthorne, Foxtrot Outpost: 0.84 m down
            (510113, 56.97f, 56.5),       // Sergeant Zuelke, Hydro Plant: 0.47 m down
            (510114, 56.9f, 57.29),       // Sgt. Yeager, Hydro Plant: 0.39 m up
            (510115, 170.92f, 170.01),    // Shaman Horea, Thoria Das: 0.91 m down
            (510116, 116.11f, 115.9),     // Spec. Kerr, Foreas Base: 0.21 m down
            (510117, 170.62f, 170.01),    // Warrior Mela, Thoria Das: 0.61 m down
            // adv_foreas_concordia_divide_torcastraprison (1349)
            (500168, 16.9f, 13.61),       // Torcastra Prison AFS Medical Officer: 3.29 m down
            (500169, 100.1f, 96.73),      // Torcastra Prison Field Medic: 3.37 m down
            (500170, 99.7f, 96.81),       // Vendor: Torcastra Prison: 2.89 m down
            (500171, 19.3f, 13.61),       // Vendor: Torcastra Prison: 5.69 m down
            (510120, 97.99f, 96.81),      // Airman Hamilton, Torcastra Prison: 1.18 m down
            // adv_foreas_concordia_palisades (1244)
            (500174, 139.0f, 138.17),     // Military Surplus: Cumbria Research Facility: 0.83 m down
            (510121, 150.58f, 150.04),    // Captain McShay, Staging Point: 0.54 m down
            (510123, 141.78f, 141.13),    // Commander Aldrin, Cumbria Research Facility: 0.65 m down
            (510124, 141.73f, 140.84),    // Corporal Hutchison, Cumbria Research Facility: 0.89 m down
            (510125, 152.23f, 151.52),    // Field Commander Twitty, Staging Point: 0.71 m down
            (510126, 188.76f, 188.45),    // Field Lt. Brody, Walk of Giants: 0.31 m down
            (510127, 152.38f, 151.76),    // Field Technician McCain, Staging Point: 0.62 m down
            (510130, 168.48f, 168.3),     // Private Robertson, Fort Dew: 0.18 m down
            (510131, 154.24f, 153.58),    // Ranger Cyrida, Hightower Outpost: 0.66 m down
            (510132, 173.38f, 170.99),    // Ranger Gorodai, Treeback Ridge: 2.39 m down
            (510133, 178.17f, 178.0),     // Ranger Jorai, Viands Village: 0.17 m down
            (510134, 110.09f, 110.35),    // Sergeant Barrasso, River-base Krimm: 0.26 m up
            (510135, 110.03f, 110.43),    // Sergeant Mullen, River-base Krimm: 0.40 m up
            (510136, 119.32f, 118.71),    // Thomas Jansona, Cumbria Weald: 0.61 m down
            (510137, 141.62f, 141.42),    // Valerie Corman, Cumbria Research Facility: 0.20 m down
            (510138, 188.19f, 188.45),    // Warden Brocail, Walk of Giants: 0.26 m up
            (510139, 188.95f, 188.45),    // Warden Kahlee, Walk of Giants: 0.50 m down
            (510140, 189.71f, 188.4),     // Warden Lagori, Walk of Giants: 1.31 m down
            // adv_foreas_concordia_palisades_elohtemples (1803)
            (500188, 27.4f, 23.99),       // Temple of the Bowed Patriarch Entrance: 3.41 m down
            (500189, 27.2f, 23.99),       // Temple of the Proud Patriarch Entrance: 3.21 m down
            (500190, 27.7f, 23.97),       // Temple of the Raging Patriarch Entrance: 3.73 m down
            // adv_foreas_concordia_wilderness (1220)
            (101, 238.757, 238.4),        // Field Sgt. Witherspoon: 0.36 m down
            (194, 238.11785, 237.93),     // AFS Information Specialist Savious: 0.19 m down
            (501003, 294.1f, 293.88),     // Commando Trainer: Alia Das: 0.22 m down
            (501012, 220.3f, 221.27),     // Biotechnician Trainer: Twin Pillars: 0.97 m up
            (510001, 220.72f, 220.57),    // Archaeologist Wynne Topper, Twin Pillars: 0.15 m down
            (510004, 294.71f, 294.42),    // Dr. Elise Corman, Alia Das: 0.29 m down
            (510006, 294.05008, 293.88),  // Training Officer Kincaid, Alia Das: 0.17 m down
            // adv_foreas_concordia_wilderness_clrf (1721)
            (500198, 142.3f, 137.81),     // Crater Lake Medic: 4.49 m down
            (500199, 140.4f, 137.81),     // Crater Lake Medic: 2.59 m down
            // adv_foreas_howlingmaw1 (2051)
            (510142, 201.0f, 200.82),     // Colonel Sherk, Gangus Outpost: 0.18 m down
            (510143, 201.04f, 200.82),    // Commander Lovell, Gangus Outpost: 0.22 m down
            (510147, 214.82f, 216.16),    // Recon Commander McReddy, Dia Toma: 1.34 m up
            (510148, 190.4f, 190.56),     // Scholar Rui, Dia Uyona: 0.16 m up
            (510150, 201.0f, 200.82),     // Spec-Ops Quartermaster Olivia, Gangus Outpost: 0.18 m down
            // adv_foreas_howlingmaw_cuthahbase (2162)
            (510151, 253.01f, 252.39),    // Doctor Splicer, Bioresearch Wing: 0.62 m down
            // adv_foreas_howlingmaw_deathburrow (2136)
            (510152, 77.64f, 77.27),      // Elder Hathen, Velon Hollow: 0.37 m down
            // adv_foreas_valverde_descent (2047)
            (500225, 240.3f, 239.86),     // Hospital: Drill Resonator (Control Point): 0.44 m down
            (510153, 737.71f, 737.4),     // Captain North, Fort Virgil: 0.31 m down
            (510154, 737.71f, 737.4),     // Chief Medical Officer Reynolds, Fort Virgil: 0.31 m down
            (510155, 472.3f, 471.12),     // Commander Merrick, Mal Dys: 1.18 m down
            (510156, 472.29f, 471.72),    // Fortuna Corman, Mal Dys: 0.57 m down
            (510157, 737.6f, 737.4),      // Reception Officer Arvis, Fort Virgil: 0.20 m down
            // adv_foreas_valverde_descent_therefuge (2156)
            (500242, 10.9f, 11.07),       // Eloh Sanctuary: 0.17 m up
            (500243, 4.0f, 1.55),         // AFS Field Medic: 2.45 m down
            // adv_foreas_valverde_marshes (1454)
            (510161, 224.25f, 224.05),    // Field Medic DuBois, Falcon Hold: 0.20 m down
            (510162, 219.28f, 219.11),    // Lieutenant Morrison, Falcon Hold: 0.17 m down
            (510165, 219.28f, 219.11),    // Researcher Vetter, Falcon Hold: 0.17 m down
            (510167, 219.61f, 219.36),    // Retread Samuel, Bane Refueling Station: 0.25 m down
            (510169, 219.28f, 219.11),    // Sergeant Elway, Falcon Hold: 0.17 m down
            // adv_foreas_valverde_marshes_logosresearchfacility (1700)
            (500262, 324.8f, 321.61),     // Logos Research Field Medic: 3.19 m down
            (500263, 325.1f, 321.61),     // Logos Research Field Medic: 3.49 m down
            // adv_foreas_valverde_marshes_villageruins (1743)
            (500264, 63.7f, 64.6),        // Healing Shaman: 0.90 m up
            (500265, 62.9f, 60.12),       // Healing Shaman: 2.78 m down
            // adv_foreas_valverde_plateau (1497)
            (500275, 410.0f, 407.87),     // Armor Vendor: Fort Defiance: 2.13 m down
            (501013, 399.4f, 398.54),     // Commando Trainer: Fort Defiance: 0.86 m down
            (501014, 399.4f, 398.37),     // Ranger Trainer: Fort Defiance: 1.03 m down
            (501015, 399.4f, 398.37),     // Sapper Trainer: Fort Defiance: 1.03 m down
            (501016, 399.4f, 398.37),     // Biotechnician Trainer: Fort Defiance: 1.03 m down
            (501022, 399.4f, 399.85),     // Engineer Trainer: Fort Defiance: 0.45 m up
            (501023, 399.4f, 398.37),     // Medic Trainer: Fort Defiance: 1.03 m down
            (501024, 399.4f, 398.37),     // Exobiologist Trainer: Fort Defiance: 1.03 m down
            (510173, 423.68f, 423.45),    // Bartender McLaughlin, Fort Defiance: 0.23 m down
            (510176, 447.73f, 447.95),    // Colonel Bosley, Camp Resistance: 0.22 m up
            (510177, 425.56f, 426.07),    // Colonel Thibodeau, Wedge Rock Outpost: 0.51 m up
            (510179, 425.67f, 426.08),    // Comm Officer McKinley, Wedge Rock Outpost: 0.41 m up
            (510180, 423.42f, 423.25),    // Commander Grissom, Fort Defiance: 0.17 m down
            (510181, 366.05f, 364.48),    // Elder Hundra, New Velon Village: 1.57 m down
            (510182, 423.68f, 423.45),    // Field Lt. Peterson, Fort Defiance: 0.23 m down
            (510183, 269.28f, 269.12),    // Field Sgt. Garde, Valverde Chasm: 0.16 m down
            (510184, 423.68f, 423.45),    // Lieutenant Colonel Doss, Fort Defiance: 0.23 m down
            (510185, 364.76f, 362.96),    // Loremaster Talisen, New Velon Village: 1.80 m down
            (510186, 365.64f, 365.02),    // Luminary Sampei, New Velon Village: 0.62 m down
            (510187, 423.68f, 423.45),    // MP Sergeant Norton, Fort Defiance: 0.23 m down
            (510188, 424.71f, 424.54),    // Private Remick, Wedge Rock Outpost: 0.17 m down
            (510189, 367.03f, 366.17),    // Ranger Kaely, New Velon Village: 0.86 m down
            (510190, 269.28f, 269.11),    // Ranger Porthus, Valverde Chasm: 0.17 m down
            (510191, 425.32f, 424.08),    // Science Officer Clark, Wedge Rock Outpost: 1.24 m down
            (510192, 423.48f, 423.25),    // Senior Engineer Mauer, Fort Defiance: 0.23 m down
            // adv_foreas_valverde_plateau_sanctusgrotto (1823)
            (510194, 232.4f, 231.8),      // Captain Danny Bowers, Sanctus Grotto: 0.60 m down
            // adv_foreas_valverde_plateau_ustoryard (1502)
            (500293, 170.4f, 169.36),     // Ustor Yard Field Medic: 1.04 m down
            // adv_foreas_valverde_pools (1304)
            (510195, 862.2f, 861.59),     // Amee Corman, Retread Camp: 0.61 m down
            (510196, 791.54f, 791.12),    // Corporal Orton, Obelisk: 0.42 m down
            (510197, 657.0f, 656.5),      // Corporal Sheen, East Listening Post: 0.50 m down
            (510198, 656.84f, 656.5),     // Lieutenant Chester, East Listening Post: 0.34 m down
            (510199, 862.26f, 861.37),    // Lieutenant Holloway, Retread Camp: 0.89 m down
            (510200, 657.13f, 656.5),     // Lt. Cujhan, East Listening Post: 0.63 m down
            (510201, 862.89f, 863.12),    // Retread McCormick, Retread Camp: 0.23 m up
        };

        /// <summary>A pool, where it was, and the free spot it is put on.</summary>
        private static readonly (uint Pool, double X, double Y, double Z, double ToX, double ToY, double ToZ)[] Moves =
        {
            // Class Trainer, adv_arieki_ligo_thunderhead (1911): on a workstation, 1.15 m over the shop floor
            (500046, 632.6f, 341.4f, -546.8f, 631.6, 340.25, -545.8),
            // Hospital: Raksha Robotics (Control Point), adv_arieki_torden_incline_wardenbotfactory (2111): in a bank of computers
            (500088, -81.0f, 10.1f, -14.6f, -82.0, 10.06, -14.6),
            // Ranger Trainer: Torden Mires, adv_arieki_torden_mires (1759): in a bed
            (501026, -616.9f, 228.5f, -516.6f, -617.4, 228.52, -515.35),
            // Guardian Trainer: Torden Mires, adv_arieki_torden_mires (1759): between a bed and a dresser
            (501030, -629.1f, 228.5f, -516.6f, -628.85, 228.52, -515.85),
            // Exobiologist Trainer: Torden Mires, adv_arieki_torden_mires (1759): at the foot of the barracks, in the edge of its floor
            (501036, -616.9f, 228.5f, -523.6f, -616.4, 226.86, -523.6),
            // Tahrendra Base Field Medic, adv_arieki_torden_mires_tahrendrabase (2125): 3 m over the edge of a ramp
            (500105, 196.7f, 120.2f, 45.5f, 196.95, 117.23, 46.0),
            // Class Trainers: Irendas Penal Colony, adv_arieki_torden_plains (1764): in the edge of a shrine
            (500113, 165.3f, 417.5f, -253.5f, 164.8, 417.5, -253.5),
            // Medical Vendor: Hydro Plant, adv_foreas_concordia_divide (1148): in a chair
            (500154, -285.8f, 57.4f, 27.1f, -286.55, 57.38, 27.1),
            // Ranger Trainer: Alia Das, adv_foreas_concordia_wilderness (1220): in a cot
            (501004, 761.0f, 294.1f, 386.1f, 761.0, 294.12, 386.85),
            // Sapper Trainer: Twin Pillars, adv_foreas_concordia_wilderness (1220): on the barracks' ledge where its roof comes down to 0.9 m
            (501011, -103.5f, 220.3f, -520.4f, -103.25, 220.27, -521.15),
            // Brigadier General Beacham, Alia Das, adv_foreas_concordia_wilderness (1220): in the sandbags
            (510002, 812.9f, 294.69f, 388.2f, 813.4, 294.52, 387.95),
            // Grenadier Trainer: Fort Defiance, adv_foreas_valverde_plateau (1497): at the foot of the headquarters, in the edge of its floor
            (501017, -68.8f, 399.4f, 950.1f, -68.8, 398.33, 950.35),
            // Sniper Trainer: Fort Defiance, adv_foreas_valverde_plateau (1497): against a weapon counter
            (501019, -72.3f, 399.4f, 944.0f, -72.05, 399.4, 944.0),
            // Vendors, adv_wargame_edmundrange2 (2374): on a stack of crates, 2.58 m over the catwalk
            (500319, -32.6f, 366.1f, -397.5f, -33.85, 363.52, -398.0),
            // Vendors, adv_wargame_provinggroundsv002 (2361): on a stack of crates, 2.58 m over the catwalk
            (500325, 71.4f, 366.1f, -51.7f, 70.15, 363.52, -52.2),
        };

        public static readonly string[] WorldUp = Statements(false);

        public static readonly string[] WorldDown = Statements(true);

        /// <summary>Where each pool stands after Up: every pool this changes, and the position it is given.</summary>
        public static IEnumerable<(uint Pool, double? X, double Y, double? Z)> Corrected()
        {
            foreach (var (pool, _, floor) in Heights)
                yield return (pool, null, floor, null);

            foreach (var (pool, _, _, _, x, y, z) in Moves)
                yield return (pool, x, y, z);
        }

        private static string[] Statements(bool back)
        {
            var statements = new List<string>();

            foreach (var (pool, was, floor) in Heights)
            {
                var (from, to) = back ? (floor, was) : (was, floor);

                statements.Add($"update spawnpool set pos_y = {N(to)} where id = {pool} and abs(pos_y - {N(from)}) < 0.005;");
            }

            foreach (var (pool, x, y, z, toX, toY, toZ) in Moves)
            {
                var (fx, fy, fz, tx, ty, tz) = back ? (toX, toY, toZ, x, y, z) : (x, y, z, toX, toY, toZ);

                statements.Add($"update spawnpool set pos_x = {N(tx)}, pos_y = {N(ty)}, pos_z = {N(tz)} where id = {pool}"
                    + $" and abs(pos_x - {N(fx)}) < 0.005 and abs(pos_y - {N(fy)}) < 0.005 and abs(pos_z - {N(fz)}) < 0.005;");
            }

            return statements.ToArray();
        }

        private static string N(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}

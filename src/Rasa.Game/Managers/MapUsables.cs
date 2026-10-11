using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Usables the client's own map files place, given the state the client draws them in.
    ///
    /// The client's map loader makes every entity of a .map itself, under the id the map gives it
    /// (client/gamemap.py _LoadEntities3: CreateEntity(classId, entityId), then AddToWorld), so a
    /// usable among them is a usable on the client with no state: Usable.__init__ leaves
    /// _curStateId None, and only Recv_UsableInfo, Recv_ForceState and Recv_Use from the server
    /// set one. Until one is set it plays no state animation and carries no state effect
    /// (UsableState.__call__: SwapMesh, AttachStateSFX, PlayAnimation). The map's entities are in
    /// the client's entity manager (AddEntity) under those ids, as the server's own are, and a
    /// method call is sent to an id; ForceState to one sets its state.
    ///
    /// Each is sent its state as the player arrives on the map (AssignPlayer), on every arrival,
    /// as the client makes the map's entities anew on every load. A copy of a map loads the same
    /// file and gets the same.
    ///
    /// The Forean fire pits. UsableTwoStateForeanFirePitV01 (6137) and V03 (6212) are two-state
    /// switches (augmentation 9) whose fire is the special effect of USE_TS_STATE_0 (55)
    /// (usabledata.specialFX (class, 55, USE_STATE_NULL): arch_forean_fire_pit_v01.pkg and
    /// _v03.pkg, the flames and their heat shimmer), with the state's animation
    /// (usabledata.animation: Usable state 0 static, arch_forean_fire_pit.anm on both meshes).
    /// USE_TS_STATE_1 (56) has neither: the pit cold. The .maps attach no particles of their own
    /// to them, so with no state they have stood unlit. There are three, one on each of three
    /// maps; V02 (6211) is on none, and has no effect of its own. Entity ids, positions and
    /// facings are the .maps'.
    ///
    /// Lighting and putting out. The client offers a usable it knows Use on click: the right-click
    /// menu's USE_OBJECT (Usable.OnGetUseAction) while it is enabled, which it is from the start,
    /// and not locked, which one with no state is (IsLocked: _curStateId == the lock state, None
    /// == None). Use is RequestUseObject (USE_OBJECT 80 with the class's use arg, 1 for these:
    /// usabledata.lookup has none), and the server's Use (Recv_Use) moves it to the state it
    /// names. A two-state switch's transitions are 55 to 56 and 56 to 55
    /// (usableaugmentationstatetransition 9), neither with an animation or effect of its own: out,
    /// the state's fire is taken off (UsableStateTransition: DetachStateSFX, then 56's, which is
    /// none); lit, 55's is put back with its animation. The request is answered as a footlocker's
    /// is: the windup to the user, the recovery to whoever is near, and at the recovery the pit
    /// goes to its other state on that map channel and every client on it is sent the Use.
    /// One use of a pit at a time; one interrupted changes nothing.
    ///
    /// A pit stays as it was left for as long as its channel runs; whoever arrives is sent it as
    /// it is. A new channel, and so a restart, starts it lit.
    ///
    /// The Brann dissection tables and the Bane stasis chambers. Both are inert destroyables
    /// (augmentation 41) that the .maps place, and both have their idle animation and effect in
    /// USE_IDES_STATE_INTACT (110), so both have stood still. The tables
    /// (UsableInertDestBrannTableDissectionAttaV01DELETEDUPE, 25269 - the maps use the
    /// DELETEDUPE class; 24607, the same table, is on none): "Usable Inactive", animation family
    /// 277 as recurring one-shots (type 3), the Atta on the table between
    /// prop_brann_table_dissection_atta_v01.anm and _v02.anm, with
    /// prop_brann_table_dissection_atta.pkg, the table's lasers (vfx_prop_brann_table_atta_laser)
    /// under its grate lights. The chambers (UsableInertDestBaneStasisChamber, 7197): the same
    /// spec, looping, arch_bane_industrial_room_stasis_chamber.anm, with
    /// arch_bane_Industrial_stasis_chamber_idle.pkg (arch_bane_industrial_room_stasis_chamber_fx).
    /// Neither is used: an inert destroyable offers only a loot use (Lootable), and these have
    /// none. Neither is destroyed here either: the client takes either for undamageable
    /// (usabledata.lookup has no hit points for them), and the explosion and wreck their
    /// destroyed states carry are left for when something destroys them.
    ///
    /// The Brann monitors. UsableTwoStateBrannMonitorGenericV01 (23885) and V02 (23886) are
    /// two-state switches like the fire pits, the other way about: USE_TS_STATE_1 (56) is the
    /// one with an animation (usabledata.animation: Usable state 1 static, played looping,
    /// arch_brann_gen_obj_monitor_v01_on.anm on both meshes), and USE_TS_STATE_0 (55) has
    /// nothing, a dark screen. Neither has an effect, a transition animation or an alternate
    /// mesh. They are sent 56, on. Their classes are not targetable (entityclass target flag 0),
    /// so the client offers no Use and they are not used. Sixty-eight on the maps: thirty-one
    /// V01 and thirty-three V02 in the Comm Tower, four V01 in Burning Steps. The Brann monitor
    /// props the .maps also place (PropBrannMonitorV04 and the destroyed ones) are no usables
    /// and have no states.
    ///
    /// The Bane articulated drills. UsableInertDestBaneArticulatedDrillV01 (6318) is an inert
    /// destroyable like the tables and chambers, and works in USE_IDES_STATE_INTACT (110):
    /// "Usable Inactive", animation family 277 as recurring one-shots (type 3), the drill going
    /// between arch_bane_gen_obj_articulateddrill_drilling_v01.anm, _idle_fidget_v01.anm and
    /// _idle_long_v01.anm, with arch_bane_gen_obj_articulateddrill_v01.pkg. Its damaged states
    /// (50% and 25%: the drilling alone) and its destroyed one (wreck, explosion) are not used,
    /// as the client has no hit points for it. Three on the maps: Concordia Divide, by the Timora
    /// Mines tunnel; the Treeback Camp; Thunderhead.
    ///
    /// Not here: the sonic towers of Maligo Base (UsableInertDestBaneSonicTowerV02, 21964, two
    /// on adv_foreas_valverde_plateau_maligobasev3). Their class has nothing in any intact
    /// state - no animation, no effect - only its destruction (the 25% to destroyed transition,
    /// arch_bane_gen_obj_sonictower_v02_exploding.anm and _exploding.pkg, then the wreck and
    /// _destroyed.pkg), and it is neither targetable nor given hit points, so there is no state
    /// to send that would show anything.
    ///
    /// Three more two-state switches, none targetable (entityclass target flag 0), so none used,
    /// each sent the state that has it running:
    ///  - UsableTwoStateBrannTeleporter (24805): USE_TS_STATE_1 (56), "Usable state 1 static"
    ///    looping, prop_brann_teleporter_on_v01.anm, with arch_brann_teleporter_on_v01.pkg; in 55
    ///    it is the dark mesh, prop_brann_teleporter_off_v01.geo (usabledata.alternateMesh). Two,
    ///    one above the other, by the Staal Detention Facility in Burning Steps.
    ///  - UsableTwoStateBrannWormhole (25273): 56, prop_brann_wormhole_v01_on.anm looping, with
    ///    prop_brann_wormhole_v01_on.pkg; in 55 it stands at the end of _on_to_off.anm. One, in
    ///    Tahrendra Base.
    ///  - UsableTwoStateBaneMiningCoreDrillV01 (25263): the other way about, 55, "Usable state
    ///    0 static" looping, arch_bane_drill_mining_v01_on.anm, with
    ///    arch_bane_mining_core_drill_on.pkg; 56 is it stopped, at the end of _on_to_off.anm.
    ///    One, in the Refuge.
    ///  - UsableTwoStateBrannAlarmLightV01 (24679): 56, arch_brann_gen_obj_alarmlight_on_v01.anm
    ///    looping, with arch_brann_gen_obj_alarm_light_on.pkg; 55 is the still off pose
    ///    (_off_v01.anm). Neither way has a transition. Ten, in the Brann Water Refinery, which the
    ///    Warden-bots have overrun: on throughout.
    ///  - UsableTeslaCoilBaneV01 (3899), a TeslaCoil (augmentation 58: USE_STATE_POWER_DOWN 171,
    ///    USE_STATE_POWER_UP 172, destroyed): the coil's arcing, weapon_bane_teslacoil_active.pkg,
    ///    is the state effect of 171 (usabledata.specialFX (3899, 171, USE_STATE_NULL)), whatever
    ///    its name; 172 has none. Five, in Concordia Divide, sent 171. They zap players near them
    ///    (TeslaCoils) and can be shot down (WorldDestructibles).
    ///
    /// The tables, chambers, drills and coils can be destroyed by players and come back
    /// (WorldDestructibles); their state on a channel follows (SetState).
    ///
    /// The last two-state switches the .maps place with something in a state (augmentation 9:
    /// USE_TS_STATE_0 55, USE_TS_STATE_1 56; usabledata.specialFX and alternateMesh):
    ///  - UsableTwoStateBaneTeleporterV01 (8044): its base mesh, arch_bane_teleporter_v01.geo,
    ///    with arch_bane_teleporter_v01.pkg, the teleporter running, in 56; in 55 the dark
    ///    arch_bane_teleporter_v01_off.geo. Thirty-three, none on a teleport of the server's:
    ///    56 in the Bane bases (Minos Caverns 11, Devil's Den 14, Pravus Research 6), 55 for the
    ///    one in Thunderhead and the one on the Incline.
    ///  - UsableTwoStateHumTeleporter (7895): arch_hum_teleporter.geo as it stands, and in 56
    ///    arch_hum_teleporter_disabled.geo; 55 has nothing. Of Thunderhead Base's ten, six stand
    ///    where the server's local teleporters are (teleporter 542-545, 547, 548: class 29648, a
    ///    fifth of a metre off) and keep the working mesh, unsent. The four on the deck above,
    ///    at 383 m, have none behind them, and are sent 56, disabled. test_nexus2's 39 are left.
    ///  - UsableTwoStateElohTemplesSpawner (22954): arch_eloh_temples_spawner_on.pkg in 56.
    ///    Nineteen in the Eloh Temples, on.
    ///  - UsableTwoStateBaneIndustrialRoomLogosStoneAnalyzer (7241):
    ///    arch_bane_industrial_room_logosstone_analyzer_on.pkg in 56. One each in Minos Caverns
    ///    and Devil's Den, on.
    ///  - The Dell XPS notebook (26498) and tower (26500): prop_hum_computer_dell_xps_off.pkg in
    ///    55, the screen lit in 56 (..._notebook_v01_on.pkg, ..._tower_v01_on.pkg). Fourteen in
    ///    the Abyss, one in the Mires, one on Torden Plains, on. Their classes are targetable
    ///    (entityclass target flag 1), so the client offers Use, and a use turns one off or on
    ///    again for everyone on the map, as a fire pit is lit and put out.
    /// </summary>
    public static class MapUsables
    {
        public sealed class Usable
        {
            public Usable(uint mapContextId, ulong entityId, uint classId, Vector3 position, UseObjectState state, UseObjectState? usedState, string name)
            {
                MapContextId = mapContextId;
                EntityId = entityId;
                ClassId = classId;
                Position = position;
                State = state;
                UsedState = usedState;
                Name = name;
            }

            public uint MapContextId { get; }

            /// <summary>The id the .map gives it, which is the client's id for it.</summary>
            public ulong EntityId { get; }

            public uint ClassId { get; }

            /// <summary>Where the .map puts it.</summary>
            public Vector3 Position { get; }

            /// <summary>The state a map channel starts it in.</summary>
            public UseObjectState State { get; }

            /// <summary>The state a use takes it to from <see cref="State"/>, and back; null if it is not to be used.</summary>
            public UseObjectState? UsedState { get; }

            public string Name { get; }

            /// <summary>The state a use takes it to from <paramref name="current"/>, or null.</summary>
            public UseObjectState? UsedFrom(UseObjectState current) =>
                UsedState == null ? null : current == State ? UsedState : current == UsedState ? State : null;

            public override string ToString() => $"{Name} ({EntityId}, class {ClassId}, map {MapContextId})";
        }

        public const uint ConcordiaDivide = 1148;
        public const uint ConcordiaPalisades = 1244;
        public const uint ValverdePlateau = 1497;
        public const uint PravusResearch = 1430;        // adv_foreas_concordia_wilderness_pravusresearch
        public const uint TestWeaponsCenter = 1465;     // adv_foreas_valverde_pools_test_weapons_center
        public const uint PenalResearch = 2034;         // adv_arieki_torden_plains_penalresearch
        public const uint StaalJunkyard = 2138;         // adv_arieki_ligo_staaljunkyard
        public const uint BurningSteps = 1993;          // adv_arieki_ligo_burningsteps
        public const uint CommTower = 2085;             // adv_arieki_torden_incline_commtower
        public const uint TreebackCamp = 1397;          // adv_foreas_concordia_palisades_treebackcamp
        public const uint Thunderhead = 1911;           // adv_arieki_ligo_thunderhead
        public const uint TahrendraBase = 2125;         // adv_arieki_torden_mires_tahrendrabase
        public const uint TheRefuge = 2156;             // adv_foreas_valverde_descent_therefuge
        public const uint BrannWaterRefinery = 2093;    // adv_arieki_torden_plains_brannwaterrefinery
        public const uint MinosCaverns = 1347;          // adv_foreas_concordia_divide_minoscaverns
        public const uint DevilsDen = 1394;             // adv_foreas_concordia_palisades_devilsden
        public const uint Incline = 1761;               // adv_arieki_torden_incline
        public const uint Mires = 1759;                 // adv_arieki_torden_mires
        public const uint TordenPlains = 1764;          // adv_arieki_torden_plains
        public const uint ElohTemples = 1803;           // adv_foreas_concordia_palisades_elohtemples
        public const uint Abyss = 2028;                 // adv_arieki_torden_abyss

        public const uint ForeanFirePitV01 = 6137;     // UsableTwoStateForeanFirePitV01
        public const uint ForeanFirePitV03 = 6212;     // UsableTwoStateForeanFirePitV03
        public const uint BaneStasisChamber = 7197;    // UsableInertDestBaneStasisChamber
        public const uint BrannDissectionTable = 25269; // UsableInertDestBrannTableDissectionAttaV01DELETEDUPE
        public const uint BrannMonitorV01 = 23885;     // UsableTwoStateBrannMonitorGenericV01
        public const uint BrannMonitorV02 = 23886;     // UsableTwoStateBrannMonitorGenericV02
        public const uint BaneArticulatedDrill = 6318; // UsableInertDestBaneArticulatedDrillV01
        public const uint BrannTeleporter = 24805;     // UsableTwoStateBrannTeleporter
        public const uint BrannWormhole = 25273;       // UsableTwoStateBrannWormhole
        public const uint BaneMiningCoreDrill = 25263; // UsableTwoStateBaneMiningCoreDrillV01
        public const uint BrannAlarmLight = 24679;     // UsableTwoStateBrannAlarmLightV01
        public const uint BaneTeslaCoil = 3899;        // UsableTeslaCoilBaneV01
        public const uint BaneTeleporter = 8044;       // UsableTwoStateBaneTeleporterV01
        public const uint HumanTeleporter = 7895;      // UsableTwoStateHumTeleporter
        public const uint ElohTemplesSpawner = 22954;  // UsableTwoStateElohTemplesSpawner
        public const uint LogosStoneAnalyzer = 7241;   // UsableTwoStateBaneIndustrialRoomLogosStoneAnalyzer
        public const uint DellNotebook = 26498;        // UsableTwoStateHumComputerDellXpsNotebookV01
        public const uint DellTower = 26500;           // UsableTwoStateHumComputerDellXpsTowerV01

        public static readonly IReadOnlyList<Usable> All = new[]
        {
            // Thoria Das, beside the weapon vendor (202.9, 166.7, 925.6).
            new Usable(ConcordiaDivide, 132770324522631, ForeanFirePitV03, new Vector3(202.922f, 166.713f, 925.613f),
                UseObjectState.TsState0, UseObjectState.TsState1, "Thoria Das fire pit"),

            // Mount Reverence, by its waypoint (-765.7, 442.9, 424.2).
            new Usable(ValverdePlateau, 134269267723598, ForeanFirePitV03, new Vector3(-765.687f, 442.858f, 424.154f),
                UseObjectState.TsState0, UseObjectState.TsState1, "Mount Reverence fire pit"),

            // Below the Temple of the Raging Patriarch (-575.0, 187.6, -110.0).
            new Usable(ConcordiaPalisades, 133182640922310, ForeanFirePitV01, new Vector3(-575.0f, 187.6259f, -110.0f),
                UseObjectState.TsState0, UseObjectState.TsState1, "Raging Patriarch fire pit"),

            // The Brann dissection tables: one in Staal Junkyard, eighteen in Penal Research.
            DissectionTable(StaalJunkyard, 134419591465348, -96.6515f, 253.7253f, -102.9956f, "Staal Junkyard dissection table"),
            DissectionTable(PenalResearch, 134419591466928, -8.5f, -31.44f, -44.75f, "Penal Research dissection table 1"),
            DissectionTable(PenalResearch, 134419591466950, -103.2895f, -47.4f, -61.5658f, "Penal Research dissection table 2"),
            DissectionTable(PenalResearch, 134419591466953, -104.04f, -47.4f, -82.2f, "Penal Research dissection table 3"),
            DissectionTable(PenalResearch, 134419591467561, -2.25f, -31.44f, -45.0f, "Penal Research dissection table 4"),
            DissectionTable(PenalResearch, 134419591467562, -2.0f, -31.44f, -40.0f, "Penal Research dissection table 5"),
            DissectionTable(PenalResearch, 134419591467563, -8.25f, -31.44f, -39.75f, "Penal Research dissection table 6"),
            DissectionTable(PenalResearch, 134419591467566, -2.0f, -31.44f, -66.25f, "Penal Research dissection table 7"),
            DissectionTable(PenalResearch, 134419591467567, -8.25f, -31.44f, -66.0f, "Penal Research dissection table 8"),
            DissectionTable(PenalResearch, 134419591467568, -2.25f, -31.44f, -71.25f, "Penal Research dissection table 9"),
            DissectionTable(PenalResearch, 134419591467569, -8.5f, -31.44f, -71.0f, "Penal Research dissection table 10"),
            DissectionTable(PenalResearch, 134419591467570, -23.5f, -31.44f, -66.25f, "Penal Research dissection table 11"),
            DissectionTable(PenalResearch, 134419591467571, -29.75f, -31.44f, -66.0f, "Penal Research dissection table 12"),
            DissectionTable(PenalResearch, 134419591467572, -23.75f, -31.44f, -71.25f, "Penal Research dissection table 13"),
            DissectionTable(PenalResearch, 134419591467573, -30.0f, -31.44f, -71.0f, "Penal Research dissection table 14"),
            DissectionTable(PenalResearch, 134419591467574, -24.0f, -31.44f, -40.25f, "Penal Research dissection table 15"),
            DissectionTable(PenalResearch, 134419591467575, -30.25f, -31.44f, -40.0f, "Penal Research dissection table 16"),
            DissectionTable(PenalResearch, 134419591467576, -24.25f, -31.44f, -45.25f, "Penal Research dissection table 17"),
            DissectionTable(PenalResearch, 134419591467577, -30.5f, -31.44f, -45.0f, "Penal Research dissection table 18"),

            // The Bane stasis chambers: one each in Pravus Research and the Test Weapons Center.
            StasisChamber(PravusResearch, 133981504835290, 224.0f, 7.6172f, 40.0f, "Pravus Research stasis chamber"),
            StasisChamber(TestWeaponsCenter, 134131828656196, 280.0f, -24.5f, 24.0f, "Test Weapons Center stasis chamber"),

            // The Bane articulated drills: Concordia Divide (by the Timora Mines tunnel), the Treeback Camp, Thunderhead.
            Drill(ConcordiaDivide, 132770324750504, -554.8872f, 183.4147f, -1104.4224f, "Concordia Divide articulated drill"),
            Drill(TreebackCamp, 133835475914614, 248.069f, 145.4277f, -266.646f, "Treeback Camp articulated drill"),
            Drill(Thunderhead, 134419591464915, -238.4222f, 438.671f, 695.0093f, "Thunderhead articulated drill"),

            // The Brann teleporters by the Staal Detention Facility in Burning Steps, the Brann wormhole
            // of Tahrendra Base, and the Bane mining core drill of the Refuge: all on.
            Running(BurningSteps, 134419591469779, BrannTeleporter, UseObjectState.TsState1, -206.738f, 183.3619f, 317.945f, "Burning Steps Brann teleporter, lower"),
            Running(BurningSteps, 134419591469780, BrannTeleporter, UseObjectState.TsState1, -214.899f, 244.4991f, 319.7714f, "Burning Steps Brann teleporter, upper"),
            Running(TahrendraBase, 134419591464274, BrannWormhole, UseObjectState.TsState1, -166.0f, 111.5642f, 12.0f, "Tahrendra Base Brann wormhole"),
            Running(TheRefuge, 134419591462928, BaneMiningCoreDrill, UseObjectState.TsState0, -178.5924f, 99.6789f, 182.8202f, "The Refuge mining core drill"),

            // The Brann alarm lights of the Brann Water Refinery, on.
            Running(BrannWaterRefinery, 134419591464234, BrannAlarmLight, UseObjectState.TsState1, 2.0f, 421.0f, -137.0f, "Brann Water Refinery alarm light 1"),
            Running(BrannWaterRefinery, 134419591464373, BrannAlarmLight, UseObjectState.TsState1, -23.5f, 427.5f, -153.25f, "Brann Water Refinery alarm light 2"),
            Running(BrannWaterRefinery, 134419591464387, BrannAlarmLight, UseObjectState.TsState1, -75.3133f, 421.0f, -137.0032f, "Brann Water Refinery alarm light 3"),
            Running(BrannWaterRefinery, 134419591464406, BrannAlarmLight, UseObjectState.TsState1, -52.25f, 427.5f, -152.75f, "Brann Water Refinery alarm light 4"),
            Running(BrannWaterRefinery, 134419591464407, BrannAlarmLight, UseObjectState.TsState1, -23.75f, 427.5f, -121.25f, "Brann Water Refinery alarm light 5"),
            Running(BrannWaterRefinery, 134419591464408, BrannAlarmLight, UseObjectState.TsState1, -52.5f, 427.5f, -120.75f, "Brann Water Refinery alarm light 6"),
            Running(BrannWaterRefinery, 134419591464559, BrannAlarmLight, UseObjectState.TsState1, -52.5f, 427.5f, -225.0f, "Brann Water Refinery alarm light 7"),
            Running(BrannWaterRefinery, 134419591464560, BrannAlarmLight, UseObjectState.TsState1, -23.75f, 427.5f, -225.5f, "Brann Water Refinery alarm light 8"),
            Running(BrannWaterRefinery, 134419591464561, BrannAlarmLight, UseObjectState.TsState1, -24.0f, 427.5f, -193.5f, "Brann Water Refinery alarm light 9"),
            Running(BrannWaterRefinery, 134419591464562, BrannAlarmLight, UseObjectState.TsState1, -52.75f, 427.5f, -193.0f, "Brann Water Refinery alarm light 10"),

            // The Bane tesla coils of Concordia Divide: active (WorldDestructibles, TeslaCoils).
            Running(ConcordiaDivide, 132770324036699, BaneTeslaCoil, UseObjectState.StatePowerDown, -360.4334f, 74.0802f, -592.9681f, "Concordia Divide tesla coil 1"),
            Running(ConcordiaDivide, 132770324036701, BaneTeslaCoil, UseObjectState.StatePowerDown, -347.3694f, 74.1182f, -551.7658f, "Concordia Divide tesla coil 2"),
            Running(ConcordiaDivide, 132770324315881, BaneTeslaCoil, UseObjectState.StatePowerDown, -752.6387f, 119.9923f, -865.8325f, "Concordia Divide tesla coil 3"),
            Running(ConcordiaDivide, 132770324749115, BaneTeslaCoil, UseObjectState.StatePowerDown, -793.5133f, 131.8226f, -815.2523f, "Concordia Divide tesla coil 4"),
            Running(ConcordiaDivide, 132770324749116, BaneTeslaCoil, UseObjectState.StatePowerDown, -844.6591f, 127.9965f, -800.4529f, "Concordia Divide tesla coil 5"),

            // The Brann monitors, on: thirty-one V01 and thirty-three V02 in the Comm Tower, four V01 in Burning Steps.
            Monitor(CommTower, 134419591463952, BrannMonitorV01, 5.6896f, 226.5989f, -14.1714f, "Comm Tower monitor 1"),
            Monitor(CommTower, 134419591464163, BrannMonitorV01, -40.1694f, 250.7855f, 100.8428f, "Comm Tower monitor 2"),
            Monitor(CommTower, 134419591464292, BrannMonitorV01, -11.4553f, 250.5626f, 44.0937f, "Comm Tower monitor 3"),
            Monitor(CommTower, 134419591464293, BrannMonitorV01, -11.4554f, 251.6273f, 44.0937f, "Comm Tower monitor 4"),
            Monitor(CommTower, 134419591464342, BrannMonitorV01, -45.8915f, 234.5242f, 131.542f, "Comm Tower monitor 5"),
            Monitor(CommTower, 134419591464429, BrannMonitorV01, -40.5197f, 249.7524f, 100.1137f, "Comm Tower monitor 6"),
            Monitor(CommTower, 134419591464578, BrannMonitorV01, 79.6341f, 242.7855f, 23.4896f, "Comm Tower monitor 7"),
            Monitor(CommTower, 134419591464587, BrannMonitorV01, 36.5315f, 242.4655f, -4.9972f, "Comm Tower monitor 8"),
            Monitor(CommTower, 134419591464710, BrannMonitorV01, -75.9235f, 250.2799f, 11.8509f, "Comm Tower monitor 9"),
            Monitor(CommTower, 134419591464740, BrannMonitorV01, -110.2525f, 251.5214f, -2.9568f, "Comm Tower monitor 10"),
            Monitor(CommTower, 134419591464945, BrannMonitorV01, -73.872f, 274.774f, -68.0116f, "Comm Tower monitor 11"),
            Monitor(CommTower, 134419591464947, BrannMonitorV01, -72.4938f, 275.2623f, -67.9698f, "Comm Tower monitor 12"),
            Monitor(CommTower, 134419591464952, BrannMonitorV01, -103.9827f, 266.4601f, -48.7736f, "Comm Tower monitor 13"),
            Monitor(CommTower, 134419591464953, BrannMonitorV01, -103.9827f, 267.5248f, -48.7736f, "Comm Tower monitor 14"),
            Monitor(CommTower, 134419591465017, BrannMonitorV01, -29.4334f, 227.2181f, 33.789f, "Comm Tower monitor 15"),
            Monitor(CommTower, 134419591465198, BrannMonitorV01, 107.6415f, 250.7032f, -15.3032f, "Comm Tower monitor 16"),
            Monitor(CommTower, 134419591465210, BrannMonitorV01, 102.7083f, 250.7855f, -53.0515f, "Comm Tower monitor 17"),
            Monitor(CommTower, 134419591465213, BrannMonitorV01, 103.0586f, 249.7524f, -52.3223f, "Comm Tower monitor 18"),
            Monitor(CommTower, 134419591465229, BrannMonitorV01, 5.3097f, 226.7032f, 193.3934f, "Comm Tower monitor 19"),
            Monitor(CommTower, 134419591465232, BrannMonitorV01, 6.0403f, 225.7524f, 192.7309f, "Comm Tower monitor 20"),
            Monitor(CommTower, 134419591465244, BrannMonitorV01, -51.0347f, 266.7032f, 136.7289f, "Comm Tower monitor 21"),
            Monitor(CommTower, 134419591465247, BrannMonitorV01, -50.2495f, 265.7524f, 136.1321f, "Comm Tower monitor 22"),
            Monitor(CommTower, 134419591465250, BrannMonitorV01, -107.9876f, 250.7737f, 39.0919f, "Comm Tower monitor 23"),
            Monitor(CommTower, 134419591465255, BrannMonitorV01, -107.3222f, 249.2524f, 38.3375f, "Comm Tower monitor 24"),
            Monitor(CommTower, 134419591465257, BrannMonitorV01, -104.722f, 250.7311f, -15.9945f, "Comm Tower monitor 25"),
            Monitor(CommTower, 134419591465259, BrannMonitorV01, -104.5862f, 250.1606f, -15.5536f, "Comm Tower monitor 26"),
            Monitor(CommTower, 134419591465262, BrannMonitorV01, -109.7274f, 250.4767f, -3.9442f, "Comm Tower monitor 27"),
            Monitor(CommTower, 134419591465347, BrannMonitorV01, -13.9901f, 227.2737f, 158.3583f, "Comm Tower monitor 28"),
            Monitor(CommTower, 134419591465387, BrannMonitorV01, 79.2665f, 241.7524f, 24.2101f, "Comm Tower monitor 29"),
            Monitor(CommTower, 134419591465396, BrannMonitorV01, -53.8683f, 267.4548f, 88.2033f, "Comm Tower monitor 30"),
            Monitor(CommTower, 134419591465397, BrannMonitorV01, -54.9231f, 266.4102f, 87.8315f, "Comm Tower monitor 31"),
            Monitor(CommTower, 134419591463954, BrannMonitorV02, 4.8277f, 226.6189f, -14.624f, "Comm Tower monitor 32"),
            Monitor(CommTower, 134419591464298, BrannMonitorV02, -11.0027f, 250.5826f, 43.2318f, "Comm Tower monitor 33"),
            Monitor(CommTower, 134419591464344, BrannMonitorV02, -45.7713f, 235.5889f, 131.623f, "Comm Tower monitor 34"),
            Monitor(CommTower, 134419591464346, BrannMonitorV02, -46.3538f, 234.5442f, 130.6853f, "Comm Tower monitor 35"),
            Monitor(CommTower, 134419591464427, BrannMonitorV02, -41.16f, 250.7032f, 100.7105f, "Comm Tower monitor 36"),
            Monitor(CommTower, 134419591464428, BrannMonitorV02, -41.3756f, 251.2737f, 101.1183f, "Comm Tower monitor 37"),
            Monitor(CommTower, 134419591464577, BrannMonitorV02, 80.1292f, 242.7032f, 24.3576f, "Comm Tower monitor 38"),
            Monitor(CommTower, 134419591464579, BrannMonitorV02, 80.5843f, 243.2737f, 24.282f, "Comm Tower monitor 39"),
            Monitor(CommTower, 134419591464585, BrannMonitorV02, 36.3865f, 243.5302f, -4.9972f, "Comm Tower monitor 40"),
            Monitor(CommTower, 134419591464586, BrannMonitorV02, 37.3934f, 242.4855f, -4.5446f, "Comm Tower monitor 41"),
            Monitor(CommTower, 134419591464711, BrannMonitorV02, -76.0559f, 250.1976f, 12.8415f, "Comm Tower monitor 42"),
            Monitor(CommTower, 134419591464739, BrannMonitorV02, -110.2525f, 250.4567f, -2.8119f, "Comm Tower monitor 43"),
            Monitor(CommTower, 134419591464946, BrannMonitorV02, -74.445f, 273.741f, -65.8285f, "Comm Tower monitor 44"),
            Monitor(CommTower, 134419591464954, BrannMonitorV02, -103.8021f, 266.2155f, -46.1963f, "Comm Tower monitor 45"),
            Monitor(CommTower, 134419591464992, BrannMonitorV02, -13.2554f, 226.7032f, -22.7853f, "Comm Tower monitor 46"),
            Monitor(CommTower, 134419591465018, BrannMonitorV02, -30.8875f, 226.7298f, 34.2361f, "Comm Tower monitor 47"),
            Monitor(CommTower, 134419591465196, BrannMonitorV02, 108.922f, 250.7855f, -15.1709f, "Comm Tower monitor 48"),
            Monitor(CommTower, 134419591465197, BrannMonitorV02, 108.5716f, 249.7524f, -15.9f, "Comm Tower monitor 49"),
            Monitor(CommTower, 134419591465211, BrannMonitorV02, 103.9145f, 251.2737f, -53.3269f, "Comm Tower monitor 50"),
            Monitor(CommTower, 134419591465212, BrannMonitorV02, 103.6988f, 250.7032f, -52.9191f, "Comm Tower monitor 51"),
            Monitor(CommTower, 134419591465214, BrannMonitorV02, 103.966f, 249.7524f, -52.3223f, "Comm Tower monitor 52"),
            Monitor(CommTower, 134419591465228, BrannMonitorV02, 5.1363f, 225.7524f, 192.8094f, "Comm Tower monitor 53"),
            Monitor(CommTower, 134419591465230, BrannMonitorV02, 6.5968f, 226.7855f, 193.4145f, "Comm Tower monitor 54"),
            Monitor(CommTower, 134419591465231, BrannMonitorV02, 5.2745f, 227.2737f, 193.8058f, "Comm Tower monitor 55"),
            Monitor(CommTower, 134419591465245, BrannMonitorV02, -49.7542f, 266.7854f, 136.8612f, "Comm Tower monitor 56"),
            Monitor(CommTower, 134419591465246, BrannMonitorV02, -51.1054f, 267.2737f, 137.1367f, "Comm Tower monitor 57"),
            Monitor(CommTower, 134419591465248, BrannMonitorV02, -51.157f, 265.7524f, 136.1321f, "Comm Tower monitor 58"),
            Monitor(CommTower, 134419591465251, BrannMonitorV02, -106.7252f, 250.2855f, 39.9409f, "Comm Tower monitor 59"),
            Monitor(CommTower, 134419591465254, BrannMonitorV02, -107.4447f, 250.2032f, 39.0524f, "Comm Tower monitor 60"),
            Monitor(CommTower, 134419591465256, BrannMonitorV02, -76.6909f, 249.2434f, 12.0563f, "Comm Tower monitor 61"),
            Monitor(CommTower, 134419591465258, BrannMonitorV02, -105.5852f, 250.2429f, -14.7417f, "Comm Tower monitor 62"),
            Monitor(CommTower, 134419591465398, BrannMonitorV02, -53.725f, 266.3901f, 88.1817f, "Comm Tower monitor 63"),
            Monitor(CommTower, 134419591465433, BrannMonitorV02, -29.7443f, 226.6476f, 34.2359f, "Comm Tower monitor 64"),
            Monitor(BurningSteps, 134419591467501, BrannMonitorV01, -200.0808f, 199.7323f, 192.3138f, "Burning Steps monitor 1"),
            Monitor(BurningSteps, 134419591467502, BrannMonitorV01, -189.4679f, 199.7323f, 192.284f, "Burning Steps monitor 2"),
            Monitor(BurningSteps, 134419591467503, BrannMonitorV01, -210.58f, 199.7323f, 192.246f, "Burning Steps monitor 3"),
            Monitor(BurningSteps, 134419591467504, BrannMonitorV01, -220.9307f, 199.7323f, 192.2075f, "Burning Steps monitor 4"),

            // The Bane teleporters: active in the Bane bases of Minos Caverns, Devil's Den and Pravus Research, switched off in Thunderhead and the Incline.
            Running(MinosCaverns, 133620727550419, BaneTeleporter, UseObjectState.TsState1, 224.5f, 11.2805f, -91.75f, "Minos Caverns Bane teleporter 1"),
            Running(MinosCaverns, 133620727550420, BaneTeleporter, UseObjectState.TsState1, 224.5f, 11.3124f, -100.25f, "Minos Caverns Bane teleporter 2"),
            Running(MinosCaverns, 133620727550453, BaneTeleporter, UseObjectState.TsState1, 156.014f, 3.3142f, 152.0728f, "Minos Caverns Bane teleporter 3"),
            Running(MinosCaverns, 133620727550715, BaneTeleporter, UseObjectState.TsState1, 178.9704f, 3.0f, 63.8991f, "Minos Caverns Bane teleporter 4"),
            Running(MinosCaverns, 133620727550723, BaneTeleporter, UseObjectState.TsState1, 215.5772f, 11.2f, -63.9974f, "Minos Caverns Bane teleporter 5"),
            Running(MinosCaverns, 133620727550724, BaneTeleporter, UseObjectState.TsState1, 224.8f, 11.2f, -64.0f, "Minos Caverns Bane teleporter 6"),
            Running(MinosCaverns, 133620727550730, BaneTeleporter, UseObjectState.TsState1, 186.8294f, 3.6319f, -22.6393f, "Minos Caverns Bane teleporter 7"),
            Running(MinosCaverns, 133620727550747, BaneTeleporter, UseObjectState.TsState1, 213.9138f, 1.6172f, 106.4332f, "Minos Caverns Bane teleporter 8"),
            Running(MinosCaverns, 133620727550748, BaneTeleporter, UseObjectState.TsState1, 213.8195f, 1.6561f, 117.689f, "Minos Caverns Bane teleporter 9"),
            Running(MinosCaverns, 133620727550820, BaneTeleporter, UseObjectState.TsState1, 158.3235f, 3.6077f, 39.121f, "Minos Caverns Bane teleporter 10"),
            Running(MinosCaverns, 133620727550821, BaneTeleporter, UseObjectState.TsState1, 158.1085f, 3.6077f, 58.7645f, "Minos Caverns Bane teleporter 11"),
            Running(DevilsDen, 133822591016206, BaneTeleporter, UseObjectState.TsState1, 302.7842f, 129.1017f, 505.733f, "Devil's Den Bane teleporter 1"),
            Running(DevilsDen, 133822591016207, BaneTeleporter, UseObjectState.TsState1, 302.8708f, 129.0f, 438.2707f, "Devil's Den Bane teleporter 2"),
            Running(DevilsDen, 133822591016208, BaneTeleporter, UseObjectState.TsState1, 317.1948f, 129.1575f, 462.8693f, "Devil's Den Bane teleporter 3"),
            Running(DevilsDen, 133822591016209, BaneTeleporter, UseObjectState.TsState1, 316.7856f, 129.1575f, 477.8071f, "Devil's Den Bane teleporter 4"),
            Running(DevilsDen, 133822591016460, BaneTeleporter, UseObjectState.TsState1, 177.9686f, 143.9858f, 680.1897f, "Devil's Den Bane teleporter 5"),
            Running(DevilsDen, 133822591016461, BaneTeleporter, UseObjectState.TsState1, 158.1994f, 143.9856f, 699.9169f, "Devil's Den Bane teleporter 6"),
            Running(DevilsDen, 133822591016462, BaneTeleporter, UseObjectState.TsState1, 178.0263f, 143.9856f, 719.7197f, "Devil's Den Bane teleporter 7"),
            Running(DevilsDen, 133822591016622, BaneTeleporter, UseObjectState.TsState1, 224.3984f, 143.6f, 694.1672f, "Devil's Den Bane teleporter 8"),
            Running(DevilsDen, 133822591016869, BaneTeleporter, UseObjectState.TsState1, -108.4758f, 184.1134f, 509.4501f, "Devil's Den Bane teleporter 9"),
            Running(DevilsDen, 133822591016872, BaneTeleporter, UseObjectState.TsState1, -82.0607f, 181.8f, 500.2865f, "Devil's Den Bane teleporter 10"),
            Running(DevilsDen, 133822591017187, BaneTeleporter, UseObjectState.TsState1, 194.8186f, 150.8167f, 655.7701f, "Devil's Den Bane teleporter 11"),
            Running(DevilsDen, 133822591017209, BaneTeleporter, UseObjectState.TsState1, 237.2841f, 143.6f, 690.1017f, "Devil's Den Bane teleporter 12"),
            Running(DevilsDen, 133822591017212, BaneTeleporter, UseObjectState.TsState1, 220.3609f, 143.6f, 704.5945f, "Devil's Den Bane teleporter 13"),
            Running(DevilsDen, 133822591017213, BaneTeleporter, UseObjectState.TsState1, 230.1767f, 143.6f, 681.7839f, "Devil's Den Bane teleporter 14"),
            Running(PravusResearch, 133981504848293, BaneTeleporter, UseObjectState.TsState1, 143.8782f, 9.5727f, 151.4298f, "Pravus Research Bane teleporter 1"),
            Running(PravusResearch, 133981504848294, BaneTeleporter, UseObjectState.TsState1, 143.8509f, 9.5727f, 184.56f, "Pravus Research Bane teleporter 2"),
            Running(PravusResearch, 133981504848602, BaneTeleporter, UseObjectState.TsState1, 184.0f, 8.0148f, 128.0f, "Pravus Research Bane teleporter 3"),
            Running(PravusResearch, 133981504848603, BaneTeleporter, UseObjectState.TsState1, 264.0f, 8.0148f, 128.0f, "Pravus Research Bane teleporter 4"),
            Running(PravusResearch, 133981504848604, BaneTeleporter, UseObjectState.TsState1, 264.0f, 8.0148f, 208.0f, "Pravus Research Bane teleporter 5"),
            Running(PravusResearch, 133981504848605, BaneTeleporter, UseObjectState.TsState1, 184.0f, 8.0148f, 208.0f, "Pravus Research Bane teleporter 6"),
            Running(Incline, 134419591482129, BaneTeleporter, UseObjectState.TsState0, -443.8862f, 250.2645f, -31.8161f, "Incline Bane teleporter 1"),
            Running(Thunderhead, 134419591464848, BaneTeleporter, UseObjectState.TsState0, 0.0f, 377.3421f, 134.0f, "Thunderhead Bane teleporter 1"),

            // The four human teleporters of Thunderhead Base with no local teleporter behind them, on the deck above the working ones: disabled.
            Running(Thunderhead, 134419591464289, HumanTeleporter, UseObjectState.TsState1, 653.4601f, 383.0f, -496.3599f, "Thunderhead disabled human teleporter 1"),
            Running(Thunderhead, 134419591464290, HumanTeleporter, UseObjectState.TsState1, 640.3851f, 383.0f, -509.596f, "Thunderhead disabled human teleporter 2"),
            Running(Thunderhead, 134419591476322, HumanTeleporter, UseObjectState.TsState1, 659.4149f, 383.0f, -490.4725f, "Thunderhead disabled human teleporter 3"),
            Running(Thunderhead, 134419591476326, HumanTeleporter, UseObjectState.TsState1, 634.4655f, 383.0f, -515.3791f, "Thunderhead disabled human teleporter 4"),

            // The Eloh temple spawners, on.
            Running(ElohTemples, 134419591464763, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, 8.0f, -44.0f, "the Eloh Temples Eloh temple spawner 1"),
            Running(ElohTemples, 134419591464768, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, 8.0f, -76.0f, "the Eloh Temples Eloh temple spawner 2"),
            Running(ElohTemples, 134419591464769, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, 8.0f, -44.0f, "the Eloh Temples Eloh temple spawner 3"),
            Running(ElohTemples, 134419591464770, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, 8.0f, -76.0f, "the Eloh Temples Eloh temple spawner 4"),
            Running(ElohTemples, 134419591464778, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, -8.0f, 148.0f, "the Eloh Temples Eloh temple spawner 5"),
            Running(ElohTemples, 134419591464780, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, -8.0f, 116.0f, "the Eloh Temples Eloh temple spawner 6"),
            Running(ElohTemples, 134419591464782, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, -8.0f, 148.0f, "the Eloh Temples Eloh temple spawner 7"),
            Running(ElohTemples, 134419591464783, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, -8.0f, 116.0f, "the Eloh Temples Eloh temple spawner 8"),
            Running(ElohTemples, 134419591464792, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, -24.0f, 308.0f, "the Eloh Temples Eloh temple spawner 9"),
            Running(ElohTemples, 134419591464794, ElohTemplesSpawner, UseObjectState.TsState1, -600.0f, -24.0f, 340.0f, "the Eloh Temples Eloh temple spawner 10"),
            Running(ElohTemples, 134419591464795, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, -24.0f, 308.0f, "the Eloh Temples Eloh temple spawner 11"),
            Running(ElohTemples, 134419591464797, ElohTemplesSpawner, UseObjectState.TsState1, -424.0f, -24.0f, 340.0f, "the Eloh Temples Eloh temple spawner 12"),
            Running(ElohTemples, 134419591464800, ElohTemplesSpawner, UseObjectState.TsState1, -528.0f, -24.0f, 412.0f, "the Eloh Temples Eloh temple spawner 13"),
            Running(ElohTemples, 134419591464801, ElohTemplesSpawner, UseObjectState.TsState1, -496.0f, -24.0f, 412.0f, "the Eloh Temples Eloh temple spawner 14"),
            Running(ElohTemples, 134419591464808, ElohTemplesSpawner, UseObjectState.TsState1, -828.0f, 8.0f, -112.0f, "the Eloh Temples Eloh temple spawner 15"),
            Running(ElohTemples, 134419591464809, ElohTemplesSpawner, UseObjectState.TsState1, -828.0f, 8.0f, -144.0f, "the Eloh Temples Eloh temple spawner 16"),
            Running(ElohTemples, 134419591464811, ElohTemplesSpawner, UseObjectState.TsState1, -652.0f, 8.0f, -144.0f, "the Eloh Temples Eloh temple spawner 17"),
            Running(ElohTemples, 134419591464812, ElohTemplesSpawner, UseObjectState.TsState1, -652.0f, 8.0f, -112.0f, "the Eloh Temples Eloh temple spawner 18"),
            Running(ElohTemples, 134419591464837, ElohTemplesSpawner, UseObjectState.TsState1, -828.0f, -8.0f, 48.0f, "the Eloh Temples Eloh temple spawner 19"),

            // The Bane logos stone analyzers of Minos Caverns and Devil's Den, on.
            Running(MinosCaverns, 133620727550316, LogosStoneAnalyzer, UseObjectState.TsState1, 172.0f, 2.0f, -96.0f, "Minos Caverns logos stone analyzer 1"),
            Running(DevilsDen, 133822591015846, LogosStoneAnalyzer, UseObjectState.TsState1, 177.9305f, 141.6172f, 700.0169f, "Devil's Den logos stone analyzer 1"),

            // The Dell XPS notebooks and towers, screens on; a player can turn one off and on again.
            Computer(Mires, 134419591476281, DellNotebook, 651.3418f, 225.2908f, 374.9236f, "the Mires Dell XPS notebook 1"),
            Computer(TordenPlains, 134419591474839, DellTower, -542.7815f, 450.9174f, 426.9781f, "Torden Plains Dell XPS tower 1"),
            Computer(Abyss, 134419591470385, DellNotebook, -690.0879f, 537.459f, -197.6664f, "the Abyss Dell XPS notebook 1"),
            Computer(Abyss, 134419591470455, DellNotebook, -672.6851f, 537.459f, -213.2009f, "the Abyss Dell XPS notebook 2"),
            Computer(Abyss, 134419591470456, DellNotebook, -673.464f, 537.459f, -213.2143f, "the Abyss Dell XPS notebook 3"),
            Computer(Abyss, 134419591470459, DellNotebook, -676.7834f, 537.459f, -213.2594f, "the Abyss Dell XPS notebook 4"),
            Computer(Abyss, 134419591470482, DellNotebook, -652.599f, 537.459f, -197.0656f, "the Abyss Dell XPS notebook 5"),
            Computer(Abyss, 134419591470483, DellNotebook, -653.8553f, 537.459f, -197.0096f, "the Abyss Dell XPS notebook 6"),
            Computer(Abyss, 134419591471262, DellNotebook, -12.4639f, 535.0626f, -141.12f, "the Abyss Dell XPS notebook 7"),
            Computer(Abyss, 134419591470383, DellTower, -671.2767f, 537.459f, -197.1556f, "the Abyss Dell XPS tower 1"),
            Computer(Abyss, 134419591470392, DellTower, -690.9185f, 537.459f, -198.382f, "the Abyss Dell XPS tower 2"),
            Computer(Abyss, 134419591470452, DellTower, -671.2449f, 537.459f, -213.1727f, "the Abyss Dell XPS tower 3"),
            Computer(Abyss, 134419591470457, DellTower, -677.7497f, 537.459f, -213.0578f, "the Abyss Dell XPS tower 4"),
            Computer(Abyss, 134419591470458, DellTower, -675.7101f, 537.459f, -213.1527f, "the Abyss Dell XPS tower 5"),
            Computer(Abyss, 134419591470471, DellTower, -655.4609f, 537.459f, -196.7045f, "the Abyss Dell XPS tower 6"),
            Computer(Abyss, 134419591470480, DellTower, -651.5505f, 537.459f, -197.1041f, "the Abyss Dell XPS tower 7"),
        };

        /// <summary>A Brann dissection table, intact.</summary>
        private static Usable DissectionTable(uint mapContextId, ulong entityId, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, BrannDissectionTable, new Vector3(x, y, z), UseObjectState.IdesStateIntact, null, name);

        /// <summary>A Bane stasis chamber, intact.</summary>
        private static Usable StasisChamber(uint mapContextId, ulong entityId, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, BaneStasisChamber, new Vector3(x, y, z), UseObjectState.IdesStateIntact, null, name);

        /// <summary>A two-state machine in the state that has it running.</summary>
        private static Usable Running(uint mapContextId, ulong entityId, uint classId, UseObjectState state, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, classId, new Vector3(x, y, z), state, null, name);

        /// <summary>A Bane articulated drill, intact.</summary>
        private static Usable Drill(uint mapContextId, ulong entityId, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, BaneArticulatedDrill, new Vector3(x, y, z), UseObjectState.IdesStateIntact, null, name);

        /// <summary>A Dell XPS computer, its screen on; a use turns it off, and the next on again.</summary>
        private static Usable Computer(uint mapContextId, ulong entityId, uint classId, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, classId, new Vector3(x, y, z), UseObjectState.TsState1, UseObjectState.TsState0, name);

        /// <summary>A Brann monitor, on.</summary>
        private static Usable Monitor(uint mapContextId, ulong entityId, uint classId, float x, float y, float z, string name) =>
            new Usable(mapContextId, entityId, classId, new Vector3(x, y, z), UseObjectState.TsState1, null, name);

        /// <summary>How long a use takes, as a footlocker's (DynamicObjectManager, Lockbox).</summary>
        public const int UseWindupMs = 100;

        /// <summary>Each map channel's states, of the usables used on it.</summary>
        private static readonly ConditionalWeakTable<MapChannel, Dictionary<ulong, UseObjectState>> States = new();

        /// <summary>The map's usables that are given a state.</summary>
        public static IEnumerable<Usable> OnMap(uint mapContextId) => All.Where(usable => usable.MapContextId == mapContextId);

        /// <summary>The usable with this id, on any map, or null.</summary>
        public static Usable Find(ulong entityId) => entityId == 0 ? null : All.FirstOrDefault(usable => usable.EntityId == entityId);

        /// <summary>
        /// Sets the state a usable is in on a map channel, which is what whoever arrives is sent. The
        /// clients already there are told by whoever changes it (WorldDestructibles, the fire pits' use).
        /// </summary>
        internal static void SetState(MapChannel mapChannel, ulong entityId, UseObjectState state)
        {
            if (mapChannel != null)
                States.GetOrCreateValue(mapChannel)[entityId] = state;
        }

        /// <summary>The state a usable is in on a map channel.</summary>
        public static UseObjectState StateOf(MapChannel mapChannel, Usable usable)
        {
            if (mapChannel != null && States.TryGetValue(mapChannel, out var states) && states.TryGetValue(usable.EntityId, out var state))
                return state;

            return usable.State;
        }

        /// <summary>
        /// Gives the player's client the state of each usable of the map they have arrived on.
        /// Returns how many were sent.
        /// </summary>
        public static int PlayerEnteredMap(Client client)
        {
            var map = client?.Player?.MapChannel?.MapInfo;

            if (map == null)
                return 0;

            var sent = 0;

            foreach (var usable in OnMap(map.MapContextId))
            {
                client.CallMethod(usable.EntityId, new ForceStatePacket(StateOf(client.Player.MapChannel, usable), 0));
                sent++;
            }

            return sent;
        }

        /// <summary>
        /// RequestUseObject: whether it names one of these. If it does it is answered here, used
        /// or refused, and is nothing else's to answer.
        /// </summary>
        public static bool TryRequestUse(Client client, RequestUseObjectPacket packet)
        {
            var usable = Find(packet?.EntityId ?? 0);

            if (usable == null)
                return false;

            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null || mapChannel?.MapInfo == null || client.State != ClientState.Ingame)
                return true;

            // Another map's: the client has no such entity, so only a client that made it up asks.
            if (mapChannel.MapInfo.MapContextId != usable.MapContextId)
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} asked to use {usable}, which is not on their map. Ignored.");
                return true;
            }

            if (packet.ActionId != ActionId.UseObject || usable.UsedState == null)
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmUseObjectNotUsable);
                return true;
            }

            if (player.State == CharacterState.Dead)
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmCannotPerformActionNow);
                return true;
            }

            if (Vector3.Distance(player.Position, usable.Position) > DynamicObjectManager.MaxUseDistance)
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmTargetOutOfRange);
                return true;
            }

            // One use of anything at a time for the player, and one use of the pit at a time.
            if (mapChannel.PerformRecovery.Any(action => action.ActionId == ActionId.UseObject &&
                    (action.Actor == player || action.SourceId == usable.EntityId)))
            {
                ActorManager.RefuseRequest(client, packet.ActionId, packet.ActionArgId, PlayerMessage.PmCannotPerformActionNow);
                return true;
            }

            client.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
            mapChannel.PerformRecovery.Add(new ActionData(player, packet.ActionId, packet.ActionArgId, UseWindupMs) { SourceId = usable.EntityId });

            return true;
        }

        /// <summary>Whether a use-object action is a use of one of these.</summary>
        public static bool IsUseOf(ActionData action) =>
            action != null && action.ActionId == ActionId.UseObject && Find(action.SourceId) != null;

        /// <summary>
        /// The recovery of a use of one of these: unless it was interrupted, or its user died or
        /// left the map in the meantime, the usable goes to its other state on the channel, and
        /// every client on the channel is told. Returns the state it went to, or null.
        /// </summary>
        public static UseObjectState? UseRecovery(MapChannel mapChannel, ActionData action)
        {
            var usable = Find(action?.SourceId ?? 0);

            if (usable == null || mapChannel?.MapInfo?.MapContextId != usable.MapContextId || action.IsInrerrupted)
                return null;

            if (action.Actor is not Manifestation user || user.State == CharacterState.Dead || user.MapChannel != mapChannel)
                return null;

            var next = usable.UsedFrom(StateOf(mapChannel, usable));

            if (next == null)
                return null;

            States.GetOrCreateValue(mapChannel)[usable.EntityId] = next.Value;

            // Everyone on the map has it, near or not: the client loads the whole .map. One
            // still loading it is sent the state when it arrives (PlayerEnteredMap).
            foreach (var client in mapChannel.ClientList.ToList())
                if (client?.Player != null && !client.AwaitingMapLoaded)
                    client.CallMethod(usable.EntityId, new UsePacket(user.EntityId, next.Value, 0));

            return next;
        }
    }
}

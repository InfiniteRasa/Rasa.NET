using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// To a Subterranean Chamber (native mission 1674), Valverde Marshes: Retread Vincent in Retread City tells the player of
    /// the Retreads' secret teleporter in the lake, to the Eloh chamber under it, where the Eloh Jumna speaks with the player.
    ///
    /// Native: texts 16109-16113 (missionconversation): log "Swim to the secret location in the lake near Retread City to
    /// locate and speak with the Eloh hidden nearby.", offer "Out in the middle of the lake here, by some rocks, we've got a
    /// secret teleporter that will take you to the Eloh chamber", turn-in (Jumna) "Now go - seek out my brother on Arieki";
    /// objective 1 "Locate and speak with the Eloh Guardian" 16119/16120 (missionobjective); objective conversation 1674,1
    /// package 2036 16121 (Jumna: "Ahhh, another Human come to my chamber - and a receptive, no less!"). Indicator name 384
    /// "Eloh Chamber" (missionobjectiveindicatorlanguage). Name 9951 Jumna.
    ///
    /// Sourced: TaRapedia To a Subterranean Chamber: giver Retread Vincent; requires Leggo My Logos; level 38; 40,000 XP,
    /// 3,900 credits; one of Class VII Advanced Med Pack (2), Class VII Armor Charger (2), Class VII EMP Grenade (4), Class
    /// VII Concussion Grenade (4); the lake teleporter in "the horseshoe of three rocks". TaRapedia Jumna: Eloh, level 45,
    /// "Retread City Subterranean Chamber", -160, 111, -902; his debrief is the turn-in text (the infobox's receiver Vincent
    /// is not: the client's objective and texts are the Eloh's, the triage's conflict 5).
    ///
    /// Reconstructed (ESTIMATE): the receiver Jumna (MarshesLogosResearchNpcsV1, public in the chamber); his talk completes 1.
    /// The way down and back the two local teleporters of MarshesLogosResearchNpcsV1 (the lake's rock under the lily glows,
    /// the chamber's plates). Markers: the client's "Eloh Chamber" on Jumna, and one at the lake's teleporter.
    /// </summary>
    public static class MarshesSubterraneanChamberV1
    {
        public const uint MissionId = 1674;
        public const uint Level = 38;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, Jumna = MarshesLogosResearchNpcsV1.Jumna;
        public const uint Speak = 1;
        public const uint ElohChamber = 384;            // missionobjectiveindicatorlanguage

        public const uint Experience = 40000;
        public const uint Credits = 3900;
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;   // Class VII Advanced Med Pack
        public const uint ArmorCharger = 118898, ArmorChargers = 2;        // Class VII Armor Charger
        public const uint EmpGrenade = 111028, EmpGrenades = 4;            // Class VII EMP Grenade
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4;   // Class VII Concussion Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "To a Subterranean Chamber", 16109, Vincent, Jumna, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[]
            {
                (AdvancedMedPack, AdvancedMedPacks), (ArmorCharger, ArmorChargers), (EmpGrenade, EmpGrenades), (ConcussionGrenade, ConcussionGrenades)
            }, new (uint, uint)[0]);
            mission.Prerequisite(MarshesLeggoMyLogosV1.MissionId);

            mission.Objective(Speak, 16119, 16120, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, MarshesLogosResearchNpcsV1.JumnaPackage);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);

            var jumna = MarshesLogosResearchNpcsV1.JumnaAt;
            var pad = MarshesLogosResearchNpcsV1.LakePadAt;
            mission.NamedIndicator(Speak, ElohChamber, jumna.X, jumna.Y, jumna.Z);
            mission.Indicator(Speak, 1, pad.X, pad.Y, pad.Z, 6);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1674",
                "Native1 'Locate and speak with the Eloh Guardian' 16119/16120; objectiveconversation 1674,1 pkg2036 16121 (Jumna). " +
                "Texts 16109-16113. Indicator name384 'Eloh Chamber'. Name9951 Jumna. Map: the Eloh chamber under the lake " +
                "(ArchElohTemplesRoomSpiritV01 -160,136,-796, floor y112), the lily glows on the lake (-160,214.2,-868).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/To_a_Subterranean_Chamber",
                "Giver Retread Vincent; requires Leggo My Logos; level38; 40000XP, 3900 credits; Class VII Advanced Med Pack (2)/" +
                "Armor Charger (2)/EMP Grenade (4)/Concussion Grenade (4); lake teleporter in the horseshoe of three rocks. Jumna: " +
                "Eloh, level45, -160,111,-902.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesSubterraneanChamberV1",
                "Receiver Jumna 760372 (the client's texts; TaRapedia's infobox says Vincent). Public local teleporters 907 (lake " +
                "rock -160,213.68,-868) and 908 (chamber plates -160,112.43,-808), linked both ways, no mission gate.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}

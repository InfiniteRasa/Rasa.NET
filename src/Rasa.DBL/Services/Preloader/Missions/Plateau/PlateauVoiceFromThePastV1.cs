using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// A Voice From The Past (native mission 934), Plateau: Caretaker Tendahi of Mount Reverence sends the
    /// player into Vogren's Tomb, where Vogren's hologram speaks. It opens Trinity Bridge Logos: Past (1044).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5026-5029 and 6479 (missionconversation); objective
    /// 1 "Investigate Vogren's Tomb." 5053/6480, no counter (missionobjective); the hologram's "I am a
    /// holographic representation of the Forean known as Vogren." on package 1014 (5054)
    /// (objectiveconversation, flag 1). Tendahi and the hologram are PlateauVogrenNpcsV1's creatures 750027
    /// and 750028.
    ///
    /// Sourced (TaRapedia A Voice From The Past, stub): giver Caretaker Tendahi, Mount Reverence; reward from
    /// the Holographic Projection Device, Vogren's Tomb; level "32 (possibly less)", no requirement,
    /// follow-up Trinity Bridge Logos: Past; 22,500 XP, 3,200 credits, "Consumables".
    ///
    /// Reconstructed (ESTIMATE): level 32; "Consumables" read as The Defiant Ones' (887), the same XP and
    /// credits: two Class VI Standard Med Packs and a Res Trauma Kit, given with the mission, no choice.
    /// </summary>
    public static class PlateauVoiceFromThePastV1
    {
        public const uint MissionId = 934;
        public const uint Level = 32;
        public const uint Tendahi = PlateauVogrenNpcsV1.Tendahi;
        public const uint Hologram = PlateauVogrenNpcsV1.Hologram, HologramPackage = PlateauVogrenNpcsV1.HologramPackage;

        public const uint Experience = 22500;
        public const uint Credits = 3200;
        public const uint MedPack = PlateauDefiantOnesV1.MedPack, MedPacks = PlateauDefiantOnesV1.MedPacks;
        public const uint ResKit = PlateauDefiantOnesV1.ResKit;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "A Voice From The Past", 5026, Tendahi, Hologram, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new (uint, uint)[] { (MedPack, MedPacks), (ResKit, 1) });

            mission.Objective(1, 5053, 6480, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, HologramPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var at = PlateauVogrenNpcsV1.HologramAt;
            mission.Indicator(1, 1, at.X, at.Y, at.Z);         // the hologram in Vogren's Tomb

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/934",
                "Native1 'Investigate Vogren's Tomb.' 5053/6480, no counter; objectiveconversation 934,1,1014 text5054. " +
                "Texts 5026-5029, 6479. Tomb 7649 at -672,465,540 (client map); no creature name for Tendahi or Vogren.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Voice_From_The_Past",
                "Stub. Giver Caretaker Tendahi, Mount Reverence; reward from the Holographic Projection Device, Vogren's Tomb; " +
                "level '32 (possibly less)'; no requirement; follow-up Trinity Bridge Logos: Past; 22500XP, 3200 credits, " +
                "'Consumables'.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauVoiceFromThePastV1",
                "Level32. Consumables as The Defiant Ones': 2 Standard Med Packs 45058, 1 Res Trauma Kit 47598. Tendahi and a " +
                "stand-in hologram creature (class24591, package1014) added (PlateauVogrenNpcsV1).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}

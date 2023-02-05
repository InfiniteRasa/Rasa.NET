namespace Rasa.Structures
{
    using Structures.World;

    public class Mission : MissionInfo
    {
        public uint MissionId { get; set; }
        public uint MissionGiver { get; set; }
        public uint MissionReciver { get; set; }

        public Mission(NpcMissionEntry mission)
        {
            MissionId = mission.Id;
            MissionGiver = mission.GiverId;
            MissionReciver = mission.ReciverId;
            MissionConstantData.Level = mission.Level;
            MissionConstantData.GroupType = mission.GroupType;
            MissionConstantData.CategoryId = mission.CategoryId;
            MissionConstantData.Shareable = mission.Shareable;
            MissionConstantData.RadioCompletable = mission.RadioCompleteable;
        }
    }
}

namespace Rasa.Structures
{
    public class Mission
    {
        public uint MissionId { get; set; }
        public uint MissionGiver { get; set; }
        public uint MissionReciver { get; set; }
        public MissionInfo MissionInfo = new MissionInfo();

        public Mission(uint missionId)
        {
            MissionId = missionId;
        }
    }
}

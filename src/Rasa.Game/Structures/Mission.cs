namespace Rasa.Structures
{
    public class Mission
    {
        public uint MissionId { get; set; }
        public int MissionGiver { get; set; }
        public int MissionReciver { get; set; }
        public MissionInfo MissionInfo = new MissionInfo();

        public Mission(uint missionId)
        {
            MissionId = missionId;
        }
    }
}

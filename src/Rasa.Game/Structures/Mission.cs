namespace Rasa.Structures
{
    public class Mission
    {
        public int MissionId { get; set; }
        public int MissionGiver { get; set; }
        public int MissionReciver { get; set; }
        public MissionInfo MissionInfo = new MissionInfo();

        public Mission(int missionId)
        {
            MissionId = missionId;
        }
    }
}

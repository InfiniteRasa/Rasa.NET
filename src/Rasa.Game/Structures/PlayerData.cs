namespace Rasa.Structures
{
    public class PlayerData
    {
        public Actor Actor { get; set; }
        //public ActorAppearanceData AppearanceData { get; set; }
        public MapChannelClient ControllerUser { get; set; }
        public int RaceId { get; set; }
        public int ClassId { get; set; }
    }
}

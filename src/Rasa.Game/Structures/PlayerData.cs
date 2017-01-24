using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class PlayerData
    {
        public Actor Actor { get; set; }
        public Client Client { get; set; }
        public Dictionary<int, AppearanceData> AppearanceData { get; set; }
        public MapChannelClient ControllerUser { get; set; }
        public uint CharacterId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public uint AccountId { get; set; }
        public int SlotId { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int RaceId { get; set; }
        public int ClassId { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public int CloneCredits { get; set; }
        public int NumLogins { get; set; }
        public int TotalTimePlayed { get; set; }
        public int TimeSinceLastPlayed { get; set; }
        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public int Credits { get; set; }
        public int Prestige { get; set; }
        public int SpentBody { get; set; }
        public int SpentMind { get; set; }
        public int SpentSpirit { get; set; }
        public Dictionary<int, SkillsData> Skills = new Dictionary<int, SkillsData>();
        public Dictionary<int, AbilityDrawerData> Abilities = new Dictionary<int, AbilityDrawerData>();
        public List<int> Titles { get; set; } = new List<int>();
        public int CurrentAbilityDrawer { get; set; }
        public int MissionStateCount { get; set; }
        //public CharacterMissionData MissionStateData { get; set; }
        public int LoginTime { get; set; }
        public List<byte> Logos = new List<byte>();
    }
}

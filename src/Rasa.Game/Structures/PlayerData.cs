using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class PlayerData
    {
        public Actor Actor { get; set; }
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        public MapChannelClient ControllerUser { get; set; }
        public uint CharacterId { get; set; }
        public uint AccountId { get; set; }
        public uint CharacterSlot { get; set; }
        public string FamilyName { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int Race { get; set; }
        public uint Class { get; set; }
        public uint Experience { get; set; }
        public byte Level { get; set; }
        public uint Body { get; set; }
        public uint Mind { get; set; }
        public uint Spirit { get; set; }
        public uint CloneCredits { get; set; }
        public uint NumLogins { get; set; }
        public uint TotalTimePlayed { get; set; }
        public DateTime? TimeSinceLastPlayed { get; set; }
        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public uint LockboxCredits { get; set; }
        public uint LockboxTabs { get; set; }
        public int Credits { get; set; }
        public int Prestige { get; set; }
        public int SpentBody { get; set; }
        public int SpentMind { get; set; }
        public int SpentSpirit { get; set; }
        public Dictionary<int, SkillsData> Skills = new Dictionary<int, SkillsData>();
        public Dictionary<int, AbilityDrawerData> Abilities = new Dictionary<int, AbilityDrawerData>();
        public List<uint> Titles { get; set; } = new List<uint>();
        public uint CurrentTitle { get; set; }
        public int CurrentAbilityDrawer { get; set; }
        public Dictionary<int, MissionLog> Missions { get; set; } = new Dictionary<int, MissionLog>();
        public int LoginTime { get; set; }
        public List<int> Logos = new List<int>();
        public bool WeaponReady { get; set; }
        public uint TargetEntityId { get; set; }
    }
}

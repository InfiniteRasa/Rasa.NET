using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class Manifestation
    {
        public Actor Actor { get; set; }
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        public uint CharacterId { get; set; }
        public List<CharacterOptions> CharacterOptions = new List<CharacterOptions>();
        public string FamilyName { get; set; }
        public double Scale { get; set; }
        public int Race { get; set; }
        public uint Class { get; set; }
        public uint Experience { get; set; }
        public byte Level { get; set; }
        public uint CloneCredits { get; set; }
        public uint NumLogins { get; set; }
        public uint TotalTimePlayed { get; set; }
        public DateTime? TimeSinceLastPlayed { get; set; }
        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public uint LockboxCredits { get; set; }
        public uint LockboxTabs { get; set; }
        public Dictionary<CurencyType, uint> Credits = new Dictionary<CurencyType, uint>();
        //public uint Prestige { get; set; }
        public List<ResistanceData> ResistanceData = new List<ResistanceData>();
        public int SpentBody { get; set; }
        public int SpentMind { get; set; }
        public int SpentSpirit { get; set; }
        public Dictionary<int, SkillsData> Skills = new Dictionary<int, SkillsData>();
        public Dictionary<int, AbilityDrawerData> Abilities = new Dictionary<int, AbilityDrawerData>();
        public List<uint> Titles { get; set; } = new List<uint>();
        public uint CurrentTitle { get; set; }
        public int CurrentAbilityDrawer { get; set; }
        public Dictionary<int, MissionLog> Missions { get; set; } = new Dictionary<int, MissionLog>();
        public DateTime LoginTime { get; set; }
        public List<int> Logos = new List<int>();
        public uint TargetEntityId { get; set; }
        public uint TrackingTargetEntityId { get; set; }
        public byte ActiveWeapon { get; set; }
        public List<uint> GainedWaypoints = new List<uint>();

        public Manifestation(CharacterEntry character, Dictionary<EquipmentData, AppearanceData> appearence)
        {
            // CharacterData
            CharacterId = character.Id;
            Scale = character.Scale;
            Race = character.Race;
            Class = character.Class;
            Experience = character.Experience;
            Level = character.Level;
            SpentBody = character.Body;
            SpentMind = character.Mind;
            SpentSpirit = character.Spirit;
            CloneCredits = character.CloneCredits;
            Credits.Add(CurencyType.Credits, character.Credits);
            Credits.Add(CurencyType.Prestige, character.Prestige);
            ActiveWeapon = character.ActiveWeapon;
            NumLogins = character.NumLogins + 1;
            TotalTimePlayed = character.TotalTimePlayed;
            TimeSinceLastPlayed = character.LastLogin;
            // AppearanceData
            AppearanceData = appearence;
        }
    }
}

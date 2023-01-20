using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Char;
    using Data;
    using Repositories.Char.Character;

    public class Manifestation : Actor, ICharacterChange
    {
        public uint Id { get; set; }
        public uint Gender { get; set; }
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        public List<CharacterOptions> CharacterOptions = new();
        public double Scale { get; set; }
        public int Race { get; set; }
        public uint Class { get; set; }
        public uint Experience { get; set; }
        public byte Level { get; set; }
        public uint CloneCredits { get; set; }
        public uint NumLogins { get; set; }
        public uint TotalTimePlayed { get; set; }
        public DateTime? TimeSinceLastPlayed { get; set; }
        public uint ClanId { get; set; }
        public string ClanName { get; set; }
        public int LockboxCredits { get; set; }
        public int LockboxTabs { get; set; }
        public Dictionary<CurencyType, int> Credits = new();

        public List<ResistanceData> ResistanceData = new();
        public int SpentBody { get; set; }
        public int SpentMind { get; set; }
        public int SpentSpirit { get; set; }
        public Dictionary<SkillId, SkillsData> Skills = new();
        public Dictionary<int, AbilityDrawerData> Abilities = new();
        public List<uint> Titles { get; set; } = new List<uint>();
        public uint CurrentTitle { get; set; }
        public int CurrentAbilityDrawer { get; set; }
        public Dictionary<int, MissionLog> Missions { get; set; } = new();
        public DateTime LoginTime { get; set; }
        public List<uint> Logos = new();
        public ulong TargetEntityId { get; set; }
        public ulong TrackingTargetEntityId { get; set; }
        public byte ActiveWeapon { get; set; }
        public List<uint> GainedWaypoints = new();
        public bool IsAFK { get; set; }

        // Inventory
        public Inventory Inventory { get; set; } = new Inventory();

        // Party
        internal uint PartyId { get; set; }
        internal ulong PartyInviterId { get; set; }

        // Social
        internal List<uint> Friends = new();
        internal List<uint> IgnoredPlayers = new();
        public MapChannel MapChannel { get; set; }
        public bool Disconected { get; set; }
        public bool LogoutActive { get; set; }
        public bool RemoveFromMap { get; set; }
        // chat
        public int JoinedChannels { get; set; }
        public int[] ChannelHashes = new int[14];
        // gm flags
        public bool GmFlagAlwaysFriendly { get; set; }


        public Manifestation()
        {
        }

        public Manifestation(CharacterEntry character, Dictionary<EquipmentData, AppearanceData> appearence)
        {
            // CharacterData
            Id = character.Id;
            Scale = character.Scale;
            Race = character.Race;
            Class = character.Class;
            Experience = character.Experience;
            Level = character.Level;
            SpentBody = character.Body;
            SpentMind = character.Mind;
            SpentSpirit = character.Spirit;
            CloneCredits = character.CloneCredits;
            Credits.Add(CurencyType.Credits, character.Credit);
            Credits.Add(CurencyType.Prestige, character.Prestige);
            ActiveWeapon = character.ActiveWeapon;
            NumLogins = character.NumLogins + 1;
            TotalTimePlayed = character.TotalTimePlayed;
            TimeSinceLastPlayed = character.LastLogin;
            // AppearanceData
            AppearanceData = appearence;
            // Actor
            EntityClass = character.Gender == 0 ? EntityClasses.HumanBaseMale : EntityClasses.HumanBaseFemale;
            Name = character.Name;
            FamilyName = character.GameAccount.FamilyName;
            Position = new Vector3((float)character.CoordX, (float)character.CoordY, (float)character.CoordZ);
            Rotation = (float)character.Rotation;
            MapContextId = character.MapContextId;
            IsRunning = character.IsRunning();
            InCombatMode = false;
            Attributes = new Dictionary<Attributes, ActorAttributes>() {
                        { Data.Attributes.Body, new ActorAttributes(Data.Attributes.Body, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Mind, new ActorAttributes(Data.Attributes.Mind, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Spirit, new ActorAttributes(Data.Attributes.Spirit, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Health, new ActorAttributes(Data.Attributes.Health, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Chi, new ActorAttributes(Data.Attributes.Chi, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Power, new ActorAttributes(Data.Attributes.Power, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Aware, new ActorAttributes(Data.Attributes.Aware, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Armor, new ActorAttributes(Data.Attributes.Armor, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Speed, new ActorAttributes(Data.Attributes.Speed, 0, 0, 0, 0, 0) },
                        { Data.Attributes.Regen, new ActorAttributes(Data.Attributes.Regen, 0, 0, 0, 0, 0) }
                };
        }
    }
}

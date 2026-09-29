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
        private uint _id;
        public uint Id
        {
            get => _id;
            set
            {
                if (_id != value)
                    MissionLocationEpoch = Guid.NewGuid();
                _id = value;
            }
        }
        public uint Gender { get; set; }
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        public List<CharacterOptions> CharacterOptions = new();
        public double Scale { get; set; }
        public Race Race { get; set; }
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
        public Dictionary<uint, MissionLog> Missions { get; set; } = new();
        public Dictionary<uint, MissionState> MissionHistory { get; set; } = new();
        public HashSet<uint> MissionSuccessHistory { get; set; } = new();
        public Dictionary<uint, DateTime> MissionRewardTimes { get; set; } = new();
        internal bool StartingExperienceCompleted { get; set; }
        public Dictionary<uint, uint> PlayerFlags { get; set; } = new();
        public DateTime LoginTime { get; set; }
        public List<uint> Logos = new();

        /// <summary>Actions this player may not perform, each with the reasons it is blocked for; see Managers.ActionBlocks.</summary>
        public Dictionary<ActionId, HashSet<string>> ActionBlocks { get; } = new();
        public byte ActiveWeapon { get; set; }
        public List<CharacterTeleporterEntry> GainedWaypoints = new();
        public bool IsAFK { get; set; }

        /// <summary>
        /// The best quality the client will pick up by walking over a corpse. Set by
        /// SetAutoLootThreshold, which the client sends at login and whenever the option changes.
        /// Junk is the client's own default (gameui: GetOptionString(..., 'Junk')), so an account
        /// that has never touched the option auto-loots junk and nothing else.
        /// </summary>
        public LootQuality AutoLootThreshold { get; set; } = LootQuality.Junk;

        /// <summary>
        /// Environment.TickCount64 at the player's last movement or action. Monotonic, so a
        /// wall-clock change on the server cannot make everyone idle at once.
        /// </summary>
        public long LastActivityTick { get; set; } = Environment.TickCount64;

        /// <summary>Whether PlayerInactiveWarning has gone out for the current idle stretch.</summary>
        public bool InactiveWarningSent { get; set; }

        /// <summary>
        /// Metres of movement the player has in hand, and when it was last topped up. A Move is
        /// paid for out of this; it refills at the player's own speed, so a client cannot move
        /// faster over any stretch of time than the character could have walked it.
        /// </summary>
        public double MoveBudget { get; set; }

        /// <summary>Environment.TickCount64 when MoveBudget was last brought up to date.</summary>
        public long MoveBudgetTick { get; set; } = Environment.TickCount64;

        /// <summary>
        /// Environment.TickCount64 of the last movement correction sent to this client, so a
        /// client that keeps sending refused positions is snapped back and logged at a bounded
        /// rate rather than once per packet.
        /// </summary>
        public long LastMoveCorrectionTick { get; set; }

        /// <summary>How many Moves have been refused since the last one was reported.</summary>
        public int RefusedMoves { get; set; }

        /// <summary>
        /// Puts the player somewhere because the server says so - a map change, a dropship, a
        /// waypoint, a summon, /stuck, a GM command - rather than because the client claimed it.
        ///
        /// Always this and never a bare Position assignment, so that the movement check starts
        /// again from where the server put them. Assigning Position alone leaves the check
        /// measuring the client's next Move from wherever the player used to be, which reads as
        /// one enormous step and refuses a move nobody made.
        /// </summary>
        /// <summary>Polymorph: the creature weapon the player fires while morphed; null when they are themselves.</summary>
        public Item MorphWeapon { get; set; }

        /// <summary>The creature's combat actions a polymorphed player has in their drawer, and may perform (AbilityManager.Polymorph).</summary>
        public System.Collections.Generic.List<(Data.ActionId ActionId, uint Level)> MorphAbilities { get; set; } = new System.Collections.Generic.List<(Data.ActionId, uint)>();

        /// <summary>
        /// What the player counts as to creatures: FRIENDLY, whatever they look like. Polymorph
        /// used to make a morphed player their disguise's side, which read well against the
        /// tooltip's "including their faction" but played badly: the aggro scan passed over
        /// them, so a morph in the middle of a fight was a way out of it and a morphed player
        /// could walk through a Bane camp untouched.
        /// </summary>
        public Data.TargetCategory CombatCategory => Data.TargetCategory.Friendly;

        public void PlaceAt(Vector3 position)
        {
            Position = position;
            MoveBudget = 0;
            MoveBudgetTick = Environment.TickCount64;

            // Put somewhere, not fallen there: whatever descent was under way is over.
            Fall.Reset();

            // Where to put them back if they fall out of the world before they have stood anywhere
            // (Managers.SafetyFloor). Map changes set MapContextId before placing.
            ArrivalPosition = position;
            ArrivalMapContextId = MapContextId;
        }

        /// <summary>The last place on the map the player stood on walkable ground (Managers.SafetyFloor), and which map; null until they have.</summary>
        public Vector3? LastSafePosition { get; set; }
        public uint LastSafeMapContextId { get; set; }
        public long LastSafeTick { get; set; }

        /// <summary>Where the player was last put or entered the world, and on which map (Managers.SafetyFloor).</summary>
        public Vector3? ArrivalPosition { get; set; }
        public uint ArrivalMapContextId { get; set; }

        /// <summary>The descent under way, if any, for falling damage (Managers.FallDamage).</summary>
        public FallTracker Fall { get; } = new FallTracker();

        /// <summary>
        /// Always false: this server has no trial accounts. The single source for every packet
        /// that reports the flag (IsTrialAccount, WhoAck), so the client never shows the trial
        /// tag and no trial-only restriction - whisper, party or clan invites, trial chat
        /// channels - ever applies.
        /// </summary>
        public bool IsTrialAccount => false;

        /// <summary>
        /// Whether the player's helmet is drawn, as their client last said (ChangeShowHelmet); null
        /// until it has said, when the saved User.Appearance.ShowHelmet option stands in
        /// (ManifestationManager.ShowsHelmet).
        /// </summary>
        public bool? ShowHelmet { get; set; }

        // Inventory
        public Inventory Inventory { get; set; } = new Inventory();

        // Party
        internal uint PartyId { get; set; }
        /// <summary>AcceptPartyInvitesChanged; invitations to a player who turned them off are refused.</summary>
        internal bool AcceptPartyInvites { get; set; } = true;

        // Social
        internal List<uint> Friends = new();
        internal List<uint> IgnoredPlayers = new();
        private MapChannel _mapChannel;
        internal Guid MissionLocationEpoch { get; private set; } = Guid.NewGuid();
        public MapChannel MapChannel
        {
            get => _mapChannel;
            set
            {
                if (!ReferenceEquals(_mapChannel, value))
                    MissionLocationEpoch = Guid.NewGuid();
                _mapChannel = value;
            }
        }
        public bool Disconected { get; set; }
        /// <summary>Set by RequestLogout, cleared by CancelLogoutRequest.</summary>
        public bool LogoutActive { get; set; }

        /// <summary>
        /// Whether this player is in a fight, as the server counts it: they have dealt or taken
        /// damage within the last <see cref="Data.CombatRegen.CombatTimeoutMs"/>.
        ///
        /// Distinct from <c>InCombatMode</c> on Actor, which is the visual weapon stance the
        /// client asks for with RequestVisualCombatMode and which says nothing about whether
        /// anyone is actually fighting.
        /// </summary>
        public bool InCombat { get; set; }

        /// <summary>The combat stance the player's own client asked to hold (RequestVisualCombatMode); see ActorManager.CombatModeOf.</summary>
        public bool RequestedCombatMode { get; set; }

        /// <summary>Whether the player is holding auto-fire down, which holds the stance for the others who see them.</summary>
        public bool AutoFireCombatMode { get; set; }

        /// <summary>
        /// The buffs taken off at the last map change that go on again on arrival, their clocks
        /// stopped in between, and whether each was already paused before (Managers.EffectCarry).
        /// </summary>
        public List<(GameEffect Effect, bool WasPaused)> CarriedEffects { get; } = new();

        /// <summary>Environment.TickCount64 at which combat lapses, refreshed by every hit.</summary>
        public long CombatExpiresAt { get; set; }

        /// <summary>
        /// Armour regeneration per second from the armour worn (armorclass.regen_rate summed),
        /// set by UpdateStatsValues. Armor.RefreshAmount carries it out of combat and 0 in combat
        /// (ManifestationManager.ApplyRegenPeriod).
        /// </summary>
        public int ArmorRegenRate { get; set; }

        /// <summary>Seconds of regeneration ticked so far (ActorManager.Regenerate); the in-combat period is a multiple of them.</summary>
        public long RegenSeconds { get; set; }

        /// <summary>
        /// Environment.TickCount64 at which this player's next shot is due, whichever weapon fires
        /// it. On the player rather than the weapon so that switching drawer slots between shots
        /// does not give each weapon a clock of its own. Read through ManifestationManager's shot
        /// clock, which lets a shot come a little before or after this and still charges the next
        /// one from it.
        /// </summary>
        public long NextShotAt { get; set; }

        /// <summary>
        /// The aiming bead as the client runs it (Managers.Accuracy): where it is, the ceiling it
        /// is heading for, how fast (per ms, negative when falling), and when it was last brought
        /// up to date.
        /// </summary>
        public double AccuracyValue { get; set; }
        public double AccuracyMax { get; set; }
        public double AccuracyRate { get; set; }
        public long AccuracyUpdatedTick { get; set; }

        /// <summary>
        /// Environment.TickCount64 at which this player's next melee (alternate) attack is due.
        /// Its own clock: a swing does not wait on the gun's refire, nor the gun on the swing, as
        /// the client times them separately - each from its own action's recovery and reuse.
        /// </summary>
        public long NextMeleeAt { get; set; }

        /// <summary>
        /// Percent of a creature's aggro range at which it notices this player: 100 normally, less
        /// in Stealth Armor (ManifestationManager.SyncSkillPassives keeps it).
        /// </summary>
        public int DetectionRangePercent { get; set; } = 100;

        /// <summary>Environment.TickCount64 when the pending logout was requested.</summary>
        public long LogoutRequestedTick { get; set; }
        public bool RemoveFromMap { get; set; }

        /// <summary>
        /// Ids of the map links whose trigger radius the player is standing in. A link fires
        /// when its id joins this set, so a player who arrives inside the reciprocal gate is not
        /// bounced straight back: MapLinkManager seeds it on arrival and clears it on leaving.
        /// </summary>
        internal HashSet<uint> InsideMapLinks = new();

        /// <summary>
        /// The region ids the client was last told the player is in (UpdateRegions), sorted, so
        /// RegionManager only sends again when the set changes. Null until the first send on a map.
        /// </summary>
        internal List<uint> RegionIds;

        /// <summary>Set by .setregion: the list above was forced and the worker leaves it alone until released or the map changes.</summary>
        internal bool RegionsHeld;
        // chat
        public int JoinedChannels { get; set; }
        public int[] ChannelHashes = new int[14];
        // gm flags
        public bool GmFlagAlwaysFriendly { get; set; }

        /// <summary>The camera script the player's client is running, 0 for none (CameraScripts).</summary>
        public uint CameraScriptId { get; set; }

        /// <summary>When the player stops counting as watching it if FinishedCameraScript never comes (TickCount64).</summary>
        public long CameraScriptUntil { get; set; }

        /// <summary>The scriptable client events the player's client is reporting (ScriptableClientEvents). Lock it to use it.</summary>
        internal readonly HashSet<ScriptableClientEvent> TrackedClientEvents = new();

        /// <summary>Set by .clientevent: each tracked event that comes back is also told to the player.</summary>
        public bool EchoClientEvents { get; set; }


        public Manifestation()
        {
        }

        public Manifestation(CharacterEntry character, Dictionary<EquipmentData, AppearanceData> appearence)
        {
            // CharacterData
            Id = character.Id;
            Scale = character.Scale;
            Race = (Race)character.Race;
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
            // A saved slot the drawer does not have (none can be saved, but the column is only a
            // byte) is the first one.
            CurrentAbilityDrawer = character.CurrentAbilitySlot < Managers.ManifestationManager.AbilityDrawerSlots ? character.CurrentAbilitySlot : 0;
            NumLogins = character.NumLogins + 1;
            TotalTimePlayed = character.TotalTimePlayed;
            TimeSinceLastPlayed = character.LastLogin;
            // AppearanceData
            AppearanceData = appearence;
            // Actor
            EntityClass = character.Gender == 0 ? EntityClasses.HumanBaseMale : EntityClasses.HumanBaseFemale;
            Name = character.Name;
            FamilyName = character.GameAccount?.FamilyName;
            Position = new Vector3((float)character.CoordX, (float)character.CoordY, (float)character.CoordZ);
            Rotation = (float)character.Rotation;
            MapContextId = character.MapContextId;
            IsRunning = character.IsRunning();
            InCombatMode = false;
            State = CharacterState.Normal;
            MovementSpeed = 1.0d;
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

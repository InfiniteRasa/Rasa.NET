using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The toys: account reward and holiday items used like abilities.
    ///
    ///  - Emote items, ACCOUNTREWARD_EMOTE_ITEM (464, abilities.accountrewardemoteitem). Each level
    ///    is one emote. Its SET_PLAYER_FLAG_ID is the player flag the emote's gesture row needs
    ///    (actiondata playerFlagReqs, (2, argId)). The item is its own requirement, so it is used
    ///    up, and the flag is set in the same write. An emote the player knows is refused.
    ///  - Title items, ACCOUNTREWARD_TITLE_ITEM (507), the same client module. The value is a
    ///    titledata id (905 Shadow, 941 Special Forces): the title is added to character_title in
    ///    the same write that uses the item up, and TitleAdded tells the client, which prints "you
    ///    have gained the title". A title the player has is refused.
    ///  - Soyuz ISS Model Rocket, VISUAL_BY_ACTOR (509, abilities.fixedvisual). The client plays
    ///    only the launch animations and reads none of the level's properties, so the rocket is an
    ///    FX package played DROP_DISTANCE_CENTIMETERS in front of the player for DURATION seconds
    ///    through a short-lived FXPackageEmitter. SFX_FAMILY_ID picks the rocket; the families are
    ///    not in the client's Python data, so <see cref="ModelRocketPackages"/> pairs them with the
    ///    nine vfx_emote_model_rocket packages by the item each level belongs to. Kept; one in the
    ///    air at a time.
    ///  - Fireworks (CONSUMABLE_FIREWORK 482, abilities.firework, ten colours) and the Flare Gun
    ///    (CONSUMABLE_FLARE_GUN 483, abilities.flaregun): TARGET_LOCATION, with the level's
    ///    GAME_EFFECT_ID / GAME_EFFECT_LEVEL as the class's targetGameEffect (FIREWORK_EFFECT 364,
    ///    whose specialFX differs per level; FLARE_GUN_EFFECT 374). A ParticleLocationProxy (1400,
    ///    the classes' splatClassId) is put down at the aimed spot carrying the effect, and is the
    ///    recovery's hit, so the client's OnServerResolution announces the effect on it. After
    ///    DURATION seconds the effect is detached and the proxy taken away once the client's
    ///    fxFadeoutMs has played. Used up.
    ///  - Snowball (ABILITY_NULL 528, abilities.nullability): TARGET_FRIENDLY with doBlindShots and
    ///    nothing to resolve; the target is the recovery's hit, so the throw and the splash
    ///    (actorActionFXFamily 2040 / 2039) play on them, or the client splats it on the ground.
    ///    A player or nobody. Used up.
    ///  - Pets and companions (ACCOUNTREWARD_PET 460, abilities.companion, 20 levels): the level's
    ///    CREATURE_VARIANT_ID names the creature. The variant table is not in the client, so
    ///    <see cref="PetVariants"/> pairs each with its creature class by name (the summoner item
    ///    AccountReward_PetSummoner_Grisel, the creature Ambient_Pet_Grisel). The pet is a
    ///    DECORATION creature beside the player that follows them: it takes no part in any fight
    ///    and takes no minion commands. One at a time; the same item again sends it home; it goes
    ///    when the player leaves the map. The summoner is kept (its requirement's quantity is 0).
    ///    COMPANION_MASTER / COMPANION_SLAVE (346, 347) have no classes in the client's module, so
    ///    they are not sent.
    /// </summary>
    public partial class AbilityManager
    {
        public const string EmoteItemModule = "abilities.accountrewardemoteitem";
        public const string FixedVisualModule = "abilities.fixedvisual";
        public const string FireworkModule = "abilities.firework";
        public const string FlareGunModule = "abilities.flaregun";
        public const string SnowballModule = "abilities.nullability";
        public const string CompanionModule = "abilities.companion";

        /// <summary>ParticleLocationProxy: firework.py and flaregun.py's splatClassId.</summary>
        public const EntityClasses LocationProxyClass = (EntityClasses)1400;

        /// <summary>FireworkEffect and FlareGunEffect's fxFadeoutMs: the proxy stays this long after the effect is detached.</summary>
        public const int LocationEffectFadeMs = 3000;

        /// <summary>When the level does not say (DURATION, seconds).</summary>
        public const int ModelRocketDefaultSeconds = 10;
        public const int LocationEffectDefaultSeconds = 15;

        /// <summary>
        /// SFX_FAMILY_ID of each VISUAL_BY_ACTOR level to the FX package that shows that rocket, by
        /// the rocket item the level belongs to (ItemTemplateActions): levels 4 and 5 are the Circuit
        /// City and Ten Ton Hammer rockets, whose packages are numbered v05 and v04.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, uint> ModelRocketPackages = new Dictionary<int, uint>
        {
            [1902] = 48915, // level 1, Soyuz ISS Model Rocket: vfx_emote_model_rocket_v01
            [1910] = 49237, // level 2, Best Buy: vfx_emote_model_rocket_v02_bestbuy
            [1920] = 49680, // level 3, GameStop: vfx_emote_model_rocket_v03_gamespot
            [1941] = 49871, // level 4, Circuit City: vfx_emote_model_rocket_v05_circuitcity
            [1942] = 49870, // level 5, Ten Ton Hammer: vfx_emote_model_rocket_v04_tenton
            [1943] = 49880, // level 6, Amazon: vfx_emote_model_rocket_v06_amazon
            [1961] = 49986, // level 7, Tabula Rasa: vfx_emote_model_rocket_v07_tabularasa
            [1962] = 49987, // level 8, NCsoft: vfx_emote_model_rocket_v08_ncsoft
            [1963] = 49988  // level 9, Destination Games: vfx_emote_model_rocket_v09_destinationgames
        };

        /// <summary>CREATURE_VARIANT_ID of each ACCOUNTREWARD_PET level to the creature class that is that pet, by name.</summary>
        public static readonly IReadOnlyDictionary<int, (uint ClassId, string Name)> PetVariants = new Dictionary<int, (uint, string)>
        {
            [864] = (7747, "Pineock"),              // level 1, Companion Pine-Ock: AccountReward_Pet_PineOck
            [837] = (7235, "Boo Bot"),              // level 2, Companion Boo Bot: AccountReward_Pet_BooBot
            [873] = (9554, "Shellbot"),             // level 3, Companion Shell Bot: AccountReward_Pet_ShellBot
            [4625] = (29219, "Red Lumin"),          // level 4, Pet Lumin (Red): Ambient_Pet_Lumin_Red
            [4627] = (29220, "Blue Lumin"),         // level 5, Pet Lumin (Blue): Ambient_Pet_Lumin_Blue
            [4626] = (29221, "Green Lumin"),        // level 6, Pet Lumin (Green): Ambient_Pet_Lumin_Green
            [4628] = (29222, "Purple Lumin"),       // level 7, Pet Lumin (Purple): Ambient_Pet_Lumin_Purple
            [4974] = (30619, "Creepa"),             // level 8, Pet: Cavern Creepa: Ambient_Pet_Cavern_Creepa
            [4975] = (30620, "Geyser Hopper"),      // level 9: Ambient_Pet_Geyser_Hopper
            [4976] = (30621, "Grisel"),             // level 10: Ambient_Pet_Grisel
            [4977] = (30622, "Kalastride"),         // level 11: Ambient_Pet_Kalastride
            [4978] = (30623, "Lavar"),              // level 12: Ambient_Pet_Lavar
            [4979] = (30624, "Lognar"),             // level 13: Ambient_Pet_Lognar
            [4980] = (30625, "Mush Digger"),        // level 14: Ambient_Pet_Mush_Digger
            [4981] = (30626, "Rhegit"),             // level 15: Ambient_Pet_Rhegit
            [4982] = (30627, "Salvage Bot"),        // level 16: Ambient_Pet_Salvage_Bot_Reconstructor
            [4983] = (30628, "Skitterin"),          // level 17: Ambient_Pet_Skitterin
            [4984] = (30629, "Spore Hitcher"),      // level 18: Ambient_Pet_Spore_Hitcher
            [4985] = (30630, "Swamp Rat"),          // level 19: Ambient_Pet_Swamp_Rat
            [4986] = (30631, "Yimma")               // level 20: Ambient_Pet_Yimma
        };

        /// <summary>What a toy's write committed, for the recovery to tell the client.</summary>
        private sealed class ToyCommit
        {
            public IReadOnlyDictionary<uint, uint> Flags;
            public uint TitleId;
        }

        /// <summary>A rocket or a firework in the world, taken away by ToyWorker.</summary>
        private sealed class WorldToy
        {
            public Manifestation Owner;
            public MapEmitter Emitter;
            public DynamicObject Proxy;
            public int EffectId;
            public long DetachAt;
            public long RemoveAt;
        }

        private sealed class Pet
        {
            public Manifestation Owner;
            public MapChannel MapChannel;
            public Creature Creature;
            public uint Level;
        }

        private static readonly ConditionalWeakTable<MapChannel, List<WorldToy>> WorldToys = new();
        private static readonly List<Pet> Pets = new();
        private static readonly object PetsLock = new();

        #region Which

        private static bool IsEmoteItem(ActionInfo action, ActionLevelInfo info)
        {
            return action.ActionId == ActionId.AccountrewardEmoteItem && action.Module == EmoteItemModule
                   && CharacterFlagIds.IsMissionFlag((uint)Math.Max(0, info.Get(AbilityProperty.SetPlayerFlagId)));
        }

        private static bool IsTitleItem(ActionInfo action, ActionLevelInfo info)
        {
            return action.ActionId == ActionId.AccountrewardTitleItem && action.Module == EmoteItemModule
                   && info.Get(AbilityProperty.SetPlayerFlagId) > 0;
        }

        private static bool IsModelRocket(ActionInfo action, ActionLevelInfo info)
        {
            return action.Module == FixedVisualModule
                   && ModelRocketPackages.TryGetValue(info.Get(AbilityProperty.SfxFamilyId), out var packageId)
                   && FxPackages.Names.ContainsKey(packageId);
        }

        private static bool IsLocationEffect(ActionInfo action, ActionLevelInfo info)
        {
            return (action.Module == FireworkModule || action.Module == FlareGunModule) && info.Get(AbilityProperty.GameEffectId) > 0;
        }

        private static bool IsSnowball(ActionInfo action) => action.Module == SnowballModule;

        private static bool IsPet(ActionInfo action, ActionLevelInfo info)
        {
            return action.Module == CompanionModule && PetVariants.ContainsKey(info.Get(AbilityProperty.CreatureVariantId));
        }

        private static bool IsToy(ActionInfo action, ActionLevelInfo info)
        {
            return IsEmoteItem(action, info) || IsTitleItem(action, info) || IsModelRocket(action, info)
                   || IsLocationEffect(action, info) || IsSnowball(action) || IsPet(action, info);
        }

        /// <summary>A rocket or a pet is kept when used; everything else used from an item is used up.</summary>
        private static bool KeepsSourceItem(ActionInfo action) => action.Module == FixedVisualModule || action.Module == CompanionModule;

        #endregion

        #region Refusals

        /// <summary>Whether the player has the emote the item teaches.</summary>
        public static bool KnowsEmote(Manifestation player, ActionLevelInfo info)
        {
            var flags = player?.PlayerFlags;
            return flags != null && flags.TryGetValue((uint)info.Get(AbilityProperty.SetPlayerFlagId), out var value) && value != 0;
        }

        /// <summary>Whether the player has the title the item gives.</summary>
        public static bool HasTitle(Manifestation player, ActionLevelInfo info)
        {
            var titles = player?.Titles;

            if (titles == null)
                return false;

            lock (titles)
                return titles.Contains((uint)info.Get(AbilityProperty.SetPlayerFlagId));
        }

        private static bool AlreadyHasReward(Manifestation player, ActionInfo action, ActionLevelInfo info)
        {
            return IsEmoteItem(action, info) && KnowsEmote(player, info)
                   || IsTitleItem(action, info) && HasTitle(player, info);
        }

        /// <summary>Why a toy cannot be used now, or null.</summary>
        private static PlayerMessage? ToyRefusal(MapChannel mapChannel, Manifestation player, ActionInfo action, ActionLevelInfo info)
        {
            if (AlreadyHasReward(player, action, info))
                return PlayerMessage.PmCannotPerformActionNow;

            if (IsModelRocket(action, info) && RocketInAir(mapChannel, player))
                return PlayerMessage.PmCannotPerformActionNow;

            return null;
        }

        #endregion

        #region The write

        /// <summary>The emote's flag or the title, written in the same transaction as the item is used up; null for anything else.</summary>
        private static Action<ICharUnitOfWork> ToyWrite(ActionInfo action, ActionLevelInfo info, Manifestation player, ToyCommit commit)
        {
            var value = (uint)Math.Max(0, info.Get(AbilityProperty.SetPlayerFlagId));

            if (IsEmoteItem(action, info))
                return unit =>
                {
                    unit.CharacterFlags.Set(player.Id, value, 1);
                    commit.Flags = unit.CharacterFlags.Get(player.Id);
                };

            if (IsTitleItem(action, info))
                return unit =>
                {
                    if (unit.CharacterTitles.Add(player.Id, value))
                        commit.TitleId = value;
                };

            return null;
        }

        #endregion

        /// <summary>The toy has landed: what it does.</summary>
        private void ResolveToy(MapChannel mapChannel, Client client, Manifestation player, ActionInfo actionInfo, ActionLevelInfo info, ActionData action, ToyCommit commit)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);

            if (IsEmoteItem(actionInfo, info))
                LearnEmote(client, commit);
            else if (IsTitleItem(actionInfo, info))
                GainTitle(client, player, commit);
            else if (IsModelRocket(actionInfo, info))
                LaunchModelRocket(mapChannel, player, info);
            else if (IsLocationEffect(actionInfo, info))
                SetOffAtLocation(mapChannel, player, info, action, recovery);
            else if (IsSnowball(actionInfo))
                ThrowSnowball(mapChannel, action, recovery);
            else if (IsPet(actionInfo, info))
                SummonPet(mapChannel, player, info, action, recovery);

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        #region Emotes and titles

        private void LearnEmote(Client client, ToyCommit commit)
        {
            if (commit.Flags == null)
                return;

            client.FlagProjection.ApplyCommitted(client, commit.Flags);
            (_missionManager ?? MissionApplication.Instance).PublishCharacterFlags(client);
        }

        private static void GainTitle(Client client, Manifestation player, ToyCommit commit)
        {
            if (commit.TitleId == 0)
                return;

            ManifestationManager.TitleGained(client, commit.TitleId);
        }

        #endregion

        #region Rockets, fireworks and flares

        private static bool RocketInAir(MapChannel mapChannel, Manifestation player)
        {
            if (mapChannel == null || !WorldToys.TryGetValue(mapChannel, out var toys))
                return false;

            lock (toys)
                return toys.Any(toy => toy.Owner == player && toy.Emitter != null);
        }

        private static void Keep(MapChannel mapChannel, WorldToy toy)
        {
            var toys = WorldToys.GetValue(mapChannel, _ => new List<WorldToy>());

            lock (toys)
                toys.Add(toy);
        }

        private static void LaunchModelRocket(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var packageId = ModelRocketPackages[info.Get(AbilityProperty.SfxFamilyId)];
            var distance = info.Get(AbilityProperty.DropDistanceCentimeters) / 100f;
            var seconds = Math.Max(1, info.Get(AbilityProperty.Duration, ModelRocketDefaultSeconds));
            var spot = player.Position + FacingOf(player) * distance;

            var emitter = EmitterManager.Instance.PlayTemporary(mapChannel, player.MapContextId, spot, player.Rotation, packageId,
                $"model rocket of {player.FamilyName}");

            Keep(mapChannel, new WorldToy { Owner = player, Emitter = emitter, RemoveAt = Environment.TickCount64 + seconds * 1000L });
        }

        /// <summary>A firework or a flare: a proxy at the aimed spot carrying the level's effect, which the recovery announces.</summary>
        private static void SetOffAtLocation(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action, AbilityRecoveryPacket recovery)
        {
            var aimed = action.TargetLocation ?? player.Position + FacingOf(player) * 10f;
            var seconds = Math.Max(1, info.Get(AbilityProperty.Duration, LocationEffectDefaultSeconds));

            var proxy = new DynamicObject
            {
                EntityClassId = LocationProxyClass,
                Position = NavMeshManager.SnapToGround(mapChannel, aimed),
                MapContextId = player.MapContextId,
                IsEnabled = false
            };

            CellManager.Instance.AddToWorld(mapChannel, proxy);

            var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);

            // Not announced here: the recovery's hit is what announces it, as the client's own
            // OnServerResolution does for a targetGameEffect.
            CellManager.Instance.CellCallMethod(proxy, new GameEffectAttachedPacket
            {
                EffectTypeId = info.Get(AbilityProperty.GameEffectId),
                EffectId = effectId,
                EffectLevel = (uint)Math.Max(1, info.Get(AbilityProperty.GameEffectLevel, 1)),
                SourceId = player.EntityId,
                Announced = false,
                Duration = null,
                DamageType = 0,
                AttrId = 1,
                IsActive = true,
                IsBuff = true,
                IsDebuff = false,
                IsNegativeEffect = false,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            });

            recovery.Hits.Add(new AbilityHit { EntityId = proxy.EntityId });

            var detachAt = Environment.TickCount64 + seconds * 1000L;

            Keep(mapChannel, new WorldToy { Owner = player, Proxy = proxy, EffectId = effectId, DetachAt = detachAt, RemoveAt = detachAt + LocationEffectFadeMs });
        }

        #endregion

        #region Snowballs

        /// <summary>The target is the hit, when there is one; with none the client splats it where it was thrown.</summary>
        private static void ThrowSnowball(MapChannel mapChannel, ActionData action, AbilityRecoveryPacket recovery)
        {
            if (action.TargetId == 0)
                return;

            var target = ResolveTarget(mapChannel, action.TargetId);

            if (target != null)
                recovery.Hits.Add(new AbilityHit { EntityId = target.EntityId });
        }

        #endregion

        #region Pets

        /// <summary>The pet the player has out, or null.</summary>
        private static Pet PetOf(Manifestation player)
        {
            lock (PetsLock)
                return Pets.FirstOrDefault(pet => pet.Owner == player);
        }

        /// <summary>The player's pet, if they have one out: its creature.</summary>
        public static Creature PetCreatureOf(Manifestation player) => PetOf(player)?.Creature;

        /// <summary>The same pet's item used again while it is out: it goes home. True when it did.</summary>
        private bool TryDismissPet(Manifestation player, ActionInfo action, uint level)
        {
            if (action.Module != CompanionModule)
                return false;

            var pet = PetOf(player);

            if (pet == null || pet.Level != level)
                return false;

            DismissPet(player);
            return true;
        }

        /// <summary>Sends the player's pet home, if they have one out. Called when they leave the map too.</summary>
        public static void DismissPet(Manifestation player)
        {
            if (player == null)
                return;

            List<Pet> gone;

            lock (PetsLock)
            {
                gone = Pets.Where(pet => pet.Owner == player).ToList();
                Pets.RemoveAll(gone.Contains);
            }

            foreach (var pet in gone)
                RemovePet(pet);
        }

        private static void RemovePet(Pet pet)
        {
            if (EntityManager.Instance.Creatures.TryGetValue(pet.Creature.EntityId, out var registered) && registered == pet.Creature)
                CellManager.Instance.RemoveCreatureFromWorld(pet.MapChannel, pet.Creature);
        }

        private static void SummonPet(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action, AbilityRecoveryPacket recovery)
        {
            DismissPet(player);

            var (classId, name) = PetVariants[info.Get(AbilityProperty.CreatureVariantId)];
            var spot = NavMeshManager.SnapToGround(mapChannel, player.Position - FacingOf(player) * 2f);

            var creature = new Creature
            {
                EntityClass = (EntityClasses)classId,
                TargetCategory = TargetCategory.Decoration,
                Level = Math.Max(1u, (uint)player.Level),
                MaxHitPoints = 100,
                RunSpeed = 9f,
                WalkSpeed = 5f,
                AggroRange = 0f,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                Name = name,
                MasterEntityId = player.EntityId,
                Stance = MinionStance.Passive
            };

            creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0));
            creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));

            CreatureManager.Instance.SetLocation(creature, spot, player.Rotation, player.MapContextId);
            CellManager.Instance.AddToWorld(mapChannel, creature);

            // It trails its owner and nothing more: no assist, no minion commands.
            BehaviorManager.Instance.SetActionFollow(creature, player.EntityId);

            lock (PetsLock)
                Pets.Add(new Pet { Owner = player, MapChannel = mapChannel, Creature = creature, Level = action.ActionArgId });

            recovery.Hits.Add(new AbilityHit { EntityId = creature.EntityId });
        }

        #endregion

        /// <summary>
        /// Takes away the rockets and fireworks whose time is up, and the pets whose owner is no
        /// longer on this map. <paramref name="now"/> is for tests; 0 is the clock.
        /// </summary>
        internal void ToyWorker(MapChannel mapChannel, long now = 0)
        {
            if (now == 0)
                now = Environment.TickCount64;

            if (WorldToys.TryGetValue(mapChannel, out var toys))
            {
                List<WorldToy> detach, done;

                lock (toys)
                {
                    detach = toys.Where(toy => toy.Proxy != null && toy.DetachAt != 0 && now >= toy.DetachAt).ToList();
                    done = toys.Where(toy => now >= toy.RemoveAt).ToList();
                    toys.RemoveAll(done.Contains);
                }

                foreach (var toy in detach)
                {
                    toy.DetachAt = 0;
                    CellManager.Instance.CellCallMethod(toy.Proxy, new GameEffectDetachedPacket { EffectId = toy.EffectId });
                }

                foreach (var toy in done)
                {
                    if (toy.Emitter != null)
                        EmitterManager.Instance.RemoveTemporary(mapChannel, toy.Emitter);

                    if (toy.Proxy != null)
                        CellManager.Instance.RemoveFromWorld(mapChannel, toy.Proxy);
                }
            }

            List<Pet> orphaned;

            lock (PetsLock)
            {
                orphaned = Pets.Where(pet => pet.MapChannel == mapChannel &&
                                             (!EntityManager.Instance.Players.TryGetValue(pet.Owner.EntityId, out var owner) || owner != pet.Owner ||
                                              owner.MapChannel != mapChannel ||
                                              !EntityManager.Instance.Creatures.TryGetValue(pet.Creature.EntityId, out var creature) || creature != pet.Creature))
                    .ToList();
                Pets.RemoveAll(orphaned.Contains);
            }

            foreach (var pet in orphaned)
                RemovePet(pet);
        }
    }
}

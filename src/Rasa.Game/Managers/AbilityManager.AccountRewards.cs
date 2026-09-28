using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Account reward items that are used like abilities:
    ///
    ///  - Emote items, ACCOUNTREWARD_EMOTE_ITEM (464), client module abilities.accountrewardemoteitem.
    ///    Each level is one emote ("Logos Fist" Emote, "Drunk" Emote...). Its SET_PLAYER_FLAG_ID is
    ///    the player flag that the emote's gesture row needs (actiondata playerFlagReqs, (2, argId)),
    ///    so /logosfist works once the flag is set. The item is its own requirement (itemReqs), so
    ///    it is used up, and the flag is set in the same write. An emote the player already knows is
    ///    refused, so the item is not wasted.
    ///  - Soyuz ISS Model Rocket, VISUAL_BY_ACTOR (509), abilities.fixedvisual. The client only plays
    ///    the launch animations (windup 1485, recovery 1486) and reads none of the level's properties,
    ///    so the rocket is the server's to show: an FX package played DROP_DISTANCE_CENTIMETERS in
    ///    front of the player for DURATION seconds, through a short-lived FXPackageEmitter
    ///    (EmitterManager.PlayTemporary). The level's SFX_FAMILY_ID picks the rocket. The sfx
    ///    families are not in the client's Python data, so <see cref="ModelRocketPackages"/> pairs
    ///    them with the nine vfx_emote_model_rocket packages by the item each level belongs to.
    ///    The rocket is a toy and is kept. One per player in the air at a time.
    ///
    /// ACCOUNTREWARD_TITLE_ITEM (507) shares the emote items' client module, but its value is a
    /// title id; it is left refused until titles can be stored more than one to a character.
    /// </summary>
    public partial class AbilityManager
    {
        public const string EmoteItemModule = "abilities.accountrewardemoteitem";
        public const string FixedVisualModule = "abilities.fixedvisual";

        /// <summary>When the level does not say (DURATION, seconds).</summary>
        public const int ModelRocketDefaultSeconds = 10;

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

        private sealed class ModelRocket
        {
            public Manifestation Owner;
            public MapEmitter Emitter;
            public long RemoveAt;
        }

        private static readonly ConditionalWeakTable<MapChannel, List<ModelRocket>> ModelRockets = new();

        private static bool IsEmoteItem(ActionInfo action, ActionLevelInfo info)
        {
            return action.ActionId == ActionId.AccountrewardEmoteItem && action.Module == EmoteItemModule
                   && info.Get(AbilityProperty.SetPlayerFlagId) > 0
                   && CharacterFlagIds.IsMissionFlag((uint)info.Get(AbilityProperty.SetPlayerFlagId));
        }

        private static bool IsModelRocket(ActionInfo action, ActionLevelInfo info)
        {
            return action.Module == FixedVisualModule
                   && ModelRocketPackages.TryGetValue(info.Get(AbilityProperty.SfxFamilyId), out var packageId)
                   && FxPackages.Names.ContainsKey(packageId);
        }

        /// <summary>A model rocket is kept after it is launched; everything else used from an item is used up.</summary>
        private static bool KeepsSourceItem(ActionInfo action) => action.Module == FixedVisualModule;

        /// <summary>Whether the player has the emote the item teaches.</summary>
        public static bool KnowsEmote(Manifestation player, ActionLevelInfo info)
        {
            var flags = player?.PlayerFlags;
            return flags != null && flags.TryGetValue((uint)info.Get(AbilityProperty.SetPlayerFlagId), out var value) && value != 0;
        }

        /// <summary>Why an account reward cannot be used now, or null.</summary>
        private static PlayerMessage? AccountRewardRefusal(MapChannel mapChannel, Manifestation player, ActionInfo action, ActionLevelInfo info)
        {
            if (IsEmoteItem(action, info) && KnowsEmote(player, info))
                return PlayerMessage.PmCannotPerformActionNow;

            if (IsModelRocket(action, info) && RocketInAir(mapChannel, player))
                return PlayerMessage.PmCannotPerformActionNow;

            return null;
        }

        /// <summary>The emote item's flag, written in the same transaction as the item is used up; null for anything else.</summary>
        private static Action<ICharUnitOfWork> EmoteItemFlagWrite(ActionInfo action, ActionLevelInfo info, Manifestation player,
            Action<IReadOnlyDictionary<uint, uint>> committed)
        {
            if (!IsEmoteItem(action, info))
                return null;

            var flagId = (uint)info.Get(AbilityProperty.SetPlayerFlagId);

            return unit =>
            {
                unit.CharacterFlags.Set(player.Id, flagId, 1);
                committed(unit.CharacterFlags.Get(player.Id));
            };
        }

        /// <summary>The flag is written: the client is told (PlayerFlags), and the action ends.</summary>
        private void LearnEmote(Client client, Manifestation player, ActionData action, IReadOnlyDictionary<uint, uint> committedFlags)
        {
            if (committedFlags != null)
            {
                client.FlagProjection.ApplyCommitted(client, committedFlags);
                (_missionManager ?? MissionApplication.Instance).PublishCharacterFlags(client);
            }

            CellManager.Instance.CellCallMethod(player.MapChannel, player,
                new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
        }

        private static bool RocketInAir(MapChannel mapChannel, Manifestation player)
        {
            if (mapChannel == null || !ModelRockets.TryGetValue(mapChannel, out var rockets))
                return false;

            lock (rockets)
                return rockets.Any(rocket => rocket.Owner == player);
        }

        private void LaunchModelRocket(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var packageId = ModelRocketPackages[info.Get(AbilityProperty.SfxFamilyId)];
            var distance = info.Get(AbilityProperty.DropDistanceCentimeters) / 100f;
            var seconds = Math.Max(1, info.Get(AbilityProperty.Duration, ModelRocketDefaultSeconds));
            var spot = player.Position + FacingOf(player) * distance;

            var emitter = EmitterManager.Instance.PlayTemporary(mapChannel, player.MapContextId, spot, player.Rotation, packageId,
                $"model rocket of {player.FamilyName}");

            var rockets = ModelRockets.GetValue(mapChannel, _ => new List<ModelRocket>());

            lock (rockets)
                rockets.Add(new ModelRocket { Owner = player, Emitter = emitter, RemoveAt = Environment.TickCount64 + seconds * 1000L });

            CellManager.Instance.CellCallMethod(mapChannel, player,
                new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
        }

        /// <summary>Takes the rockets whose time is up off the map. <paramref name="now"/> is for tests; 0 is the clock.</summary>
        internal void ModelRocketWorker(MapChannel mapChannel, long now = 0)
        {
            if (!ModelRockets.TryGetValue(mapChannel, out var rockets))
                return;

            List<ModelRocket> done;

            if (now == 0)
                now = Environment.TickCount64;

            lock (rockets)
            {
                done = rockets.Where(rocket => now >= rocket.RemoveAt).ToList();

                if (done.Count == 0)
                    return;

                rockets.RemoveAll(done.Contains);
            }

            foreach (var rocket in done)
                EmitterManager.Instance.RemoveTemporary(mapChannel, rocket.Emitter);
        }
    }
}

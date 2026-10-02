using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.ClientMethod.Server;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// A player's death, and their way back.
    ///
    /// Dying. A player brought to zero health dies (<see cref="AtZero"/>): they are dead
    /// (CharacterState.Dead), every creature fighting them lets go (Threat.Forget), what they had
    /// running stops - auto-fire, constant fire, a pending ability - and the effects on them come
    /// off, all but Rez Trauma. Everyone around is told (ActorKilled, for a client that missed the
    /// death blow), and they are sent PlayerDead with the hospitals they may go back to
    /// (Hospitals.AvailableTo). Until they are back nothing they ask for is done: no ability, no
    /// weapon, no move, no travel. A client that meets them while they are dead is told so
    /// (DeadOnArrival).
    ///
    /// Not dying:
    ///  - a GameMaster account, unless they have said .allowdeath, stands back up at full;
    ///  - in a duel the loser is defeated, not killed (Pvp.Defeat), as before;
    ///  - in a clan feud the loser dies, and the kill counts for the feud (Pvp.CountKill).
    ///
    /// Going back. From the hospital window, ReviveMe(graveyardId) puts them at that hospital - or,
    /// with none given or one not theirs to choose, the nearest of theirs; or where they fell, on a
    /// map with no hospital at all - on full health and armour. A player killed by an enemy player
    /// has PvP Safety on as they arrive. BuryMe, the death window's Revive (which canRevive leaves
    /// off), goes to the hospital the same way.
    ///
    /// Being revived. A medic's Resuscitate (Cure P3, P5 for the squad) or a healing disc at
    /// Healing 3 aimed at a dead player offers them a revive (<see cref="OfferRevive"/>): the
    /// client's revive window, "%(reviverName)s wants to Revive you. Do you accept?", open for
    /// REVIVE_REQUEST_DURATION (120 s). Accepting (RequestRevive) brings them back where they lie
    /// with what the offer gives - Resuscitate's ATTRIBUTE_MAX_CHANGE of their health, the disc's
    /// heal - and refusing (RefuseRevive) or the time running out drops the offer.
    ///
    /// The price, from shared/gameconstants.py, from DEATH_PENALTY_MIN_LEVEL (5) up, on coming back
    /// either way (the client's help: "When you are resuscitated, your equipment as well as your
    /// body will have sustained damage"):
    ///  - Rez Trauma (REZ_SICKNESS 196): REZ_SICKNESS_PENALTY (-20%) Body, Mind and Spirit, for
    ///    REZ_SICKNESS_DURATION (120 s). Another death while it lasts adds another -20%, to
    ///    REZ_SICKNESS_MAX_PENALTY (-60%, REZ_SICKNESS_MAX_STACK 3), and another 120 s, to
    ///    REZ_SICKNESS_MAX_DURATION (360 s) - "an additional two minutes for each resuscitation
    ///    in the same time frame up to a maximum of six minutes";
    ///  - REZ_SICKNESS_NO_HEAL (195) for REZ_SICKNESS_NO_HEAL_DURATION (30 s): no healing;
    ///  - EQUIPMENT_DAMAGE_PER_DEATH: every equipped piece loses 10% (Durability.WearForDeath).
    /// A clan feud's deaths cost the same (WARGAME_FLAGS_CLAN carries WARGAME_REZ_SICKNESS and
    /// WARGAME_WEAPON_DECAY).
    ///
    /// A Hominis Machina's Self Revive (POLY_SELF_RES, "useable once") gets them up where they fell
    /// on full health: the morph, and the button in its drawer, outlast the death while it is
    /// unspent (<see cref="SelfRevive"/>); going to a hospital instead ends the morph.
    ///
    /// An enemy who takes their body - Reanimation, Reanimation Wave, Hortimonculus - sends them
    /// to their nearest hospital (<see cref="ForceToHospital"/>), and a Cadaver Immolation burning
    /// on the body goes off as they get up, however they do (AbilityManager.OnCorpseRising).
    ///
    /// Leaving the game dead sends them to their hospital first, so they come back alive there.
    /// Rez Trauma and the no-healing go with them: across a map change (EffectCarry) and a logout
    /// (RelogVitals), so neither a map link nor a relog is a way out of them.
    /// </summary>
    public static class PlayerDeath
    {
        public const int RezSicknessTypeId = 196;          // REZ_SICKNESS
        public const int RezSicknessNoHealTypeId = 195;    // REZ_SICKNESS_NO_HEAL

        public const int PenaltyMinLevel = 5;              // DEATH_PENALTY_MIN_LEVEL
        public const int RezSicknessMaxStack = 3;          // REZ_SICKNESS_MAX_STACK
        public const int RezSicknessPenaltyPercent = 20;   // REZ_SICKNESS_PENALTY -20.0
        public const int RezSicknessSeconds = 120;         // REZ_SICKNESS_DURATION
        public const int RezSicknessMaxSeconds = 360;      // REZ_SICKNESS_MAX_DURATION
        public const int NoHealSeconds = 30;               // REZ_SICKNESS_NO_HEAL_DURATION
        public const int ReviveRequestSeconds = 120;       // REVIVE_REQUEST_DURATION

        private sealed class Offer
        {
            public ulong ReviverId;
            public long ExpiresAt;
            public int Health;
        }

        private static readonly Dictionary<ulong, Dictionary<ulong, Offer>> Offers = new Dictionary<ulong, Dictionary<ulong, Offer>>();

        /// <summary>The clock offers run on; replaceable for tests.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        #region Dying

        /// <summary>A GameMaster account that has not said .allowdeath: put back on its feet rather than killed.</summary>
        public static bool IsDeathless(Client client)
        {
            return client?.Player != null && ChatCommandsManager.HasLevel(client, GmLevel.GameMaster) && !client.Player.AllowDeath;
        }

        /// <summary>
        /// A player's health has reached zero, source's doing (null for a fall): they die, or are
        /// defeated, or stand back up - see the remarks. Returns whether they are dead.
        /// </summary>
        public static bool AtZero(MapChannel mapChannel, Manifestation victim, Actor source)
        {
            if (victim == null)
                return false;

            if (victim.State == CharacterState.Dead)
                return true;

            var client = ClientOf(mapChannel, victim);

            if (Pvp.Defeats(source, victim))
            {
                // A duel or a squad wargame ends in a defeat; so does anything for a GM who may not die.
                if (client == null || Pvp.InDuel(mapChannel, Pvp.Controller(source), victim)
                    || Pvp.InSquadWargame(mapChannel, Pvp.Controller(source), victim) || IsDeathless(client))
                {
                    Pvp.Defeat(mapChannel, victim, source);
                    return false;
                }

                Pvp.CountKill(mapChannel, victim, source);
                Kill(mapChannel, client, victim, source, pvp: true);
                return true;
            }

            // A player with nobody connected to them, and a GM who may not die, stand back up.
            if (client == null || IsDeathless(client))
            {
                StandUp(mapChannel, victim);
                return false;
            }

            Kill(mapChannel, client, victim, source, pvp: false);
            return true;
        }

        /// <summary>Back on full health where they stand: what every player at zero used to get.</summary>
        private static void StandUp(MapChannel mapChannel, Manifestation player)
        {
            if (!player.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            health.Current = health.CurrentMax;

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateHealthPacket(health, 0));
        }

        private static void Kill(MapChannel mapChannel, Client client, Manifestation victim, Actor source, bool pvp)
        {
            victim.StateBeforeDeath = victim.State;
            victim.State = CharacterState.Dead;
            victim.DiedInPvp = pvp;

            if (victim.Attributes.TryGetValue(Attributes.Health, out var health))
                health.Current = 0;

            // What they were doing stops with them.
            ManifestationManager.RemoveAutoFire(client);
            ConstantFire.Stop(client, release: false);
            mapChannel?.PerformRecovery.RemoveAll(a => a.Actor == victim);

            if (mapChannel != null)
            {
                // Nothing goes on fighting a dead player.
                Threat.Forget(mapChannel, victim);

                // Their effects end with them; Rez Trauma is what stays, and a Hominis Machina's
                // morph while its Self Revive is unspent - the button is in the morph's drawer.
                var keepMorph = AbilityManager.CanSelfRevive(victim);

                foreach (var effect in victim.ActiveEffects.Values.Where(e => !e.IsSkillPassive && e.TypeId != RezSicknessTypeId && e.Parent == null
                             && !(keepMorph && e.TypeId == AbilityManager.PolymorphTypeId)).ToList())
                    GameEffectManager.Instance.DettachEffect(mapChannel, victim, effect);

                if (health != null)
                    CellManager.Instance.CellCallMethod(mapChannel, victim, new UpdateHealthPacket(health, 0));

                // For a client that missed the death blow (Actor.Recv_ActorKilled).
                CellManager.Instance.CellCallMethod(mapChannel, victim, new ActorKilledPacket());
            }

            DropOffers(victim);

            var hospitals = mapChannel != null
                ? Hospitals.AvailableTo(victim, mapChannel.MapInfo.MapContextId)
                : new List<Hospitals.Hospital>();

            client?.CallMethod(victim.EntityId, new PlayerDeadPacket(source?.EntityId ?? 0,
                hospitals.Select(h => new PlayerDeadPacket.Graveyard { Id = h.GraveyardId, Position = h.Position, IsSafe = h.IsSafe }).ToList(),
                canRevive: false));

            Logger.WriteLog(LogType.Debug, $"{victim.FamilyName} ({victim.Id}) died{(source != null ? $", killed by {source.EntityId}" : "")}.");
        }

        #endregion

        #region Going back

        /// <summary>
        /// A Hominis Machina's Self Revive (POLY_SELF_RES): up where they fell on full health,
        /// once. False when they are not dead or have no Self Revive left.
        /// </summary>
        public static bool SelfRevive(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null || mapChannel == null || player.State != CharacterState.Dead || !AbilityManager.CanSelfRevive(player))
                return false;

            AbilityManager.SpendSelfRevive(player);

            var health = player.Attributes.TryGetValue(Attributes.Health, out var attribute) ? attribute.CurrentMax : 1;

            Revive(mapChannel, client, player.Position, health, player, atHospital: false);

            return true;
        }

        /// <summary>
        /// Sends a dead player to the nearest of their hospitals, whether they asked or not: an
        /// enemy has taken their body (Reanimation, Hortimonculus). Returns whether they went.
        /// </summary>
        public static bool ForceToHospital(MapChannel mapChannel, Manifestation player)
        {
            var client = ClientOf(mapChannel, player);

            if (client == null || player.State != CharacterState.Dead)
                return false;

            ReviveMe(client, null);

            return player.State != CharacterState.Dead;
        }

        /// <summary>ReviveMe(graveyardId): to that hospital, or the nearest of theirs.</summary>
        public static void ReviveMe(Client client, int? graveyardId)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null || mapChannel == null || player.State != CharacterState.Dead)
                return;

            var available = Hospitals.AvailableTo(player, mapChannel.MapInfo.MapContextId);
            var hospital = graveyardId.HasValue ? available.FirstOrDefault(h => h.GraveyardId == (uint)graveyardId.Value) : null;

            hospital ??= Hospitals.Nearest(available, player.Position);

            // A morph kept for its Self Revive does not come along to the hospital.
            foreach (var morph in player.ActiveEffects.Values.Where(e => e.TypeId == AbilityManager.PolymorphTypeId).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, player, morph);

            var at = hospital != null ? hospital.Position + new Vector3(0f, GmMapCommands.PadHeight, 0f) : player.Position;
            var health = player.Attributes.TryGetValue(Attributes.Health, out var attribute) ? attribute.CurrentMax : 1;

            Revive(mapChannel, client, at, health, player, atHospital: true);
        }

        /// <summary>
        /// Back to life: penalties on, then health (and at a hospital, full armour and the move
        /// there), and everyone told.
        /// </summary>
        private static void Revive(MapChannel mapChannel, Client client, Vector3 at, int health, Actor reviver, bool atHospital)
        {
            var player = client.Player;

            DropOffers(player);

            // The body is getting up: a Cadaver Immolation on it goes off now, where it lay.
            AbilityManager.OnCorpseRising(mapChannel, player);

            player.State = player.StateBeforeDeath == CharacterState.Dead ? 0 : player.StateBeforeDeath;

            // A death in a battleground's match costs nothing: its wargame has neither the
            // sickness nor the wear (WARGAME_FLAGS_TEAM).
            var penalised = player.Level >= PenaltyMinLevel && !Battlegrounds.Instance.NoDeathPenalty(player);

            if (penalised)
            {
                AddRezTrauma(mapChannel, player);
                Durability.WearForDeath(client);
            }

            if (player.Attributes.TryGetValue(Attributes.Health, out var healthAttribute))
            {
                healthAttribute.Current = Math.Clamp(health, 1, Math.Max(1, healthAttribute.CurrentMax));
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateHealthPacket(healthAttribute, 0));
            }

            if (atHospital && player.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                armor.Current = armor.CurrentMax;
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateArmorPacket(armor, 0));
            }

            if (Vector3.Distance(at, player.Position) > 0.01f)
            {
                player.PlaceAt(at);

                var movement = new Movement(at, client.Movement?.ViewDirection ?? new Vector2(0f, 0f));
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), false);
            }

            // Actor.Recv_Revived: stood up, and the death, hospital and revive windows closed.
            CellManager.Instance.CellCallMethod(mapChannel, player, new RevivedPacket(reviver?.EntityId ?? player.EntityId));

            if (penalised)
                AddNoHeal(mapChannel, player);

            // Back from a PvP death at a hospital: safe for a while, as a defeated player is.
            if (atHospital && player.DiedInPvp)
                Pvp.GiveSafety(mapChannel, player);

            player.DiedInPvp = false;

            // The padlock of a locked wagered item went with the rest of their effects.
            InventoryManager.SyncWagerLock(player);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} ({player.Id}) is back{(atHospital ? " at a hospital" : $", revived by {reviver?.EntityId}")}.");
        }

        /// <summary>Rez Trauma: one more death's worth, to three, and two minutes more, to six.</summary>
        private static void AddRezTrauma(MapChannel mapChannel, Manifestation player)
        {
            var now = Environment.TickCount64;
            var current = player.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == RezSicknessTypeId && !e.IsExpired);
            var stacks = Math.Min(RezSicknessMaxStack, (current?.Stacks ?? 0) + 1);
            var leftMs = current != null ? Math.Max(0, current.ExpiresTick - now) : 0;
            var durationMs = Math.Min(RezSicknessMaxSeconds * 1000L, leftMs + RezSicknessSeconds * 1000L);

            AttachRezTrauma(mapChannel, player, stacks, durationMs);
        }

        /// <summary>Whether an effect type is one of the penalties a revive leaves: Rez Trauma or the no-healing after it.</summary>
        public static bool IsDeathPenalty(int typeId) => typeId == RezSicknessTypeId || typeId == RezSicknessNoHealTypeId;

        /// <summary>
        /// The penalties a player left the world with, back on as they arrive (RelogVitals): Rez
        /// Trauma at that many deaths' worth for what it had left, and the no-healing for its. Each
        /// is bounded by what a revive can give, so a row cannot hand out more than the game would.
        /// </summary>
        public static void RestorePenalties(MapChannel mapChannel, Manifestation player, int traumaStacks, long traumaMs, long noHealMs)
        {
            if (mapChannel == null || player == null || player.State == CharacterState.Dead)
                return;

            if (traumaStacks > 0 && traumaMs > 0)
                AttachRezTrauma(mapChannel, player, Math.Min(traumaStacks, RezSicknessMaxStack), Math.Min(traumaMs, RezSicknessMaxSeconds * 1000L));

            if (noHealMs > 0)
                AttachNoHeal(mapChannel, player, Math.Min(noHealMs, NoHealSeconds * 1000L));
        }

        private static void AttachRezTrauma(MapChannel mapChannel, Manifestation player, int stacks, long durationMs)
        {
            var now = Environment.TickCount64;

            var trauma = new GameEffect
            {
                TypeId = RezSicknessTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = (uint)stacks,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = false,
                AnnounceOnAttach = true,
                Stacks = stacks,
                PrimaryAttributesPercent = -RezSicknessPenaltyPercent * stacks,
                ExpiresTick = now + durationMs
            };

            // Attach replaces the one before.
            GameEffectManager.Instance.Attach(mapChannel, player, trauma);
        }

        private static void AddNoHeal(MapChannel mapChannel, Manifestation player) =>
            AttachNoHeal(mapChannel, player, NoHealSeconds * 1000L);

        private static void AttachNoHeal(MapChannel mapChannel, Manifestation player, long durationMs)
        {
            GameEffectManager.Instance.Attach(mapChannel, player, new GameEffect
            {
                TypeId = RezSicknessNoHealTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                IsBuff = false,
                AnnounceOnAttach = true,
                BlocksHealing = true,
                ExpiresTick = Environment.TickCount64 + durationMs
            });
        }

        /// <summary>Leaving the game, or the map, dead: to their hospital first, so they come back alive there.</summary>
        public static void PlayerLeaving(Client client)
        {
            if (client?.Player?.State == CharacterState.Dead)
                ReviveMe(client, null);
        }

        #endregion

        #region Being revived

        /// <summary>
        /// Offers a dead player a revive from reviver, bringing them back with health: the
        /// client's revive window, for REVIVE_REQUEST_DURATION. False when there is nobody to
        /// offer it to - alive, gone, or an enemy of the reviver.
        /// </summary>
        public static bool OfferRevive(MapChannel mapChannel, Manifestation reviver, Manifestation dead, int health)
        {
            if (reviver == null || dead == null || dead.State != CharacterState.Dead || Pvp.AreEnemies(reviver, dead))
                return false;

            var client = ClientOf(mapChannel, dead);

            if (client == null)
                return false;

            lock (Offers)
            {
                if (!Offers.TryGetValue(dead.EntityId, out var offers))
                    Offers[dead.EntityId] = offers = new Dictionary<ulong, Offer>();

                offers[reviver.EntityId] = new Offer
                {
                    ReviverId = reviver.EntityId,
                    ExpiresAt = Now() + ReviveRequestSeconds * 1000L,
                    Health = Math.Max(1, health)
                };
            }

            client.CallMethod(SysEntity.ClientMethodId,
                new ReviveRequestInfoPacket(reviver.EntityId, $"{reviver.Name} {reviver.FamilyName}".Trim(), 0, ReviveRequestSeconds, dead.Position));

            return true;
        }

        /// <summary>RequestRevive(reviverId): the offer taken, if it still stands.</summary>
        public static void RequestRevive(Client client, ulong reviverId)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (player == null || mapChannel == null || player.State != CharacterState.Dead)
                return;

            Offer offer;

            lock (Offers)
            {
                if (!Offers.TryGetValue(player.EntityId, out var offers) || !offers.TryGetValue(reviverId, out offer))
                    return;

                offers.Remove(reviverId);
            }

            if (Now() > offer.ExpiresAt)
                return;

            EntityManager.Instance.Actors.TryGetValue(reviverId, out var reviver);

            Revive(mapChannel, client, player.Position, offer.Health, reviver ?? player, atHospital: false);
        }

        /// <summary>RefuseRevive(reviverId): the offer dropped.</summary>
        public static void RefuseRevive(Client client, ulong reviverId)
        {
            var player = client?.Player;

            if (player == null)
                return;

            lock (Offers)
                if (Offers.TryGetValue(player.EntityId, out var offers))
                    offers.Remove(reviverId);
        }

        /// <summary>Whether a revive from reviver is waiting for this player.</summary>
        public static bool HasOffer(Manifestation player, ulong reviverId)
        {
            lock (Offers)
                return Offers.TryGetValue(player.EntityId, out var offers) && offers.TryGetValue(reviverId, out var offer) && Now() <= offer.ExpiresAt;
        }

        private static void DropOffers(Manifestation player)
        {
            lock (Offers)
                Offers.Remove(player.EntityId);
        }

        #endregion

        private static Client ClientOf(MapChannel mapChannel, Manifestation player)
        {
            if (player == null)
                return null;

            return mapChannel?.ClientList?.FirstOrDefault(c => c?.Player == player);
        }
    }
}

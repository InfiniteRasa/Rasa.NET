using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Who can see whom. Until now a creature noticed any living player within its aggro range,
    /// shortened by the player's Stealth Armor (Manifestation.DetectionRangePercent). Two things
    /// can now stop it outright:
    ///
    ///  - the player is hidden (Cloak Wave's STEALTH_EFFECT, a GameEffect with Hides): creatures
    ///    do not notice them at all, they come off every creature's aggro table (Threat.Forget)
    ///    so one fighting them turns to whoever it hates next, and players outside their squad
    ///    are not told they are there;
    ///  - the creature is blinded (Tactical Evasion's mag flash, a GameEffect with Blinds): it
    ///    drops what it was fighting and notices nobody until it wears off.
    ///
    /// Hiding a player from other players is done by destroying and re-creating their entity on
    /// those clients, which is what the cell code does when a player walks out of and back into
    /// range. Squad mates keep seeing them: it is a squad cloak, and its shimmer is theirs to
    /// follow.
    ///
    /// A GM can also go out of sight on purpose (.invisible, <see cref="SetGmInvisible"/>): from
    /// every player who is not a GM, squad mates included, and from every creature. The same
    /// destroying and re-creating does it, and the two do not undo each other: a cloak that ends
    /// on an invisible GM gives them back to the other GMs only, and a GM who turns visible while
    /// cloaked is given only to their squad.
    /// </summary>
    public static class Detection
    {
        /// <summary>Whether anything on this actor is keeping it out of sight: a cloak, or a GM's invisibility.</summary>
        public static bool IsHidden(Actor actor)
        {
            return IsCloaked(actor) || actor is Manifestation { GmInvisible: true };
        }

        /// <summary>Whether an effect on this actor is cloaking it (Cloak Wave's STEALTH_EFFECT).</summary>
        public static bool IsCloaked(Actor actor)
        {
            if (actor == null)
                return false;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.Hides)
                    return true;

            return false;
        }

        /// <summary>Whether this client sees invisible GMs: a GM's own, of any level.</summary>
        public static bool SeesInvisibleGms(Client viewer)
        {
            return (viewer?.AccountEntry?.Level ?? 0) >= (byte)GmLevel.Observer;
        }

        /// <summary>Whether a GM's invisibility keeps them from this client: it is on, and the client is not a GM's.</summary>
        public static bool IsGmHiddenFrom(Manifestation player, Client viewer)
        {
            return player != null && player.GmInvisible && viewer?.Player != player && !SeesInvisibleGms(viewer);
        }

        /// <summary>Whether anything on this actor is keeping it from seeing.</summary>
        public static bool IsBlind(Actor actor)
        {
            if (actor == null)
                return false;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.Blinds)
                    return true;

            return false;
        }

        /// <summary>
        /// Whether creatures pass this player by: out of sight (<see cref="IsHidden"/>), or a GM's
        /// Untargetable (GmAbilities), who players still see. Noticing, keeping at a fight and
        /// staying on a threat table all ask this.
        /// </summary>
        public static bool UnnoticedByCreatures(Manifestation player) =>
            IsHidden(player) || player is { GmUntargetable: true };

        /// <summary>Whether a creature can see a target at all - before any question of range.</summary>
        public static bool CanSee(Creature creature, Actor target)
        {
            // A GM's Untargetable (GmAbilities): seen by players, never by a creature.
            return !IsBlind(creature) && !IsHidden(target) && target is not Manifestation { GmUntargetable: true };
        }

        /// <summary>
        /// Whether a player is out of sight for this onlooker: an invisible GM and the onlooker no
        /// GM, or cloaked and the onlooker not in their squad.
        /// </summary>
        public static bool IsHiddenFrom(Manifestation player, Client viewer)
        {
            if (player == null || viewer?.Player == null || viewer.Player == player)
                return false;

            if (IsGmHiddenFrom(player, viewer))
                return true;

            if (!IsCloaked(player))
                return false;

            return !SameSquad(player, viewer.Player);
        }

        public static bool SameSquad(Manifestation one, Manifestation other)
        {
            return one.PartyId != 0 && one.PartyId == other.PartyId;
        }

        /// <summary>
        /// A player has gone out of sight: every creature fighting them gives up, and the clients
        /// of players outside their squad are told to take their entity away.
        /// </summary>
        public static void Hide(MapChannel mapChannel, Manifestation player)
        {
            // Off every creature's table: they turn to whoever they hate next, or give up.
            Threat.Forget(mapChannel, player);

            foreach (var viewer in Onlookers(mapChannel, player))
            {
                // An invisible GM's: they never had it.
                if (IsGmHiddenFrom(player, viewer))
                    continue;

                TakeFrom(viewer, player);
            }

            // And a team mate, not of their squad, who held them from across the map.
            FarAllies.Hidden(player);
        }

        /// <summary>A player is in sight again: the clients that lost them are given them back.</summary>
        public static void Reveal(MapChannel mapChannel, Manifestation player)
        {
            var client = mapChannel.ClientList.Find(c => c?.Player == player);

            if (client == null)
                return;

            foreach (var viewer in Onlookers(mapChannel, player))
            {
                // Still an invisible GM to this one.
                if (IsGmHiddenFrom(player, viewer))
                    continue;

                GiveTo(viewer, client);
            }
        }

        /// <summary>
        /// .invisible: a GM out of sight of every player who is not a GM, or back in it. Whoever
        /// it changes things for - those around them, creatures fighting them, the team mates who
        /// hold them from across the map, their friends and their clan - is told. False when it
        /// was already so.
        /// </summary>
        public static bool SetGmInvisible(Client gm, bool invisible)
        {
            var player = gm?.Player;

            if (player == null || player.GmInvisible == invisible)
                return false;

            var mapChannel = player.MapChannel;
            var sawThem = mapChannel == null
                ? new List<Client>()
                : ViewersAround(mapChannel, player).Where(viewer => !IsHiddenFrom(player, viewer)).ToList();

            player.GmInvisible = invisible;
            player.GmInvisibleWhispered.Clear();

            if (mapChannel != null)
            {
                if (invisible)
                {
                    // Off every creature's table, as a cloak takes them.
                    Threat.Forget(mapChannel, player);

                    foreach (var viewer in sawThem)
                        if (IsHiddenFrom(player, viewer))
                            TakeFrom(viewer, player);

                    FarAllies.Hidden(player);
                }
                else
                {
                    // Not to anyone a cloak still keeps them from; the team mates across the map
                    // are given them back by FarAllies' next pass.
                    foreach (var viewer in ViewersAround(mapChannel, player))
                        if (!sawThem.Contains(viewer) && !IsHiddenFrom(player, viewer))
                            GiveTo(viewer, gm);
                }
            }

            if (invisible)
                ShowGmStealth(gm);
            else
                gm.CallMethod(player.EntityId, new GameEffectDetachedPacket { EffectId = GmStealthEffectId });

            SocialManager.Instance.GmVisibilityChanged(gm);
            ClanManager.Instance.GmVisibilityChanged(gm);

            return true;
        }

        /// <summary>The client's GM Stealth effect (gameeffectdata.GM_STEALTH).</summary>
        public const int GmStealthEffectTypeId = 81;

        /// <summary>The GM Stealth marker's effect id: far above any the server hands out.</summary>
        public const int GmStealthEffectId = 2_000_000_081;

        /// <summary>
        /// An invisible GM's reminder that they are: the client's own GM Stealth effect (a
        /// stealth effect), its icon on their buff bar, on their client alone. It is
        /// not a server effect - nothing else sees it, and it does nothing - so it is sent again
        /// on every map they arrive on (EffectCarry.Restore), as the new map's client starts bare.
        /// </summary>
        public static void ShowGmStealth(Client gm)
        {
            var player = gm?.Player;

            if (player == null || !player.GmInvisible)
                return;

            gm.CallMethod(player.EntityId, new GameEffectAttachedPacket
            {
                EffectTypeId = GmStealthEffectTypeId,
                EffectId = GmStealthEffectId,
                EffectLevel = 1,
                SourceId = player.EntityId,
                // The buff bar only takes an effect that is announced (BaseGameEffect.AnnounceAttach
                // posts UI_ADD_STATUS_EFFECT); unannounced, it is attached and never shown.
                Announced = true,
                IsActive = true,
                IsBuff = true
            });
        }

        /// <summary>A player's entity, taken off one client.</summary>
        private static void TakeFrom(Client viewer, Manifestation player)
        {
            viewer.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(player.EntityId));
            AbilityManager.HideMorphFrom(viewer, player);
        }

        /// <summary>A player's entity, given to one client as it is now: clan, effects, a claim under way.</summary>
        private static void GiveTo(Client viewer, Client client)
        {
            var player = client.Player;

            ClanManager.Instance.ShowClanTo(viewer, player);
            viewer.CallMethod(SysEntity.ClientMethodId,
                new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, ManifestationManager.Instance.CreatePlayerEntityData(client, viewer)));
            GameEffectManager.ShowEffectsTo(viewer, player);

            // Partway through claiming a control point: shown at it.
            DynamicObjectManager.Instance.ShowClaimsTo(viewer, new[] { client });
        }

        /// <summary>Every other player around this one, squad or not.</summary>
        private static List<Client> ViewersAround(MapChannel mapChannel, Manifestation player)
        {
            var found = new List<Client>();

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var viewer in cell.ClientList)
                    if (viewer?.Player != null && viewer.Player != player && !found.Contains(viewer))
                        found.Add(viewer);

            return found;
        }

        /// <summary>The players around this one who are not in their squad - who lose sight of them.</summary>
        private static List<Client> Onlookers(MapChannel mapChannel, Manifestation player)
        {
            var found = new List<Client>();

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var viewer in cell.ClientList)
                    if (viewer?.Player != null && viewer.Player != player && !found.Contains(viewer) && !SameSquad(player, viewer.Player))
                        found.Add(viewer);

            return found;
        }
    }
}

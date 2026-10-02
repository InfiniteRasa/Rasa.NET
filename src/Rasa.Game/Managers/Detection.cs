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
    /// </summary>
    public static class Detection
    {
        /// <summary>Whether anything on this actor is keeping it out of sight.</summary>
        public static bool IsHidden(Actor actor)
        {
            if (actor == null)
                return false;

            foreach (var effect in actor.ActiveEffects.Values)
                if (effect.Hides)
                    return true;

            return false;
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

        /// <summary>Whether a creature can see a target at all - before any question of range.</summary>
        public static bool CanSee(Creature creature, Actor target)
        {
            return !IsBlind(creature) && !IsHidden(target);
        }

        /// <summary>Whether a player is out of sight for this onlooker: hidden, and not in their squad.</summary>
        public static bool IsHiddenFrom(Manifestation player, Client viewer)
        {
            if (player == null || viewer?.Player == null || viewer.Player == player)
                return false;

            if (!IsHidden(player))
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
                viewer.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(player.EntityId));
                AbilityManager.HideMorphFrom(viewer, player);
            }
        }

        /// <summary>A player is in sight again: the clients that lost them are given them back.</summary>
        public static void Reveal(MapChannel mapChannel, Manifestation player)
        {
            var client = mapChannel.ClientList.Find(c => c?.Player == player);

            if (client == null)
                return;

            foreach (var viewer in Onlookers(mapChannel, player))
            {
                viewer.CallMethod(SysEntity.ClientMethodId,
                    new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, ManifestationManager.Instance.CreatePlayerEntityData(client, viewer)));
                GameEffectManager.ShowEffectsTo(viewer, player);

                // Partway through claiming a control point: shown at it.
                DynamicObjectManager.Instance.ShowClaimsTo(viewer, new[] { client });
            }
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

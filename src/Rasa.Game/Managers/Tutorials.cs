using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// The tutorials the client leaves to the server (DisplayPlayerTutorialNotification): the ones
    /// nothing on the client raises itself. The client raises its own for a level gained, a squad
    /// or clan invite, a duel, a waypoint, a footlocker opened, ammunition run out, a jam, a buff,
    /// a Critical Death and the rest (UI_DISPLAY_PLAYER_TUTORIAL), and those are not sent here.
    ///
    /// Each is raised when the thing it explains first happens, by its own text:
    ///  - CONTROL_POINT "You are approaching a Control Point!": within <see cref="ControlPointMetres"/> of one;
    ///  - TUTMISSIONS "You have accepted a mission": a mission accepted (MissionApplication);
    ///  - TUTMISSIONGIVER "NPCs with yellow icons...": the first NPC shown with a mission to give (NpcManager);
    ///  - TUTFIRE: a weapon fired; TUTTARGET: a hostile creature targeted; RELOAD: a reload
    ///    begun; TUTCROUCH: a crouch (ManifestationManager);
    ///  - TUTHEALTHPOWER "The red, deep blue and light blue bars...": the first hit taken (ActorManager.Damage);
    ///  - TUTINVENTORYTOTRAY "Your weapon tray...": a weapon armed from the tray;
    ///    TUTINVENTORYABILITY "your ability tray primarily holds Logos Abilities...": something put in the ability tray;
    ///  - the HUD's own, which nothing in particular brings up, a little after the player first
    ///    arrives in the world (<see cref="Arrival"/>): movement, the minimap, the trays, the
    ///    radial menu - spaced so each has its turn, since the window minimises the one showing
    ///    when the next comes;
    ///  - BATTLEGROUND_RULES_INTRO, the Edmund Range rules: on every arrival on a battleground's
    ///    map (Battlegrounds.PlayerEntered). It is the one the client's table marks always-display.
    ///
    /// The client remembers which tutorials a character has seen, saved with its options, and
    /// shows each once unless it is always-display or the player has turned tutorials off; so
    /// asking twice is harmless, and the server asks once a session (Manifestation.TutorialsRaised)
    /// rather than on every shot. Not in the client, and chosen here: every trigger above but the
    /// control point's and the battleground's, the distance and the arrival timings.
    /// TUTHEALTH (the same text as TUTHEALTHPOWER), TUTABILITIES (raised by the Bootcamp scene),
    /// the duplicates of tutorials the client raises itself (TUTLEVELUP, TUTFOOTLOCKER,
    /// TUTWAYPOINTS), TUTFORCEFIELD (its title is still the placeholder "tutforcefield name") and
    /// the twelve whose text is still the placeholder ("tutexplore body") are not.
    /// </summary>
    public static class Tutorials
    {
        /// <summary>How near a control point the player comes to be told about it.</summary>
        public const float ControlPointMetres = 40f;

        /// <summary>The HUD's tutorials after a first arrival in a session: milliseconds after it, and which.</summary>
        public static readonly (long AfterMs, TutorialId Tutorial)[] Arrival =
        {
            (5_000, TutorialId.Tutmovement),
            (45_000, TutorialId.Tutminimap),
            (90_000, TutorialId.Trays),
            (135_000, TutorialId.Tutctrl),
        };

        /// <summary>The ones the client's table shows every time (tutorialdata column 5).</summary>
        private static readonly HashSet<TutorialId> AlwaysDisplay = new HashSet<TutorialId> { TutorialId.BattlegroundRulesIntro };

        /// <summary>How the clock is read; tests set their own.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>
        /// Raises the tutorial for this player, once a session - every time for an always-display
        /// one. Returns whether it was sent.
        /// </summary>
        public static bool Raise(Client client, TutorialId tutorial)
        {
            var player = client?.Player;

            // Not only in the world: an arrival is told while its map loads, as Battlegrounds tells it the teams.
            if (player == null || player.Disconected)
                return false;

            if (!AlwaysDisplay.Contains(tutorial) && !player.TutorialsRaised.Add(tutorial))
                return false;

            CommunicatorManager.Instance.DisplayPlayerTutorial(client, tutorial);
            return true;
        }

        /// <summary>As <see cref="Raise(Client, TutorialId)"/>, for an actor: nothing for a creature.</summary>
        public static bool Raise(MapChannel mapChannel, Actor actor, TutorialId tutorial)
        {
            if (!(actor is Manifestation player) || player.TutorialsRaised.Contains(tutorial) && !AlwaysDisplay.Contains(tutorial))
                return false;

            return mapChannel != null && Raise(GameEffectManager.ClientOf(mapChannel, actor), tutorial);
        }

        /// <summary>A player has arrived on a map (MapChannelManager): on the first arrival of the session the HUD's tutorials are lined up.</summary>
        public static void PlayerEntered(Client client)
        {
            var player = client?.Player;

            if (player == null || player.TutorialsLinedUp)
                return;

            player.TutorialsLinedUp = true;

            var now = Now();

            foreach (var (afterMs, tutorial) in Arrival)
                player.TutorialsDue.Add((now + afterMs, tutorial));
        }

        /// <summary>From the map channel worker, once a second: the HUD's tutorials that are due, and and the control point's, raised by coming near one.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null)
                return;

            var points = ControlPoints.Instance.OnMap(mapChannel.MapInfo.MapContextId).Where(point => point.Object != null).ToList();
            var now = Now();

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || player.Disconected || player.State == CharacterState.Dead)
                    continue;

                if (player.TutorialsDue.Count > 0 && player.TutorialsDue[0].Due <= now)
                {
                    // One at a time, the next a tick later at the soonest.
                    var (_, tutorial) = player.TutorialsDue[0];
                    player.TutorialsDue.RemoveAt(0);
                    Raise(client, tutorial);
                }

                if (!player.TutorialsRaised.Contains(TutorialId.ControlPoint)
                    && points.Any(point => Vector3.Distance(player.Position, point.Position) <= ControlPointMetres))
                    Raise(client, TutorialId.ControlPoint);
            }
        }
    }
}

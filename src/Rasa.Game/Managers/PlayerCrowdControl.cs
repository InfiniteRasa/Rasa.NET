using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;
    using Models;

    /// <summary>
    /// Stuns and knockbacks on players.
    ///
    /// Where they come from: a creature's attack whose action carries them, from its action data
    /// (CreatureActionHit) - KNOCKBACK_DISTANCE knocks back (Thrax Kick, CR_THRAX_KICK 397/1:
    /// 10 m; the other knockback actions - Boargar, Reaver, Thrax Force Blast, Kael / Mech ground
    /// pound, Strider ground pulse - work the same once a creature has them), STUN_CHANCE /
    /// STUN_DURATION stuns (Howler sonic attack, CR_HOWLER_SONIC_ATTACK 200), and
    /// abilities.stun's DURATION does (Boargar stun, CR_BOARGAR_STUN 182: 1 s).
    ///
    /// On the client:
    /// - a stun is STUN (86): StunEffect.OnAnnounceAttach puts the player in its uncontrolled
    ///   state with movement blocked, and OnDetach lets them go;
    /// - a knockback is KNOCKBACK (8), KnockbackEffect.OnAttach(target, duration). The client
    ///   has a native KnockbackState, but nothing in its Python starts it, so the server moves the
    ///   player itself: the same destination a creature is knocked to (CrowdControl
    ///   KnockbackDestination - straight away from the source, stopped where the navmesh ends),
    ///   set as their position and sent to them and everyone around (MoveObject), with movement
    ///   blocked (RequestMovementBlock) until the effect ends - the flight at KnockbackSpeed and
    ///   GetupMs on the ground.
    ///
    /// Either is a stun on the server (GameEffect.IsStun, Stuns.IsStunned): no weapon fire,
    /// melee or abilities until it ends.
    ///
    /// Graviton Armor: "Knockback / Stun Resist: X%" (+3% a pump per piece): the chance that a
    /// stun or knockback does not land at all (GameEffectManager.KnockbackStunResistOf), shown
    /// as "Resisted" when it does not.
    /// </summary>
    public static class PlayerCrowdControl
    {
        /// <summary>The chance, in percent, that a stun or knockback on this actor is resisted: its Graviton Armor, at most 100.</summary>
        public static int ResistPercent(Actor actor) => Math.Min(100, GameEffectManager.KnockbackStunResistOf(actor));

        private static bool CanBeHeld(Manifestation player)
        {
            return player != null && player.State != CharacterState.Dead
                && player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
        }

        private static Client ClientOf(MapChannel mapChannel, Manifestation player)
        {
            return mapChannel?.ClientList.FirstOrDefault(c => c?.Player == player);
        }

        /// <summary>Stuns a player for durationMs, unless their Graviton Armor resists it. Returns whether it landed.</summary>
        public static bool Stun(MapChannel mapChannel, Manifestation player, Actor source, int durationMs)
        {
            if (!CanBeHeld(player) || durationMs <= 0 || mapChannel == null)
                return false;

            if (Stuns.Roll(ResistPercent(player)))
            {
                Resisted(mapChannel, player, Stuns.StunTypeId, source);
                return false;
            }

            var stun = new GameEffect
            {
                TypeId = Stuns.StunTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source?.EntityId ?? 0,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? (int)((source as Creature)?.Level ?? 1),
                IsBuff = false,
                IsStun = true,
                ExpiresTick = Environment.TickCount64 + durationMs
            };

            // StunEffect.OnAnnounceAttach: uncontrolled, movement blocked.
            GameEffectManager.Instance.Attach(mapChannel, player, stun);

            return true;
        }

        /// <summary>
        /// Knocks a player back distance metres from source, unless their Graviton Armor resists
        /// it: moved to where the knockback ends, and held there for the flight, the getup and
        /// extraStunMs more. Returns whether it landed.
        /// </summary>
        public static bool Knockback(MapChannel mapChannel, Manifestation player, Actor source, float distance, int extraStunMs = 0)
        {
            if (!CanBeHeld(player) || source == null || distance <= 0f || mapChannel == null)
                return false;

            if (Stuns.Roll(ResistPercent(player)))
            {
                Resisted(mapChannel, player, CrowdControl.KnockbackTypeId, source);
                return false;
            }

            var dir = CrowdControl.AwayFrom(source.Position, player.Position);
            var destination = CrowdControl.KnockbackDestination(mapChannel, player.Position, dir, distance);
            var travelled = Vector3.Distance(player.Position, destination);
            var downMs = KnockdownMs(travelled, extraStunMs);
            var client = ClientOf(mapChannel, player);

            var knock = new GameEffect
            {
                TypeId = CrowdControl.KnockbackTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source.EntityId,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? (int)((source as Creature)?.Level ?? 1),
                IsBuff = false,
                IsStun = true,
                ExpiresTick = Environment.TickCount64 + downMs
            };

            // Held until it ends, however it ends - run out, cleared on leaving the map.
            if (client != null)
            {
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                knock.OnDetached = (map, actor, e) => client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
            }

            // KnockbackEffect.OnAttach(target, duration).
            GameEffectManager.Instance.Attach(mapChannel, player, knock, downMs / 1000.0);

            if (travelled > 0.1f && client != null)
            {
                player.PlaceAt(destination);

                // To them and to everyone around: the player is where the knockback left them.
                var movement = new Movement(destination, client.Movement?.ViewDirection ?? new Vector2(0f, 0f));
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), false);
            }

            return true;
        }

        /// <summary>
        /// Graviton Armor shrugged it off: "Resisted" floats over the player for everyone around
        /// (GameEffectAttachFailed, COMBAT_RESIST_ANNOUNCED), as it would over a creature.
        /// </summary>
        private static void Resisted(MapChannel mapChannel, Manifestation player, int typeId, Actor source)
        {
            CellManager.Instance.CellCallMethod(mapChannel, player,
                new GameEffectAttachFailedPacket(typeId, GameEffectAttachFailedPacket.FailReason.Resist, source?.EntityId ?? 0));
        }

        /// <summary>How long a knockback holds its target: the flight at KnockbackSpeed, the getup, and any extra stun.</summary>
        public static int KnockdownMs(float travelled, int extraStunMs = 0)
        {
            return (int)(Math.Max(0f, travelled) / CrowdControl.KnockbackSpeed * 1000f) + CrowdControl.GetupMs + Math.Max(0, extraStunMs);
        }

        /// <summary>
        /// What a creature's attack does to the player it hit beyond its damage, from the attack's
        /// action data: KNOCKBACK_DISTANCE knocks them back, a stun (Stuns.OfAbility, or
        /// abilities.stun's DURATION) stuns them.
        /// </summary>
        public static (float Knockback, int StunChance, int StunMs) OfCreatureAction(string module, ActionLevelInfo info)
        {
            if (info == null)
                return (0f, 0, 0);

            var knockback = info.Get(AbilityProperty.KnockbackDistance);
            var (chance, ms) = Stuns.OfAbility(module, info);

            if (ms <= 0 && module == "abilities.stun")
                (chance, ms) = (100, info.Get(AbilityProperty.Duration) * 1000);

            return (Math.Max(0, knockback), chance, ms);
        }

        /// <summary>Whether the action's knockback carries its stun too, rather than the stun being what it does when it does not knock back.</summary>
        public static bool KnocksDownAndStuns(string module) => module == "abilities.tectonicstrike";

        /// <summary>A creature's attack has hit a player: its knockback or stun, if its action carries one.</summary>
        public static void CreatureActionHit(MapChannel mapChannel, Creature attacker, Manifestation player, ActionId actionId, uint actionArgId)
        {
            if (attacker == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetAction(actionId, actionArgId, out var module, out var info))
                return;

            var (knockback, chance, ms) = OfCreatureAction(module, info);

            // CHANCE_KNOCK_BACK, where the action gives one (Kael rushing blow: 30%), is the
            // chance the knockback lands; one that does not can still stagger. A Tectonic Strike
            // knocks back and stuns, as a player's does: the stun keeps them down after the
            // getup (the Treeback's stomp: 20 m, then 8 s).
            if (knockback > 0 && Stuns.Roll(info.Get(AbilityProperty.ChanceKnockBack, 100)))
                Knockback(mapChannel, player, attacker, knockback, KnocksDownAndStuns(module) ? ms : 0);
            else if (ms > 0 && Stuns.Roll(chance))
                Stun(mapChannel, player, attacker, ms);
        }
    }
}

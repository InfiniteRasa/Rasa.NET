using System;
using System.Collections.Generic;
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
    /// - a knockback is KNOCKBACK (8), KnockbackEffect.OnAttach(target, duration), which only
    ///   keeps the duration. What throws the player is the movement: a MoveObject of
    ///   MovementType.Knockback puts their controller into its KnockbackState, which flies the
    ///   arc to the position at KnockbackSpeed and keeps them down for GetupMs, stunned and then
    ///   blocked, on their own client and on everyone's who sees them. The position is the
    ///   destination a creature is knocked to (CrowdControl KnockbackDestination - straight away
    ///   from the source, stopped where the navmesh ends). The server has them there from the
    ///   start, with movement blocked (RequestMovementBlock) until the effect ends - the flight
    ///   and the getup, the same length as the client's state.
    ///
    /// Either is a stun on the server (GameEffect.IsStun, Stuns.IsStunned): no weapon fire,
    /// melee or abilities until it ends.
    ///
    /// Graviton Armor: "Knockback / Stun Resist: X%" (+3% a pump per piece): the chance that a
    /// stun or knockback does not land at all (GameEffectManager.KnockbackStunResistOf), shown
    /// as "Resisted" when it does not. An armor module's "Resist: Stun" or "Resist: Knockback"
    /// adds to it for its own kind (ItemModuleBonuses.ControlResistPercent).
    /// </summary>
    public static class PlayerCrowdControl
    {
        /// <summary>
        /// The chance, in percent, that a stun or a knockback on this actor is resisted: its
        /// Graviton Armor, which is for both, and its item modules' Resist for the one it is
        /// (ItemModuleBonuses) - at most 100.
        /// </summary>
        public static int ResistPercent(Actor actor, DamageType kind) =>
            Math.Min(100, GameEffectManager.KnockbackStunResistOf(actor) + ItemModuleBonuses.ControlResistPercent(actor, kind));

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

            if (Stuns.Roll(ResistPercent(player, DamageType.Stun)))
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

            return player.ActiveEffects.ContainsKey(stun.EffectId);
        }

        /// <summary>
        /// Knocks a player back distance metres from source, unless their Graviton Armor resists
        /// it: thrown to where the knockback ends, and held there for the flight, the getup and
        /// extraStunMs more. Returns whether it landed.
        /// </summary>
        public static bool Knockback(MapChannel mapChannel, Manifestation player, Actor source, float distance, int extraStunMs = 0, float turnDegrees = 0f)
        {
            return source != null && Knockback(mapChannel, player, source, source.Position, source.EntityId, distance, extraStunMs, turnDegrees);
        }

        /// <summary>
        /// A knockback from a point rather than an actor - an object's blast (ObjectAbilities) -
        /// with the entity that threw it named as its source. source is the actor behind it, if
        /// there is one; none for a blast that is nobody's.
        /// </summary>
        public static bool Knockback(MapChannel mapChannel, Manifestation player, Actor source, Vector3 from, ulong sourceId, float distance, int extraStunMs = 0, float turnDegrees = 0f)
        {
            if (!CanBeHeld(player) || distance <= 0f || mapChannel == null)
                return false;

            if (Stuns.Roll(ResistPercent(player, DamageType.KnockBack)))
            {
                Resisted(mapChannel, player, CrowdControl.KnockbackTypeId, source);
                return false;
            }

            // Straight away from the source, or turned off it by the attack's KNOCKBACK_HEADING (CrowdControl.TurnOf).
            var dir = CrowdControl.Turned(CrowdControl.AwayFrom(from, player.Position), turnDegrees);
            var destination = CrowdControl.KnockbackDestination(mapChannel, player.Position, dir, distance);

            // Not through a field that stops players: a Strider's 20 m throw ends at a Bane one.
            destination = CrowdControl.BeforeFields(mapChannel, player, player.Position, destination);

            var travelled = Vector3.Distance(player.Position, destination);
            var downMs = KnockdownMs(travelled, extraStunMs);
            var client = ClientOf(mapChannel, player);

            var knock = new GameEffect
            {
                TypeId = CrowdControl.KnockbackTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = sourceId,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? (int)((source as Creature)?.Level ?? 1),
                IsBuff = false,
                IsStun = true,
                ExpiresTick = Environment.TickCount64 + downMs
            };

            // Held until it ends, however it ends - run out, cleared on leaving the map.
            // KnockbackEffect.OnAttach(target, duration).
            HoldInPlace(mapChannel, player, knock, downMs / 1000.0);

            // Refused (Cure's immunity, PvP Safety): not moved either.
            if (!player.ActiveEffects.ContainsKey(knock.EffectId))
                return false;

            if (travelled > 0.1f && client != null)
            {
                player.PlaceAt(destination);

                // To them and to everyone around: thrown to where the knockback leaves them.
                var movement = Movement.Knockback(destination, client.Movement?.ViewDirection ?? new Vector2(0f, 0f));
                client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, movement), false);
            }

            return true;
        }

        /// <summary>
        /// Holds a player where they stand for durationMs - an enemy player's net gun: movement
        /// blocked (RequestMovementBlock) until the effect ends, but not a stun, so they may still
        /// shoot. Shown as the given effect type. Returns whether it landed.
        /// </summary>
        public static bool Root(MapChannel mapChannel, Manifestation player, Actor source, int typeId, int durationMs)
        {
            if (!CanBeHeld(player) || durationMs <= 0 || mapChannel == null)
                return false;

            var root = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source?.EntityId ?? 0,
                Source = source,
                SourceLevel = (source as Manifestation)?.Level ?? (int)((source as Creature)?.Level ?? 1),
                IsBuff = false,
                IsRoot = true,
                ExpiresTick = Environment.TickCount64 + durationMs
            };

            HoldInPlace(mapChannel, player, root);

            return true;
        }

        /// <summary>
        /// Attaches a rooting effect to a player with their movement blocked (RequestMovementBlock)
        /// until it comes off - the client does not block a player's own movement for a root.
        /// </summary>
        public static void HoldInPlace(MapChannel mapChannel, Manifestation player, GameEffect root, params object[] args)
        {
            GameEffectManager.Instance.Attach(mapChannel, player, root, args);

            // Refused (Cure's immunity): nothing to hold them with.
            var client = ClientOf(mapChannel, player);

            if (client == null || !player.ActiveEffects.ContainsKey(root.EffectId))
                return;

            client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());

            var detached = root.OnDetached;

            root.OnDetached = (map, actor, e) =>
            {
                client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                detached?.Invoke(map, actor, e);
            };
        }

        /// <summary>
        /// Drags a player towards puller over flailMs - an enemy player's Vortex - to the point
        /// CrowdControl.PullPath gives a creature, unless their Graviton Armor resists it: moved
        /// there for them and everyone around, and held there for the flail. Returns whether it
        /// landed.
        /// </summary>
        public static bool Pull(MapChannel mapChannel, Manifestation player, Actor puller, int flailMs)
        {
            if (!CanBeHeld(player) || puller == null || mapChannel == null)
                return false;

            var (destination, _) = CrowdControl.PullPath(mapChannel, player.Position, puller.Position, flailMs);

            // Nor dragged through one.
            destination = CrowdControl.BeforeFields(mapChannel, player, player.Position, destination);

            if (Vector3.Distance(player.Position, destination) <= 0.1f)
                return false;

            if (Stuns.Roll(ResistPercent(player, DamageType.KnockBack)))
            {
                Resisted(mapChannel, player, CrowdControl.KnockbackTypeId, puller);
                return false;
            }

            var client = ClientOf(mapChannel, player);

            // Nothing to show of its own - the client plays the flail from the Vortex's recovery -
            // so the hold is the server's alone.
            var hold = new GameEffect
            {
                TypeId = CrowdControl.KnockbackTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = puller.EntityId,
                Source = puller,
                IsBuff = false,
                IsStun = true,
                ServerOnly = true,
                ExpiresTick = Environment.TickCount64 + Math.Max(250, flailMs)
            };

            HoldInPlace(mapChannel, player, hold);

            if (!player.ActiveEffects.ContainsKey(hold.EffectId))
                return false;

            if (client != null)
            {
                player.PlaceAt(destination);

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

        /// <summary>The creature classes whose targetGameEffect is KnockbackEffect: they throw whoever they hit.</summary>
        private static readonly HashSet<string> ThrowingModules = new HashSet<string>
        {
            "abilities.ai.stridergroundpulseability", "abilities.ai.kaelgroundpoundability"
        };

        /// <summary>
        /// How far a creature's attack throws the player it hit, standing where they stand:
        ///  - KNOCKBACK_DISTANCE where the action gives one, and none where it gives 0 (the Thrax
        ///    lightning's knockback class with KNOCKBACK_DISTANCE 0);
        ///  - an action that throws without a distance - a class whose target effect is the
        ///    knockback (the Strider ground pulse), or a CHANCE_KNOCK_BACK - throws out of its
        ///    area: to RADIUS_AROUND_SOURCE from the creature, its edge;
        ///  - with no area, the client's KNOCKBACK_DEFAULT_DISTANCE (CrowdControl.DefaultKnockbackDistance,
        ///    10 m: the Atta Soldier's rock throw);
        ///  - otherwise nothing.
        /// </summary>
        public static float KnockbackDistanceOf(string module, ActionLevelInfo info, Vector3 source, Vector3 victim)
        {
            if (info == null)
                return 0f;

            if (info.Has(AbilityProperty.KnockbackDistance))
                return Math.Max(0, info.Get(AbilityProperty.KnockbackDistance));

            if (!ThrowingModules.Contains(module) && !info.Has(AbilityProperty.ChanceKnockBack))
                return 0f;

            var radius = info.Get(AbilityProperty.RadiusAroundSource);

            if (radius > 0)
                return Math.Max(0f, radius - Vector3.Distance(source, victim));

            return CrowdControl.DefaultKnockbackDistance;
        }

        /// <summary>
        /// How long a creature's knockback keeps its player down after the getup: a Tectonic
        /// Strike's stun, or DURATION_KNOCK_BACK where the action gives one (the rock throw's 2-3 s).
        /// </summary>
        public static int ExtraDownMs(string module, ActionLevelInfo info, int stunMs)
        {
            if (KnocksDownAndStuns(module))
                return stunMs;

            return info == null ? 0 : Math.Max(0, info.Get(AbilityProperty.DurationKnockBack)) * 1000;
        }

        /// <summary>Whether the action's knockback carries its stun too, rather than the stun being what it does when it does not knock back.</summary>
        public static bool KnocksDownAndStuns(string module) => module == "abilities.tectonicstrike";

        /// <summary>A creature's attack has hit a player: its knockback or stun, if its action carries one.</summary>
        public static void CreatureActionHit(MapChannel mapChannel, Creature attacker, Manifestation player, ActionId actionId, uint actionArgId)
        {
            if (attacker == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetAction(actionId, actionArgId, out var module, out var info))
                return;

            var (_, chance, ms) = OfCreatureAction(module, info);
            var knockback = KnockbackDistanceOf(module, info, attacker.Position, player.Position);

            // CHANCE_KNOCK_BACK, where the action gives one (Kael rushing blow: 30%), is the
            // chance the knockback lands; one that does not can still stagger. A Tectonic Strike
            // knocks back and stuns, as a player's does: the stun keeps them down after the
            // getup (the Treeback's stomp: 20 m, then 8 s). KNOCKBACK_HEADING, which only the
            // Boargar's knockback gives (105), is read against the client's default heading of 180,
            // straight away: 75 degrees off it (CrowdControl.TurnOf).
            if (knockback > 0 && Stuns.Roll(info.Get(AbilityProperty.ChanceKnockBack, 100)))
                Knockback(mapChannel, player, attacker, knockback, ExtraDownMs(module, info, ms), CrowdControl.TurnOf(info));
            else if (ms > 0 && Stuns.Roll(chance))
                Stun(mapChannel, player, attacker, ms);
        }
    }
}

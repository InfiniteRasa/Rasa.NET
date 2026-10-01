using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The control abilities on an enemy player across a wargame (Pvp), which has no mind or
    /// aggro table to turn:
    ///
    ///  - Mind Control P1 (Frighten) stuns the player for DURATION, halved (Pvp.ScaleDuration).
    ///    P2-P5 leave them unable to attack (GameEffect.NoAttack: no weapon fire, no hostile
    ///    ability) for DURATION, halved as any debuff from an enemy is; P4-P5 leave them unable to
    ///    help anyone else as well (GameEffect.NoAssist: no ability at another player, no heal or
    ///    buff on anyone else, no squad wave beyond themselves). The player is then immune to it
    ///    for DURATION more, as a creature is. The effect is MIND_CONTROL_EFFECT, announced by the
    ///    recovery's EffectAttach hit; Frighten's hit announces the stun.
    ///  - Traitor (GameEffect.Restrains): for DURATION, halved, the player keeps their side but
    ///    may not hurt the caster, the caster's side, or a creature of theirs - the hit lands as
    ///    Immune (Pvp.Restrained) - and it comes off the moment the caster hurts them (Pvp.OnHit).
    ///  - Hack on an enemy player's machine (a turret, a construction bot): it is held where it
    ///    stands for DURATION (HACKED_EFFECT, a stun on the server) until it takes damage, or its
    ///    master takes damage or attacks (<see cref="ReleaseHackedPets"/>).
    ///  - Tactical Evasion's mag flash blinds enemy players in its radius too: the client's
    ///    BlindEffect whites out the screen of the one it lands on, drops their target and keeps
    ///    target lock off until it ends, halved.
    ///
    /// Mind Control and Traitor do not take an enemy player's creature: it is announced immune.
    /// </summary>
    public partial class AbilityManager
    {
        private sealed class HackedPet
        {
            public MapChannel MapChannel;
            public Creature Pet;
            public GameEffect Hold;
            public ulong MasterId;
        }

        private static readonly List<HackedPet> HackedPets = new List<HackedPet>();
        private static readonly object HackedPetsLock = new object();

        /// <summary>Mind Control on an enemy player, as the pump says; false when it did not land.</summary>
        private bool AttachPlayerMindControl(MapChannel mapChannel, Manifestation player, Manifestation enemy, ActionLevelInfo info, out int shownTypeId)
        {
            shownTypeId = MindControlTypeId;

            if (!Pvp.IsEnemyTarget(player, enemy) || Pvp.Attack(mapChannel, player, enemy))
                return false;

            var now = Environment.TickCount64;

            if (IsMindControlImmune(enemy.EntityId, now) || enemy.ActiveEffects.Values.Any(e => e.TypeId == MindControlTypeId))
            {
                CellManager.Instance.CellCallMethod(mapChannel, enemy,
                    new GameEffectAttachFailedPacket(MindControlTypeId, GameEffectAttachFailedPacket.FailReason.Immune, player.EntityId));
                return false;
            }

            var pump = Math.Clamp((int)info.Level, MindControlFrighten, MindControlInfectious);
            var seconds = info.Get(AbilityProperty.Duration, 15);
            var heldMs = Pvp.ScaleDuration(player, enemy, seconds * 1000);
            bool landed;

            if (pump == MindControlFrighten)
            {
                // "Target flees combat" is a stun on a player.
                landed = PlayerCrowdControl.Stun(mapChannel, enemy, player, heldMs);
                shownTypeId = Stuns.StunTypeId;
            }
            else
            {
                var control = NewEffect(mapChannel, player, info, MindControlTypeId, seconds);

                control.IsBuff = false;
                control.AllowDetach = false;
                control.NoAttack = true;
                control.NoAssist = pump >= MindControlEnslavement;

                GameEffectManager.Instance.Attach(mapChannel, enemy, control);
                landed = enemy.ActiveEffects.ContainsKey(control.EffectId);

                if (landed)
                    heldMs = (int)Math.Max(0, control.ExpiresTick - now);
            }

            if (landed)
                lock (MindControlLock)
                    MindControlImmuneUntil[enemy.EntityId] = now + heldMs + seconds * 1000L;

            return landed;
        }

        /// <summary>Controlled now, or not long since: by entity id, player or creature.</summary>
        public static bool IsMindControlImmune(ulong entityId, long now)
        {
            lock (MindControlLock)
                return MindControlImmuneUntil.TryGetValue(entityId, out var until) && now < until;
        }

        /// <summary>Traitor on an enemy player; false when it did not land.</summary>
        private bool AttachPlayerTraitor(MapChannel mapChannel, Manifestation player, Manifestation enemy, ActionLevelInfo info)
        {
            if (!Pvp.IsEnemyTarget(player, enemy) || Pvp.Attack(mapChannel, player, enemy))
                return false;

            var traitor = NewEffect(mapChannel, player, info, TraitorTypeId, info.Get(AbilityProperty.Duration, 10));

            traitor.IsBuff = false;
            traitor.AllowDetach = false;
            traitor.Restrains = true;

            GameEffectManager.Instance.Attach(mapChannel, enemy, traitor);

            return enemy.ActiveEffects.ContainsKey(traitor.EffectId);
        }

        /// <summary>
        /// A creature of an enemy player's that Mind Control or Traitor was aimed at: it is not
        /// theirs to turn, and everyone who can see it is told it is immune.
        /// </summary>
        private static bool RefuseEnemyPet(MapChannel mapChannel, Manifestation player, Creature target, int typeId)
        {
            if (target == null || !Pvp.IsEnemySummon(player, target))
                return false;

            CellManager.Instance.CellCallMethod(mapChannel, target,
                new GameEffectAttachFailedPacket(typeId, GameEffectAttachFailedPacket.FailReason.Immune, player.EntityId));

            return true;
        }

        /// <summary>
        /// Hack on an enemy player's machine: held for DURATION, until it is hurt or its master is
        /// hurt or attacks. False when it did not land.
        /// </summary>
        private bool HackPet(MapChannel mapChannel, Manifestation player, Creature pet, ActionLevelInfo info)
        {
            if (!Pvp.IsEnemySummon(player, pet) || !CanHack(pet) || pet.State == CharacterState.Dead || pet.State == CharacterState.Dying)
                return false;

            if (IsHackImmune(pet))
            {
                CellManager.Instance.CellCallMethod(mapChannel, pet,
                    new GameEffectAttachFailedPacket(HackedTypeId, GameEffectAttachFailedPacket.FailReason.Immune, player.EntityId));
                return false;
            }

            var hold = NewEffect(mapChannel, player, info, HackedTypeId, info.Get(AbilityProperty.Duration, 10));

            hold.IsBuff = false;
            hold.IsStun = true;
            hold.AllowDetach = false;
            hold.BreaksOnDamage = true;

            GameEffectManager.Instance.Attach(mapChannel, pet, hold);

            if (!pet.ActiveEffects.ContainsKey(hold.EffectId))
                return false;

            BehaviorManager.Instance.StopMoving(pet);

            var hacked = new HackedPet { MapChannel = mapChannel, Pet = pet, Hold = hold, MasterId = pet.MasterEntityId };

            hold.OnDetached = (map, actor, e) =>
            {
                lock (HackedPetsLock)
                    HackedPets.Remove(hacked);
            };

            lock (HackedPetsLock)
                HackedPets.Add(hacked);

            return true;
        }

        /// <summary>A creature has taken damage: a hold that breaks on it comes off.</summary>
        internal static void ReleaseOnDamage(MapChannel mapChannel, Creature creature)
        {
            foreach (var hold in creature.ActiveEffects.Values.Where(e => e.BreaksOnDamage).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, creature, hold);
        }

        /// <summary>The master of hacked machines has taken damage or attacked: their machines are let go.</summary>
        internal static void ReleaseHackedPets(MapChannel mapChannel, Manifestation master)
        {
            if (master == null || mapChannel == null)
                return;

            List<HackedPet> theirs;

            lock (HackedPetsLock)
                theirs = HackedPets.Where(h => h.MasterId == master.EntityId).ToList();

            foreach (var hacked in theirs)
            {
                if (hacked.Pet.ActiveEffects.ContainsKey(hacked.Hold.EffectId))
                    GameEffectManager.Instance.DettachEffect(hacked.MapChannel, hacked.Pet, hacked.Hold);
                else
                    lock (HackedPetsLock)
                        HackedPets.Remove(hacked);
            }
        }

        /// <summary>
        /// Tactical Evasion's mag flash on the enemy players around the performer: blinded for the
        /// flash's time, halved (GameEffectManager.Attach), each a hit on the recovery.
        /// </summary>
        private static void FlashEnemies(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, float radius, int durationMs, AbilityRecoveryPacket recovery)
        {
            foreach (var enemy in Pvp.EnemiesWithin(mapChannel, player, player.Position, radius))
            {
                if (Pvp.Attack(mapChannel, player, enemy))
                    continue;

                var flash = NewEffect(mapChannel, player, info, MagFlashTypeId, null);

                flash.IsBuff = false;
                flash.Blinds = true;
                flash.ExpiresTick = Environment.TickCount64 + durationMs;

                GameEffectManager.Instance.Attach(mapChannel, enemy, flash);

                if (!enemy.ActiveEffects.ContainsKey(flash.EffectId))
                    continue;

                // The client drops its target itself (BlindEffect, without telling the server);
                // the server lets go of it too.
                var client = mapChannel.ClientList.Find(c => c?.Player == enemy);

                if (client != null)
                    ManifestationManager.Instance.SetTargetId(client, 0);

                Hit(recovery, enemy);
            }
        }
    }
}

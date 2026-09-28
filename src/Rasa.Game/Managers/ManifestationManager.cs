using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Rasa.Models;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Persistence;
    using Game.Missions.Protocol;
    using Packets;
    using Packets.Communicator.Server;
    using Packets.Game.Server;
    using Packets.Inventory.Server;
    using Packets.Manifestation.Client;
    using Packets.Manifestation.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    public class ManifestationManager
    {
        /* Actor: Player "bodies" (ManifestationClass)
         * 
         *    Manifestation Packets:
         *  - CurrentCharacterId                => implemented
         *  - AllCredits                        => implemented
         *  - UpdateCredits                     => implemented
         *  - LockboxFunds                      => implemented
         *  - WonBattleground                   => ToDo
         *  - LostBattleground                  => ToDo
         *  - WeaponDrawerSlot                  => implemented
         *  - AbilityDrawerSlot                 => implemented
         *  - AbilityDrawer                     => implemented
         *  - ArmWeaponFailed                   => ToDo
         *  - ArmAbilityFailed                  => ToDo
         *  - AdvancementStats                  => implemented
         *  - ExperienceChanged                 => ToDo
         *  - LevelChanged                      => ToDo
         *  - CharacterClass                    => implemented
         *  - AvailableAllocationPoints
         *  - AvailableCharacterClasses
         *  - TierAdvancementInfo
         *  - InvitedToJoinFriend
         *  - InvitationDeclined
         *  - InvitationCancelled
         *  - CannotInvite
         *  - InvitedToAddAndJoinFriend
         *  - RequestToJoin
         *  - JoinFriendDeclined
         *  - JoinFriendCancelled
         *  - CannotJoin
         *  - ForceConverse
         *  - LogosStoneTabula
         *  - LogosStoneAdded
         *  - LogosStoneRemoved
         *  - ShowHelmetChanged
         *  - Titles
         *  - TitleChanged
         *  - TitleAdded
         *  - TitleRemoved
         *  - PlayerFlags
         *  - CloneCredits
         *  - WaypointGained
         *  - GraveyardGained
         *  - CharacterName
         *  - RaceId
         *  - PlayerAfk                        => implemented
         *  - PlayerInactiveWarning            => implemented
         *  - ClanId
         *  - IsTrialAccount                   => implemented (always false)
         *  - PlayerEnteredCombat
         *  - PlayerExitedCombat
         *  - MinionAdded
         *  - MinionStayAck
         *  - MinionGoAck
         *  - MinionFollowMeAck
         *  - MinionFollowTargetAck
         *  - MinionTargetMeAck
         *  - MinionTargetAck
         *  - MinionAssistMeAck
         *  - MinionAssistTargetAck
         *  - MinionTemperamentAck
         *  - MinionCommandAck
         *  
         *  Manifestation Handlrs:
         *  - AutoFireKeepAlive         => ToDo
         *  - ChangeShowHelmet          => ToDo
         *  - ChangeTitle               => implemented, but need more work on it
         *  - RespondToAddAndJoinFriend => ToDo
         *  - RespondToJoinFriend       => ToDo
         *  - RespondToRequestToJoin    => ToDo
         *  - RequestArmAbility         => implemented
         *  - RequestArmWeapon          => implemented
         *  - RequestSetAbilitySlot     => implemented
         *  - RequestSwapAbilitySlots   => implemented
         *  - StartAutoFire             => implemented, but need more work on it
         *  - StopAutoFire              => implemented, but need more work on it
         */
        private static ManifestationManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly CharacterManager _characterManager;
        private readonly MissionApplication _missionManager;

        private static List<AutoFireTimer> AutoFire = new List<AutoFireTimer>();

        /// <summary>
        /// How far a shot may be handled from when it was due, early or late, without the shot
        /// clock holding it against the player, in ms.
        ///
        /// A shot is timed when it is handled, which is a tick at a time, after a network that
        /// delays packets unevenly: two shots sent a refire apart can be handled closer together
        /// than that, or one late and the next on time. A strict clock would refuse the second
        /// of a close pair, and charge a late shot's delay to the one after it. Within the
        /// allowance a shot is charged from when it was due instead, so over any stretch of time
        /// no more than one shot per refire is fired - plus, once, twice the allowance's worth.
        /// </summary>
        private const long ShotTolerance = 250;

        /// <summary>
        /// The least a shot is charged, in ms. The auto-fire list is walked once every 100 ms
        /// (MapChannelManager's "AutoFire" timer), which is the fastest the server fires a weapon
        /// by itself; a weapon whose refire reads 0 is held to that rather than to nothing.
        /// </summary>
        private const long MinRefire = 100;

        private enum FireResult
        {
            Fired,
            /// <summary>Everything else allowed the shot, and the shot clock did not yet.</summary>
            TooSoon,
            NotFired
        }

        public static byte MaxPlayerLevel = 50;

        /// <summary>The levels that award a clone credit, per the live game's own rules.</summary>
        public static readonly byte[] CloneCreditLevels = { 5, 15, 30 };
        public static ManifestationManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ManifestationManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal ManifestationManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, MissionApplication missionManager = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _characterManager = new CharacterManager(gameUnitOfWorkFactory);
            _missionManager = missionManager;
        }

        // constant skillId data
        public readonly int[] SkillIById = {
            1,8,14,19,20,21,22,23,24,
            25,26,28,30,31,32,34,35,
            36,37,39,40,43,47,48,49,
            50,54,55,57,58,63,66,67,
            68,72,73,77,79,80,82,89,
            92,102,110,111,113,114,121,135,
            136,147,148,149,150,151,152,153,
            154,155,156,157,158,159,160,161,
            162,163,164,165,166,172,173,174
        };
        // table for skillId to skillIndex mapping
        private readonly int[] SkillId2Idx =
        {
            -1,0,-1,-1,-1,-1,-1,-1,1,-1,-1,-1,-1,-1,2,-1,-1,-1,-1,3,
            4,5,6,7,8,9,10,-1,11,-1,12,13,14,-1,15,16,17,18,-1,19,
            20,-1,-1,21,-1,-1,-1,22,23,24,25,-1,-1,-1,26,27,-1,28,29,-1,
            -1,-1,-1,30,-1,-1,31,32,33,-1,-1,-1,34,35,-1,-1,-1,36,-1,37,
            38,-1,39,-1,-1,-1,-1,-1,-1,40,-1,-1,41,-1,-1,-1,-1,-1,-1,-1,
            -1,-1,42,-1,-1,-1,-1,-1,-1,-1,43,44,-1,45,46,-1,-1,-1,-1,-1,
            -1,47,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,48,49,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,50,51,52,53,54,55,56,57,58,59,60,61,62,
            63,64,65,66,67,68,69,-1,-1,-1,-1,-1,70,71,72,-1,-1,-1,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1
        };
        // table for skillIndex to ability mapping
        public readonly int[] SkillIdx2AbilityId =
        {
            -1, -1, -1, -1, 137, -1, -1, -1, -1, 178, 177, 158, -1, -1,
            197, 186, 188, 162, 187, -1, -1, 233, 234, -1, 194, -1, -1,
            -1, -1, -1, 301, -1, -1, 185, 251, 240, 302, 232, 229, -1,
            231, 305, 392, 252, 282, 381, 267, 298, 246, 253, 307, 393,
            281, 390, 295, 304, 386, 193, 385, 176, 260, 384, 383, 303,
            388, 389, 387, 380, 401, 430, 262, 421, 446
        };

        /// <summary>
        /// Points to reach each skill rank, cumulative. The index is the rank, so the array's
        /// last index is the cap.
        /// </summary>
        public readonly int[] requiredSkillLevelPoints = { 0, 1, 3, 6, 10, 15 };

        /// <summary>The highest rank any skill goes to, which is what the client's own UI caps at.</summary>
        public const int MaxSkillLevel = 5;

        #region Handlers
        public void AutoFireKeepAlive(Client client, int keepAliveDelay)
        {
            // Four times the client's own interval, within reason: the value is the client's,
            // and zero or negative would stop the fire on the next tick, huge would keep it
            // going for a crashed client.
            var aliveTime = Math.Clamp((long)keepAliveDelay * 4, 1000, 30000);

            foreach (var timer in AutoFire)
                if (timer.Client == client)
                    timer.MaxAliveTime = aliveTime;
        }

        public void ChangeShowHelmet(Client client, ChangeShowHelmetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo ChangeShowHelmet");
        }

        public void ChangeTitle(Client client, uint titleId)
        {
            //if (titleId != 0)
            //{
            client.Player.CurrentTitle = titleId;
            client.CallMethod(client.Player.EntityId, new TitleChangedPacket(titleId));
            /*}
            else
            {
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new TitleRemovedPacket(client.MapClient.Player.CurrentTitle));
                client.MapClient.Player.CurrentTitle = titleId;
            }

            client.MapClient.Player.CurentTitle = titleId;*/
        }

        public bool PlayerTryFireWeapon(Client client) => TryFireWeapon(client) == FireResult.Fired;

        #region Weapon skills

        /// <summary>
        /// Metres past a melee action's range a target may be and still be struck; the client
        /// checks range before it swings, so this only covers a target that moved.
        /// </summary>
        private const float MeleeRangeSlack = 2.5f;

        /// <summary>The weapon skill an item is used under: its template's skill requirement, 0 for none.</summary>
        public static int WeaponSkillOf(Item weapon)
        {
            return weapon?.ItemTemplate?.EquipableInfo?.SkillId ?? 0;
        }

        /// <summary>The player's pump level in a skill, 0 when they do not have it.</summary>
        public static int SkillPump(Manifestation player, int skillId)
        {
            if (player == null || skillId <= 0)
                return 0;

            return player.Skills.TryGetValue((SkillId)skillId, out var skill) ? skill.SkillLevel : 0;
        }

        /// <summary>
        /// How long this player takes to reload this weapon: its reload time under the reload
        /// bonus of its skill and tool type (Leech Guns, Launchers' rockets, Firearms' pistols),
        /// timed as weaponreload.py times it from the effects SyncWeaponSkills gives the client.
        /// </summary>
        public static long ReloadTimeFor(Manifestation player, Item weapon)
        {
            var reloadMs = weapon.ItemTemplate.WeaponInfo.ReloadTime;
            var skillId = WeaponSkillOf(weapon);
            var haste = WeaponSkills.ReloadHaste(skillId, weapon.ItemTemplate.WeaponInfo.ToolType, SkillPump(player, skillId));

            return WeaponSkills.ReloadMs(reloadMs, haste);
        }

        /// <summary>
        /// The alternate attack: the melee swing every weapon has besides its own fire, sent as
        /// RequestWeaponAttack with isAltAction set and the weapon's alt action pair (WeaponInfo
        /// AltActionId/AltActionArgId, which the server gave the client in WeaponInfo). It uses
        /// no ammunition and puts no heat on the barrel - baseweaponattack.py does neither for an
        /// alt action - strikes for the weapon's alt damage and type, and takes Hand to Hand's
        /// bonus ("Allows unarmed attacks and alternate attacks (melee) with all weapons"). A
        /// staff's or a blade's own attack is not this: that is the weapon's primary, and goes
        /// through the fire path under the Staff or Blades skill.
        ///
        /// Timed on its own clock from the alt action's recovery and reuse, as the client times
        /// it. The target has to be within the alt action's range.
        /// </summary>
        public void TryMeleeAttack(Client client, RequestWeaponAttackPacket packet)
        {
            var player = client.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null || client.State != ClientState.Ingame || player.State == CharacterState.Dead)
                return;

            var weapon = InventoryManager.Instance.CurrentWeapon(client);
            var weaponInfo = weapon?.ItemTemplate?.WeaponInfo;

            if (weaponInfo == null)
                return;

            if (!player.WeaponReady)
            {
                RequestWeaponDraw(client);
                return;
            }

            // Only the swing this weapon has. A client naming any other action as its alt attack
            // is asking for something the weapon cannot do.
            if ((uint)packet.ActionId != weaponInfo.AltActionId || (uint)packet.ActionArgId != weaponInfo.AltActionArgId)
            {
                Logger.WriteLog(LogType.Security, $"{player.FamilyName} sent an alt attack {packet.ActionId}/{packet.ActionArgId} for a weapon whose alt attack is {weaponInfo.AltActionId}/{weaponInfo.AltActionArgId}");
                return;
            }

            // A reload still pending has not been interrupted; the client interrupts before it swings.
            if (IsReloading(player))
                return;

            AbilityManager.Instance.TryGetLevel(packet.ActionId, (uint)packet.ActionArgId, out var level);

            var now = Environment.TickCount64;

            if (player.NextMeleeAt > now + ShotTolerance)
                return;

            var interval = level != null ? level.RecoveryMs + level.ReuseMs : 0;
            player.NextMeleeAt = Math.Max(player.NextMeleeAt, now - ShotTolerance) + Math.Max(MinRefire, interval);

            var targetId = packet.TargetId > 0 ? (ulong)packet.TargetId : player.Target;

            if (targetId != 0 && level != null && level.MaxRange > 0)
            {
                Actor target = EntityManager.Instance.GetEntityType(targetId) switch
                {
                    EntityType.Creature => EntityManager.Instance.GetCreature(targetId),
                    EntityType.Character => EntityManager.Instance.GetPlayer(targetId),
                    _ => null
                };

                if (target != null && Vector3.Distance(player.Position, target.Position) > level.MaxRange + MeleeRangeSlack)
                    return;
            }

            // The alt damage is a single figure - the tooltip shows one number for it - and so is the swing.
            var damage = (int)(weaponInfo.WeaponAltInfo?.AltMaxDamage ?? 0);
            var pump = SkillPump(player, WeaponSkills.HandToHand);

            damage = GameEffectManager.ApplyDamageDealt(player, damage, WeaponSkills.DamagePercent(WeaponSkills.HandToHand, pump));

            var action = new ActionData(player, packet.ActionId, (uint)packet.ActionArgId, targetId, 0);

            MissileManager.Instance.MissileLaunch(mapChannel, action, damage);
        }

        /// <summary>
        /// Gives the player's client the weapon skill bonuses it predicts for itself, as the
        /// hidden effects the original server attached: SKILL_LIMITED_COOL_RATE_MODIFIER_EFFECT
        /// for heat dissipation (OnAttach(skillIds, modifier), read by Actor.GetCoolRateModifier
        /// and multiplied into its heat meter's cooling) and
        /// SKILL_LIMITED_BY_TYPE_RELOAD_MODIFIER_EFFECT for reload speed (OnAttach(modifier,
        /// skillIds, typeIds), summed by weaponreload.py into the reload bar). One effect per
        /// bonus, seen by this client alone, unannounced, with no end. Without them the client's
        /// meters ran at the base rates while the server's ran faster - a reload bar still
        /// filling after the clip was full, a heat meter showing a jam the server had cooled.
        ///
        /// The damage and armour-bypass bonuses need nothing on the client: the server says what
        /// every hit did. Run on every map arrival - effects end with the map - and whenever the
        /// player's skills change.
        /// </summary>
        public void SyncWeaponSkills(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return;

            foreach (var old in player.ActiveEffects.Values.Where(e => e.IsSkillPassive).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, player, old);

            foreach (var skillId in new[] { WeaponSkills.MachineGuns, WeaponSkills.PropellantGuns })
            {
                var pump = SkillPump(player, skillId);
                var modifier = WeaponSkills.CoolRateModifier(skillId, pump);

                if (modifier > 1.0)
                    GameEffectManager.Instance.Attach(mapChannel, player, SkillPassive(mapChannel, player, SkillLimitedCoolRateTypeId, pump),
                        new List<int> { skillId }, modifier);
            }

            foreach (var (haste, skillId, toolType) in WeaponSkills.ReloadBonuses(id => SkillPump(player, id)))
                GameEffectManager.Instance.Attach(mapChannel, player, SkillPassive(mapChannel, player, SkillLimitedReloadTypeId, SkillPump(player, skillId)),
                    haste, new List<int> { skillId }, toolType.HasValue ? new List<int> { (int)toolType.Value } : null);
        }

        /// <summary>gameeffectdata.SKILL_LIMITED_COOL_RATE_MODIFIER_EFFECT.</summary>
        public const int SkillLimitedCoolRateTypeId = 249;

        /// <summary>gameeffectdata.SKILL_LIMITED_BY_TYPE_RELOAD_MODIFIER_EFFECT.</summary>
        public const int SkillLimitedReloadTypeId = 252;

        private static GameEffect SkillPassive(MapChannel mapChannel, Manifestation player, int typeId, int pump)
        {
            return new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = (uint)Math.Max(1, pump),
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                ExpiresTick = long.MaxValue,
                AnnounceOnAttach = false,
                AllowDetach = false,
                IsSkillPassive = true
            };
        }

        #endregion

        /// <summary>
        /// Fires the weapon in hand if it can be fired now. Every shot a player makes comes through
        /// here, by three routes: the auto-fire list, the first shot of StartAutoFire, and
        /// RequestWeaponAttack.
        /// </summary>
        private FireResult TryFireWeapon(Client client)
        {
            // Reached from the auto-fire list on the main loop as well as from the handler; a
            // client that has left the world since must not be fired for.
            if (client.Player == null || client.State != ClientState.Ingame)
                return FireResult.NotFired;

            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            // Nothing in hand fires nothing. Tested before the draw below, because an empty
            // hand and a stowed weapon look the same from WeaponReady: arming an empty drawer
            // slot clears both the weapon and WeaponReady, so a player still holding the
            // trigger reached the draw with no weapon to describe it, and the auto-fire list
            // is walked at the top of the map channel worker - the dereference took the whole
            // tick with it, on every map, for as long as the client kept the fire alive.
            if (weapon == null)
                return FireResult.NotFired;

            // A jammed weapon does nothing until it is reloaded. Checked before WeaponReady so
            // that a jam does not get mistaken for a weapon that is merely stowed and silently
            // drawn instead.
            if (weapon.IsJammed)
            {
                // Once per trigger pull would be once per tick while auto-fire is held, so the
                // message is not repeated - the client already showed it when the jam arrived,
                // and its ammo readout still says "Jammed".
                return FireResult.NotFired;
            }

            // ToDo: isOverheated, and some other checks
            if (!client.Player.WeaponReady)
            {
                RequestWeaponDraw(client);
                return FireResult.NotFired;
            }

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            if (weaponClassInfo == null)
                return FireResult.NotFired;

            // A weapon being reloaded does not fire. The client never asks it to: primary fire
            // waits for a reload to finish, and anything else the player does interrupts the
            // reload first - RequestActionInterrupt, then the attack, handled in that order - so
            // by the time a legitimate shot is looked at here the reload is already marked. A
            // shot with the reload still live fired out of the clip while the reload went on to
            // top the clip up anyway, so a reload never kept anyone from firing.
            if (IsReloading(client.Player))
                return FireResult.NotFired;

            // A weapon with no ammunition class - every blade and staff - is never loaded and
            // never runs dry. The client knows: baseweaponattack.py only checks the clip when
            // the weapon has an ammo class, and blades and staves attack with useAmmoInAction 0.
            // The server asked for a reload of a clip that could never be filled, and so a blade
            // or a staff never struck at all.
            var usesAmmo = weaponClassInfo.AmmoClassId != 0;

            // do we need to reload?
            if (usesAmmo && weapon.CurrentAmmo < weapon.ItemTemplate.WeaponInfo.AmmoPerShot)
            {
                RequestWeaponReload(client, true);
                return FireResult.NotFired;
            }

            // The shot clock. Nothing used to time shots at all: RequestWeaponAttack fired as
            // often as it arrived, a clip per tick if the client sent a clip's worth, and every
            // StartAutoFire fired at once whatever the last shot had been, so pressing fire over
            // and over outran holding it down. Last of the checks, so that TooSoon always means a
            // shot that would otherwise have gone.
            //
            // The refire is the weapon's own, and the same one the auto-fire timer waits between
            // shots, so a player cannot make a weapon fire faster than the server fires it itself.
            var now = Environment.TickCount64;

            if (ShotWait(client.Player, now) > 0)
                return FireResult.TooSoon;

            if (usesAmmo)
            {
                var ammoAfter = weapon.CurrentAmmo - weapon.ItemTemplate.WeaponInfo.AmmoPerShot;
                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                    unitOfWork.ExecuteTransaction(() =>
                    {
                        var saved = unitOfWork.Items.GetItem(weapon.Id);
                        if (saved == null || saved.AmmoCount != weapon.CurrentAmmo)
                            throw new GameplayRejectionException("Weapon clip changed before the shot committed.");

                        unitOfWork.Items.UpdateAmmo(new Item
                        {
                            Id = weapon.Id,
                            CurrentAmmo = ammoAfter
                        });
                    });
                }
                catch (Exception error) when (
                    error is GameplayRejectionException ||
                    error is DbUpdateException ||
                    error is DbException)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Could not persist shot for item {weapon.Id}: {error.Message}");
                    return FireResult.NotFired;
                }

                weapon.CurrentAmmo = ammoAfter;
                client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(ammoAfter));
            }

            client.Player.NextShotAt = Math.Max(client.Player.NextShotAt, now - ShotTolerance) +
                                       Math.Max(MinRefire, weapon.ItemTemplate.WeaponInfo.Refire);

            // The barrel gets hotter. Done after the shot has been paid for in ammo, so a shot
            // that did not happen does not heat anything, and before the missile, so a shot that
            // reaches capacity is still fired - the jam stops the next one, not this one.
            AddWeaponHeat(client, weapon);

            // let's calculate damage
            var damageRange = weaponClassInfo.MaxDamage - weaponClassInfo.MinDamage;
            var damage = weaponClassInfo.MinDamage + new Random().Next(0, damageRange + 1);

            // Then the weapon skill's bonus (+10% a pump from pump 2) and what the effects on
            // the shooter do to it - Rage's bonus, Sacrifice's trade - added together.
            var skillId = WeaponSkillOf(weapon);
            var pump = SkillPump(client.Player, skillId);

            damage = GameEffectManager.ApplyDamageDealt(client.Player, damage, WeaponSkills.DamagePercent(skillId, pump));
            var action = new ActionData(client.Player, weaponClassInfo.WeaponAttackActionId, weaponClassInfo.WeaponAttackArgId, client.Player.Target, 0);
            // launch correct missile type depending on weapon type
            MissileManager.Instance.MissileLaunch(client.Player.MapChannel, action, damage, WeaponSkills.ArmorBypassPercent(skillId, pump));
            
            return FireResult.Fired;
        }

        /// <summary>
        /// How long before the player's next shot may be fired, in ms; 0 when it may be fired now.
        /// A shot is allowed <see cref="ShotTolerance"/> ahead of <see cref="Manifestation.NextShotAt"/>.
        /// </summary>
        private static long ShotWait(Manifestation player, long now)
        {
            return Math.Max(0, player.NextShotAt - ShotTolerance - now);
        }

        /// <summary>
        /// Whether the player has a reload waiting out its reload time that has not been
        /// interrupted. An interrupted one stays in the queue until the next tick sees to it, and
        /// is already over as far as the player is concerned.
        /// </summary>
        private static bool IsReloading(Manifestation player)
        {
            var queue = player.MapChannel?.PerformRecovery;

            if (queue == null)
                return false;

            foreach (var action in queue)
                if (action.Actor == player && action.ActionId == ActionId.WeaponReload && !action.IsInrerrupted)
                    return true;

            return false;
        }

        /// <summary>
        /// This weapon's heat, brought up to date.
        ///
        /// Cooling is applied on read rather than on a timer, which is exactly what the client
        /// does - <c>_UpdateWeaponHeat</c> cools by the elapsed time whenever the value is
        /// touched. Nothing needs to tick, and a weapon nobody is firing costs nothing.
        /// </summary>
        /// <param name="coolModifier">
        /// The owner's cool-rate multiplier for this weapon - the Machine Guns and Propellant
        /// Guns heat dissipation bonus, which the client applies the same way
        /// (coolRate x deltaTime x GetCoolRateModifier()).
        /// </param>
        public double CurrentHeat(Item weapon, double coolModifier = 1.0)
        {
            if (weapon?.ItemTemplate?.WeaponInfo == null)
                return 0;

            var now = Environment.TickCount64;

            if (weapon.HeatUpdatedAt == 0)
            {
                weapon.HeatUpdatedAt = now;
                return weapon.Heat;
            }

            var cooled = weapon.Heat - WeaponHeat.Cooling(weapon.ItemTemplate.WeaponInfo.CoolRate * coolModifier, now - weapon.HeatUpdatedAt);

            weapon.Heat = cooled < 0 ? 0 : cooled;
            weapon.HeatUpdatedAt = now;

            return weapon.Heat;
        }

        /// <summary>
        /// Adds one shot's worth of heat, and jams the weapon if that reaches capacity.
        ///
        /// A worn weapon heats faster: the client scales the shot by
        /// <c>(100 + (100 - condition)) / 100</c>, so a weapon at half condition heats at one and
        /// a half times the rate and one at zero at double. That is the client's own arithmetic,
        /// and this matches it so the heat meter the player is watching agrees with the jam they
        /// get.
        /// </summary>
        public void AddWeaponHeat(Client client, Item weapon)
        {
            if (weapon?.ItemTemplate?.WeaponInfo == null)
                return;

            var skillId = WeaponSkillOf(weapon);
            var coolModifier = WeaponSkills.CoolRateModifier(skillId, SkillPump(client.Player, skillId));
            var heat = CurrentHeat(weapon, coolModifier) + WeaponHeat.PerShot(weapon.ItemTemplate.WeaponInfo.HeatPerShot, ConditionPercent(weapon));

            weapon.Heat = heat;

            if (heat >= WeaponHeat.Capacity)
                JamWeapon(client, weapon);
        }

        /// <summary>
        /// Jams a weapon and tells its owner.
        ///
        /// Public because overheating is not the only way in: the client's help says "some
        /// creatures have been known to jam weapons", and a creature action that did so would
        /// call this. Nothing does yet - no row in creature_action identifies itself as a jamming
        /// attack, and picking one would be a guess - so this is the hook and not the feature.
        /// </summary>
        public void JamWeapon(Client client, Item weapon)
        {
            if (client?.Player == null || weapon == null || weapon.IsJammed)
                return;

            weapon.IsJammed = true;

            // Pinned at capacity rather than left to drift above it, so that cooling from a jam
            // always starts from the same place however far past the line the shot went.
            weapon.Heat = WeaponHeat.Capacity;
            weapon.HeatUpdatedAt = Environment.TickCount64;

            client.CallMethod(weapon.EntityId, new WeaponJammedPacket(true));
        }

        /// <summary>
        /// Frees a jammed weapon. Reloading is the only way a player has to do this, which is why
        /// the reload path lets a jammed weapon through checks that would otherwise refuse it.
        /// </summary>
        public void ClearJam(Client client, Item weapon)
        {
            if (client?.Player == null || weapon == null || !weapon.IsJammed)
                return;

            weapon.IsJammed = false;

            // Cleared, not merely below the line: leaving the barrel full would jam again on the
            // first shot after the reload.
            weapon.Heat = 0;
            weapon.HeatUpdatedAt = Environment.TickCount64;

            client.CallMethod(weapon.EntityId, new WeaponJammedPacket(false));
        }

        /// <summary>An item's hit points as a percentage of its class maximum, the way the client reads condition.</summary>
        private static double ConditionPercent(Item item)
        {
            var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);
            var max = classInfo?.ItemClassInfo?.MaxHitPoints ?? 0;

            if (max <= 0)
                return 100;

            return 100.0 * item.CurrentHitPoints / max;
        }

        // ------------------------------------------------------------------ combat state

        /// <summary>
        /// Puts the player in combat, or extends the time they stay there. Called from both ends
        /// of a damage event - dealing it and taking it - because either is being in a fight.
        /// </summary>
        public void EnterCombat(Client client)
        {
            if (client?.Player == null || client.State != ClientState.Ingame)
                return;

            client.Player.CombatExpiresAt = Environment.TickCount64 + CombatRegen.CombatTimeoutMs;

            if (client.Player.InCombat)
                return;

            client.Player.InCombat = true;

            ApplyRegenPeriod(client.Player);

            client.CallMethod(client.Player.EntityId, new PlayerEnteredCombatPacket());

            // The rate change is not in that packet - it carries nothing - so the attributes go
            // too. AttributeInfo rather than UpdateAttributes because only AttributeInfo carries
            // refreshPeriod, which is the field the modifier moves.
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
        }

        /// <summary>Takes the player out of combat and restores their regeneration.</summary>
        public void ExitCombat(Client client)
        {
            if (client?.Player == null || !client.Player.InCombat)
                return;

            client.Player.InCombat = false;
            client.Player.CombatExpiresAt = 0;

            ApplyRegenPeriod(client.Player);

            if (client.State != ClientState.Ingame)
                return;

            client.CallMethod(client.Player.EntityId, new PlayerExitedCombatPacket());
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
        }

        /// <summary>
        /// Sets the health and armour refresh periods for the player's current combat state.
        ///
        /// The period rather than the amount, because both are integers on the wire and a base
        /// amount of 2 scaled by 0.2 truncates to nothing. See <see cref="CombatRegen"/>.
        ///
        /// This is the only place either period is set, including the out-of-combat value, so
        /// that there is one answer to what the period is rather than two that have to agree. It
        /// is called at the end of UpdateStatsValues for that reason and for a second one:
        /// UpdateStatsValues recomputes the rates from scratch, so without it, changing a piece
        /// of armour mid-fight would quietly restore full regeneration.
        ///
        /// A period of zero would stop regeneration entirely - the client's
        /// _EvaluatePredictedRefresh returns early on one - so neither branch may yield it.
        /// </summary>
        public void ApplyRegenPeriod(Manifestation player)
        {
            if (player == null)
                return;

            var period = player.InCombat
                ? CombatRegen.InCombatRegenPeriodSeconds
                : CombatRegen.RegenPeriodSeconds;

            player.Attributes[Attributes.Health].RefreshPeriod = period;
            player.Attributes[Attributes.Armor].RefreshPeriod = period;
        }

        /// <summary>Drops players out of combat once their timer has run out.</summary>
        public void CombatWorker(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;

            foreach (var client in mapChannel.ClientList)
            {
                if (client?.Player == null || !client.Player.InCombat)
                    continue;

                if (now >= client.Player.CombatExpiresAt)
                    ExitCombat(client);
            }
        }

        /// <summary>
        /// Consumes a clone-credit item and gives its owner the credit.
        ///
        /// The client sends this from the item's right-click menu and expects nothing back except
        /// the new total: <c>Recv_CloneCredits</c> raises the "clone credit added" message and the
        /// tutorial itself, but only when the number it is given is higher than the one it had, so
        /// the packet has to go out after the increment and not before.
        ///
        /// Everything is checked here rather than trusted. The client only offers the right-click
        /// on an item with the CloneCredit augmentation, but the entity id arrived over the wire.
        /// </summary>
        public void RequestUseCloneCredit(Client client, RequestUseCloneCreditPacket packet)
        {
            if (client?.Player == null)
                return;

            // Theirs, and in the pack rather than a lockbox or someone else's window.
            if (!client.Player.Inventory.PersonalInventory.Contains(packet.EntityId))
                return;

            var item = EntityManager.Instance.GetItem(packet.EntityId);

            if (item?.ItemTemplate == null)
                return;

            var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);

            // The augmentation is what makes an item a clone credit - not its template id, so a
            // second one added later works without touching this.
            if (classInfo == null || !classInfo.Augmentations.Contains(AugmentationType.CloneCredit))
            {
                Logger.WriteLog(LogType.Error,
                    $"RequestUseCloneCredit: {client.Player.Name} used item {packet.EntityId} (class {item.ItemTemplate.Class}), which is not a clone credit");
                return;
            }

            if (item.StackSize == 0)
                return;

            lock (client.SyncRoot)
            {
                var player = client.Player;
                var previousCredits = player.CloneCredits;
                var consumption = new InventoryManager.InventoryConsumption();
                try
                {
                    using var unit = _gameUnitOfWorkFactory.CreateChar();
                    unit.ExecuteTransaction(() =>
                    {
                        if (MissionItemProtection.IsProtected(item, unit))
                            throw new GameplayRejectionException("Assignment-owned clone credits cannot be redeemed.");
                        if (previousCredits == uint.MaxValue || unit.Characters.Get(player.Id).CloneCredits != previousCredits)
                            throw new GameplayRejectionException("Clone credit balance changed or is already full.");
                        consumption.PlanAndSave(client, new Dictionary<ulong, uint> { [item.EntityId] = 1 }, unit);
                        unit.Characters.UpdateCharacterCloneCredits(player.Id, previousCredits + 1);
                        TransactionValidation.Add(unit, () =>
                        {
                            if (player.CloneCredits != previousCredits ||
                                unit.Characters.Get(player.Id).CloneCredits != previousCredits + 1)
                                throw new GameplayRejectionException("Clone credit balance changed during persistence.");
                        });
                    });
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Could not redeem clone credit item {item.Id}: {error.Message}");
                    return;
                }

                consumption.Publish(client);
                player.CloneCredits = previousCredits + 1;
                client.CallMethod(player.EntityId, new CloneCreditsPacket(player.CloneCredits));
                foreach (var progress in consumption.ProgressEvents)
                    (_missionManager ?? MissionApplication.Instance).RecordProgress(client, progress);
            }
        }

        public void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            if (client?.Player == null || abilityDrawerSlot < 0 || abilityDrawerSlot >= 25)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (unitOfWork.Characters.Find(client.Player.Id) == null)
                return;

            try
            {
                unitOfWork.ExecuteTransaction(() =>
                    unitOfWork.Characters.UpdateCharacterAbilitySlot(client.Player.Id, (byte)abilityDrawerSlot));
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist ability drawer selection {abilityDrawerSlot} for character {client.Player.Id}: {error.Message}");
                return;
            }

            client.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            client.CallMethod(client.Player.EntityId, new AbilityDrawerSlotPacket(abilityDrawerSlot));
        }

        public void RequestArmWeapon(Client client, uint requestedWeaponDrawerSlot)
        {
            // The drawer has five slots; the index came straight from the client.
            if (client.Player == null || requestedWeaponDrawerSlot >= client.Player.Inventory.WeaponDrawer.Count)
                return;

            client.Player.ActiveWeapon = (byte)requestedWeaponDrawerSlot;

            client.CallMethod(client.Player.EntityId, new WeaponDrawerSlotPacket(requestedWeaponDrawerSlot, true));

            var weapon = EntityManager.Instance.GetItem(client.Player.Inventory.WeaponDrawer[client.Player.ActiveWeapon]);

            // A drawer slot holding something that is not a weapon is armed as an empty one.
            // RequestEquipWeapon refuses to put anything else there now, but a drawer loaded
            // before it did still has to be survivable: arming that slot used to ask for an
            // appearance the class has none of and disconnect the player, which made the slot a
            // trap they could not clear from in front of it.
            if (weapon != null && EntityClassManager.Instance.GetEquipableClassInfo(weapon)?.EquipmentSlotId != EquipmentData.Weapon)
            {
                Logger.WriteLog(LogType.Error,
                    $"{client.Player.Name} has {weapon.ItemTemplate.Class} in weapon drawer slot {requestedWeaponDrawerSlot}, which is not a weapon; armed as empty.");

                weapon = null;
            }

            // The weapon in hand follows the active slot, empty included: arming an empty slot
            // used to leave the previous weapon in EquippedInventory[13], so the player kept
            // firing a weapon they had put away.
            client.Player.Inventory.EquippedInventory[13] = weapon?.EntityId ?? 0;

            if (weapon == null)
            {
                if (client.Player.WeaponReady)
                    WeaponReady(client, false);

                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.ActiveWeapon, (byte)requestedWeaponDrawerSlot);
                return;
            }

            NotifyEquipmentUpdate(client);
            SetAppearanceItem(client, weapon);
            UpdateAppearance(client);
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.ActiveWeapon, (byte)requestedWeaponDrawerSlot);
            // update ammo info
            client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));
        }

        public void RequestSetAbilitySlot(Client client, RequestSetAbilitySlotPacket packet)
        {
            if (client?.Player == null || packet == null || packet.SlotId < 0 || packet.SlotId >= 25)
                return;

            var clearing = packet.AbilityId == 0 && packet.AbilityLevel == 0;

            if (!clearing)
            {
                if (packet.AbilityId <= 0 || packet.AbilityId > int.MaxValue ||
                    packet.AbilityLevel <= 0 || packet.AbilityLevel > MaxSkillLevel ||
                    !client.Player.Skills.Values.Any(skill =>
                        skill.AbilityId == packet.AbilityId && skill.SkillLevel >= packet.AbilityLevel))
                    return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (unitOfWork.Characters.Find(client.Player.Id) == null)
                return;

            try
            {
                unitOfWork.ExecuteTransaction(() => unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                    client.Player.Id, packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel));
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist ability drawer slot {packet.SlotId} for character {client.Player.Id}: {error.Message}");
                return;
            }

            if (clearing)
                client.Player.Abilities.Remove(packet.SlotId);
            else
                client.Player.Abilities[packet.SlotId] =
                    new AbilityDrawerData(packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel);

            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(client.Player.Abilities));
        }

        public void RequestSwapAbilitySlots(Client client, RequestSwapAbilitySlotsPacket packet)
        {
            if (client?.Player == null || packet == null ||
                packet.FromSlot < 0 || packet.FromSlot >= 25 ||
                packet.ToSlot < 0 || packet.ToSlot >= 25 ||
                packet.FromSlot == packet.ToSlot)
                return;

            var abilities = client.Player.Abilities;
            abilities.TryGetValue(packet.FromSlot, out var from);
            abilities.TryGetValue(packet.ToSlot, out var to);

            if (from == null && to == null)
                return;

            var next = abilities.ToDictionary(
                entry => entry.Key,
                entry => new AbilityDrawerData(entry.Key, entry.Value.AbilityId, entry.Value.AbilityLevel));

            if (to == null)
                next.Remove(packet.FromSlot);
            else
                next[packet.FromSlot] = new AbilityDrawerData(packet.FromSlot, to.AbilityId, to.AbilityLevel);

            if (from == null)
                next.Remove(packet.ToSlot);
            else
                next[packet.ToSlot] = new AbilityDrawerData(packet.ToSlot, from.AbilityId, from.AbilityLevel);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (unitOfWork.Characters.Find(client.Player.Id) == null)
                return;

            try
            {
                unitOfWork.ExecuteTransaction(() =>
                {
                    var fromValue = next.GetValueOrDefault(packet.FromSlot);
                    var toValue = next.GetValueOrDefault(packet.ToSlot);
                    unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.FromSlot,
                        fromValue?.AbilityId ?? 0, fromValue?.AbilityLevel ?? 0);
                    unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.ToSlot,
                        toValue?.AbilityId ?? 0, toValue?.AbilityLevel ?? 0);
                });
            }
            catch (Exception error) when (error is DbUpdateException || error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist ability drawer swap for character {client.Player.Id}: {error.Message}");
                return;
            }

            client.Player.Abilities = next;
            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(next));
        }

        public void StartAutoFire(Client client, double yaw)
        {
            // ToDo:
            // yaw is probobly used to mach player and target orientation,
            // some creatures recive more damage from back then from front

            switch (TryFireWeapon(client))
            {
                case FireResult.Fired:
                    ActorManager.Instance.RequestVisualCombatMode(client, true);
                    RegisterAutoFire(client);
                    break;

                // Pressed again before the last shot's refire was up. The press still starts the
                // fire, from when the clock allows: only a first shot that went used to start the
                // timer, so a first shot held back would leave the trigger down and nothing firing
                // until the player let go and pressed again.
                case FireResult.TooSoon:
                    if (RegisterAutoFire(client, ShotWait(client.Player, Environment.TickCount64)))
                        ActorManager.Instance.RequestVisualCombatMode(client, true);
                    break;
            }
        }

        public void StopAutoFire(Client client)
        {
            ActorManager.Instance.RequestVisualCombatMode(client, false);

            RemoveAutoFire(client);
        }

        #endregion

        #region Helper Functions

        public void AllocateAttributePoints(Client client, AllocateAttributePointsPacket packet)
        {
            if (client?.Player == null || packet == null ||
                client.State != ClientState.Ingame ||
                !CellManager.Instance.IsInWorld(client) ||
                client.Player.Level < 1 || client.Player.Level > MaxPlayerLevel)
                return;

            // The three counts are the client's word, and used to be added as they came: no
            // check against the points the character has actually earned, and no check for a
            // negative that would take spent points back. Health and armour are derived from
            // the spent points and written to the row, so one packet with Body = 100000 was a
            // permanent giant health pool.
            var available = GetAvailableAttributePoints(client.Player);
            var requested = (long) packet.Body + packet.Mind + packet.Spirit;

            if (packet.Body < 0 || packet.Mind < 0 || packet.Spirit < 0 || requested <= 0 || requested > available)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to allocate {packet.Body}/{packet.Mind}/{packet.Spirit} attribute points with {available} available.");

                // Whatever the client's window thinks, this is where the character stands.
                client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
                return;
            }

            int bodyAfter;
            int mindAfter;
            int spiritAfter;
            try
            {
                bodyAfter = checked(client.Player.SpentBody + packet.Body);
                mindAfter = checked(client.Player.SpentMind + packet.Mind);
                spiritAfter = checked(client.Player.SpentSpirit + packet.Spirit);
            }
            catch (OverflowException)
            {
                return;
            }

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var character = unitOfWork.Characters.Find(client.Player.Id);
                    if (character == null ||
                        character.Body != client.Player.SpentBody ||
                        character.Mind != client.Player.SpentMind ||
                        character.Spirit != client.Player.SpentSpirit ||
                        character.Level != client.Player.Level)
                        throw new GameplayRejectionException(
                            "Durable attributes changed before allocation.");

                    unitOfWork.Characters.UpdateCharacterAttributes(
                        client.Player.Id, bodyAfter, mindAfter, spiritAfter);
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist attribute allocation for character {client.Player.Id}: {error.Message}");
                return;
            }

            client.Player.SpentBody = bodyAfter;
            client.Player.SpentMind = mindAfter;
            client.Player.SpentSpirit = spiritAfter;
            UpdateStatsValues(client, false);
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
            SendAvailableAllocationPoints(client);
        }

        public void AssignPlayer(Client client)
        {
            var player = client.Player;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // get charaterOptions. Cleared first: this runs again on every map change, and the
            // list used to gain another copy of every option each time.
            player.CharacterOptions.Clear();

            var optionsList = unitOfWork.CharacterOptions.Get(player.Id);

            foreach (var characterOption in optionsList)
                player.CharacterOptions.Add(new CharacterOptions((CharacterOption)characterOption.OptionId, characterOption.Value));

            client.CallMethod(SysEntity.ClientMethodId, new CharacterOptionsPacket(player.CharacterOptions));

            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(player.EntityId));

            // Inventory deltas precede LoginOk. Refresh the tray after its controlled actor
            // exists so the initial image does not depend on opening the equipment selector.
            client.CallMethod(SysEntity.ClientInventoryManagerId,
                new Packets.Inventory.Server.InventoryCreatePacket(
                    InventoryType.WeaponDrawerInventory, player.Inventory.WeaponDrawer.ToList(),
                    player.Inventory.WeaponDrawer.Count));
            client.CallMethod(player.EntityId, new WeaponDrawerSlotPacket(player.ActiveWeapon, false));

            client.CallMethod(SysEntity.ClientGameMapId, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(client.Player.MapChannel.MapInfo.MapContextId));

            SocialManager.Instance.SetSocialContactList(client);

            client.CallMethod(player.EntityId, new ActorInfoPacket(player));
            MissionApplication.Instance.PublishInitialState(client);
            _characterManager.OfferStartingExperienceMission(client);

            // The regions the player is standing in; re-sent by RegionManager.Worker as they move.
            RegionManager.Instance.PlayerEnteredMap(client);
            MapMarkerManager.Instance.PlayerEnteredMap(client);

            client.CallMethod(player.EntityId, new AdvancementStatsPacket(
                player.Level,
                player.Experience,
                GetAvailableAttributePoints(player),
                0,       // trainPoints (are not used by the client??)
                GetSkillPointsAvailable(player)
            ));

            client.CallMethod(player.EntityId, new SkillsPacket(player.Skills));

            client.CallMethod(player.EntityId, new AbilitiesPacket(player.Skills));

            PublishAbilityLoadout(client);

            client.CallMethod(player.EntityId, new TitlesPacket(player.Titles));

            client.CallMethod(player.EntityId, new UpdateAttributesPacket(player.Attributes, 0));

            client.CallMethod(player.EntityId, new UpdateHealthPacket(player.Attributes[Attributes.Health], 0));

            client.CallMethod(player.EntityId, new LogosStoneTabulaPacket(player.Logos));

            client.CallMethod(player.EntityId, new AllCreditsPacket(player.Credits));

            client.CallMethod(player.EntityId, new LockboxFundsPacket(player.LockboxCredits));

            // After the skills: the weapon skill bonuses the client predicts from its own effects.
            SyncWeaponSkills(client);
        }

        internal void PublishAbilityLoadout(Client client)
        {
            if (client?.Player == null ||
                client.Player.CurrentAbilityDrawer < 0 ||
                client.Player.CurrentAbilityDrawer >= 25 ||
                client.Player.Abilities.Any(entry =>
                    entry.Key < 0 || entry.Key >= 25 ||
                    entry.Value == null ||
                    entry.Value.AbilitySlotId != entry.Key))
                return;

            client.CallMethod(client.Player.EntityId,
                new AbilityDrawerPacket(client.Player.Abilities));
            client.CallMethod(client.Player.EntityId,
                new AbilityDrawerSlotPacket(client.Player.CurrentAbilityDrawer));
        }

        public void AutoFireTimerDoWork(long delta)
        {
            // go backwards through list
            for (var i = AutoFire.Count - 1; i >= 0; i--)
            {
                var timer = AutoFire[i];
                // we dont want to server keep fireing if client crash 
                timer.MaxAliveTime -= delta;

                // Nothing in hand ends the fire here rather than in each of the several places
                // that can empty it - arming an empty drawer slot, moving the weapon out of the
                // armed slot, dropping or selling it. The trigger is still held, but there is no
                // longer a weapon to pull it on, and a timer kept for one cannot be fired
                // without asking what it is.
                if (timer.MaxAliveTime <= 0
                    || timer.Client.Player == null
                    || timer.Client.State != ClientState.Ingame
                    || InventoryManager.Instance.CurrentWeapon(timer.Client) == null)
                {
                    AutoFire.RemoveAt(i);
                    continue;
                }

                timer.Delay -= delta;

                if (timer.Delay <= 0)
                {
                    // This list is walked at the top of the map channel worker, before any map
                    // is touched. A shot that throws used to abandon the whole tick - every
                    // map's queued actions, missiles, creature behaviour and visibility - and
                    // the delay below never being reached meant the same client threw again on
                    // the very next tick, so one player could hold the world still. The timer
                    // that could not be fired is dropped instead, and costs only itself.
                    try
                    {
                        PlayerTryFireWeapon(timer.Client);
                    }
                    catch (Exception e)
                    {
                        AutoFire.RemoveAt(i);

                        Logger.WriteLog(LogType.Error, $"Auto-fire for entity {timer.Client.Player?.EntityId} threw and was stopped: {e}");

                        continue;
                    }

                    timer.Delay = timer.RefireTime;
                }
            }
        }

        public void CellDiscardClientToPlayers(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(client.Player.EntityId));
            }
        }

        public void CellDiscardPlayersToClient(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(tempClient.Player.EntityId));
            }

        }

        public void CellIntroduceClientToPlayers(Client client, List<Client> clientList)
        {
            var player = client.Player;

            foreach (var tempClient in clientList)
            {
                // don't send data about yourself
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, CreatePlayerEntityData(client, tempClient)));

            }
        }

        public void CellIntroduceClientToSefl(Client client)
        {
            lock (client.SyncRoot)
            {
                var player = client.Player;
                var entityData = CreatePlayerEntityData(client, client);
                client.CallMethod(SysEntity.ClientMethodId,
                    new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, entityData));
                client.FlagProjection.Acknowledge(client, player, entityData.OfType<PlayerFlagsPacket>().Single());
            }
        }

        public void CellIntroducePlayersToClient(Client client, List<Client> clientList)
        {
            foreach (var tempClient in clientList)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(tempClient.Player.EntityId, tempClient.Player.EntityClass, CreatePlayerEntityData(tempClient, client)));
            }
        }
		
        public List<PythonPacket> CreatePlayerEntityData(Client client, Client recipient)
        {
            var player = client.Player;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(player.EntityClass).TargetFlag),
                new WorldLocationDescriptorPacket(player.Position, player.Rotation),
                // Manifestation
                new CurrentCharacterIdPacket(player.EntityId),
                new CharacterClassPacket(player.Class),
                new AttributeInfoPacket(player.Attributes),
                new PreloadDataPacket(client.Player.Inventory.EquippedInventory[13], player.Abilities),
                new AppearanceDataPacket(player.AppearanceData),
                new ResistanceDataPacket(player.ResistanceData),
                new ActorControllerInfoPacket(true),
                new LevelPacket(player.Level),
                new CharacterNamePacket(player.Name),
                new ActorNamePacket(player.FamilyName),
                new IsRunningPacket(player.IsRunning),
                new TargetCategoryPacket(Factions.AFS),
                new PlayerFlagsPacket(ReferenceEquals(client, recipient)
                    ? CharacterFlagProjection.ToNativeIds(player.PlayerFlags)
                    : Array.Empty<uint>()),
                new IsTrialAccountPacket(player.IsTrialAccount),
                new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory),
                // "Received because the manifestation was loaded on the server", and only ever
                // for the player's own. It is what fills the advancement tracker; it offers
                // nothing, so it goes out whether the list is empty or not.
                new TierAdvancementInfoPacket(AvailableClassIds(player))
            };

            return entityData;
        }

        internal sealed class ProgressionGrant
        {
            internal bool HasChanges { get; init; }
            internal uint ExperienceAward { get; init; }
            internal uint TotalExperience { get; init; }
            internal byte PreviousLevel { get; init; }
            internal byte FinalLevel { get; init; }
            internal uint PreviousCloneCredits { get; init; }
            internal uint FinalCloneCredits { get; init; }
        }

        internal ProgressionGrant PlanExperience(
            Client client,
            uint experience,
            CharacterEntry durableCharacter,
            ICharUnitOfWork unitOfWork)
        {
            var player = client.Player;
            if (experience == 0 || player.Level >= MaxPlayerLevel)
                return new ProgressionGrant();
            if (player.Level < 1 ||
                durableCharacter.Id != player.Id ||
                durableCharacter.Experience != player.Experience ||
                durableCharacter.Level != player.Level ||
                durableCharacter.CloneCredits != player.CloneCredits)
                throw new GameplayRejectionException(
                    "Runtime progression no longer matches durable character state.");

            uint totalExperience;
            var previousLevel = durableCharacter.Level;
            var finalLevel = previousLevel;
            var cloneCredits = durableCharacter.CloneCredits;
            try
            {
                totalExperience = checked(durableCharacter.Experience + experience);
                while (finalLevel < MaxPlayerLevel)
                {
                    var requiredExperience = GetLevelNeededExperience(finalLevel);
                    if (requiredExperience < 0 || totalExperience < requiredExperience)
                        break;
                    finalLevel++;
                }

                foreach (var level in CloneCreditLevels)
                    if (level > previousLevel && level <= finalLevel)
                        cloneCredits = checked(cloneCredits + 1);
            }
            catch (OverflowException error)
            {
                throw new GameplayRejectionException(
                    "Mission reward progression exceeds the supported range.", error);
            }

            unitOfWork.Characters.UpdateCharacterProgression(
                player.Id, totalExperience, finalLevel);
            if (cloneCredits != durableCharacter.CloneCredits)
                unitOfWork.Characters.UpdateCharacterCloneCredits(
                    player.Id, cloneCredits);

            return new ProgressionGrant
            {
                HasChanges = true,
                ExperienceAward = experience,
                TotalExperience = totalExperience,
                PreviousLevel = previousLevel,
                FinalLevel = finalLevel,
                PreviousCloneCredits = durableCharacter.CloneCredits,
                FinalCloneCredits = cloneCredits
            };
        }

        internal void PublishExperience(Client client, ProgressionGrant grant)
        {
            if (grant == null || !grant.HasChanges)
                return;

            var player = client.Player;
            player.Experience = grant.TotalExperience;
            client.CallMethod(
                player.EntityId,
                new ExperienceChangedPacket(
                    new XPInfo(
                        grant.TotalExperience,
                        grant.ExperienceAward,
                        grant.ExperienceAward)));

            var cloneCredits = grant.PreviousCloneCredits;
            for (var level = grant.PreviousLevel + 1;
                 level <= grant.FinalLevel;
                 level++)
            {
                player.Level = (byte)level;
                if (Array.IndexOf(CloneCreditLevels, player.Level) >= 0)
                {
                    cloneCredits++;
                    player.CloneCredits = cloneCredits;
                    client.CallMethod(
                        player.EntityId,
                        new CloneCreditsPacket(player.CloneCredits));
                }

                client.CellCallMethod(
                    client,
                    player.EntityId,
                    new LevelUpPacket(player.Level));
                var msgArg = new Dictionary<string, string>
                {
                    { "level", player.Level.ToString() },
                    { "attributePts", GetAvailableAttributePoints(player).ToString() },
                    { "skillPts", GetSkillPointsAvailable(player).ToString() }
                };
                client.CallMethod(
                    SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(
                        PlayerMessage.PmLevelIncreased,
                        msgArg,
                        MsgFilterId.LeveledUp));
                UpdateStatsValues(client, true);
                client.CallMethod(
                    player.EntityId,
                    new AttributeInfoPacket(player.Attributes));
                SendAvailableAllocationPoints(client);

                var opened = AvailableClassIds(player);
                if (opened.Count > 0 &&
                    CharacterClassTree.LevelFor((CharacterClass)opened[0]) == player.Level)
                {
                    client.CallMethod(
                        player.EntityId,
                        new AvailableCharacterClassesPacket(opened));
                }
            }

            player.Level = grant.FinalLevel;
            player.CloneCredits = grant.FinalCloneCredits;
            if (grant.FinalLevel != grant.PreviousLevel)
                PartyManager.Instance.MemberInfoChanged(client);
        }

        internal bool ValidateProgressionForClient(Client client)
        {
            var player = client?.Player;
            if (player != null && player.Level >= 1 && player.Level <= MaxPlayerLevel)
                return true;

            Logger.WriteLog(
                LogType.Error,
                $"Rejected progression for character {player?.Id ?? 0}: invalid level {player?.Level ?? 0}.");
            return false;
        }

        internal void GainExperience(Client client, uint experience)
        {
            var player = client?.Player;
            if (player == null || experience == 0 || client.State != ClientState.Ingame ||
                !CellManager.Instance.IsInWorld(client) ||
                player.Level < 1 || player.Level >= MaxPlayerLevel)
                return;

            uint experienceAfter;
            try
            {
                experienceAfter = checked(player.Experience + experience);
            }
            catch (OverflowException)
            {
                return;
            }

            var levelBefore = player.Level;
            var levelAfter = levelBefore;
            while (levelAfter < MaxPlayerLevel)
            {
                var threshold = GetLevelNeededExperience(levelAfter);
                if (threshold < 0 || experienceAfter < threshold)
                    break;
                levelAfter++;
            }

            var cloneCreditsAfter = player.CloneCredits;
            try
            {
                for (var level = levelBefore + 1; level <= levelAfter; level++)
                    if (Array.IndexOf(CloneCreditLevels, level) >= 0)
                        cloneCreditsAfter = checked(cloneCreditsAfter + 1);
            }
            catch (OverflowException)
            {
                return;
            }

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var character = unitOfWork.Characters.Find(player.Id);
                    if (character == null ||
                        character.Experience != player.Experience ||
                        character.Level != player.Level ||
                        character.CloneCredits != player.CloneCredits)
                        throw new GameplayRejectionException(
                            "Durable progression changed before experience could be awarded.");

                    unitOfWork.Characters.UpdateCharacterProgression(
                        player.Id, experienceAfter, levelAfter);
                    if (cloneCreditsAfter != player.CloneCredits)
                        unitOfWork.Characters.UpdateCharacterCloneCredits(
                            player.Id, cloneCreditsAfter);
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist experience for character {player.Id}: {error.Message}");
                return;
            }

            player.Experience = experienceAfter;
            client.CallMethod(player.EntityId,
                new ExperienceChangedPacket(new XPInfo(experienceAfter, experience, experience)));

            for (var level = levelBefore + 1; level <= levelAfter; level++)
            {
                player.Level = (byte)level;
                if (Array.IndexOf(CloneCreditLevels, player.Level) >= 0)
                {
                    player.CloneCredits++;
                    client.CallMethod(player.EntityId,
                        new CloneCreditsPacket(player.CloneCredits));
                }

                client.CellCallMethod(client, player.EntityId,
                    new LevelUpPacket(player.Level));

                var msgArg = new Dictionary<string, string>
                {
                    { "level", player.Level.ToString() },
                    { "attributePts", GetAvailableAttributePoints(player).ToString() },
                    { "skillPts", GetSkillPointsAvailable(player).ToString() }
                };
                client.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(
                        PlayerMessage.PmLevelIncreased,
                        msgArg,
                        MsgFilterId.LeveledUp));

                UpdateStatsValues(client, true);
                client.CallMethod(player.EntityId,
                    new AttributeInfoPacket(player.Attributes));
                SendAvailableAllocationPoints(client);
            }

            if (levelAfter != levelBefore)
                PartyManager.Instance.MemberInfoChanged(client);
        }

        /// <summary>
        /// Adrenaline (chi) earned for a kill, as a percent of the bar. Not a live-game figure:
        /// the client says only that adrenaline is "gained by defeating enemies" and that the
        /// Regen stat "improves ... Adrenaline gain", so the shape - a share of the bar per
        /// kill, scaled by Regen - is the client's and the number is a placeholder to tune.
        /// Five kills at 100% Regen fill an empty bar; sprint then runs for about a minute.
        /// </summary>
        public const int AdrenalinePerKillPercent = 20;

        /// <summary>
        /// Adds adrenaline to the player's bar, up to its maximum, and tells the client. whoId 0
        /// makes the client announce the change (the floating number over the bar), as the live
        /// server did from the killing blow.
        /// </summary>
        internal void GainAdrenaline(Client client, int amount)
        {
            var player = client.Player;

            if (player == null || player.State == CharacterState.Dead || amount <= 0)
                return;

            if (!player.Attributes.TryGetValue(Attributes.Chi, out var chi) || chi.Current >= chi.CurrentMax)
                return;

            chi.Current = Math.Min(chi.CurrentMax, chi.Current + amount);

            client.CallMethod(player.EntityId, new UpdateChiPacket(chi, 0));
        }

        /// <summary>The adrenaline one kill is worth to this player: AdrenalinePerKillPercent of the bar, scaled by Regen.</summary>
        internal int AdrenalineForKill(Client client)
        {
            var player = client.Player;

            if (player == null || !player.Attributes.TryGetValue(Attributes.Chi, out var chi))
                return 0;

            var regenPercent = player.Attributes.TryGetValue(Attributes.Regen, out var regen) ? regen.CurrentMax : 100;

            return (int)Math.Round(chi.CurrentMax * AdrenalinePerKillPercent / 100D * regenPercent / 100D);
        }

        /// <summary>
        /// The classes this character may advance into right now: the direct children of their
        /// own class, once the level that tier opens at is reached.
        /// </summary>
        public List<uint> AvailableClassIds(Manifestation player)
        {
            var available = new List<uint>();

            foreach (var characterClass in CharacterClassTree.AdvancementsFor((CharacterClass)player.Class, player.Level))
                available.Add((uint)characterClass);

            return available;
        }

        /// <summary>Whether there is an advancement waiting - what the trainer's Train button reads.</summary>
        public bool CanAdvance(Manifestation player)
        {
            return AvailableClassIds(player).Count > 0;
        }

        /// <summary>
        /// The Train button in the tier select window.
        ///
        /// One-way, as it was live: there is no route back down the tree and no way to swap to
        /// the sibling class. Cloning is the respec - a clone credit copies the character with
        /// its skills reset - and that is already wired.
        ///
        /// Refused rather than thrown, for the reason LevelSkills is: an exception out of a
        /// packet handler is a closed connection, and a client that offers a class the server
        /// will not grant should be told no rather than dropped.
        /// </summary>
        public void SelectNewCharacterClass(Client client, SelectNewCharacterClassPacket packet)
        {
            var player = client.Player;
            var current = (CharacterClass)player.Class;
            var chosen = (CharacterClass)packet.ClassId;

            string refusal = null;

            if (!CharacterClassTree.Exists(chosen))
                refusal = $"class {packet.ClassId}, which is not a character class";
            else if (!CharacterClassTree.CanAdvanceTo(current, chosen))
                refusal = $"{chosen}, which does not advance from {current}";
            else if (player.Level < CharacterClassTree.LevelFor(chosen))
                refusal = $"{chosen} at level {player.Level}, which opens at {CharacterClassTree.LevelFor(chosen)}";

            if (refusal != null)
            {
                Logger.WriteLog(LogType.Security,
                    $"{player.FamilyName} asked to advance into {refusal}. Ignored.");

                // Put their client back where the server is.
                client.CallMethod(player.EntityId, new CharacterClassPacket(player.Class));
                client.CallMethod(player.EntityId, new TierAdvancementInfoPacket(AvailableClassIds(player)));
                return;
            }

            player.Class = (uint)chosen;
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Class, player.Class);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} advanced from {current} to {chosen}.");

            client.CallMethod(player.EntityId, new CharacterClassPacket(player.Class));

            // The skills the new class grants become trainable through the gate in
            // ValidateSkillLevels, which reads Player.Class - so nothing else has to change for
            // them to unlock. Resending the skills and the points is what makes the window
            // redraw with them in it.
            client.CallMethod(player.EntityId, new SkillsPacket(player.Skills));
            SendAvailableAllocationPoints(client);

            // What is ahead from here: the next tier's pair, or nothing at tier 4.
            client.CallMethod(player.EntityId, new TierAdvancementInfoPacket(AvailableClassIds(player)));

            // Class is the third field of the party tuple, as DebugChgPlayerClass notes.
            PartyManager.Instance.MemberInfoChanged(client);
        }

        public void DebugChgPlayerClass(Client client, uint newClassId)
        {
            client.Player.Class = newClassId;
            client.CallMethod(client.Player.EntityId, new CharacterClassPacket(client.Player.Class));
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Class, client.Player.Class);

            // Class is the third field of the same party tuple, so it goes stale the same way.
            PartyManager.Instance.MemberInfoChanged(client);
        }

        /// <summary>
        /// Puts <paramref name="credits"/> into the player's purse and tells them they received it.
        /// The amount is positive and is added.
        /// </summary>
        /// <remarks>
        /// These two were the same unsigned update under different names - neither looked at the
        /// sign, so the direction was decided entirely at the call site and every charge had to
        /// remember to negate. <see cref="InventoryManager.PurchaseLockboxTab"/> believed the name
        /// instead, so the four lockbox tabs paid the player 100 K to 100 M credits each.
        /// The names mean what they say now, and the sign is not the caller's to choose.
        /// </remarks>
        public bool GainCredits(Client client, int credits)
        {
            if (credits <= 0)
            {
                Logger.WriteLog(LogType.Error, $"GainCredits({credits}) for {client.Player?.FamilyName}: credits are gained in positive amounts. Nothing moved.");
                return false;
            }

            if (!_characterManager.UpdateCharacter(client, CharacterUpdate.Credits, credits))
                return false;

            // send player message
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmGotMoneyLootFromUnknown, new Dictionary<string, string> { { "amount", credits.ToString() } }, MsgFilterId.LootObtained));
            return true;
        }

        /// <summary>
        /// Takes <paramref name="credits"/> out of the player's purse. The amount is positive and
        /// is subtracted. Returns false, having moved nothing, if they cannot pay - so a caller
        /// that forgets its own funds check refuses the purchase rather than running up a debt.
        /// </summary>
        public bool LossCredits(Client client, int credits)
        {
            if (credits < 0)
            {
                Logger.WriteLog(LogType.Error, $"LossCredits({credits}) for {client.Player?.FamilyName}: charges are positive amounts. Nothing moved.");
                return false;
            }

            if (credits == 0)
                return true;

            if (client.Player.Credits[CurencyType.Credits] < credits)
                return false;

            return _characterManager.UpdateCharacter(
                client, CharacterUpdate.Credits, -credits);
        }

        public int GetAvailableAttributePoints(Manifestation player)
        {
            var points = 3 * (player.Level - 1);
            points -= player.SpentBody;
            points -= player.SpentMind;
            points -= player.SpentSpirit;
            //points = Math.Max(points, 0); Probably do not need this? (StaticVariable)
            return points;
        }

        public void GetCustomizationChoices(Client client, GetCustomizationChoicesPacket packet)
        {
            // ToDo
            var test = EntityManager.Instance.GetEntityType(packet.EntityId);
            var testChoices = new Dictionary<int, int>
            {
                { 3663, 36 },
                { 3672, 42 },
                { 3812, 60 }
            };
            client.CallMethod(SysEntity.ClientMethodId, new CustomizationChoicesPacket(packet.EntityId, testChoices));
        }

        private int GetLevelNeededExperience(int level)
        {
            if (level < 1 || level >= 50)
                return -1;

            return ExpPerLevel.ExpRequred[level];
        }

        public int GetSkillIndexById(int skillId)
        {
            return skillId < 0 ? -1 : skillId >= 200 ? -1 : SkillId2Idx[skillId];
        }

        public int GetSkillPointsAvailable(Manifestation player)
        {
            var level = player.Level;

            var pointsAvailable = (player.Level - 1) * 2;
            pointsAvailable += 5; // add five points because of the recruit skills that start at level 1

            if (level >= 5)
                pointsAvailable += 2;

            if (level >= 15)
                pointsAvailable += 2;

            if (level >= 30)
                pointsAvailable += 2;

            if (level >= 50)
                pointsAvailable += 4;

            // subtract spent skill levels
            foreach (var skill in player.Skills)
            {
                var skillLevel = skill.Value.SkillLevel;
                if (skillLevel < 0 || skillLevel > 5)
                    continue; // should not be possible
                pointsAvailable -= requiredSkillLevelPoints[skillLevel];
            }
            return Math.Max(0, pointsAvailable);
        }

        /// <summary>
        /// Which class grants each skill and the level it takes, from the client's own
        /// skillCharacter table. Loaded once at startup.
        /// </summary>
        private readonly Dictionary<SkillId, (CharacterClass Class, int Level)> _skillClasses = new();

        public void LoadSkillClasses()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            foreach (var entry in unitOfWork.Actions.GetSkillCharacters())
                _skillClasses[(SkillId)entry.Id] = ((CharacterClass)entry.ClassId, (int)entry.RequiredLevel);

            Logger.WriteLog(LogType.Initialize, $"Loaded {_skillClasses.Count} skill class requirements.");
        }

        /// <summary>
        /// Why this request may not be honoured, or null if it may.
        ///
        /// The client runs all of this before it will even draw the spend buttons - the skills
        /// window shows them only when IsCharacterClass(skill's class, player's class) holds, and
        /// only up to the levels the player has - but none of it constrains the wire. Without a
        /// copy here, the only cost of training a Tier 4 skill on a level 1 Recruit was the skill
        /// points, and abilities are granted by the skills a player holds: AbilityManager.Owns
        /// walks Player.Skills and asks nothing about how they got there.
        ///
        /// Checked as a whole before anything is written, because the request is a set: a list
        /// that is good up to its fifth entry must not leave the first four trained.
        /// </summary>
        private string ValidateSkillLevels(Client client, LevelSkillsPacket packet)
        {
            if (packet.SkillIds == null || packet.SkillLevels == null)
                return "a request with no skill list at all";

            if (packet.ListLenght < 0 || packet.ListLenght > packet.SkillIds.Length
                                      || packet.ListLenght > packet.SkillLevels.Length)
                return "a list length that does not match the list";

            var playerClass = (CharacterClass)client.Player.Class;
            var seen = new HashSet<SkillId>();
            var pointsAvailable = GetSkillPointsAvailable(client.Player);

            for (var i = 0; i < packet.ListLenght; i++)
            {
                var rawId = packet.SkillIds[i];
                var skillId = (SkillId)rawId;

                // GetSkillIndexById answers -1 for an id outside the table, and the index went
                // straight into SkillIdx2AbilityId - so a skill id of 0, or anything past 199,
                // was an IndexOutOfRangeException out of the handler, which is a disconnect.
                var index = GetSkillIndexById(rawId);

                if (index < 0 || index >= SkillIdx2AbilityId.Length)
                    return $"skill id {rawId}, which is not in the skill table";

                // The same id twice would be counted once against the points and written twice.
                if (!seen.Add(skillId))
                    return $"skill {rawId} twice in one request";

                if (!_skillClasses.TryGetValue(skillId, out var requirement))
                    return $"skill {rawId}, which no class grants";

                // gameuiutil's IsCharacterClass: the skill's class has to be the player's own or
                // one they advanced through. A Commando keeps their Soldier and Recruit skills
                // and can never train a Ranger's.
                if (!CharacterClassTree.Is(playerClass, requirement.Class))
                    return $"skill {rawId}, which belongs to {requirement.Class} and not to {playerClass}";

                if (client.Player.Level < requirement.Level)
                    return $"skill {rawId} at level {client.Player.Level}, which needs {requirement.Level}";

                var newSkillLevel = packet.SkillLevels[i];
                var oldSkillLevel = client.Player.Skills.TryGetValue(skillId, out var held) ? held.SkillLevel : 0;

                if (newSkillLevel < 0 || newSkillLevel > MaxSkillLevel)
                    return $"skill {rawId} at rank {newSkillLevel}";

                // Levelling a skill down is not a refund, it is a way to spend the same points
                // twice - the points come back and the ranks already bought stay.
                if (newSkillLevel < oldSkillLevel)
                    return $"skill {rawId} lowered from {oldSkillLevel} to {newSkillLevel}";

                pointsAvailable -= requiredSkillLevelPoints[newSkillLevel] - requiredSkillLevelPoints[oldSkillLevel];
            }

            if (pointsAvailable < 0)
                return "more skill points than this character has";

            return null;
        }

        public void LevelSkills(Client client, LevelSkillsPacket packet)
        {
            // Refused rather than thrown. Every one of these used to be an exception out of a
            // packet handler, which Client.Update catches as a malformed packet and answers by
            // closing the connection - so a client one version out of step, or one repeating a
            // request the player had already spent the points on, was disconnected rather than
            // told no.
            var refusal = ValidateSkillLevels(client, packet);

            if (refusal != null)
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} sent a LevelSkills this character may not have: {refusal}. Ignored.");

                // Their copy of the skills is now ahead of ours; put it back.
                client.CallMethod(client.Player.EntityId, new SkillsPacket(client.Player.Skills));
                SendAvailableAllocationPoints(client);
                return;
            }

            var updated = client.Player.Skills.ToDictionary(
                entry => entry.Key,
                entry => new SkillsData(
                    entry.Value.SkillId,
                    entry.Value.AbilityId,
                    entry.Value.SkillLevel));

            for (var i = 0; i < packet.ListLenght; i++)
            {
                var skillId = (SkillId)packet.SkillIds[i];
                var abilityId = SkillIdx2AbilityId[GetSkillIndexById(packet.SkillIds[i])];
                updated[skillId] = new SkillsData(
                    skillId, abilityId, packet.SkillLevels[i]);
            }
            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var durable = unitOfWork.CharacterSkills
                        .GetCharacterSkills(client.Player.Id)
                        .ToDictionary(entry => (SkillId)entry.SkillId);

                    if (durable.Count != client.Player.Skills.Count ||
                        client.Player.Skills.Any(entry =>
                            !durable.TryGetValue(entry.Key, out var saved) ||
                            saved.AbilityId != entry.Value.AbilityId ||
                            saved.SkillLevel != entry.Value.SkillLevel))
                        throw new GameplayRejectionException(
                            "Durable learned skills changed before training.");

                    foreach (var skillId in packet.SkillIds.Take(packet.ListLenght)
                                 .Select(id => (SkillId)id))
                    {
                        var skill = updated[skillId];
                        unitOfWork.CharacterSkills.AddOrUpdate(
                            client.Player.Id,
                            (uint)skill.SkillId,
                            skill.AbilityId,
                            skill.SkillLevel);
                    }
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist skill training for character {client.Player.Id}: {error.Message}");
                return;
            }

            client.Player.Skills = updated;
            client.CallMethod(client.Player.EntityId,
                new SkillsPacket(updated));
            client.CallMethod(client.Player.EntityId,
                new AbilitiesPacket(updated));
            SendAvailableAllocationPoints(client);
            SyncWeaponSkills(client);
        }

        public void NotifyEquipmentUpdate(Client client)
        {
            client.CallMethod(client.Player.EntityId, new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory));
        }

        /// <param name="delay">Milliseconds until the timer's first shot; a whole refire when left out.</param>
        /// <returns>false when there is no weapon in hand to fire, and no timer was started.</returns>
        public bool RegisterAutoFire(Client client, long delay = -1)
        {
            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            if (weapon?.ItemTemplate?.WeaponInfo == null)
                return false;

            // One timer per client: a second StartAutoFire used to add a second timer and
            // double the rate of fire.
            RemoveAutoFire(client);

            var refire = weapon.ItemTemplate.WeaponInfo.Refire;

            AutoFire.Add(new AutoFireTimer(client, refire, delay < 0 ? refire : delay));

            return true;
        }

        private static void RemoveAutoFire(Client client)
        {
            for (var i = AutoFire.Count - 1; i >= 0; i--)
                if (AutoFire[i].Client == client)
                    AutoFire.RemoveAt(i);
        }

        public void RemovePlayerCharacter(Client client)
        {
            if (client?.Player?.MapChannel != null)
                LootDispenserManager.Instance.RemoveForOwner(client.Player.MapChannel, client);

            // Called from MapChannelManager.RemovePlayer. A client that dropped while holding
            // fire stayed in the auto-fire list; once its items were destroyed CurrentWeapon
            // was null, and the next tick dereferenced it on the main loop.
            RemoveAutoFire(client);
        }

        public void RemoveAppearanceItem(Client client, EquipmentData equipmentSlotId)
        {
            if (equipmentSlotId == 0)
                return;

            // A slot nothing was ever shown in is already clear. SetAppearanceItem is what adds
            // the key, so a slot whose item never got that far - one holding something with no
            // appearance at all - has no entry, and the indexer threw where there was simply
            // nothing to remove.
            if (!client.Player.AppearanceData.ContainsKey(equipmentSlotId))
                return;

            client.Player.AppearanceData[equipmentSlotId].Class = 0;
            // update appearance data in database
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAppearances.AddOrUpdate(client.Player.Id, new CharacterAppearanceEntry((uint)equipmentSlotId, 0, 0));
            unitOfWork.Complete();
        }

        public void RequestCustomization(Client client, RequestCustomizationPacket packet)
        {
            // ToDo
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestCustomization");
        }

        #region Movement

        /// <summary>
        /// How fast a character runs, in metres per second: the run rate the client reads for the
        /// player avatar classes out of its own entitymovementrate table (HumanBaseMale and
        /// HumanBaseFemale are both 0.0 stopped / 2.5 walking / 6.5 running), which is what its
        /// movement manager drives the body at.
        /// </summary>
        private const double RunSpeed = 6.5d;

        /// <summary>
        /// Headroom on that speed. The client's own movement is not a constant 6.5 - momentum
        /// down a slope, a jump arc, the blend out of an animation and plain float drift all
        /// overshoot it - and none of that is worth refusing a move over. This is the factor by
        /// which a player may beat their own top speed before the server stops believing it.
        /// </summary>
        private const double SpeedTolerance = 1.5d;

        /// <summary>
        /// The most movement a player may have saved up, in seconds of running.
        ///
        /// A budget that filled for as long as the player stood still would let them stand for a
        /// minute and then cross half a zone in one packet. Ten seconds is chosen to be longer
        /// than any ordinary stall - the connection is TCP, so a hitch delivers the whole burst
        /// of Moves that piled up behind it and they all have to be payable - and short enough
        /// that waiting is never a faster way to travel than walking.
        /// </summary>
        private const double MaxBankedSeconds = 10d;

        /// <summary>
        /// The furthest a player may move in one Move, in metres, whatever they have banked.
        ///
        /// The budget bounds the average speed but not a single step: a player who stood still
        /// for a while could spend ten seconds of it at once. This is what keeps banked distance
        /// something that still has to be walked, in steps, so the cheat is never faster than the
        /// walk it replaces. Four seconds of running is far larger than any step a real client
        /// sends and far smaller than the reach the exploit wants.
        /// </summary>
        private const double MaxStepDistance = RunSpeed * SpeedTolerance * 4d;

        /// <summary>
        /// How much of a step's budget a rise costs. Climbing is not faster than running, but a
        /// jump is a short burst of it, and stairs and ramps add their rise to a step that was
        /// paid for horizontally. Falling is not measured at all: gravity is faster than anything
        /// a character does under its own power, and a player dropping off a cliff is not
        /// cheating.
        /// </summary>
        private const double RiseFactor = 2.5d;

        /// <summary>Quiet time between movement corrections for one player.</summary>
        private const long MoveCorrectionQuietMs = 1000;

        /// <summary>
        /// Whether to believe where a client says it is.
        ///
        /// Position used to be taken as given: one Move set it to any point the wire format could
        /// carry, and everything downstream reads it as the truth. That bought free travel - a
        /// map link fires on proximity alone, and so does every waypoint and every dropship pad -
        /// along with every range check on the server at once (use, harvest, cipher, craft,
        /// trade, ability and missile range, aggro and leash), a remote entity scanner out of the
        /// visibility system, and a hiding place: a player parked in an empty cell is dropped
        /// from everyone else's view and from the creature scan, while still able to act on
        /// anything they can name by entity id.
        ///
        /// What is checked is the movement rather than the position, which is the only thing the
        /// server can honestly judge. It does not simulate the world, so it cannot know what is
        /// walkable or what is solid; it does know how fast a character moves and how long it has
        /// been since it last heard from this one. A step is paid for out of a budget that
        /// refills at the player's own speed - their effects included, so a sprint pays for
        /// itself - and no single step may be a long one however much has been banked. A client
        /// cannot then travel faster than the character could have walked, whatever it claims,
        /// which leaves the exploit worth no more than the walk it was trying to skip.
        ///
        /// A refused Move is not applied and the client is put back where the server last had the
        /// player. Refusing without correcting would leave the two disagreeing for the rest of
        /// the session, which is worse than the move was.
        /// </summary>
        public bool AcceptMove(Client client, Movement movement)
        {
            var player = client.Player;
            var now = Environment.TickCount64;

            var verdict = JudgeMove(player.Position, movement.Position, player.MovementSpeed,
                now - player.MoveBudgetTick, player.MoveBudget);

            player.MoveBudget = verdict.Budget;
            player.MoveBudgetTick = now;

            if (verdict.Accepted)
                return true;

            RefuseMove(client, movement, verdict.Refusal);
            return false;
        }

        /// <summary>What <see cref="JudgeMove"/> made of a step.</summary>
        public readonly struct MoveVerdict
        {
            public bool Accepted { get; }

            /// <summary>What is left of the budget: the step's cost taken off, or untouched if it was refused.</summary>
            public double Budget { get; }

            /// <summary>Why not, in words, or null when it was accepted.</summary>
            public string Refusal { get; }

            public MoveVerdict(bool accepted, double budget, string refusal = null)
            {
                Accepted = accepted;
                Budget = budget;
                Refusal = refusal;
            }
        }

        /// <summary>
        /// The decision on its own, as arithmetic over what a step costs and what has been saved
        /// up to pay for it. Kept apart from the connection it arrived on so that it can be
        /// reasoned about, and tested, without one.
        /// </summary>
        public static MoveVerdict JudgeMove(Vector3 from, Vector3 to, double movementSpeed, long elapsedMs, double budget)
        {
            // Their own speed, effects included: MovementSpeed is the product of every movement
            // modifier on them (GameEffectManager.UpdateMovementMod), so a sprint raises the
            // budget by exactly what the sprint gave them and nothing here has to know that
            // sprint exists. Never below 1, so a snare cannot tighten the check: being slowed is
            // the client's business to obey, and holding a slowed player to their slowed speed
            // would refuse them the moment the effect ended a step before the server said so.
            var speed = RunSpeed * Math.Max(1.0d, movementSpeed) * SpeedTolerance;
            var elapsed = Math.Max(0, elapsedMs) / 1000d;

            budget = Math.Min(budget + elapsed * speed, speed * MaxBankedSeconds);

            // Horizontal and vertical apart: a character's speed is a speed over the ground, and
            // the rise of a step is its own thing.
            var horizontal = Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Z - from.Z) * (to.Z - from.Z));
            var rise = Math.Max(0d, to.Y - from.Y);
            var spend = Math.Max(horizontal, rise / RiseFactor);

            // Every step is charged, however small. A step waved through below some threshold is
            // not a tolerance, it is a hole: at ten packets a second, steps of "only" a metre and
            // a half are three times running speed, and nothing would ever have been charged for
            // them. Jitter is absorbed by the budget itself, which is what it is for - a packet
            // that arrives late brings its neighbour's time with it.
            if (spend > MaxStepDistance)
                return new MoveVerdict(false, budget, $"{horizontal:F0} m across and {rise:F0} m up in one step");

            if (spend > budget)
                return new MoveVerdict(false, budget,
                    $"{horizontal:F0} m across and {rise:F0} m up with {budget:F0} m in hand at speed {movementSpeed:F2}");

            return new MoveVerdict(true, budget - spend);
        }

        /// <summary>
        /// Drops a Move and puts the client back where the server has the player.
        ///
        /// Corrections and log lines are both rate limited per player. A client that is refused
        /// once is usually about to be refused for as long as it keeps sending the same position,
        /// and answering every one would be a packet out per packet in and a log line per packet,
        /// which is a worse thing to be on the end of than the movement was.
        /// </summary>
        private static void RefuseMove(Client client, Movement movement, string what)
        {
            var player = client.Player;
            var now = Environment.TickCount64;

            player.RefusedMoves++;

            if (now < player.LastMoveCorrectionTick + MoveCorrectionQuietMs)
                return;

            var repeat = player.RefusedMoves > 1 ? $" ({player.RefusedMoves} refused since the last of these)" : "";

            Logger.WriteLog(LogType.Security,
                $"{player.FamilyName} tried to move {what} on map {player.MapContextId}, from {player.Position} to {movement.Position}{repeat}; put back.");

            player.LastMoveCorrectionTick = now;
            player.RefusedMoves = 0;

            client.MoveObject(player.EntityId, new Movement(player.Position, movement.ViewDirection));
        }

        #endregion

        /// <summary>
        /// /stuck. Moves a player who has become wedged in the world to the nearest place they
        /// can stand.
        ///
        /// The position in the packet is the client's, and is not used: a client is free to claim
        /// it is stuck anywhere, and acting on that would turn this into a teleport. The server
        /// works from the position it holds, and the navmesh decides where they end up, so the
        /// destination is always somewhere they could have walked to.
        ///
        /// A dead player is left alone - they have a respawn for that, and moving a corpse would
        /// take the body away from anyone about to revive it.
        /// </summary>
        public void RequestUnstick(Client client, RequestUnstickPacket packet)
        {
            if (client.Player == null)
                return;

            if (client.Player.Attributes[Attributes.Health].Current <= 0 || client.Player.State == CharacterState.Dead)
            {
                Unhelpful(client, PlayerMessage.PmAboutToRespawn);
                return;
            }

            var from = client.Player.Position;
            var destination = NavMeshManager.NearestWalkable(client.Player.MapChannel, from);

            if (destination == null)
            {
                // No navmesh for this map, or the player is further from walkable ground than the
                // search reaches. Moving them somewhere arbitrary would be worse than saying so.
                Unhelpful(client, PlayerMessage.PmCannotPerformActionNow);
                Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} used /stuck at {from} on map {client.Player.MapChannel?.MapInfo?.MapContextId}, where nothing walkable was found.");
                return;
            }

            client.Player.PlaceAt(destination.Value);
            client.MoveObject(client.Player.EntityId, new Movement(destination.Value, client.Movement.ViewDirection));

            // The message the live game showed for this command.
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(PlayerMessage.PmStuckBugReportSent, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} unstuck from {from} to {destination.Value} ({Vector3.Distance(from, destination.Value):F1} m).");
        }

        private static void Unhelpful(Client client, PlayerMessage reason)
        {
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(reason, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        /// <summary>
        /// Firing an armed ability. AbilityManager checks it - ownership, cooldown, target, range,
        /// cost - answers a refusal with UserActionFailed, and queues an accepted one for the
        /// windup; ActorActionManager hands the recovery back to it. The position of a
        /// ground-targeted ability travels in ActionData.TargetLocation.
        /// </summary>
        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            // Ownership, cooldown, range, cost and the windup are AbilityManager's; this used to
            // queue the action for the next tick with no checks at all.
            AbilityManager.Instance.RequestPerformAbility(client, packet);
        }

        public void RequestToggleRun(Client client)
        {
            client.Player.IsRunning = !client.Player.IsRunning;

            client.CallMethod(client.Player.EntityId, new IsRunningPacket(client.Player.IsRunning));
        }

        /// <summary>Idle time before PlayerInactiveWarning, which also marks the player AFK.</summary>
        public const long InactiveWarningMs = 5 * 60 * 1000;

        /// <summary>Idle time before the player is sent back to character selection.</summary>
        public const long InactiveLogoutMs = 15 * 60 * 1000;

        /// <summary>
        /// Client traffic that does not mean the player is at the keyboard. It neither resets
        /// the inactivity timer nor clears AFK. Everything else the client sends counts as
        /// activity: movement, abilities, weapons, chat, inventory, targeting and so on.
        /// </summary>
        private static readonly HashSet<GameOpcode> AutomaticOpcodes = new()
        {
            GameOpcode.Ping,                // client resends every 1s from RequestNetworkStats()
            GameOpcode.AutoFireKeepAlive,   // client resends every 2.5s while autofire runs
            GameOpcode.MapLoaded,           // sent automatically once a zone finishes loading
            GameOpcode.TeleportAcknowledge  // automatic reply to a server-initiated teleport
        };

        /// <summary>
        /// Called for every inbound client method call. Resets the inactivity timer and clears
        /// AFK (telling everyone in range) the first time the player actually does something.
        /// </summary>
        public void NotifyPlayerActivity(Client client, GameOpcode opcode)
        {
            if (AutomaticOpcodes.Contains(opcode))
                return;

            ResetInactivity(client);

            // /afk is a deliberate action, so it resets the timer above, but clearing AFK here
            // would immediately undo the flag it is setting.
            if (opcode == GameOpcode.ToggleAfk)
                return;

            SetAfk(client, false);
        }

        /// <summary>Called for movement, which arrives outside CallServerMethod.</summary>
        public void NotifyPlayerActivity(Client client)
        {
            ResetInactivity(client);

            // SetAfk returns immediately when the flag is already clear, so this costs one
            // bool comparison on the movement path and only broadcasts on a real transition.
            SetAfk(client, false);
        }

        /// <summary>
        /// Starts a fresh idle stretch. Also called when a player enters the world or finishes
        /// a teleport, so time spent on a loading screen never counts as idle.
        /// </summary>
        public void ResetInactivity(Client client)
        {
            client.Player.LastActivityTick = Environment.TickCount64;
            client.Player.InactiveWarningSent = false;
        }

        /// <summary>
        /// Run from the MapChannelWorker every tick, ahead of its removal pass. At
        /// InactiveWarningMs the player is marked AFK and warned; at InactiveLogoutMs they are
        /// flagged the same way CharacterLogout flags a /logout, so the removal pass returns
        /// them to character selection on the same tick.
        /// </summary>
        public void CheckInactivity(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;

            foreach (var client in mapChannel.ClientList)
            {
                // Loading, teleporting, already leaving, or a dropped connection awaiting removal.
                if (client == null || client.State != ClientState.Ingame || client.Player.RemoveFromMap || client.Player.Disconected)
                    continue;

                var idle = now - client.Player.LastActivityTick;

                if (idle >= InactiveLogoutMs)
                {
                    Logger.WriteLog(LogType.Network, $"{client.Player.FamilyName} inactive for {idle / 60000} minutes, returning to character selection");

                    // Same effect as MapChannelManager.CharacterLogout, without its LogoutActive
                    // gate: that flag only exists to confirm the client asked to leave.
                    // Recv_BeginCharacterSelection switches the client to character selection
                    // from any input state, so the client does not need to cooperate.
                    client.State = ClientState.LoggedIn;
                    client.Player.RemoveFromMap = true;
                    continue;
                }

                if (idle >= InactiveWarningMs && !client.Player.InactiveWarningSent)
                {
                    client.Player.InactiveWarningSent = true;

                    // AFK first, so the player reads "You are now AFK" followed by the warning,
                    // and everyone in range sees the idle marker.
                    SetAfk(client, true);
                    client.CallMethod(client.Player.EntityId, new PlayerInactiveWarningPacket());
                }
            }
        }

        public void ToggleAfk(Client client)
        {
            SetAfk(client, !client.Player.IsAFK);
        }

        public void SetAfk(Client client, bool isAfk)
        {
            if (client.Player.IsAFK == isAfk)
                return;

            client.Player.IsAFK = isAfk;

            // Broadcast to everyone in visibility range, including the player: the client
            // shows the "you are AFK" system message only for its own manifestation, and an
            // idle indicator over anyone else's head.
            client.CellCallMethod(client, client.Player.EntityId, new PlayerAfkPacket(isAfk));

            // The squad window reads a different source: its own party tuples, not the
            // manifestation. A squadmate on another map is not in visibility range at all, so
            // without this they never learn the member went away.
            PartyManager.Instance.MemberInfoChanged(client);
        }

        /// <summary>
        /// Also a handler: the client asks to draw, and can ask with an empty weapon drawer
        /// slot armed. There is then no weapon to name a draw animation, so there is nothing
        /// to perform and nothing to be ready with.
        /// </summary>
        public void RequestWeaponDraw(Client client)
        {
            var mapChannel = client.Player?.MapChannel;

            if (mapChannel == null)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(InventoryManager.Instance.CurrentWeapon(client));

            if (weaponClassInfo == null)
                return;

            QueueWeaponReadyChange(mapChannel, new ActionData(client.Player, ActionId.WeaponDraw, weaponClassInfo.DrawActionId, 500));

            WeaponReady(client, true);
        }

        /// <summary>
        /// Puts a draw or a stow in the queue as the only one this actor has.
        ///
        /// Both handlers used to add one per packet with nothing to stop them. The client sends
        /// one per keypress, so a client sending them in a loop grew the map's recovery list
        /// without bound - a list walked on every tick of the map's worker, each entry coming
        /// due with its own PerformRecovery to everyone in range. Only the last one asked for
        /// means anything anyway: a stow that arrives while a draw is still playing replaces it
        /// rather than lining up behind it, which is also what the player meant by sending it.
        /// </summary>
        private static void QueueWeaponReadyChange(MapChannel mapChannel, ActionData action)
        {
            mapChannel.PerformRecovery.RemoveAll(queued => queued.Actor == action.Actor
                                                           && (queued.ActionId == ActionId.WeaponDraw || queued.ActionId == ActionId.WeaponStow));

            mapChannel.PerformRecovery.Add(action);
        }

        /// <summary>
        /// Ends a reload the server is not going to finish.
        ///
        /// The client plays the reload animation as a windup and waits to be told how it ended:
        /// the recovery resolves it, or an interrupt cancels it. Returning without either leaves
        /// the animation running until some other action happens to interrupt it, which is what
        /// an out-of-ammo reload did - it failed correctly and then span forever.
        ///
        /// Recv_ActionInterrupt matches the action and its arg against the actor's current
        /// action, so the arg has to be the one the windup was started with. It goes to everyone
        /// in range, not just the player: onlookers were shown the windup too.
        /// </summary>
        private void CancelReload(Client client, uint reloadActionId, PlayerMessage reason)
        {
            client.CellCallMethod(client, client.Player.EntityId,
                new ActionInterruptPacket(client.Player.EntityId, ActionId.WeaponReload, reloadActionId));

            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(reason, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        public void RequestWeaponReload(Client client, bool isRequested)
        {
            // One reload at a time. Every request used to queue another, with a windup to everyone
            // in range for each: the client never sends a second while its first is in progress,
            // but the fire path asks again on every trigger pull that finds the clip empty, so
            // holding fire through a reload queued another every refire, and a client sending the
            // request in a loop queued one per packet - each coming due with its own recovery to
            // everyone in range and its own database write.
            if (client.Player == null || IsReloading(client.Player))
                return;

            // here we only check, can we reload weapon
            // actual weapon reload happen if reaload action isn't interupted
            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            if (weapon?.ItemTemplate?.WeaponInfo == null)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            if (weaponClassInfo == null)
                return;

            var reloadActionId = (uint)weaponClassInfo.ReloadActionId;
            var foundAmmo = 0u;

            if (!weapon.IsJammed && weapon.CurrentAmmo >= weaponClassInfo.ClipSize)
                return;

            for (var i = 0; i < 50; i++)
            {
                if (client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i] == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i]);

                // A slot naming an item that is not registered. Skip that slot rather than
                // abandoning the reload: one stale row used to stop the scan, so ammo sitting in
                // a later slot was never found and the reload silently did nothing.
                if (weaponAmmo == null)
                    continue;

                if (weaponAmmo.ItemTemplate.Class == weaponClassInfo.AmmoClassId && weaponAmmo.StackSize > 0 &&
                    !MissionItemProtection.IsProtected(weaponAmmo, _gameUnitOfWorkFactory))
                {
                    // consume ammo
                    var ammoToGrab = Math.Min(weaponClassInfo.ClipSize - foundAmmo - weapon.CurrentAmmo, weaponAmmo.StackSize);
                    foundAmmo = ammoToGrab + weapon.CurrentAmmo;
                }

                if (foundAmmo == weaponClassInfo.ClipSize)
                    break;
            }

            // A jammed weapon reloads regardless of what is in the clip or the pack, because
            // reloading is the only way to clear a jam and the client already works this way:
            // weaponreload.py runs its ammo, clip-full and out-of-ammo checks inside
            // `if not weapon.isJammed`. Without this, a player who jams with a full clip or an
            // empty pack has no way out of it.
            if (weapon.IsJammed)
            {
                if (isRequested)
                    client.CellCallMethod(client, client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, reloadActionId));
                else
                    client.CellIgnoreSelfCallMethod(client, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, reloadActionId));

                client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.WeaponReload, reloadActionId, foundAmmo, ReloadTimeFor(client.Player, weapon))
                {
                    SourceId = weapon.EntityId
                });
                return;
            }

            if (foundAmmo == 0)
            {
                // Nothing to reload with.
                //
                // isRequested is false when the *player* asked: their own client has already
                // started the animation locally, which is why the windup below is sent to
                // everyone except them. So that is exactly the case where the client is sitting
                // in a windup nothing will ever end, and it has to be told.
                //
                // isRequested is true only for the reload the fire path starts when the clip is
                // empty. No windup has been sent yet, so there is nothing to cancel - and firing
                // a dry weapon comes back here on every trigger pull, so saying anything would
                // be a message per tick.
                if (!isRequested)
                    CancelReload(client, reloadActionId, PlayerMessage.PmInventoryOutOfAmmo);

                return;
            }

            if (isRequested)
                client.CellCallMethod(client, client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));
            else
                client.CellIgnoreSelfCallMethod(client, new PerformWindupPacket(PerformType.TwoArgs, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId));

            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, ActionId.WeaponReload, (uint)weaponClassInfo.ReloadActionId, foundAmmo, ReloadTimeFor(client.Player, weapon))
            {
                SourceId = weapon.EntityId
            });
        }

        /// <summary>
        /// Also a handler, and reachable with an empty weapon drawer slot armed, same as
        /// RequestWeaponDraw. An empty hand is already stowed: there is no animation to
        /// perform, but the flag is still cleared, because that is the truth either way.
        /// </summary>
        public void RequestWeaponStow(Client client)
        {
            var mapChannel = client.Player?.MapChannel;

            if (mapChannel == null)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(InventoryManager.Instance.CurrentWeapon(client));

            if (weaponClassInfo != null)
                QueueWeaponReadyChange(mapChannel, new ActionData(client.Player, ActionId.WeaponStow, (uint)weaponClassInfo.StowActionId, 500));

            WeaponReady(client, false);
        }

        public void SaveCharacterOptions(Client client, SaveCharacterOptionsPacket packet)
        {
            if (packet.OptionsList.Count == 0)
                return;

            client.Player.CharacterOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.Player.CharacterOptions)
                unitOfWork.CharacterOptions.AddOrUpdate(client.Player.Id, (uint)option.OptionId, option.Value);

            // AddOrUpdate only stages the rows; without this they were thrown away on dispose,
            // and every option the client saved was back to its default at the next login.
            unitOfWork.Complete();
        }

        // maybe move this to other manager becose it's account related
        public void SaveUserOptions(Client client, SaveUserOptionsPacket packet)
        {
            client.UserOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.UserOptions)
                unitOfWork.UserOptions.AddOrUpdate(client.AccountEntry.Id, (uint)option.OptionId, option.Value);

            unitOfWork.Complete();
        }

        internal void SendAvailableAllocationPoints(Client client)
        {
            // update available allocation points (attributes, trainPts, skillPts)

            var attributePoints = GetAvailableAttributePoints(client.Player);
            var trainPoints = 0;    // not used by te client
            var skillPoints = GetSkillPointsAvailable(client.Player);

            client.CallMethod(client.Player.EntityId, new AvailableAllocationPointsPacket(attributePoints, trainPoints, skillPoints));
        }

        public void SetAppearanceItem(Client client, Item item)
        {
            var equipable = EntityClassManager.Instance.GetEquipableClassInfo(item);

            // Only equipment is worn. Every caller establishes that before asking, so reaching
            // here without it is a caller that stopped checking rather than something a player
            // did - said plainly instead of thrown, which used to take the connection down.
            if (equipable == null)
            {
                Logger.WriteLog(LogType.Error,
                    $"SetAppearanceItem was given {item?.ItemTemplate?.Class.ToString() ?? "no item"}, which has no equipment slot; appearance unchanged.");
                return;
            }

            var equipmentSlotId = equipable.EquipmentSlotId;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (!client.Player.AppearanceData.ContainsKey(equipmentSlotId))
                client.Player.AppearanceData.Add(equipmentSlotId, new AppearanceData { SlotId = equipmentSlotId });

            client.Player.AppearanceData[equipmentSlotId].Class = (uint)item.ItemTemplate.Class;
            client.Player.AppearanceData[equipmentSlotId].Color = new Color(item.Color);
            client.Player.AppearanceData[equipmentSlotId].Hue2 = new Color(item.Color);

            // update appearance data in database

            unitOfWork.CharacterAppearances.AddOrUpdate(client.Player.Id, new CharacterAppearanceEntry((uint)equipmentSlotId, (uint)item.ItemTemplate.Class, item.Color));
            unitOfWork.Complete();
        }

        public void SetDesiredCrouchState(Client client, bool crouching)
        {
            client.Player.IsCrouching = crouching;

            client.CallMethod(client.Player.EntityId, new SetDesiredCrouchStatePacket(client.Player.IsCrouching ? CharacterState.Crouched : CharacterState.Standing));
        }

        public void SetTargetId(Client client, ulong entityId)
        {
            client.Player.Target = entityId;
        }

        public void SetTrackingTarget(Client client, ulong entityId)
        {
            client.Player.TrackingTargetEntityId = entityId;
        }

        public void UpdateAppearance(Client client)
        {
            if (client.Player == null)
                return;

            client.CellCallMethod(client, client.Player.EntityId, new AppearanceDataPacket(client.Player.AppearanceData));
        }

        // Health calculation:
        //levelBasedHealth = 20000.0
        //for (int i = level; i< 50; ++i)
        //levelBasedHealth = levelBasedHealth - 0.082995 * levelBasedHealth;

        private readonly float[] HealthBaselinePerLevel =
         {
            286.5784148f,
            312.51565127f,
            340.8003787f,
            371.6450605f,
            405.28138942f,
            441.96202792f,
            481.96250612f,
            525.58329139f,
            573.15204539f,
            625.02608535f,
            681.59506802f,
            743.28391668f,
            810.55601298f,
            883.91667764f,
            963.91696626f,
            1051.15780858f,
            1146.29452247f,
            1250.04173638f,
            1363.17875735f,
            1486.55542483f,
            1621.09849437f,
            1767.818599f,
            1927.81784069f,
            2102.29806892f,
            2292.56990847f,
            2500.06260431f,
            2726.33475751f,
            2973.08603281f,
            3242.1699258f,
            3535.60768567f,
            3855.60349799f,
            4204.56104164f,
            4585.10154431f,
            5000.08347207f,
            5452.62400104f,
            5946.1224323f,
            6484.28572615f,
            7071.15634718f,
            7711.14262974f,
            8409.05189147f,
            9170.12654399f,
            10000.08347172f,
            10905.15697485f,
            11892.14559882f,
            12968.4632023f,
            14142.19464703f,
            15422.15652808f,
            16817.9634005f,
            18340.1f,
            20000f
        };

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        public void UpdateStatsValues(Client client, bool fullreset)
        {
            var player = client.Player;
            var attribute = player.Attributes;

            int level = player.Level;

            if (level < 1 || level > MaxPlayerLevel)
                throw new InvalidProgressionLevelException(player.Id, level);

            int levelBasedBody   = 0;
            int levelBasedMind   = 0;
            int levelBasedSpirit = 0;

            switch (player.Race)
            {
                case Race.Human:
                    levelBasedBody = levelBasedMind = levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Forean:
                    levelBasedBody = (level - 1) + 10;
                    levelBasedMind = 3 * (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Brann:
                    levelBasedBody = levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 4 * (level - 1) + 10;
                    break;

                case Race.Thrax:
                    levelBasedBody = 3 * (level - 1) + 10;
                    levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;
            }

            int totalBody   = levelBasedBody   + player.SpentBody;
            int totalMind   = levelBasedMind   + player.SpentMind;
            int totalSpirit = levelBasedSpirit + player.SpentSpirit;

            // Health
            float levelBasedHealth = HealthBaselinePerLevel[level - 1];
            levelBasedHealth = levelBasedHealth / (2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10);
            int totalHealth = (int)(levelBasedHealth * (totalSpirit + 2 * totalBody));

            // The per-point factor shrinks with level; the attribute totals grow. Both sides of
            // these divisions were int, so the factor was truncated: 3 at level 1 instead of
            // 3.43, 1 from level 20 (1.11), and 0 from level 26 for power and 39 for regen - a
            // character that levelled far enough had no chi and no regeneration at all. The
            // health line above already divides as float and was right.
            float attributeDivisor = 2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10;

            // Power
            float basePower = (3 * level + 100) / attributeDivisor;
            int totalPower  = (int)(basePower * (totalBody + 2 * totalMind));

            // Regen
            float baseRegen = (2 * level + 100) / attributeDivisor;
            int totalRegen = (int)(baseRegen * (totalMind + 2 * totalSpirit));
          
            // Bonuses
            var bodyBonus = 0;
            var mindBonus = 0;
            var spiritBonus = 0;

            var healthBonus = 0;
            var chiBonus    = 0;
            var regenBonus  = 0;

            float armorBonusPercent = (float)Math.Max(0.0, (totalBody - (2 * (level - 1) + 10)) * 0.667);   // every body attribute over the default base attribute gives 0.667% bonus armo;
            float logosBonusPercent = (float)Math.Max(0.0, (totalMind - (2 * (level - 1) + 10)) * 0.375);   // every mind attribute over the default base attribute gives 0.375% bonus logos damage
            float critBonusPercent = (float)Math.Max(0.0, (totalSpirit - (2 * (level - 1) + 10)) * 0.065);  // every spirit attribute over the default base attribute gives 0.065% bonus crit chance;


            // body
            attribute[Attributes.Body].NormalMax    = totalBody;
            attribute[Attributes.Body].CurrentMax   = attribute[Attributes.Body].NormalMax + bodyBonus;
            attribute[Attributes.Body].Current      = attribute[Attributes.Body].CurrentMax;

            attribute[Attributes.Mind].NormalMax    = totalMind;
            attribute[Attributes.Mind].CurrentMax   = attribute[Attributes.Mind].NormalMax + mindBonus;
            attribute[Attributes.Mind].Current      = attribute[Attributes.Mind].CurrentMax;

            attribute[Attributes.Spirit].NormalMax  = totalSpirit;
            attribute[Attributes.Spirit].CurrentMax = attribute[Attributes.Spirit].NormalMax + spiritBonus;
            attribute[Attributes.Spirit].Current    = attribute[Attributes.Spirit].CurrentMax;

            // health
            attribute[Attributes.Health].NormalMax  = totalHealth;
            attribute[Attributes.Health].CurrentMax = totalHealth;

            // chi/adrenaline
            attribute[Attributes.Chi].NormalMax     = totalPower;
            attribute[Attributes.Chi].CurrentMax    = totalPower;

            attribute[Attributes.Regen].NormalMax   = totalRegen; // regenRate in percent
            attribute[Attributes.Regen].CurrentMax  = totalRegen;

            if (fullreset)
            {
                attribute[Attributes.Health].Current = attribute[Attributes.Health].CurrentMax;
                attribute[Attributes.Chi].Current = attribute[Attributes.Chi].CurrentMax;
            }
            else
            {
                attribute[Attributes.Health].Current = Math.Min(attribute[Attributes.Health].Current, attribute[Attributes.Health].CurrentMax);
                attribute[Attributes.Chi].Current = Math.Min(attribute[Attributes.Chi].Current, attribute[Attributes.Chi].CurrentMax);
            }


            // update regen rate: 2.0 per second at 100% regen, scaled by the rate as a
            // percentage. CurrentMax / 100 was int division, so any rate below 200% rounded
            // to the base 2 and the rate only mattered in whole multiples of 100.
            //
            // It goes on Health, not on Regen. Regen is a derived stat the attributes window
            // displays; nothing regenerates from it. The client heals from Health's own
            // refreshAmount and refreshPeriod, and Manifestation initialises both to 0, so
            // computing the rate and storing it on the wrong attribute meant no player has ever
            // regenerated health at all. The period has to be non-zero as well:
            // _EvaluatePredictedRefresh returns early on a period of 0.
            attribute[Attributes.Health].RefreshAmount = (int)Math.Round(2D * attribute[Attributes.Regen].CurrentMax / 100, 0);

            // Power regenerates at the health rate for now. The live game regenerated power by a
            // formula of its own that is not known; without any regeneration an ability could be
            // used a handful of times per map, since abilities now spend it. Interim.
            // ActorManager.Regenerate applies it server-side at the same period the client
            // predicts it with.
            //
            // Chi (adrenaline) does not regenerate. The client's own text has it "consumed by
            // specific abilities such as Rage and Sprint" and "gained by defeating enemies or by
            // using an adrenaline booster", with the Regen stat improving "Adrenaline gain" -
            // it is earned in combat, not refilled over time, and a passive refill would outpace
            // sprint's 1.5% a second drain and make it free. Kills grant it in
            // CreatureManager.HandleCreatureKill; its RefreshAmount stays 0 so the client
            // predicts nothing.
            attribute[Attributes.Power].RefreshAmount = attribute[Attributes.Health].RefreshAmount;
            attribute[Attributes.Chi].RefreshAmount = 0;
            // 2.0 per second is the base regeneration for health
            // calculate armor max
            var armorMax = 0.0d;
            //float armorBonus = 0; // todo! (From item modules)
            var armorBonusPct = player.Attributes[Attributes.Body].CurrentMax * 0.0066666d;
            var armorRegenRate = 0;

            for (var i = 1; i < Math.Min(22, client.Player.Inventory.EquippedInventory.Count); i++)
            {
                if (client.Player.Inventory.EquippedInventory[i] == 0)
                    continue;

                // skip weapon slot
                if (i == 13)
                    continue;

                var equipmentItem = EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[i]);

                if (equipmentItem == null)
                {
                    // this is very bad, how can the item disappear while it is still linked in the inventory?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Equipment item has no physical copy (item is missing)");
                    continue;
                }

                var classInfo = EntityClassManager.Instance.GetClassInfo(equipmentItem.ItemTemplate.Class);

                if (classInfo == null)
                {
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Equipment item has an unknown entity class");
                    continue;
                }

                if (classInfo.ArmorClassInfo == null)
                {
                    // how can the player equip non-armor?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Player try to equip non_armor item");
                    continue;
                }
                armorMax += equipmentItem.ItemTemplate.ArmorValue;      // ToDo
                armorRegenRate += classInfo.ArmorClassInfo.RegenRate;
                
                // what about damage absorbed? Was it used at all?
            }
            armorMax = armorMax * (1.0d + armorBonusPct);

            // The regen rate summed off the equipped armour goes on RefreshAmount. It used to be
            // assigned to Current, which the fullreset branch a few lines below overwrites
            // unconditionally - so it was computed, discarded, and armour never regenerated
            // either.
            attribute[Attributes.Armor].RefreshAmount = armorRegenRate;
            attribute[Attributes.Armor].NormalMax = (int)Math.Round(armorMax, 0);
            attribute[Attributes.Armor].CurrentMax = attribute[Attributes.Armor].NormalMax;
            if (fullreset)
                attribute[Attributes.Armor].Current = attribute[Attributes.Armor].CurrentMax;
            else
                attribute[Attributes.Armor].Current = Math.Min(attribute[Attributes.Armor].Current, attribute[Attributes.Armor].CurrentMax);
            // added by krssrb
            // power test
            attribute[Attributes.Power].NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentMind * 3;
            var powerBonus = 0;
            attribute[Attributes.Power].CurrentMax = attribute[Attributes.Power].NormalMax + powerBonus;
            if (fullreset)
                attribute[Attributes.Power].Current = attribute[Attributes.Power].CurrentMax;
            else
                attribute[Attributes.Power].Current = Math.Min(attribute[Attributes.Power].Current, attribute[Attributes.Power].CurrentMax);

            // The rates above are the out-of-combat ones. A player recomputing their stats while
            // in a fight - equipping something, levelling - keeps the penalty.
            ApplyRegenPeriod(player);
        }

        public void WeaponReady(Client client, bool isReady)
        {
            client.Player.WeaponReady = isReady;
            client.CallMethod(client.Player.EntityId, new WeaponReadyPacket(isReady));
        }

        public void WeaponReload(ActionData action)
        {
            if (action == null || action.Completed)
                return;

            // we reload weapon here
            var client = (action.Actor as Manifestation)?.MapChannel?.ClientList
                             .Find(candidate => ReferenceEquals(candidate.Player, action.Actor))
                         ?? Server.Clients.Find(candidate => ReferenceEquals(candidate.Player, action.Actor));

            // The reload was queued with a delay, and the player can be gone by the time it
            // fires - the connection dropped, the character logged out or was summoned away.
            // RemovePlayer now clears their queued actions, but this runs on the world loop,
            // where a null here used to end the process, so it is checked as well.
            if (client == null || client.State != ClientState.Ingame)
                return;

            action.Completed = true;

            // Interrupted before it finished. WeaponReload sets actionInterrupts, so the client
            // interrupts its own reload when the player performs another action - a melee or an
            // alternate attack; primary fire waits for the reload instead - cancelling it on its
            // side as it sends RequestActionInterrupt. ActorActionManager brings an
            // interrupted action forward to be seen to at once, and this took that for the reload
            // finishing: the clip was filled and the jam cleared on the next tick. Any melee or
            // alternate attack mid-reload was an instant reload, and a request followed at once by
            // an interrupt cleared a jam in one tick, so the jam never cost anything.
            //
            // It loads nothing, and the jam stays. Everyone in range was shown the windup and is
            // told it ended; the player's own client already cancelled it.
            if (action.IsInrerrupted)
            {
                client.CellIgnoreSelfCallMethod(client, new ActionInterruptPacket(client.Player.EntityId, ActionId.WeaponReload, action.ActionArgId));
                return;
            }

            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            if (weapon == null || action.SourceId != 0 && weapon.EntityId != action.SourceId)
                return;

            var weaponClassInfo = EntityClassManager.Instance.GetWeaponClassInfo(weapon);

            if (weaponClassInfo == null)
                return;

            var loaded = Math.Min(weapon.CurrentAmmo, weaponClassInfo.ClipSize);
            var consumed = new List<(Item Item, int Slot, uint Original, uint Remaining)>();

            for (var i = 0; i < 50 && loaded < weaponClassInfo.ClipSize; i++)
            {
                var entityId = client.Player.Inventory.PersonalInventory[(int)InventoryOffset.CategoryConsumable + i];

                if (entityId == 0)
                    continue;

                var weaponAmmo = EntityManager.Instance.GetItem(entityId);

                if (weaponAmmo == null || weaponAmmo.ItemTemplate.Class != weaponClassInfo.AmmoClassId || weaponAmmo.StackSize == 0 ||
                    MissionItemProtection.IsProtected(weaponAmmo, _gameUnitOfWorkFactory))
                    continue;

                var ammoToGrab = Math.Min(weaponClassInfo.ClipSize - loaded, weaponAmmo.StackSize);

                loaded += ammoToGrab;
                consumed.Add((weaponAmmo, (int)InventoryOffset.CategoryConsumable + i,
                    weaponAmmo.StackSize, weaponAmmo.StackSize - ammoToGrab));
            }

            if (consumed.Count == 0 && !weapon.IsJammed)
                return;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var savedWeapon = unitOfWork.Items.GetItem(weapon.Id);
                    if (savedWeapon == null || savedWeapon.AmmoCount != weapon.CurrentAmmo)
                        throw new GameplayRejectionException("Weapon clip changed during reload.");

                    foreach (var stack in consumed)
                    {
                        var saved = unitOfWork.Items.GetItem(stack.Item.Id);
                        if (saved == null || saved.StackSize != stack.Original || MissionItemProtection.IsProtected(stack.Item, unitOfWork))
                            throw new GameplayRejectionException("Reserve ammunition changed during reload.");

                        if (stack.Remaining == 0)
                        {
                            unitOfWork.CharacterInventories.DeleteInvItemByItemId(stack.Item.Id);
                            unitOfWork.Items.DeleteItem(stack.Item.Id);
                        }
                        else
                            unitOfWork.Items.UpdateItemStackSize(new Item(0, stack.Remaining, 0, 0)
                            {
                                Id = stack.Item.Id
                            });
                    }

                    unitOfWork.Items.UpdateAmmo(new Item
                    {
                        Id = weapon.Id,
                        CurrentAmmo = loaded
                    });
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Could not persist reload for item {weapon.Id}: {error.Message}");
                return;
            }

            foreach (var stack in consumed)
            {
                if (stack.Remaining == 0)
                {
                    EntityManager.Instance.DestroyPhysicalEntity(client, stack.Item.EntityId, EntityType.Item);
                    client.CallMethod(SysEntity.ClientInventoryManagerId,
                        new InventoryRemoveItemPacket(InventoryType.Personal, stack.Item.EntityId));
                    client.Player.Inventory.PersonalInventory[stack.Slot] = 0;
                }
                else
                {
                    stack.Item.StackSize = stack.Remaining;
                    client.CallMethod(stack.Item.EntityId, new SetStackCountPacket(stack.Remaining));
                }
            }

            ClearJam(client, weapon);
            weapon.CurrentAmmo = loaded;
            client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(loaded));
            client.Player.CurrentAction = 0;

            // send data to client
            client.CellCallMethod(client, client.Player.EntityId, new PerformRecoveryPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, loaded));
        }

        #endregion
    }
}

using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Structures;

    /// <summary>
    /// The respec tokens: Respec Skills (CONSUMABLE_RESET_SKILLS 501, abilities.resetskills, item
    /// 122035) and Respec Attributes (CONSUMABLE_RESET_ATTRIBS 502, abilities.resetattribs, item
    /// 122034). The client's ResetSkillsAbility and ResetAttribsAbility are plain self-targeted
    /// abilities with nothing of their own: what they do is the server's.
    ///
    ///  - Respec Skills: the skills go back to what a new character has of them - the Recruit
    ///    skills they hold at rank 1, nothing else (ManifestationManager.SkillsAfterRespec) - and
    ///    every other skill point is theirs to spend again;
    ///  - Respec Attributes: every point spent on Body, Mind and Spirit comes back.
    ///
    /// The change is written in the same transaction that uses the token up
    /// (ConsumeAbilityItems), so a token is never spent on a respec that was not saved, nor a
    /// respec saved for a token that was not spent. A token that would change nothing is refused
    /// and kept, at the request and again when it lands. Abilities left in the drawer from
    /// untrained skills stay there, refused when used, as they are after a level is lowered.
    /// </summary>
    public partial class AbilityManager
    {
        public const string ResetSkillsModule = "abilities.resetskills";
        public const string ResetAttributesModule = "abilities.resetattribs";

        private static bool IsRespec(ActionInfo action) =>
            action.Module == ResetSkillsModule || action.Module == ResetAttributesModule;

        /// <summary>Why a respec token cannot be used now, or null: there is nothing for it to give back.</summary>
        private static PlayerMessage? RespecRefusal(Manifestation player, ActionInfo action) => action.Module switch
        {
            ResetSkillsModule when !ManifestationManager.HasSkillsToRespec(player) => PlayerMessage.PmCannotPerformActionNow,
            ResetAttributesModule when !ManifestationManager.HasAttributesToRespec(player) => PlayerMessage.PmCannotPerformActionNow,
            _ => null
        };

        /// <summary>The respec's write, made with the token's use: the skills the respec leaves, or no attribute points spent.</summary>
        private static Action<ICharUnitOfWork> RespecWrite(ActionInfo action, Manifestation player)
        {
            if (action.Module == ResetSkillsModule)
            {
                var after = ManifestationManager.SkillsAfterRespec(player);
                var untrained = player.Skills.Keys.Where(skillId => !after.ContainsKey(skillId)).Select(skillId => (uint)skillId).ToList();

                return unit =>
                {
                    if (untrained.Count > 0)
                        unit.CharacterSkills.Delete(player.Id, untrained);

                    foreach (var skill in after.Values)
                        unit.CharacterSkills.AddOrUpdate(player.Id, (uint)skill.SkillId, skill.AbilityId, skill.SkillLevel);
                };
            }

            if (action.Module == ResetAttributesModule)
                return unit => unit.Characters.UpdateCharacterAttributes(player.Id, 0, 0, 0);

            return null;
        }

        /// <summary>The token is spent and the respec saved: the player as they now are, and the action finished.</summary>
        private void ResolveRespec(MapChannel mapChannel, Client client, Manifestation player, ActionInfo action, ActionData performed)
        {
            new ManifestationManager(_gameUnitOfWorkFactory).ApplyRespec(client,
                skills: action.Module == ResetSkillsModule, attributes: action.Module == ResetAttributesModule);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} used {action.Name}.");

            CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(performed.ActionId, performed.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
        }
    }
}

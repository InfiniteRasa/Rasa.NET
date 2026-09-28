using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Rasa.Missions.Definitions;
using Rasa.Structures.Char;

namespace Rasa.Context.Char
{
    internal static class MissionRuntimeModel
    {
        internal static void Configure(ModelBuilder model)
        {
            model.Entity<CharacterMissionEntry>().HasIndex(entry => entry.AssignmentId).IsUnique();
            model.Entity<CharacterMissionEntry>().Property(entry => entry.AssignmentId).HasDefaultValue("");
            model.Entity<CharacterMissionEntry>().Property(entry => entry.ContentRevision).HasDefaultValue("");
            model.Entity<CharacterMissionEntry>().Property(entry => entry.Generation).HasDefaultValue(0U);
            model.Entity<CharacterMissionEntry>().Property(entry => entry.Version).HasDefaultValue(0L);
            model.Entity<CharacterMissionOfferEntry>().HasKey(entry => new { entry.CharacterId, entry.MissionId });
            model.Entity<CharacterMissionOfferEntry>().HasIndex(entry => entry.OfferId).IsUnique();
            model.Entity<CharacterMissionOfferEntry>().Property(entry => entry.PartySource).HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions)null),
                value => JsonSerializer.Deserialize<MissionPartyOfferSource>(value, (JsonSerializerOptions)null));
            model.Entity<CharacterMissionOfferEntry>().HasOne<CharacterEntry>().WithMany()
                .HasForeignKey(entry => entry.CharacterId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<CharacterMissionHistoryEntry>().HasKey(entry => entry.AssignmentId);
            model.Entity<CharacterMissionHistoryEntry>().HasIndex(entry => new
                { entry.CharacterId, entry.MissionId, entry.AssignmentGeneration });
            model.Entity<CharacterMissionHistoryEntry>().HasIndex(entry => new
                { entry.CharacterId, entry.MissionId, entry.RewardWindowStartUtc }).IsUnique();
            model.Entity<CharacterMissionHistoryEntry>().HasOne<CharacterEntry>().WithMany()
                .HasForeignKey(entry => entry.CharacterId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<MissionSceneEntry>().HasIndex(entry => new { entry.MapKey, entry.Status });
            model.Entity<MissionSceneEntry>().HasIndex(entry => new { entry.OwnerCharacterId, entry.MissionId });
            model.Entity<MissionSceneEntry>().HasIndex(entry =>
                new { entry.OwnerCharacterId, entry.MissionId, entry.ScriptKey, entry.AssignmentId }).IsUnique();
            model.Entity<MissionSceneEntry>().Property(entry => entry.AssignmentId).HasDefaultValue("");
            model.Entity<MissionSceneParticipantEntry>().HasKey(entry => new { entry.RunId, entry.CharacterId });
            model.Entity<MissionSceneParticipantEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<MissionActorLeaseEntry>().HasKey(entry => new { entry.MapKey, entry.SpawnKey });
            model.Entity<MissionActorLeaseEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<MissionTimerEntry>().HasKey(entry => new { entry.RunId, entry.Name });
            model.Entity<MissionTimerEntry>().Property(entry => entry.SequenceId).HasDefaultValue(0U);
            model.Entity<MissionTimerEntry>().HasIndex(entry => new { entry.Disposition, entry.DueAtUtc });
            model.Entity<MissionTimerEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<MissionReceiptEntry>().HasKey(entry => new { entry.OwnerId, entry.Generation, entry.OperationKey });
            model.Entity<MissionWorldEffectEntry>().HasKey(entry => new { entry.RunId, entry.Generation, entry.OperationKey });
            model.Entity<MissionWorldEffectEntry>().HasIndex(entry => entry.SourceRunId);
            model.Entity<MissionWorldEffectEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<MissionCreditDeliveryEntry>().HasKey(entry => new { entry.EventId, entry.AssignmentId });
            model.Entity<MissionCreditDeliveryEntry>().HasIndex(entry => new { entry.CharacterId, entry.Status });
            model.Entity<MissionCreditDeliveryEntry>().HasOne<MissionOutcomeEntry>().WithMany()
                .HasForeignKey(entry => entry.EventId).OnDelete(DeleteBehavior.Restrict);
            model.Entity<MissionActorStateEntry>().HasKey(entry => new { entry.RunId, entry.ActorRole, entry.Generation });
            model.Entity<MissionActorStateEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Cascade);
            model.Entity<MissionSceneMessageEntry>()
                .HasIndex(entry => new { entry.RunId, entry.Generation, entry.OperationKey }).IsUnique();
            model.Entity<MissionSceneMessageEntry>().HasOne<MissionSceneEntry>().WithMany()
                .HasForeignKey(entry => entry.RunId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

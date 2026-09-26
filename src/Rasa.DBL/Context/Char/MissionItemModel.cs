using Microsoft.EntityFrameworkCore;
using Rasa.Structures.Char;

namespace Rasa.Context.Char
{
    internal static class MissionItemModel
    {
        internal static void Configure(ModelBuilder model)
        {
            var items = model.Entity<CharacterMissionItemEntry>();
            items.ToTable("character_mission_item", table =>
                table.HasCheckConstraint("CK_mission_item_quantity", "quantity > 0"));
            items.HasKey(entry => new { entry.CharacterId, entry.MissionId, entry.AssignmentId, entry.ItemKey, entry.ItemId });
            items.HasIndex(entry => entry.ItemId).IsUnique();
            items.Property(entry => entry.CharacterId).HasColumnName("character_id");
            items.Property(entry => entry.MissionId).HasColumnName("mission_id");
            items.Property(entry => entry.AssignmentId).HasColumnName("assignment_id").HasMaxLength(32);
            items.Property(entry => entry.Generation).HasColumnName("generation");
            items.Property(entry => entry.ItemKey).HasColumnName("item_key").HasMaxLength(64);
            items.Property(entry => entry.ItemId).HasColumnName("item_id");
            items.Property(entry => entry.Quantity).HasColumnName("quantity");
            items.HasOne<CharacterEntry>().WithMany().HasForeignKey(entry => entry.CharacterId).OnDelete(DeleteBehavior.Cascade);

            var receipts = model.Entity<CharacterMissionItemReceiptEntry>();
            receipts.ToTable("character_mission_item_receipt");
            receipts.HasKey(entry => new { entry.CharacterId, entry.AssignmentId, entry.OperationKey });
            receipts.Property(entry => entry.CharacterId).HasColumnName("character_id");
            receipts.Property(entry => entry.MissionId).HasColumnName("mission_id");
            receipts.Property(entry => entry.AssignmentId).HasColumnName("assignment_id").HasMaxLength(32);
            receipts.Property(entry => entry.Generation).HasColumnName("generation");
            receipts.Property(entry => entry.OperationKey).HasColumnName("operation_key").HasMaxLength(160);
            receipts.Property(entry => entry.Payload).HasColumnName("payload").IsRequired();
            receipts.HasOne<CharacterEntry>().WithMany().HasForeignKey(entry => entry.CharacterId).OnDelete(DeleteBehavior.Cascade);

            var quarantine = model.Entity<CharacterMissionItemQuarantineEntry>();
            quarantine.ToTable("character_mission_item_quarantine");
            quarantine.HasKey(entry => new { entry.CharacterId, entry.AssignmentId });
            quarantine.Property(entry => entry.CharacterId).HasColumnName("character_id");
            quarantine.Property(entry => entry.MissionId).HasColumnName("mission_id");
            quarantine.Property(entry => entry.AssignmentId).HasColumnName("assignment_id").HasMaxLength(32);
            quarantine.Property(entry => entry.Reason).HasColumnName("reason").HasMaxLength(255).IsRequired();
            quarantine.HasOne<CharacterEntry>().WithMany().HasForeignKey(entry => entry.CharacterId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

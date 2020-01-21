using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterInventoryConfiguration : IEntityTypeConfiguration<CharacterInventoryEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterInventoryEntry> builder)
        {
            builder.HasKey(e => new { e.AccountId, e.CharacterSlot, e.ItemId});

            builder.ToTable("character_inventory");

            builder.HasIndex(e => e.ItemId)
                .HasName("itemId")
                .IsUnique();

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.InventoryType)
                .HasColumnName("inventoryType")
                .HasColumnType("int(11)");

            builder.Property(e => e.ItemId)
                .HasColumnName("itemId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.SlotId)
                .HasColumnName("slotId")
                .HasColumnType("int(11)");
        }
    }
}
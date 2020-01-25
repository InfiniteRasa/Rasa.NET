using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemClassConfiguration : IEntityTypeConfiguration<ItemClassEntry>
    {
        public void Configure(EntityTypeBuilder<ItemClassEntry> builder)
        {
            builder.HasKey(e => e.ClassId)
                .HasName("PRIMARY");

            builder.ToTable("itemClass");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11)");

            builder.Property(e => e.DragAudioSetId)
                .HasColumnName("dragAudioSetId")
                .HasColumnType("int(11)");

            builder.Property(e => e.DropAudioSetId)
                .HasColumnName("dropAudioSetId")
                .HasColumnType("int(11)");

            builder.Property(e => e.HiddenInventoryFlag)
                .HasColumnName("hiddenInventoryFlag")
                .HasColumnType("bit(1)");

            builder.Property(e => e.InventoryIconStringId)
                .HasColumnName("inventoryIconStringId")
                .HasColumnType("mediumint(9)");

            builder.Property(e => e.IsConsumableFlag)
                .HasColumnName("isConsumableFlag")
                .HasColumnType("bit(1)");

            builder.Property(e => e.LootValue)
                .HasColumnName("lootValue")
                .HasColumnType("mediumint(9)");

            builder.Property(e => e.MaxHitPoints)
                .HasColumnName("maxHitPoints")
                .HasColumnType("mediumint(9)");

            builder.Property(e => e.StackSize)
                .HasColumnName("stackSize")
                .HasColumnType("int(11)");
        }
    }
}
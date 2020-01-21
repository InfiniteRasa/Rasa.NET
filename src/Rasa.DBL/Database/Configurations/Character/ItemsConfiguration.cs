using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class ItemsConfiguration : IEntityTypeConfiguration<ItemsEntry>
    {
        public void Configure(EntityTypeBuilder<ItemsEntry> builder)
        {
            builder.HasKey(e => e.ItemId)
                .HasName("PRIMARY");

            builder.ToTable("items");

            builder.HasIndex(e => e.ItemId)
                .HasName("id")
                .IsUnique();

            builder.Property(e => e.ItemId)
                .HasColumnName("itemId")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.AmmoCount)
                .HasColumnName("ammoCount")
                .HasColumnType("int(11)");

            builder.Property(e => e.Color)
                .HasColumnName("color")
                .HasColumnType("int(11)");

            builder.Property(e => e.CrafterName)
                .IsRequired()
                .HasColumnName("crafterName")
                .HasColumnType("varchar(64)")
                .HasDefaultValueSql("''''''")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("createdAt")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("'current_timestamp()'");

            builder.Property(e => e.CurrentHitPoints)
                .HasColumnName("currentHitPoints")
                .HasColumnType("int(11)");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.StackSize)
                .HasColumnName("stackSize")
                .HasColumnType("int(11)");
        }
    }
}
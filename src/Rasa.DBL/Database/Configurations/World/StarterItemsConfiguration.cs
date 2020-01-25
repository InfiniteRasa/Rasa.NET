using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class StarterItemsConfiguration : IEntityTypeConfiguration<StarterItemsEntry>
    {
        public void Configure(EntityTypeBuilder<StarterItemsEntry> builder)
        {
            builder.HasKey(e => e.ClassId)
                .HasName("PRIMARY");

            builder.ToTable("starter_items");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11)");

            builder.Property(e => e.Comment)
                .HasColumnName("comment")
                .HasColumnType("char(50)")
                .HasDefaultValueSql("NULL");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.SlotId)
                .HasColumnName("slotId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("'20'");
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateResistanceConfiguration : IEntityTypeConfiguration<ItemTemplateResistanceEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateResistanceEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate_resistance");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemtemplate_id")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ResistanceType)
                .HasColumnName("resistance_type")
                .HasColumnType("tinyint(4)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ResistanceValue)
                .HasColumnName("resistance_value")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ArmorTemplateConfiguration : IEntityTypeConfiguration<ArmorTemplateEntry>
    {
        public void Configure(EntityTypeBuilder<ArmorTemplateEntry> builder)
        {
            builder.HasKey(e => e.ItemTemplateId)
                .HasName("PRIMARY");

            builder.ToTable("itemtemplate_armor");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.ArmorValue)
                .HasColumnName("armorValue")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");
        }
    }
}
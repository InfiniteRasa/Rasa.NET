using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateItemClassConfiguration : IEntityTypeConfiguration<ItemTemplateItemClassEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateItemClassEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate_itemclass");

            builder.Property(e => e.ItemClass)
                .HasColumnName("itemClassId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");
        }
    }
}
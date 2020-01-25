using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateRequirementsConfiguration : IEntityTypeConfiguration<ItemTemplateRequirementsEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateRequirementsEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate_requirements");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.ReqType)
                .HasColumnName("reqType")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.ReqValue)
                .HasColumnName("reqValue")
                .HasColumnType("tinyint(4)");
        }
    }
}
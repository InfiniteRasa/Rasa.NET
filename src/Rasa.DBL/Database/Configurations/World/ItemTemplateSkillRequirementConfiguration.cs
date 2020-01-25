using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class ItemTemplateSkillRequirementConfiguration : IEntityTypeConfiguration<ItemTemplateSkillRequirementEntry>
    {
        public void Configure(EntityTypeBuilder<ItemTemplateSkillRequirementEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("itemtemplate_skillrequirement");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.SkillId)
                .HasColumnName("skillId")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.SkillLevel)
                .HasColumnName("skillLevel")
                .HasColumnType("tinyint(4)");
        }
    }
}
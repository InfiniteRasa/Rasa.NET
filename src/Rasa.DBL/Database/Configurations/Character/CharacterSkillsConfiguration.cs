using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterSkillsConfiguration : IEntityTypeConfiguration<CharacterSkillsEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterSkillsEntry> builder)
        {
            builder.HasKey(e => new {e.AccountId, e.CharacterSlot, e.SkillId, e.AbilityId});
            
            builder.ToTable("character_skills");

            builder.Property(e => e.AbilityId)
                .HasColumnName("abilityId")
                .HasColumnType("int(11)");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.SkillId)
                .HasColumnName("skillId")
                .HasColumnType("int(11)");

            builder.Property(e => e.SkillLevel)
                .HasColumnName("skillLevel")
                .HasColumnType("int(11)");
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterMissionsConfiguration : IEntityTypeConfiguration<CharacterMissionsEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterMissionsEntry> builder)
        {
            builder.HasKey(e => new {e.AccountId, e.CharacterSlot, e.MissionId});

            builder.ToTable("character_missions");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.MissionId)
                .HasColumnName("missionId")
                .HasColumnType("int(11)");

            builder.Property(e => e.MissionState)
                .HasColumnName("missionState")
                .HasColumnType("tinyint(4)");
        }
    }
}
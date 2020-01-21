using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class ClanMemberEntryConfiguration : IEntityTypeConfiguration<ClanMemberEntry>
    {
        public void Configure(EntityTypeBuilder<ClanMemberEntry> builder)
        {
            builder.HasKey(e => new { e.ClanId, e.CharacterId })
                .HasName("PRIMARY");

            builder.ToTable("clan_member");

            builder.HasIndex(e => e.CharacterId)
                .HasName("clan_member_index_character")
                .IsUnique();

            builder.Property(e => e.ClanId)
                .HasColumnName("clan_id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CharacterId)
                .HasColumnName("character_id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Rank)
                .HasColumnName("rank")
                .HasColumnType("tinyint(3) unsigned");

            builder.HasOne(d => d.Character)
                .WithOne(p => p.ClanMember)
                .HasForeignKey<ClanMemberEntry>(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clan_member_FK_character");

            builder.HasOne(d => d.Clan)
                .WithMany(p => p.ClanMember)
                .HasForeignKey(d => d.ClanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clan_member_FK_clan");
        }
    }
}
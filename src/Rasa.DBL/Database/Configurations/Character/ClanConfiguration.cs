using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class ClanConfiguration : IEntityTypeConfiguration<ClanEntry>
    {
        public void Configure(EntityTypeBuilder<ClanEntry> builder)
        {
            builder.ToTable("clan");

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("'current_timestamp()'");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");
        }
    }
}
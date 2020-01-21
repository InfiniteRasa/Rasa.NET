using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class AccountConfiguration : IEntityTypeConfiguration<GameAccountEntry>
    {
        public void Configure(EntityTypeBuilder<GameAccountEntry> builder)
        {
            builder.ToTable("account");

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CanSkipBootcamp)
                .HasColumnName("can_skip_bootcamp")
                .HasColumnType("bit(1)")
                .HasDefaultValueSql("b'0'");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("current_timestamp()");

            builder.Property(e => e.Email)
                .IsRequired()
                .HasColumnName("email")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");

            builder.Property(e => e.FamilyName)
                .IsRequired()
                .HasColumnName("family_name")
                .HasColumnType("varchar(64)")
                .HasDefaultValueSql("''")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");

            builder.Property(e => e.LastIP)
                .IsRequired()
                .HasColumnName("last_ip")
                .HasColumnType("varchar(15)")
                .HasDefaultValueSql("'0.0.0.0'")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");

            builder.Property(e => e.LastLogin)
                .HasColumnName("last_login")
                .HasColumnType("datetime")
                .HasDefaultValueSql("current_timestamp()");

            builder.Property(e => e.Level)
                .HasColumnName("level")
                .HasColumnType("tinyint(3) unsigned");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasColumnName("name")
                .HasColumnType("varchar(64)")
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");

            builder.Property(e => e.SelectedSlot)
                .HasColumnName("selected_slot")
                .HasColumnType("tinyint(3) unsigned");
        }
    }
}
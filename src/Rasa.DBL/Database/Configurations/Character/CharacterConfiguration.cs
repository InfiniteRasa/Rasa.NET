using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterConfiguration : IEntityTypeConfiguration<CharacterEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterEntry> builder)
        {
                builder.ToTable("character");

                builder.HasIndex(e => e.AccountId)
                    .HasName("character_index_account");

                builder.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.AccountId)
                    .HasColumnName("account_id")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.ActiveWeapon)
                    .HasColumnName("active_weapon")
                    .HasColumnType("tinyint(3)");

                builder.Property(e => e.Body)
                    .HasColumnName("body")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.Class)
                    .HasColumnName("class")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.CloneCredits)
                    .HasColumnName("clone_credits")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.CoordX).HasColumnName("coord_x");

                builder.Property(e => e.CoordY).HasColumnName("coord_y");

                builder.Property(e => e.CoordZ).HasColumnName("coord_z");

                builder.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("'current_timestamp()'");

                builder.Property(e => e.Credits)
                    .HasColumnName("credits")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                builder.Property(e => e.Experience)
                    .HasColumnName("experience")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.Gender)
                    .HasColumnName("gender")
                    .HasColumnType("bit(1)");

                builder.Property(e => e.LastLogin)
                    .HasColumnName("last_login")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("'NULL'");

                builder.Property(e => e.Level)
                    .HasColumnName("level")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValueSql("'1'");

                builder.Property(e => e.MapContextId)
                    .HasColumnName("map_context_id")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.Mind)
                    .HasColumnName("mind")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                builder.Property(e => e.NumLogins)
                    .HasColumnName("num_logins")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.Orientation).HasColumnName("orientation");

                builder.Property(e => e.Prestige)
                    .HasColumnName("prestige")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                builder.Property(e => e.Race)
                    .HasColumnName("race")
                    .HasColumnType("tinyint(3) unsigned");

                builder.Property(e => e.Scale)
                    .HasColumnName("scale")
                    .HasColumnType("double unsigned");

                builder.Property(e => e.Slot)
                    .HasColumnName("slot")
                    .HasColumnType("tinyint(3) unsigned");

                builder.Property(e => e.Spirit)
                    .HasColumnName("spirit")
                    .HasColumnType("int(11) unsigned");

                builder.Property(e => e.TotalTimePlayed)
                    .HasColumnName("total_time_played")
                    .HasColumnType("int(11) unsigned");

                builder.HasOne(d => d.Account)
                    .WithMany(p => p.Character)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("character_FK_account");
        }
    }
}